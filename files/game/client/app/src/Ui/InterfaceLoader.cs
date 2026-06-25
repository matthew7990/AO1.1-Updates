using System;
using System.IO;
using Godot;

namespace Argentum.Client.Ui;

public sealed class InterfaceLoader
{
    private readonly string? _root;

    public InterfaceLoader(string resourcesRoot) =>
        _root = string.IsNullOrWhiteSpace(resourcesRoot)
            ? null
            : Path.Combine(resourcesRoot, "interface");

    public Texture2D? LoadSpanish(string fileName)
    {
        var esName = $"es_{fileName}";
        if (_root is not null)
        {
            var esPath = Path.Combine(_root, esName);
            if (File.Exists(esPath))
            {
                return LoadBmp(esPath);
            }
            var path = Path.Combine(_root, fileName);
            if (File.Exists(path))
            {
                return LoadBmp(path);
            }
        }
        return LoadEmbedded(esName) ?? LoadEmbedded(fileName);
    }

    public Texture2D? Load(string fileName)
    {
        if (_root is not null)
        {
            var path = Path.Combine(_root, fileName);
            if (File.Exists(path))
            {
                return LoadBmp(path);
            }
        }
        return LoadEmbedded(fileName);
    }

    private static Texture2D? LoadEmbedded(string fileName)
    {
        var resPath = $"res://assets/interface/{fileName}";
        if (!ResourceLoader.Exists(resPath))
        {
            return null;
        }
        return ResourceLoader.Load<Texture2D>(resPath);
    }

    private static Texture2D? LoadBmp(string path)
    {
        var image = Image.LoadFromFile(path);
        if (image is null)
        {
            GD.PushWarning($"No se pudo cargar interfaz: {path}");
            return null;
        }
        return ImageTexture.CreateFromImage(image);
    }
}
