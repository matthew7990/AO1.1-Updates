using System.Collections.Generic;
using System.IO;
using Godot;

namespace Argentum.Client.Resources;

public sealed class TextureCache
{
    private static readonly string[] Extensions = [".png", ".bmp", ".jpg", ".gif"];

    private readonly string _root;
    private readonly Dictionary<int, Image> _images = new();
    private readonly Dictionary<int, Texture2D> _textures = new();

    public TextureCache(string root) => _root = root;

    public Image? GetImage(int fileNum)
    {
        if (fileNum <= 0)
        {
            return null;
        }
        if (_images.TryGetValue(fileNum, out var cached))
        {
            return cached;
        }
        foreach (var extension in Extensions)
        {
            var path = Path.Combine(_root, "Graficos", $"{fileNum}{extension}");
            if (!File.Exists(path))
            {
                continue;
            }
            var image = Image.LoadFromFile(path);
            if (image.GetFormat() != Image.Format.Rgba8)
            {
                image.Convert(Image.Format.Rgba8);
            }
            _images[fileNum] = image;
            return image;
        }
        return null;
    }

    public Texture2D? Get(int fileNum)
    {
        if (fileNum <= 0)
        {
            return null;
        }
        if (_textures.TryGetValue(fileNum, out var cached))
        {
            return cached;
        }
        var image = GetImage(fileNum);
        if (image is null)
        {
            return null;
        }
        var texture = ImageTexture.CreateFromImage(image);
        _textures[fileNum] = texture;
        return texture;
    }
}
