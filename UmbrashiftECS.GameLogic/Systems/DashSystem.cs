using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class DashSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public static void DecrementDashTime(in Entity entity, ref DashState dashState)
    {
        dashState.TimeRemaining--;
        if (dashState.TimeRemaining == 0)
        {
            dashState.IsDashing = false;
            var vel = entity.Get<Velocity>();
            vel.Y = 0;
            vel.X = 0;
            entity.Set<Velocity>(vel);
        }
    }

    [Query]
    public static void StartDashes(in Entity entity,
        [Data] uint currentFrame,
        ref DashState dashState,
        in DashConfig dashConfig,
        ref InputBuffer inputBuffer,
        in InputState inputState,
        in FacingDirection facingDirection)
    {
        if (dashState.IsDashing) return;

        if (dashState.AmountRemaining > 0 && inputBuffer.ConsumeActionIfPressed(currentFrame, inputBuffer.LastDashPressedFrame, ref inputBuffer.LastDashPressHandled))
        {
            dashState.AmountRemaining--;
            dashState.IsDashing = true;
            
            var direction = inputState.ArrowsDirection;
            if (direction == Direction8.None)
            {
                direction = facingDirection.Value == -1 ? Direction8.Left : Direction8.Right;
            }

            (float x, float y) = direction.ToAxes();

            var length = MathF.Sqrt(x * x + y * y);
            if (length > 0)
            {
                x /= length;
                y /= length;
            }

            dashState.DirectionX = x;
            dashState.DirectionY = y;

            dashState.TimeRemaining = dashConfig.Duration;

        }
    }

    [Query]
    public static void AddVelocityOfCurrentlyDashingEntities(in Entity entity,
        ref MovementDelta movementDelta,
        in DashState dashState,
        in DashConfig dashConfig)
    {
        if (!dashState.IsDashing) return;

        var dashVelocityX = dashState.DirectionX * dashConfig.Speed;
        if (dashState.DirectionX > 0 && movementDelta.X < dashVelocityX ||
            dashState.DirectionX < 0 && movementDelta.X > dashVelocityX)
            movementDelta.X = dashVelocityX;

        var dashVelocityY = dashState.DirectionY * dashConfig.Speed;
        if (dashState.DirectionY > 0 && movementDelta.Y < dashVelocityY ||
            dashState.DirectionY < 0 && movementDelta.Y > dashVelocityY)
            movementDelta.Y = dashVelocityY;
    }

    [Query]
    public static void RefillDashes(in Entity entity, ref DashState dashState, in DashConfig config, in GroundedState groundedState)
    {
        if (groundedState.IsGrounded && (config.Duration - dashState.TimeRemaining) > config.FramesTillRefill)
            dashState.AmountRemaining = config.Amount;
    }
}
