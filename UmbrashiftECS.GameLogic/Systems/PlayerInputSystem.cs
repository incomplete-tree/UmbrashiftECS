using Arch.Core;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class PlayerInputSystem : BaseSystem<World, uint>
{
    public PlayerInputSystem(World world) : base(world) { }

    public void SetInput(in PlayerInputStateDTO input, uint currentFrame)
    {
        BufferInputQuery(World, input, currentFrame);
        ApplyInputQuery(World, input);
    }

    [Query]
    private static void ApplyInput(
        [Data] in PlayerInputStateDTO input,
        in InputBinding inputBinding,
        ref InputState inputState)
    {
        if (inputBinding.InputBindingSlot != input.InputBindingSlot) return;

        inputState = new InputState
        {
            ArrowsDirection = input.ArrowsDirection,
            IsDashPressed = input.IsDashPressed,
            IsJumpPressed = input.IsJumpPressed,
            IsAttackPressed = input.IsAttackPressed,
            IsTogglePressed = input.IsTogglePressed,
            IsCrouchedPressed = input.IsCrouchedPressed
        };
    }

    [Query]
    private static void BufferInput(
        [Data] in PlayerInputStateDTO input,
        [Data] in uint currentFrame,
        in InputBinding inputBinding,
        in InputState inputState,
        ref InputBuffer inputBuffer)
    {
        if (inputBinding.InputBindingSlot != input.InputBindingSlot) return;

        if (input.IsDashPressed && !inputState.IsDashPressed)
        {
            inputBuffer.LastDashPressedFrame = currentFrame;
            inputBuffer.LastDashPressHandled = false;
        }
        else if (!input.IsDashPressed && inputState.IsDashPressed)
        {
            inputBuffer.LastDashReleasedFrame = currentFrame;
            inputBuffer.LastDashReleaseHandled = false;
        }

        if (input.IsJumpPressed && !inputState.IsJumpPressed)
        {
            inputBuffer.LastJumpPressedFrame = currentFrame;
            inputBuffer.LastJumpPressHandled = false;
        }
        else if (!input.IsJumpPressed && inputState.IsJumpPressed)
        {
            inputBuffer.LastJumpReleasedFrame = currentFrame;
            inputBuffer.LastJumpReleaseHandled = false;
        }

        if (input.IsAttackPressed && !inputState.IsAttackPressed)
        {
            inputBuffer.LastAttackPressedFrame = currentFrame;
            inputBuffer.LastAttackPressHandled = false;
        }
        else if (!input.IsAttackPressed && inputState.IsAttackPressed)
        {
            inputBuffer.LastAttackReleasedFrame = currentFrame;
            inputBuffer.LastAttackReleaseHandled = false;
        }

        if (input.IsTogglePressed && !inputState.IsTogglePressed)
        {
            inputBuffer.LastTogglePressedFrame = currentFrame;
            inputBuffer.LastTogglePressHandled = false;
        }
        else if (!input.IsTogglePressed && inputState.IsTogglePressed)
        {
            inputBuffer.LastToggleReleasedFrame = currentFrame;
            inputBuffer.LastToggleReleaseHandled = false;
        }
    }
    
    public override void Update(in uint currentFrame)
    {
        // Overidden to prevent query method from running during update.
        base.Update(in currentFrame);
    }
}
