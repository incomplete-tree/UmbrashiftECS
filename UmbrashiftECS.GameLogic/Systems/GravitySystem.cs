using Arch.Core;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class GravitySystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void ApplyGravity(in GravityConfig gravityConfig, in InputState inputState, ref Velocity velocity)
    {
        var gravity = gravityConfig.GravityInPixelsPerFrameSquared;
        if (gravityConfig.ChangeGravityWhenJumpHeld && inputState.IsJumpPressed)
        {
            gravity *= gravityConfig.JumpHeldModifier;
        }

        velocity.Y += gravity;
    }
}