using System;
using Arch.Core;
using Arch.Core.Extensions;
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
    public void StartsInTheRequestedEightWayDirection()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, Direction8.UpRight, new FacingDirection(1));
        player.Get<InputBuffer>().LastDashPressedFrame = 1;

        new DashSystem(world).Update(1);

        var velocity = player.Get<Velocity>();
        var diagonalSpeed = DashConfig.Default.Speed / MathF.Sqrt(2);
        Assert.Equal(diagonalSpeed, velocity.X, 5);
        Assert.Equal(-diagonalSpeed, velocity.Y, 5);
        Assert.True(player.Get<DashState>().IsDashing);
        Assert.Equal(DashConfig.Default.Duration, player.Get<DashState>().TimeRemaining);
        Assert.True(player.Get<InputBuffer>().LastDashPressHandled);
    }

    [Fact]
    public void DashUpdatesFacingWhenItHasAHorizontalDirection()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, Direction8.Left, new FacingDirection(1));
        player.Get<InputBuffer>().LastDashPressedFrame = 1;

        new DashSystem(world).Update(1);

        Assert.Equal(-1, player.Get<FacingDirection>().Value);
    }

    [Fact]
    public void NoDirectionUsesTheStoredFacingDirection()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, Direction8.None, new FacingDirection(-1));
        player.Get<InputBuffer>().LastDashPressedFrame = 1;

        new DashSystem(world).Update(1);

        Assert.Equal(-DashConfig.Default.Speed, player.Get<Velocity>().X, 5);
        Assert.Equal(0, player.Get<Velocity>().Y, 5);
    }

    [Fact]
    public void DashEndsAfterItsConfiguredDuration()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, Direction8.Right, new FacingDirection(1));
        player.Get<InputBuffer>().LastDashPressedFrame = 1;
        var system = new DashSystem(world);

        system.Update(1);
        for (uint frame = 2; frame <= 21; frame++) system.Update(frame);

        Assert.False(player.Get<DashState>().IsDashing);
        Assert.Equal(0, player.Get<DashState>().TimeRemaining);
        Assert.Equal(new Velocity(), player.Get<Velocity>());
    }

    [Fact]
    public void OneDashIsAvailableUntilTheNextGroundedFrame()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, Direction8.Right, new FacingDirection(1));
        player.Get<InputBuffer>().LastDashPressedFrame = 1;
        var system = new DashSystem(world);

        system.Update(1);
        player.Set(new DashState { AmountRemaining = 0 });
        player.Set(new GroundedState());
        player.Set(new InputState { IsDashPressed = true, ArrowsDirection = Direction8.Right });
        player.Get<InputBuffer>().LastDashPressedFrame = 2;
        player.Get<InputBuffer>().LastDashPressHandled = false;
        system.Update(2);

        Assert.False(player.Get<DashState>().IsDashing);

        player.Set(new GroundedState { IsGrounded = true });
        player.Get<InputBuffer>().LastDashPressedFrame = 3;
        player.Get<InputBuffer>().LastDashPressHandled = false;
        system.Update(3);

        Assert.True(player.Get<DashState>().IsDashing);
    }

    [Fact]
    public void CollisionStopsTheDashAndOnlyTheBlockedAxes()
    {
        using var world = World.Create();
        var player = world.Create(
            new MovementDelta { X = 0, Y = 4 },
            new DashState { IsDashing = true },
            new Velocity(4, 4));

        new DashCollisionSystem(world).Update(1);

        Assert.False(player.Get<DashState>().IsDashing);
        Assert.Equal(new Velocity(0, 4), player.Get<Velocity>());
    }

    private static Entity CreatePlayer(World world, Direction8 direction, FacingDirection facing) => world.Create(
        new InputState { ArrowsDirection = direction },
        new InputBuffer(),
        new GroundedState { IsGrounded = true },
        facing,
        DashConfig.Default,
        new DashState { AmountRemaining = DashConfig.Default.Amount },
        new Velocity());
}
