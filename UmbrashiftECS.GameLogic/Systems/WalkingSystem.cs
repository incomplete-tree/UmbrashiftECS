using Arch.Core;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.EntityComponents.Walk;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class WalkingSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void AddWalkVel(
        in GroundedState groundedState,
        ref Velocity velocity,
        ref WalkConfig walkConfig,
        in InputState inputState
    )
    {
        if (groundedState.IsGrounded)
            velocity.X += walkConfig.SpeedOnGround * inputState.ArrowsDirection.ToAxes().X;

        else
            velocity.X += walkConfig.SpeedInAir * inputState.ArrowsDirection.ToAxes().X;
    }
}