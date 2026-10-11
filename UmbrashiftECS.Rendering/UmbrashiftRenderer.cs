using System.Numerics;
using Arch.Core;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Rendering.Services;
using UmbrashiftECS.Rendering.Systems;

namespace UmbrashiftECS.Rendering;

public class UmbrashiftRenderer(World world)
{
    public bool ShowDebugHitboxes { get; set; }
    public ISystem<uint> _systems = null!;

    public void Initialize()
    {
        _systems = new Group<uint>("rendering",
            new RenderSystem(world)
        );
    }

    public (List<RenderCommand>, Matrix4x4) Update(uint currentFrame, bool? showDebugHitboxes = null)
    {
        if (showDebugHitboxes.HasValue)
        {
            ShowDebugHitboxes = showDebugHitboxes.Value;
        }

        _systems.Update(currentFrame);

        var cmds = RenderService.GatherRenderCommands(world, currentFrame, ShowDebugHitboxes);
        var cameraMatrix = CameraService.Update(world);

        return (cmds, cameraMatrix);
    }
}