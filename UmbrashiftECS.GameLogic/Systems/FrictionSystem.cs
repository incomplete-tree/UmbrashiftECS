using Arch.Core;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents.Walk;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class FrictionSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void ApplyFriction(in GroundedState groundedState, in FrictionConfig frictionConfig, ref Velocity velocity)
    {
        if (groundedState.IsGrounded)
            velocity.X *= frictionConfig.Friction;
    }
}