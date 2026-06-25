using Argentum.Client.Network;
using Argentum.Client.World;
using Xunit;

namespace Argentum.Client.Tests;

public class WorldEntryReaderTests
{
    [Fact]
    public async Task Parses_captured_server_world_entry_after_create_character()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "world_entry_create.bin");
        if (!File.Exists(path))
        {
            path = Path.Combine(FindRepoRoot(), "ao-client-godot", "Tests", "data", "world_entry_create.bin");
        }
        Assert.True(File.Exists(path), $"Falta captura de frames: {path}");

        var frames = LoadFrames(path);
        Assert.NotEmpty(frames);

        var replay = new FrameReplay(frames);
        var world = await new WorldEntryReader().ReadAsync(replay, frames[0]);
        Assert.True(world.LoggedIn);
        Assert.Equal("FrameHero", world.CharacterName);
        Assert.True(world.Body > 0);
        Assert.True(world.Head > 0);
        Assert.True(world.MapId > 0);
    }

    private static List<byte[]> LoadFrames(string path)
    {
        var data = File.ReadAllBytes(path);
        var frames = new List<byte[]>();
        var offset = 0;
        while (offset + 2 <= data.Length)
        {
            var size = data[offset] | (data[offset + 1] << 8);
            offset += 2;
            if (size <= 0 || offset + size > data.Length)
            {
                break;
            }
            var frame = new byte[size];
            Buffer.BlockCopy(data, offset, frame, 0, size);
            frames.Add(frame);
            offset += size;
        }
        return frames;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.Exists(Path.Combine(dir, "ao-client-godot")) &&
                Directory.Exists(Path.Combine(dir, "ao-server-go")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }
        throw new InvalidOperationException("No se encontró la raíz del repo AO1.1");
    }

    private sealed class FrameReplay : IAsyncFrameReader
    {
        private readonly Queue<byte[]> _frames;

        public FrameReplay(IReadOnlyList<byte[]> frames) =>
            _frames = new Queue<byte[]>(frames.Skip(1));

        public Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken = default)
        {
            if (_frames.Count == 0)
            {
                throw new InvalidOperationException("No hay más frames en la captura.");
            }
            return Task.FromResult(_frames.Dequeue());
        }
    }
}
