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

    public List<RenderCommand> Update(uint currentFrame, bool? showDebugHitboxes = null)
    {
        if (showDebugHitboxes.HasValue)
        {
            ShowDebugHitboxes = showDebugHitboxes.Value;
        }

        _systems.Update(currentFrame);

        return RenderService.GatherRenderCommands(world, currentFrame, ShowDebugHitboxes);
    }
}