using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class PlayerInputSystem : BaseSystem<World, uint>
{
    private static readonly QueryDescription InputQuery = new QueryDescription()
        .WithAll<InputBinding, InputState, InputBuffer>();

    public PlayerInputSystem(World world) : base(world) { }

    public void SetInput(in PlayerInputStateDTO input, uint currentFrame)
    {
        BufferInputQuery(World, input, currentFrame);
        ApplyInputQuery(World, input);
    }

    public void ClearUnprovidedInputs(IReadOnlyList<PlayerInputStateDTO> inputs, uint currentFrame)
    {
        var providedSlots = new HashSet<int>();
        foreach (var input in inputs) providedSlots.Add(input.InputBindingSlot);

        var missingSlots = new HashSet<int>();
        foreach (var chunk in World.Query(InputQuery).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                var slot = chunk.Entity(index).Get<InputBinding>().InputBindingSlot;
                if (!providedSlots.Contains(slot)) missingSlots.Add(slot);
            }
        }

        foreach (var slot in missingSlots)
        {
            SetInput(new PlayerInputStateDTO { InputBindingSlot = slot }, currentFrame);
        }
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
