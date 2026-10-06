using Arch.Core;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.Walk;

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