#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UmbrashiftECS.Rendering;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace UmbrashiftECS.MonoGame;

public static class RenderCommandExecutor
{
    private static readonly Dictionary<string, Texture2D> _cachedTextures = new(StringComparer.OrdinalIgnoreCase);
    private static Texture2D? _whitePixelTexture;

    public static void ExecuteRenderCommand(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, RenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(spriteBatch);

        var texture = GetTexture(graphicsDevice, command.TexturePath);
        
        var color = command.Color.HasValue
            ? new XnaColor(command.Color.Value.R, command.Color.Value.G, command.Color.Value.B, command.Color.Value.A)
            : XnaColor.White;

        XnaRectangle? sourceRect = command.SourceRect.HasValue
            ? new XnaRectangle(
                command.SourceRect.Value.X,
                command.SourceRect.Value.Y,
                command.SourceRect.Value.Width,
                command.SourceRect.Value.Height)
            : null;

        if (command.DestRect.HasValue)
        {
            var dest = command.DestRect.Value;
            var destRect = new XnaRectangle(
                command.X ?? dest.X,
                command.Y ?? dest.Y,
                dest.Width,
                dest.Height);

            if (command.IsOutline)
            {
                DrawOutline(spriteBatch, texture, destRect, color, command.Thickness);
            }
            else
            {
                spriteBatch.Draw(texture, destRect, sourceRect, color);
            }
        }
        else
        {
            var position = new Vector2(command.X ?? 0, command.Y ?? 0);
            spriteBatch.Draw(texture, position, sourceRect, color);
        }
    }

    private static void DrawOutline(SpriteBatch spriteBatch, Texture2D texture, XnaRectangle rect, XnaColor color, int thickness)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        thickness = Math.Max(1, thickness);

        // Top edge
        spriteBatch.Draw(texture, new XnaRectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom edge
        spriteBatch.Draw(texture, new XnaRectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left edge
        spriteBatch.Draw(texture, new XnaRectangle(rect.X, rect.Y + thickness, thickness, Math.Max(0, rect.Height - 2 * thickness)), color);
        // Right edge
        spriteBatch.Draw(texture, new XnaRectangle(rect.X + rect.Width - thickness, rect.Y + thickness, thickness, Math.Max(0, rect.Height - 2 * thickness)), color);
    }

    public static Texture2D GetTexture(GraphicsDevice graphicsDevice, string? texturePath)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        if (string.IsNullOrWhiteSpace(texturePath) ||
            texturePath.Equals("white", StringComparison.OrdinalIgnoreCase) ||
            texturePath.Equals("blank", StringComparison.OrdinalIgnoreCase))
        {
            return GetOrCreateWhitePixel(graphicsDevice);
        }

        if (_cachedTextures.TryGetValue(texturePath, out var cachedTexture))
        {
            if (!cachedTexture.IsDisposed && cachedTexture.GraphicsDevice == graphicsDevice)
            {
                return cachedTexture;
            }

            _cachedTextures.Remove(texturePath);
        }

        var resolvedPath = ResolveTexturePath(texturePath);
        if (resolvedPath == null)
        {
            throw new FileNotFoundException($"Could not find texture file '{texturePath}'.", texturePath);
        }

        var loadedTexture = Texture2D.FromFile(graphicsDevice, resolvedPath);
        _cachedTextures[texturePath] = loadedTexture;
        return loadedTexture;
    }

    public static void ClearCache()
    {
        foreach (var texture in _cachedTextures.Values)
        {
            texture.Dispose();
        }
        _cachedTextures.Clear();

        _whitePixelTexture?.Dispose();
        _whitePixelTexture = null;
    }

    private static Texture2D GetOrCreateWhitePixel(GraphicsDevice graphicsDevice)
    {
        if (_whitePixelTexture == null || _whitePixelTexture.IsDisposed || _whitePixelTexture.GraphicsDevice != graphicsDevice)
        {
            _whitePixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _whitePixelTexture.SetData(new[] { XnaColor.White });
        }

        return _whitePixelTexture;
    }

    private static string? ResolveTexturePath(string texturePath)
    {
        if (File.Exists(texturePath))
        {
            return texturePath;
        }

        var baseDir = AppContext.BaseDirectory;
        var candidates = new List<string>
        {
            texturePath,
            Path.Combine(baseDir, texturePath),
            Path.Combine("Content", texturePath),
            Path.Combine(baseDir, "Content", texturePath)
        };

        if (!Path.HasExtension(texturePath))
        {
            var withPng = texturePath + ".png";
            candidates.Add(withPng);
            candidates.Add(Path.Combine(baseDir, withPng));
            candidates.Add(Path.Combine("Content", withPng));
            candidates.Add(Path.Combine(baseDir, "Content", withPng));
        }

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}