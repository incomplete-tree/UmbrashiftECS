using System.Drawing;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.Components.EntityComponents.Rendering;
using UmbrashiftECS.Rendering.EntityComponents;

namespace UmbrashiftECS.Rendering.Services;

public partial class RenderService
{
    public static List<RenderCommand> GatherRenderCommands(World world, uint currentFrame, bool showDebugHitboxes = false)
    {
        var result = new List<RenderCommand>();
        var currentLayer = Layer.GameplayLayer0;
        _GetCurrentLayerQuery(world, ref currentLayer);
        
        var otherLayer = currentLayer == Layer.GameplayLayer0
            ? Layer.GameplayLayer1
            : Layer.GameplayLayer0;
        
        _RenderBasicSpritesQuery(world, result, currentFrame, currentLayer, true);
        _RenderBasicSpritesQuery(world, result, currentFrame, otherLayer, false);

        _RenderColorQuery(world, result, currentLayer);

        if (showDebugHitboxes)
        {
            _RenderDebugQuery(world, result, currentLayer);
        }
        
        return result;
    }

    [Query]
    private static void _RenderBasicSprites([Data] List<RenderCommand> result, [Data] uint currentFrame, [Data] Layer layer, [Data] bool isCurrentLayer, in Entity entity, in BasicSpriteRenderer basicSpriteRenderer, in Position position)
    {
        if (entity.TryGet<Layer>(out var entityLayer) && entityLayer != layer) return;
        
        result.Add(new RenderCommand(basicSpriteRenderer.PngPath,
            position.X + basicSpriteRenderer.OffsetX,
            position.Y + basicSpriteRenderer.OffsetY,
            Color: isCurrentLayer
                ? Color.White
                : Color.FromArgb(128, 255, 255, 255)));
    }

    [Query]
    private static void _RenderColor([Data] List<RenderCommand> result, [Data] Layer currentLayer, in Entity entity, in Position position, in ColorRenderer colorRenderer)
    {
        Rectangle destRect;
        if (colorRenderer.Rectangle.HasValue && colorRenderer.Rectangle.Value.Width > 0 && colorRenderer.Rectangle.Value.Height > 0)
        {
            var r = colorRenderer.Rectangle.Value;
            destRect = new Rectangle(position.X + r.X, position.Y + r.Y, r.Width, r.Height);
        }
        else if (entity.TryGet<AabbCollider>(out var aabb))
        {
            destRect = new Rectangle(position.X, position.Y, aabb.Width, aabb.Height);
        }
        else
        {
            return;
        }

        var isCurrentLayer = !entity.TryGet<Layer>(out var entityLayer) || entityLayer == currentLayer;
        var baseColor = colorRenderer.Color;
        var color = isCurrentLayer
            ? baseColor
            : Color.FromArgb(128, baseColor.R, baseColor.G, baseColor.B);

        result.Add(new RenderCommand(
            string.Empty,
            DestRect: destRect,
            Color: color));
    }

    [Query]
    private static void _RenderDebug([Data] List<RenderCommand> result, [Data] Layer currentLayer, in Entity entity, in DebugRenderer debugRenderer, in Position position, in AabbCollider aabb)
    {
        var isCurrentLayer = !entity.TryGet<Layer>(out var entityLayer) || entityLayer == currentLayer;
        var baseColor = debugRenderer.ColorToDrawHitbox;
        var color = isCurrentLayer
            ? baseColor
            : Color.FromArgb(128, baseColor.R, baseColor.G, baseColor.B);

        result.Add(new RenderCommand(
            string.Empty,
            DestRect: new Rectangle(position.X, position.Y, aabb.Width, aabb.Height),
            Color: color,
            IsOutline: true));
    }

    [Query]
    private static void _GetCurrentLayer([Data] ref Layer result, in ActiveLayerController activeLayerController, in Layer layer)
    {
        result = layer;
    }
}