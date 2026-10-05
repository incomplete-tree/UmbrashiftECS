using Arch.Core;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class LayerToggleSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void ToggleLayer(
        in Entity entity,
        [Data] in uint currentFrame,
        ref InputBuffer inputBuffer,
        ref Layer layer,
        ref Position position)
    {
        if (!inputBuffer.IsBuffered(currentFrame, inputBuffer.LastToggleReleasedFrame,
                inputBuffer.LastTogglePressHandled)) return;
        
        var originalLayer = layer;
        layer = originalLayer == Layer.GameplayLayer0 ? Layer.GameplayLayer1 : Layer.GameplayLayer0;
        
        if (!CollisionService.IsCollidingWith<SolidBody>(entity))
        {
            return;
        }

        for (var distance = 1; distance <= 3; distance++)
        {
            var offsets = new[]
            {
                (distance, 0),
                (-distance, 0),
                (0, distance),
                (0, -distance)
            };
            foreach (var (offsetX, offsetY) in offsets)
            {
                if (!CollisionService.IsCollidingWith<SolidBody>(entity, offsetX, offsetY))
                {
                    inputBuffer.LastTogglePressHandled = true;
                    return;
                }
            }
        }

        layer = originalLayer;
    }
}
