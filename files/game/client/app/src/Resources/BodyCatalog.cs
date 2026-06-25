using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Argentum.Client.Resources;

public readonly struct SpriteSlice
{
    public readonly int GrhIndex;
    public readonly int FileNum;
    public readonly short SX;
    public readonly short SY;
    public readonly short Width;
    public readonly short Height;

    public SpriteSlice(int grhIndex)
    {
        GrhIndex = grhIndex;
    }

    public SpriteSlice(int fileNum, short sx, short sy, short width, short height)
    {
        FileNum = fileNum;
        SX = sx;
        SY = sy;
        Width = width;
        Height = height;
    }

    public bool UsesGrh => GrhIndex > 0;
}

public sealed class BodyWalkAnim
{
    public SpriteSlice[] Frames = [];
    public float MsPerFrame = 55.556f;
    public float Speed = 1f;
}

public sealed class BodyDef
{
    public int BodyOffsetX;
    public int BodyOffsetY;
    public int HeadOffsetX;
    public int HeadOffsetY;
    public BodyWalkAnim?[] Walk = new BodyWalkAnim?[5];
}

public sealed class BodyCatalog
{
    // VB6 CargarMoldes: row J maps to this heading.
    private static readonly int[] MoldRowToHeading = [0, 3, 1, 4, 2];

    private readonly BodyDef?[] _bodies;

    public BodyCatalog(BodyDef?[] bodies) => _bodies = bodies;

    public BodyDef? Get(int bodyIndex)
    {
        if (bodyIndex <= 0 || bodyIndex >= _bodies.Length)
        {
            return null;
        }
        return _bodies[bodyIndex];
    }

    public SpriteSlice? GetWalkFrame(int bodyIndex, int heading, bool moving, double walkAnimTime)
    {
        var body = Get(bodyIndex);
        if (body is null || heading < 1 || heading > 4)
        {
            return null;
        }
        var anim = body.Walk[heading];
        if (anim is null || anim.Frames.Length == 0)
        {
            return null;
        }
        if (!moving)
        {
            return anim.Frames[0];
        }
        var frame = (int)(walkAnimTime * 1000.0 / anim.MsPerFrame) % anim.Frames.Length;
        return anim.Frames[frame];
    }

    public static BodyCatalog Load(string root, MoldCatalog molds)
    {
        var path = Path.Combine(root, "init", "cuerpos.dat");
        var sections = IniSections.Load(path);
        var count = 0;
        if (sections.TryGetValue("INIT", out var init))
        {
            count = ParseInt(init.GetValueOrDefault("NumBodies"));
        }
        if (count <= 0)
        {
            throw new InvalidDataException($"NumBodies missing in {path}");
        }
        var bodies = new BodyDef?[count + 1];
        for (var i = 1; i <= count; i++)
        {
            if (!sections.TryGetValue($"BODY{i}", out var section))
            {
                continue;
            }
            var bodyOffsetX = ParseInt(section.GetValueOrDefault("BodyOffsetX"));
            var bodyOffsetY = ParseInt(section.GetValueOrDefault("BodyOffsetY"));
            var headOffsetX = ParseInt(section.GetValueOrDefault("HeadOffsetX"));
            var headOffsetY = ParseInt(section.GetValueOrDefault("HeadOffsetY"));
            // VB6 CargarCuerpos: HeadOffset += BodyOffset.
            var body = new BodyDef
            {
                BodyOffsetX = bodyOffsetX,
                BodyOffsetY = bodyOffsetY,
                HeadOffsetX = headOffsetX + bodyOffsetX,
                HeadOffsetY = headOffsetY + bodyOffsetY,
            };
            var std = ParseInt(section.GetValueOrDefault("Std"));
            if (std > 0)
            {
                BuildMoldWalk(body, molds.Get(std), ParseInt(section.GetValueOrDefault("FileNum")), ParseSpeed(section));
            }
            else
            {
                BuildExplicitWalk(body, section);
            }
            bodies[i] = body;
        }
        return new BodyCatalog(bodies);
    }

    private static void BuildExplicitWalk(BodyDef body, Dictionary<string, string> section)
    {
        for (var heading = 1; heading <= 4; heading++)
        {
            var grhIndex = ParseInt(section.GetValueOrDefault($"Walk{heading}"));
            if (grhIndex <= 0)
            {
                continue;
            }
            body.Walk[heading] = new BodyWalkAnim
            {
                Frames = [new SpriteSlice(grhIndex)],
            };
        }
    }

    private static void BuildMoldWalk(BodyDef body, MoldDef? mold, int fileNum, float speed)
    {
        if (mold is null || fileNum <= 0 || mold.Width <= 0 || mold.Height <= 0)
        {
            return;
        }
        var animSpeed = speed <= 0f ? 1f : speed;
        // VB6 InitGrh: Grh.speed = 1/Speed/0.018 ms por frame de animación.
        var msPerFrame = 1f / animSpeed / 0.018f;
        var x = mold.X;
        var y = mold.Y;
        for (var row = 1; row <= 4; row++)
        {
            var heading = MoldRowToHeading[row];
            var frameCount = mold.DirCount[row];
            if (frameCount <= 0)
            {
                continue;
            }
            var frames = new SpriteSlice[frameCount];
            var frameX = x;
            for (var frame = 0; frame < frameCount; frame++)
            {
                frames[frame] = new SpriteSlice(
                    fileNum,
                    (short)frameX,
                    (short)y,
                    (short)mold.Width,
                    (short)mold.Height);
                frameX += mold.Width;
            }
            body.Walk[heading] = new BodyWalkAnim
            {
                Frames = frames,
                MsPerFrame = msPerFrame,
                Speed = animSpeed,
            };
            x = mold.X;
            y += mold.Height;
        }
    }

    private static float ParseSpeed(Dictionary<string, string> section)
    {
        var raw = section.GetValueOrDefault("Speed");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 1f;
        }
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 1f;
    }

    private static int ParseInt(string? value) => IniValue.ParseInt(value);
}
