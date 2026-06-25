using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Argentum.Client.Audio;
using Argentum.Client.Network;
using Argentum.Client.Protocol;
using Argentum.Client.Resources;
using Argentum.Client.World;
using Godot;

namespace Argentum.Client.Gameplay;

public sealed class GameplaySession : IDisposable
{
    private readonly FrameConnection _connection;
    private readonly AoAudio? _audio;
    private readonly GrhCatalog? _grhs;
    private readonly CancellationTokenSource _cts = new();
    private int _walkCounter;
    private int _attackCounter;
    private int _workCounter;
    private int _spellCounter;
    private int _leftClickCounter;
    private int _useItemCounter;
    private int _talkCounter;
    private bool _awaitingWarpPosition;
    private int _pendingMapId;
    private int _pendingMapResource;
    private readonly ConcurrentQueue<Action> _uiQueue = new();

    public WorldSession World { get; }
    public bool IsWarping => _awaitingWarpPosition;
    public Dictionary<(int X, int Y), byte> Blocks { get; } = new();
    public event Action? Changed;
    public event Action<string>? CommerceStarted;
    public event Action? CommerceEnded;

    public GameplaySession(FrameConnection connection, WorldSession world, AoAudio? audio = null, GrhCatalog? grhs = null)
    {
        _connection = connection;
        World = world;
        _audio = audio;
        _grhs = grhs;
        _audio?.BindListener(world);
        world.RequestAdjacentPeekAsync = SendAdjacentPeekRequestAsync;
    }

    public void Start()
    {
        _ = Task.Run(ReadLoopAsync);
    }

    /// <summary>Aplica trabajo de red en el hilo principal (UI / consola / diálogos).</summary>
    public void PumpUiQueue()
    {
        while (_uiQueue.TryDequeue(out var work))
        {
            try
            {
                work();
            }
            catch (Exception ex)
            {
                GD.PushError(ex.ToString());
            }
        }
    }

