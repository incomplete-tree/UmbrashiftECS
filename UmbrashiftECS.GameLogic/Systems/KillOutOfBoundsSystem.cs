using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class KillOutOfBoundsSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    [None<Dead>]
    public void KillOutOfBounds([Data] uint currentFrame, in DiesOutOfBounds _, in Entity entity)
    {
        if (CollisionService.IsCollidingWith<MapBounds>(entity)) return;
        
        entity.Add(new Dead(currentFrame));
    }
}