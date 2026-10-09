using System.Drawing;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.Rendering.EntityComponents;

namespace UmbrashiftECS.Rendering.Systems;

public partial class RenderSystem(World world) : BaseSystem<World,uint>(world)
{
    [Query]
    [Any<SolidBody, ActorBody>]
    [None<DebugRenderer>]
    public void AddDebugComponents(in Entity entity)
    {
        if (entity.Has<SolidBody>())
        {
            entity.Add(new DebugRenderer(Color.Cyan));
        }
        else if (entity.Has<ActorBody>())
        {
            entity.Add(new DebugRenderer(Color.DarkRed));
        }
    }
}