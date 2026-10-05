using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
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
    public void BufferedJumpUsesThePressFrame()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new GroundedState
        {
            IsGrounded = true,
            LastGroundedTime = 1
        });
        player.Set(new InputBuffer
        {
            BufferFrames = 5,
            LastJumpPressedFrame = 1,
            LastJumpPressHandled = false
        });

        new JumpSystem(world).Update(1);

        Assert.Equal(-6f, player.Get<Velocity>().Y);
        Assert.True(player.Get<InputBuffer>().LastJumpPressHandled);
    }

    [Fact]
    public void AJumpDoesNotStartFromTheDefaultReleaseFrame()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new GroundedState
        {
            IsGrounded = true,
            LastGroundedTime = 1
        });
        player.Set(new InputBuffer
        {
            BufferFrames = 5,
            LastJumpReleaseHandled = false
        });

        new JumpSystem(world).Update(1);

        Assert.Equal(0f, player.Get<Velocity>().Y);
        Assert.True(player.Get<InputBuffer>().LastJumpPressHandled);
    }

    [Fact]
    public void CoyoteTimeAcceptsARecentPressedJump()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new GroundedState
        {
            LastGroundedTime = 2
        });
        player.Set(new InputBuffer
        {
            BufferFrames = 5,
            LastJumpPressedFrame = 5,
            LastJumpPressHandled = false
        });

        new JumpSystem(world).Update(5);

        Assert.Equal(-6f, player.Get<Velocity>().Y);
    }

    [Fact]
    public void DashPreventsJumping()
    {
        using var world = World.Create();
        var player = CreatePlayer(world, new GroundedState
        {
            IsGrounded = true,
            LastGroundedTime = 1
        });
        player.Add(new DashState { IsDashing = true });
        player.Set(new InputBuffer
        {
            BufferFrames = 5,
            LastJumpPressedFrame = 1,
            LastJumpPressHandled = false
        });

        new JumpSystem(world).Update(1);

        Assert.Equal(0f, player.Get<Velocity>().Y);
    }

    private static Entity CreatePlayer(World world, GroundedState groundedState) => world.Create(
        new InputBuffer { BufferFrames = 5 },
        new JumpConfig
        {
            InitialJumpSpeed = -6,
            CoyoteTimeFrames = 3
        },
        groundedState,
        new Velocity());
}
