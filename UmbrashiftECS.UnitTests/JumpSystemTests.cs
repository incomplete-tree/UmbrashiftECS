using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class Direction8Tests
{
    [Theory]
    [InlineData(Direction8.None, 0, 0)]
    [InlineData(Direction8.Up, 0, -1)]
    [InlineData(Direction8.UpRight, 1, -1)]
    [InlineData(Direction8.Right, 1, 0)]
    [InlineData(Direction8.DownRight, 1, 1)]
    [InlineData(Direction8.Down, 0, 1)]
    [InlineData(Direction8.DownLeft, -1, 1)]
    [InlineData(Direction8.Left, -1, 0)]
    [InlineData(Direction8.UpLeft, -1, -1)]
    public void DirectionConvertsToAxes(Direction8 direction, int expectedX, int expectedY)
    {
        Assert.Equal((expectedX, expectedY), direction.ToAxes());
    }
}

public class JumpSystemTests
{
    [Fact]
    public void GroundedBufferedJumpSetsInitialVelocityAndConsumesPress()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new Position(0, 0));
        world.Create(
            new Position(0, 2),
            new SolidBody(),
            new AabbCollider { Width = 2, Height = 2 });
        var inputBuffer = player.Get<InputBuffer>();
        inputBuffer.LastJumpPressedFrame = 1;

        new JumpSystem(world).Update(1);

        Assert.Equal(-JumpConfig.Default.InitialJumpSpeed, player.Get<Velocity>().Y, 5);
        Assert.True(inputBuffer.LastJumpPressHandled);
        Assert.False(player.Get<GroundedState>().IsGrounded);
        Assert.Equal((uint)0, player.Get<GroundedState>().CoyoteFramesRemaining);
    }

    [Fact]
    public void CoyoteTimeAllowsARecentBufferedJumpAfterLeavingGround()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new Position(0, 0));
        player.Set(new GroundedState { LastGroundedTime = 1, CoyoteFramesRemaining = 3 });
        player.Get<InputBuffer>().LastJumpPressedFrame = 2;

        new JumpSystem(world).Update(3);

        Assert.Equal(-JumpConfig.Default.InitialJumpSpeed, player.Get<Velocity>().Y, 5);
        Assert.True(player.Get<InputBuffer>().LastJumpPressHandled);
        Assert.Equal((uint)0, player.Get<GroundedState>().CoyoteFramesRemaining);
    }

    [Fact]
    public void TheLastCoyoteFrameStillAcceptsAJump()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new Position(0, 0));
        player.Set(new GroundedState { CoyoteFramesRemaining = 1 });
        player.Get<InputBuffer>().LastJumpPressedFrame = 2;

        new JumpSystem(world).Update(3);

        Assert.Equal(-JumpConfig.Default.InitialJumpSpeed, player.Get<Velocity>().Y, 5);
        Assert.Equal((uint)0, player.Get<GroundedState>().CoyoteFramesRemaining);
    }

    [Fact]
    public void ConfiguredAirJumpDoesNotNeedGroundOrCoyoteTime()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new Position(0, 0));
        player.Set(JumpConfig.Default with { AllowAirJump = true });
        player.Get<InputBuffer>().LastJumpPressedFrame = 1;

        new JumpSystem(world).Update(1);

        Assert.Equal(-JumpConfig.Default.InitialJumpSpeed, player.Get<Velocity>().Y, 5);
        Assert.True(player.Get<InputBuffer>().LastJumpPressHandled);
    }

    [Fact]
    public void GravityUsesHalfStrengthWhileJumpIsHeldAndStopsAtTerminalVelocity()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new Position(0, 0));
        var system = new JumpSystem(world);

        player.Set(new InputState { IsJumpPressed = true });
        system.Update(1);
        var heldVelocity = player.Get<Velocity>().Y;

        player.Set(new InputState());
        for (uint frame = 2; frame < 20; frame++) system.Update(frame);

        Assert.Equal(JumpConfig.Default.Gravity / 2f, heldVelocity, 5);
        Assert.Equal(5f, player.Get<Velocity>().Y, 5);
    }

    private static Entity CreatePlayer(World world, Position position) => world.Create(
        position,
        new ActorBody(),
        new OffsetAabbCollider { Width = 2, Height = 2 },
        JumpConfig.Default,
        new GroundedState(),
        new InputState(),
        new InputBuffer(),
        Gravity.PlayerDefault,
        new Velocity());
}
