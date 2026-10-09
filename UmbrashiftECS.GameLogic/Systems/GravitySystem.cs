using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using UmbrashiftECS.GameLogic.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class GravitySystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void ApplyGravity(in Entity entity, in GravityConfig gravityConfig, in InputState inputState, ref Velocity velocity)
    {
        if (entity.TryGet<DashState>(out var dashState) && dashState.IsDashing) return;
        var gravity = gravityConfig.GravityInPixelsPerFrameSquared;
        if (gravityConfig.ChangeGravityWhenJumpHeld && inputState.IsJumpPressed)
        {
            gravity *= gravityConfig.JumpHeldModifier;
        }

        velocity.Y += gravity;
    }
}