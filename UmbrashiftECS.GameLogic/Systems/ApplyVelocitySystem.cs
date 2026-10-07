using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class ApplyVelocitySystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    [None<Dead>]
    private static void ApplyVelocity(in Entity entity, in Velocity velocity, ref MovementDelta movementDelta)
    {
        movementDelta = new MovementDelta()
        {
            X = velocity.X,
            Y = velocity.Y
        };
    }
}