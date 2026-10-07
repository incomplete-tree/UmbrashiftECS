using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class JumpSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void StartJumps(
        in Entity entity,
        [Data] in uint currentFrame,
        ref InputBuffer inputBuffer,
        in JumpConfig jumpConfig,
        in GroundedState groundedState,
        ref Velocity velocity)
    {
        if (entity.TryGet<DashState>(out var dashState) && dashState.IsDashing)
        {
            return;
        }

        var canJump = currentFrame - groundedState.LastGroundedTime <= jumpConfig.CoyoteTimeFrames || jumpConfig.AllowAirJump;

        if (!canJump) return;

        if (!inputBuffer.ConsumeActionIfPressed(currentFrame, inputBuffer.LastJumpPressedFrame,
            ref inputBuffer.LastJumpPressHandled)) return;

        velocity.Y += jumpConfig.InitialJumpSpeed;
    }
}