    public async Task SendResucitateAsync()
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.Resucitate);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendAttackAsync()
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _attackCounter);
        writer.WriteInt16((short)ClientPacketId.Attack);
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendMeditateAsync()
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.Meditate);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendHideAsync()
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _workCounter);
        writer.WriteInt16((short)ClientPacketId.Work);
        writer.WriteInt8(7); // Ocultarse
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendCommerceBuyAsync(int slot, int amount)
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.CommerceBuy);
        writer.WriteInt8((sbyte)slot);
        writer.WriteInt16((short)amount);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendCommerceSellAsync(int slot, int amount)
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.CommerceSell);
        writer.WriteInt8((sbyte)slot);
        writer.WriteInt16((short)amount);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendCommerceEndAsync()
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.CommerceEnd);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendCommerceStartAsync()
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.CommerceStart);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendCastSpellAsync(int slot)
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _spellCounter);
        writer.WriteInt16((short)ClientPacketId.CastSpell);
        writer.WriteInt8((sbyte)slot);
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendSpellInfoAsync(int slot)
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16((short)ClientPacketId.SpellInfo);
        writer.WriteInt8((sbyte)slot);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendWorkLeftClickAsync(int tileX, int tileY, int skill)
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _leftClickCounter);
        writer.WriteInt16((short)ClientPacketId.WorkLeftClick);
        writer.WriteInt8((sbyte)tileX);
        writer.WriteInt8((sbyte)tileY);
        writer.WriteInt8((sbyte)skill);
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendPickUpAsync(int tileX, int tileY)
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _leftClickCounter);
        writer.WriteInt16((short)ClientPacketId.PickUp);
        writer.WriteInt8((sbyte)tileX);
        writer.WriteInt8((sbyte)tileY);
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendUseItemAsync(int slot)
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _useItemCounter);
        writer.WriteInt16((short)ClientPacketId.UseItem);
        writer.WriteInt8((sbyte)slot);
        writer.WriteInt8(1); // VB6: ActiveInventoryTab = eInventory
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendEquipAsync(int slot)
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _useItemCounter);
        writer.WriteInt16((short)ClientPacketId.EquipItem);
        writer.WriteInt8((sbyte)slot);
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendLeftClickAsync(int tileX, int tileY)
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _leftClickCounter);
        writer.WriteInt16((short)ClientPacketId.LeftClick);
        writer.WriteInt8((sbyte)tileX);
        writer.WriteInt8((sbyte)tileY);
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendTalkAsync(string message)
    {
        var writer = new LegacyPacketWriter();
        var counter = Interlocked.Increment(ref _talkCounter);
        writer.WriteInt16((short)ClientPacketId.Talk);
        writer.WriteString8(message);
        writer.WriteInt32(counter);
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    public async Task SendWalkAsync(int heading, int clientSeq = 0)
    {
        var writer = new LegacyPacketWriter();
        var serverSeq = Interlocked.Increment(ref _walkCounter);
        writer.WriteInt16((short)ClientPacketId.Walk);
        writer.WriteInt8((sbyte)heading);
        writer.WriteInt32(serverSeq);
        MovementDiagnostics.Log("NET_SEND", $"Walk heading={heading} clientSeq={clientSeq} serverSeq={serverSeq} dead={World.IsDead}");
        await _connection.WriteFrameAsync(writer.ToArray());
    }

    private Task SendAdjacentPeekRequestAsync(int mapId, int srcMinX, int srcMinY, int srcMaxX, int srcMaxY)
    {
        var writer = new LegacyPacketWriter();
        writer.WriteInt16(AoExtensionPackets.RequestAdjacentPeek);
        writer.WriteInt16((short)mapId);
        writer.WriteInt8((sbyte)srcMinX);
        writer.WriteInt8((sbyte)srcMinY);
        writer.WriteInt8((sbyte)srcMaxX);
        writer.WriteInt8((sbyte)srcMaxY);
        return _connection.WriteFrameAsync(writer.ToArray());
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var payload = await _connection.ReadFrameAsync(_cts.Token);
                ApplyPacket(payload);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            NetDiagnostics.Log("NET_ERR", exception.ToString());
            GD.PushError(exception.ToString());
        }
    }

    private void ApplyPacket(byte[] payload)
    {
        var reader = new LegacyPacketReader(payload);
        var packet = (ServerPacketId)reader.ReadInt16();
        NetDiagnostics.Log("RX", $"{packet}");
        if ((short)packet == AoExtensionPackets.AdjacentPeekSync)
        {
            ApplyAdjacentPeekSync(ref reader);
            Changed?.Invoke();
            return;
        }
        switch (packet)
        {
            case ServerPacketId.CharacterMove:
                var charIndex = reader.ReadInt16();
                var x = reader.ReadUInt8();
                var y = reader.ReadUInt8();
                if (charIndex == World.CharIndex)
                {
                    if (_awaitingWarpPosition)
                    {
                        MovementDiagnostics.LogSession(World, "WARP_SPAWN",
                            $"CharacterMove ({x},{y}) pendingMap={_pendingMapId}");
                        if (_pendingMapId != 0)
                        {
                            World.LoadMap(_pendingMapId, _pendingMapResource);
                            _pendingMapId = 0;
                            _pendingMapResource = 0;
                        }
                        World.ApplyWarpArrival(x, y);
                        _awaitingWarpPosition = false;
                        if (_audio is not null)
                        {
                            MapAudioService.OnMapLoadedIfChanged(World.Map, _audio);
                        }
                    }
                    else if (World.SeamlessWarpActive)
                    {
                        if (x != World.TileX || y != World.TileY)
                        {
                            World.TileX = x;
                            World.TileY = y;
                        }
                        if (_audio is not null)
                        {
                            MapAudioService.OnMapLoadedIfChanged(World.Map, _audio);
                        }
                    }
                    else
                    {
                        MovementDiagnostics.LogSession(World, "NET_CharacterMove",
                            $"servidor ({x},{y})");
                        World.ConfirmMove(x, y);
                        if (_audio is not null)
                        {
                            var self = CreateSelfCharacter();
                            FootstepAudio.PlayForCharacter(_audio, World, _grhs, self);
                            World.StepPhase = self.StepPhase;
                        }
                    }
                }
                else if (World.Characters.TryGet(charIndex, out var moving))
                {
                    moving!.ConfirmMove(x, y);
                    if (_audio is not null)
                    {
                        FootstepAudio.PlayForCharacter(_audio, World, _grhs, moving);
                    }
                }
                break;
            case ServerPacketId.CharacterCreate:
                var created = new WorldCharacter();
                CharacterCreateReader.ReadInto(ref reader, created);
                if (created.CharIndex != World.CharIndex)
                {
                    World.Characters.Upsert(created);
                }
                break;
            case ServerPacketId.CharacterRemove:
                var removedIndex = reader.ReadInt16();
                _ = reader.ReadBoolean();
                _ = reader.ReadBoolean();
                if (removedIndex != World.CharIndex)
                {
                    World.Characters.Remove(removedIndex);
                }
                break;
            case ServerPacketId.AreaChanged:
                var areaX = reader.ReadUInt8();
                var areaY = reader.ReadUInt8();
                MovementDiagnostics.LogSession(World, "NET_AreaChanged", $"centro=({areaX},{areaY})");
                ApplyAreaChanged(areaX, areaY);
                break;
            case ServerPacketId.ChangeMap:
                _pendingMapId = reader.ReadInt16();
                _pendingMapResource = reader.ReadInt16();
                World.MapId = _pendingMapId;
                World.MapResource = _pendingMapResource;
                MovementDiagnostics.LogSession(World, "NET_ChangeMap",
                    $"pending id={_pendingMapId} resource={_pendingMapResource}");
                Blocks.Clear();
                if (World.TryBeginSeamlessWarp(_pendingMapId, _pendingMapResource))
                {
                    _pendingMapId = 0;
                    _pendingMapResource = 0;
                    _awaitingWarpPosition = false;
                }
                else
                {
                    World.WarpPreview.EnsureLoaded(_pendingMapId, _pendingMapResource);
                    World.WarpAwaitingPosition = true;
                    _awaitingWarpPosition = true;
                }
                break;
            case ServerPacketId.BlockPosition:
            {
                var bx = reader.ReadUInt8();
                var by = reader.ReadUInt8();
                var blocked = reader.ReadUInt8();
                _uiQueue.Enqueue(() =>
                {
                    BlockPositionReader.Apply(bx, by, blocked, World);
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.PosUpdate:
                if (_awaitingWarpPosition)
                {
                    MovementDiagnostics.LogSession(World, "NET_PosUpdate", "ignorado durante warp");
                    break;
                }
                var px = reader.ReadUInt8();
                var py = reader.ReadUInt8();
                MovementDiagnostics.LogSession(World, "NET_PosUpdate", $"servidor ({px},{py})");
                World.ApplyPosUpdate(px, py);
                break;
            case ServerPacketId.CharUpdateHP:
                CharHpUpdateReader.Apply(ref reader, World.Characters);
                break;
            case ServerPacketId.TextOverChar:
                TextOverCharReader.Apply(ref reader, World);
                break;
            case ServerPacketId.UpdateHP:
                UpdateHpReader.Apply(ref reader, World);
                break;
            case ServerPacketId.ArmaMov:
                ArmaMovReader.Apply(ref reader, World, World.Characters);
                break;
            case ServerPacketId.CharSwing:
                CharSwingReader.Apply(ref reader, World, World.Characters, _audio);
                break;
            case ServerPacketId.NPCHitUser:
                NpcHitUserReader.Apply(ref reader, World);
                break;
            case ServerPacketId.PlayWave:
                PlayWaveReader.Apply(ref reader, _audio, World);
                break;
            case ServerPacketId.PlayMIDI:
                PlayMidiReader.Apply(ref reader, _audio);
                break;
            case ServerPacketId.PlayWaveStep:
                PlayWaveStepReader.Apply(ref reader, _audio, World, _grhs);
                break;
            case ServerPacketId.NPCKillUser:
                NpcKillUserReader.Apply(World);
                break;
            case ServerPacketId.UpdateSta:
                UpdateStaReader.Apply(ref reader, World);
                break;
            case ServerPacketId.CharacterChange:
                CharacterChangeReader.Apply(ref reader, World, World.Characters);
                break;
            case ServerPacketId.ErrorMsg:
                var msg = ErrorMsgReader.Read(ref reader);
                var alertColor = new Godot.Color("f0d878");
                if (World.IsDead)
                {
                    World.DeathMessage = msg;
                    alertColor = new Godot.Color("e04a4a");
                }
                else
                {
                    World.GameMessage = msg;
                }
                World.Console.Add(msg, alertColor);
                _audio?.PlayUi(AoSoundIndex.Exclamacion);
                break;
            case ServerPacketId.ConsoleMsg:
                ConsoleMsgReader.Apply(ref reader, World);
                break;
            case ServerPacketId.ChatOverHead:
            {
                var chat = ChatOverHeadReader.ParseFull(ref reader);
                _uiQueue.Enqueue(() =>
                {
                    ChatOverHeadReader.Apply(chat, World, World.Characters);
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.UpdateUserStats:
                UserStatsReader.ReadInto(ref reader, World);
                break;
            case ServerPacketId.UpdateExp:
                World.Exp = reader.ReadInt32();
                break;
            case ServerPacketId.UpdateMana:
                World.MinMana = reader.ReadInt16();
                break;
            case ServerPacketId.MeditateToggle:
                ApplyMeditateToggle(ref reader);
                break;
            case ServerPacketId.SetInvisible:
                ApplySetInvisible(ref reader);
                break;
            case ServerPacketId.CommerceInit:
            {
                var vendor = reader.ReadString8();
                _uiQueue.Enqueue(() =>
                {
                    World.CommerceVendor = vendor;
                    CommerceStarted?.Invoke(vendor);
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.CommerceEnd:
            {
                _uiQueue.Enqueue(() =>
                {
                    World.CommerceOpen = false;
                    World.NpcCommerce.Clear();
                    CommerceEnded?.Invoke();
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.ChangeNPCInventorySlot:
            {
                var slot = NpcInventorySlotReader.Parse(ref reader);
                _uiQueue.Enqueue(() =>
                {
                    NpcInventorySlotReader.Apply(slot, World.NpcCommerce);
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.ObjectCreate:
            {
                var ox = reader.ReadUInt8();
                var oy = reader.ReadUInt8();
                var objIndex = reader.ReadInt16();
                var amount = reader.ReadInt16();
                var elementalTags = reader.ReadInt32();
                _uiQueue.Enqueue(() =>
                {
                    ObjectCreateReader.Apply(ox, oy, objIndex, amount, elementalTags, World);
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.UpdateGold:
                World.Gold = reader.ReadInt32();
                World.GoldPerLevel = reader.ReadInt32();
                break;
            case ServerPacketId.ChangeInventorySlot:
            {
                var slot = reader.ReadUInt8();
                var objIndex = reader.ReadInt16();
                var amount = reader.ReadInt16();
                var equipped = reader.ReadBoolean();
                _ = reader.ReadReal32();
                _ = reader.ReadUInt8();
                var elementalTags = reader.ReadInt32();
                _ = reader.ReadBoolean();
                _uiQueue.Enqueue(() =>
                {
                    World.Inventory.SetSlot(slot, objIndex, amount, equipped, elementalTags);
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.ChangeSpellSlot:
            {
                var slot = reader.ReadUInt8();
                var spellId = reader.ReadInt16();
                var index = reader.ReadInt16();
                var bindable = reader.ReadBoolean();
                _uiQueue.Enqueue(() =>
                {
                    if (slot is >= 1 and <= SpellBook.MaxSlots)
                    {
                        if (index < 0 || spellId <= 0)
                        {
                            World.Spells.SetSlot(slot, 0);
                        }
                        else
                        {
                            World.Spells.SetSlot(slot, spellId);
                        }
                    }
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.CreateFX:
                CreateFXReader.Apply(ref reader, World, World.Characters);
                break;
            case ServerPacketId.ParticleFXWithDestino:
                ParticleFXReader.ApplyWithDestino(ref reader, World, World.Characters);
                break;
            case ServerPacketId.ParticleFXWithDestinoXY:
                ParticleFXReader.ApplyWithDestinoXY(ref reader, World, World.Characters);
                break;
            case ServerPacketId.WorkRequestTarget:
            {
                var req = WorkRequestTargetReader.Parse(ref reader);
                _uiQueue.Enqueue(() =>
                {
                    if (req.Skill == 0)
                    {
                        World.UsingSkill = 0;
                        World.AreaSpellCast = false;
                    }
                    else
                    {
                        World.UsingSkill = req.Skill;
                        World.AreaSpellCast = req.Area;
                        World.AreaSpellRadio = req.Radio;
                        World.Console.Add("Seleccioná el objetivo del hechizo.", new Godot.Color("a8c8ff"));
                    }
                    Changed?.Invoke();
                });
                return;
            }
            case ServerPacketId.InventoryUnlockSlots:
                var tier = reader.ReadUInt8();
                World.Inventory.UnlockedSlots = PlayerInventory.DefaultUnlocked + tier * 6;
                if (World.Inventory.UnlockedSlots > PlayerInventory.MaxSlots)
                {
                    World.Inventory.UnlockedSlots = PlayerInventory.MaxSlots;
                }
                break;
            default:
                NetDiagnostics.Log("UNK_PKT", $"{packet}");
                return;
        }
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    public async ValueTask CloseAsync()
    {
        Dispose();
        await _connection.DisposeAsync();
    }

    private void ApplyAdjacentPeekSync(ref LegacyPacketReader reader)
    {
        var mapId = reader.ReadInt16();
        var count = reader.ReadUInt8();
        var npcs = new List<AdjacentPeekLiveNpc>(count);
        for (var i = 0; i < count; i++)
        {
            var x = reader.ReadUInt8();
            var y = reader.ReadUInt8();
            var body = reader.ReadInt16();
            var head = reader.ReadInt16();
            var heading = reader.ReadUInt8();
            var weapon = reader.ReadInt16();
            var shield = reader.ReadInt16();
            var helmet = reader.ReadInt16();
            var minHp = reader.ReadInt32();
            var maxHp = reader.ReadInt32();
            var name = reader.ReadString8();
            npcs.Add(new AdjacentPeekLiveNpc
            {
                MapId = mapId,
                SrcX = x,
                SrcY = y,
                Body = body,
                Head = head,
                Heading = heading,
                Weapon = weapon,
                Shield = shield,
                Helmet = helmet,
                MinHp = minHp,
                MaxHp = maxHp,
                Name = name,
            });
        }
        World.AdjacentPeekLive.SetMapNpcs(mapId, npcs);
    }

    private void ApplyMeditateToggle(ref LegacyPacketReader reader)
    {
        var charIndex = reader.ReadInt16();
        var fx = reader.ReadInt16();
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        if (charIndex == World.CharIndex)
        {
            World.Meditating = fx > 0;
            if (World.Meditating)
            {
                World.GameMessage = "Meditando…";
                World.Console.Add("Meditando…", new Godot.Color("a8c8ff"));
                _audio?.PlayUi(AoSoundIndex.Meditate);
            }
            else
            {
                World.GameMessage = null;
            }
            return;
        }
        if (World.Characters.TryGet(charIndex, out var ch))
        {
            ch!.Invisible = false;
        }
        _ = fx;
    }

    private void ApplySetInvisible(ref LegacyPacketReader reader)
    {
        var charIndex = reader.ReadInt16();
        var invisible = reader.ReadUInt8() != 0;
        _ = reader.ReadUInt8();
        _ = reader.ReadUInt8();
        if (charIndex == World.CharIndex)
        {
            World.Hidden = invisible;
            World.GameMessage = invisible ? "Estás oculto." : null;
            if (invisible)
            {
                World.Console.Add("Estás oculto.", new Godot.Color("c8b8ff"));
            }
            return;
        }
        if (World.Characters.TryGet(charIndex, out var ch))
        {
            ch!.Invisible = invisible;
        }
    }

    private void ApplyAreaChanged(int x, int y)
    {
        World.Characters.PruneOutsideArea(x, y);
    }

    private WorldCharacter CreateSelfCharacter() => new()
    {
        CharIndex = (short)World.CharIndex,
        TileX = World.TileX,
        TileY = World.TileY,
        StepPhase = World.StepPhase,
    };
}
