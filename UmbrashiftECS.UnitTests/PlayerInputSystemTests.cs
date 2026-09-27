using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class PlayerInputSystemTests
{
    [Fact]
    public void EngineUpdateAcceptsInputsForMultipleSlots()
    {
        using var engine = new Engine();
        engine.Initialize();
        var slot0 = engine.MainWorld.Create(
            new InputBinding { InputBindingSlot = 0 },
            new InputState(),
            new InputBuffer());
        var slot2 = engine.MainWorld.Create(
            new InputBinding { InputBindingSlot = 2 },
            new InputState(),
            new InputBuffer());

        engine.Update(
            new PlayerInputStateDTO
            {
                InputBindingSlot = 0,
                ArrowsDirection = Direction8.Right
            },
            new PlayerInputStateDTO
            {
                InputBindingSlot = 2,
                ArrowsDirection = Direction8.Left,
                IsJumpPressed = true
            });

        Assert.Equal(Direction8.Right, slot0.Get<InputState>().ArrowsDirection);
        Assert.Equal(Direction8.Left, slot2.Get<InputState>().ArrowsDirection);
        Assert.Equal((uint)1, slot2.Get<InputBuffer>().LastJumpPressedFrame);
    }

    [Fact]
    public void SetInputOnlyUpdatesTheMatchingInputBindingSlot()
    {
        using var world = World.Create();
        var slot0 = world.Create(new InputBinding { InputBindingSlot = 0 }, new InputState(), new InputBuffer());
        var slot2 = world.Create(new InputBinding { InputBindingSlot = 2 }, new InputState(), new InputBuffer());
        var system = new PlayerInputSystem(world);

        system.SetInput(new PlayerInputStateDTO
        {
            InputBindingSlot = 2,
            ArrowsDirection = Direction8.Left,
            IsDashPressed = true,
            IsJumpPressed = true,
            IsAttackPressed = true,
            IsTogglePressed = true,
            IsCrouchedPressed = true
        }, currentFrame: 12);

        Assert.Equal(new InputState(), slot0.Get<InputState>());
        Assert.Equal(new InputState
        {
            ArrowsDirection = Direction8.Left,
            IsDashPressed = true,
            IsJumpPressed = true,
            IsAttackPressed = true,
            IsTogglePressed = true,
            IsCrouchedPressed = true
        }, slot2.Get<InputState>());

        var slot2Buffer = slot2.Get<InputBuffer>();
        Assert.Equal((uint)12, slot2Buffer.LastDashPressedFrame);
        Assert.False(slot2Buffer.LastDashPressHandled);
        Assert.Equal((uint)12, slot2Buffer.LastJumpPressedFrame);
        Assert.False(slot2Buffer.LastJumpPressHandled);
        Assert.Equal((uint)12, slot2Buffer.LastAttackPressedFrame);
        Assert.False(slot2Buffer.LastAttackPressHandled);
        Assert.Equal((uint)12, slot2Buffer.LastTogglePressedFrame);
        Assert.False(slot2Buffer.LastTogglePressHandled);

        var slot0Buffer = slot0.Get<InputBuffer>();
        Assert.Equal((uint)0, slot0Buffer.LastDashPressedFrame);
        Assert.Equal((uint)0, slot0Buffer.LastJumpPressedFrame);
        Assert.Equal((uint)0, slot0Buffer.LastAttackPressedFrame);
        Assert.Equal((uint)0, slot0Buffer.LastTogglePressedFrame);
    }

    [Fact]
    public void HoldingAnActionDoesNotCreateAnotherBufferedPress()
    {
        using var world = World.Create();
        var player = world.Create(
            new InputBinding { InputBindingSlot = 2 },
            new InputState(),
            new InputBuffer());
        var system = new PlayerInputSystem(world);
        var input = new PlayerInputStateDTO
        {
            InputBindingSlot = 2,
            IsDashPressed = true
        };

        system.SetInput(input, currentFrame: 12);
        player.Get<InputBuffer>().LastDashPressHandled = true;
        system.SetInput(input, currentFrame: 13);

        Assert.Equal((uint)12, player.Get<InputBuffer>().LastDashPressedFrame);
        Assert.True(player.Get<InputBuffer>().LastDashPressHandled);

        system.SetInput(new PlayerInputStateDTO { InputBindingSlot = 2 }, currentFrame: 14);
        system.SetInput(input, currentFrame: 15);

        Assert.Equal((uint)15, player.Get<InputBuffer>().LastDashPressedFrame);
        Assert.False(player.Get<InputBuffer>().LastDashPressHandled);
    }
}
