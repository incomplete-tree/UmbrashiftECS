using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class DashSystemTests
{
    [Fact]
    public void UpdateStartsADashFromABufferedPress()
    {
        using var world = World.Create();
        var player = world.Create(
            new InputBuffer
            {
                BufferFrames = 5,
                LastDashPressedFrame = 1,
                LastDashPressHandled = false
            },
            new InputState { ArrowsDirection = Direction8.Right },
            new FacingDirection { Value = 1 },
            DashConfigForTests,
            new DashState { AmountRemaining = 1 },
            new GroundedState(),
            new MovementDelta());

        new DashSystem(world).Update(1);

        Assert.True(player.Get<DashState>().IsDashing);
        Assert.Equal(0, player.Get<DashState>().AmountRemaining);
    }

    [Fact]
    public void StartingDashAppliesMovementOnThePressFrame()
    {
        using var world = World.Create();
        var player = world.Create(
            new InputBuffer
            {
                BufferFrames = 5,
                LastDashPressedFrame = 1,
                LastDashPressHandled = false
            },
            new InputState { ArrowsDirection = Direction8.Right },
            new FacingDirection { Value = 1 },
            DashConfigForTests,
            new DashState { AmountRemaining = 1 },
            new GroundedState(),
            new MovementDelta());

        new DashSystem(world).Update(1);

        Assert.Equal(DashConfigForTests.Speed, player.Get<MovementDelta>().X);
    }

    [Fact]
    public void DashRemainsActiveForItsConfiguredDuration()
    {
        using var world = World.Create();
        var player = world.Create(
            new InputBuffer
            {
                BufferFrames = 5,
                LastDashPressedFrame = 1,
                LastDashPressHandled = false
            },
            new InputState { ArrowsDirection = Direction8.Right },
            new FacingDirection { Value = 1 },
            DashConfigForTests,
            new DashState { AmountRemaining = 1 },
            new GroundedState(),
            new MovementDelta());
        var system = new DashSystem(world);

        for (uint frame = 1; frame <= DashConfigForTests.Duration; frame++)
        {
            system.Update(frame);
            Assert.Equal(DashConfigForTests.Speed, player.Get<MovementDelta>().X);
            Assert.True(player.Get<DashState>().IsDashing);
        }

        system.Update((uint)DashConfigForTests.Duration + 1);
        Assert.False(player.Get<DashState>().IsDashing);
        Assert.Equal(0, player.Get<DashState>().TimeRemaining);
    }

    [Fact]
    public void ALeftwardDashWritesNegativeMovement()
    {
        using var world = World.Create();
        var player = world.Create(
            new DashState
            {
                IsDashing = true,
                TimeRemaining = 2,
                DirectionX = -1,
                DirectionY = 0
            },
            DashConfigForTests,
            new GroundedState(),
            new MovementDelta());

        new DashSystem(world).Update(1);

        Assert.Equal(-DashConfigForTests.Speed, player.Get<MovementDelta>().X);
    }

    private static DashConfig DashConfigForTests => new(8, 4, 1, 2);
}
