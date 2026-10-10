using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class SolidMovementTests
{
    [Fact]
    public void MoveBetweenPointsSystem_ComputesMovementDeltaTowardsTarget()
    {
        using var world = World.Create();
        var mover = world.Create(
            new Position(100, 100),
            new FractionalPositionRemainder(),
            new MovementDelta(),
            new MoveBetweenPoints(2f, new Position(100, 100), new Position(120, 100)));

        var moveSystem = new MoveBetweenPointsSystem(world);
        moveSystem.Update(1);

        var delta = mover.Get<MovementDelta>();
        Assert.Equal(2f, delta.X);
        Assert.Equal(0f, delta.Y);
    }

    [Fact]
    public void MoveBetweenPointsSystem_OscillatesBackAndForth()
    {
        using var world = World.Create();
        var mover = world.Create(
            new Position(10, 10),
            new FractionalPositionRemainder(),
            new MovementDelta(),
            new MoveBetweenPoints(2f, new Position(10, 10), new Position(14, 10)));

        var moveBetweenPointsSystem = new MoveBetweenPointsSystem(world);
        var movementSystem = new MovementSystem(world);

        // Frame 1: starts at 10, next is 10 (at start point), so advances to 14, moves delta +2 -> position 12
        moveBetweenPointsSystem.Update(1);
        movementSystem.Update(1);
        Assert.Equal(new Position(12, 10), mover.Get<Position>());

        // Frame 2: moves delta +2 -> position 14
        moveBetweenPointsSystem.Update(2);
        movementSystem.Update(2);
        Assert.Equal(new Position(14, 10), mover.Get<Position>());

        // Frame 3: at 14, advances index to 0 (point 10, 10), moves delta -2 -> position 12
        moveBetweenPointsSystem.Update(3);
        movementSystem.Update(3);
        Assert.Equal(new Position(12, 10), mover.Get<Position>());

        // Frame 4: moves delta -2 -> position 10
        moveBetweenPointsSystem.Update(4);
        movementSystem.Update(4);
        Assert.Equal(new Position(10, 10), mover.Get<Position>());
    }

    [Fact]
    public void SolidMovingHorizontally_CarriesActorRidingOnTop()
    {
        using var world = World.Create();
        // Platform at (100, 200), width 50, height 10
        var platform = world.Create(
            new Position(100, 200),
            new AabbCollider { Width = 50, Height = 10 },
            new SolidBody(),
            new MovementDelta { X = 2f, Y = 0f },
            new FractionalPositionRemainder());

        // Actor on top at (110, 170), width 20, height 30 (bottom is 200)
        var actor = world.Create(
            new Position(110, 170),
            new AabbCollider { Width = 20, Height = 30 },
            new ActorBody(),
            new MovementDelta(),
            new Velocity(),
            new FractionalPositionRemainder());

        var solidCollisionSystem = new SolidCollisionSystem(world);
        var actorCollisionSystem = new ActorCollisionSystem(world);
        var movementSystem = new MovementSystem(world);

        solidCollisionSystem.Update(1);
        actorCollisionSystem.Update(1);
        movementSystem.Update(1);

        // Platform moved by +2 -> (102, 200)
        Assert.Equal(new Position(102, 200), platform.Get<Position>());
        // Actor moved by +2 -> (112, 170)
        Assert.Equal(new Position(112, 170), actor.Get<Position>());
    }

    [Fact]
    public void SolidMovingHorizontally_PushesActorInItsPath()
    {
        using var world = World.Create();
        // Platform at (100, 200), width 50, height 10
        var platform = world.Create(
            new Position(100, 200),
            new AabbCollider { Width = 50, Height = 10 },
            new SolidBody(),
            new MovementDelta { X = 2f, Y = 0f },
            new FractionalPositionRemainder());

        // Actor directly in front of platform at (150, 200), width 10, height 10
        var actor = world.Create(
            new Position(150, 200),
            new AabbCollider { Width = 10, Height = 10 },
            new ActorBody(),
            new MovementDelta(),
            new Velocity(),
            new FractionalPositionRemainder());

        var solidCollisionSystem = new SolidCollisionSystem(world);
        var actorCollisionSystem = new ActorCollisionSystem(world);
        var movementSystem = new MovementSystem(world);

        solidCollisionSystem.Update(1);
        actorCollisionSystem.Update(1);
        movementSystem.Update(1);

        // Platform moved by +2 -> (102, 200)
        Assert.Equal(new Position(102, 200), platform.Get<Position>());
        // Actor pushed by +2 -> (152, 200)
        Assert.Equal(new Position(152, 200), actor.Get<Position>());
    }

    [Fact]
    public void Engine_MovingPlatformCarriesPlayerOverMultipleFrames()
    {
        using var engine = new Engine();
        engine.Initialize();

        // Moving platform from (100, 200) to (110, 200) with speed 1f
        var platform = engine.MainWorld.Create(
            new Position(100, 200),
            new AabbCollider { Width = 50, Height = 10 },
            new SolidBody(),
            new MovementDelta(),
            new FractionalPositionRemainder(),
            new MoveBetweenPoints(1f, new Position(100, 200), new Position(110, 200)));

        // Player standing on top of platform at (120, 170), height 30 -> bottom is 200
        var player = engine.MainWorld.Create(
            new Position(120, 170),
            new AabbCollider { Width = 20, Height = 30 },
            new ActorBody(),
            new InputBinding { InputBindingSlot = 0 },
            new InputState(),
            new InputBuffer { BufferFrames = 5 },
            new Velocity(),
            new MovementDelta(),
            new FractionalPositionRemainder(),
            new GroundedState());

        for (int frame = 0; frame < 5; frame++)
        {
            engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });
        }

        // Platform moved 5 pixels from 100 to 105
        Assert.Equal(new Position(105, 200), platform.Get<Position>());
        // Player carried 5 pixels from 120 to 125
        Assert.Equal(new Position(125, 170), player.Get<Position>());
    }

    [Fact]
    public void Engine_PlayerCanWalkWhileOnMovingPlatform()
    {
        using var engine = new Engine();
        engine.Initialize();

        // Platform moving right at 1 pixel per frame
        var platform = engine.MainWorld.Create(
            new Position(100, 200),
            new AabbCollider { Width = 100, Height = 10 },
            new SolidBody(),
            new MovementDelta(),
            new FractionalPositionRemainder(),
            new MoveBetweenPoints(1f, new Position(100, 200), new Position(200, 200)));

        // Player standing on platform at (120, 170), height 30 -> bottom is 200
        var player = engine.MainWorld.Create(
            new Position(120, 170),
            new AabbCollider { Width = 20, Height = 30 },
            new ActorBody(),
            new InputBinding { InputBindingSlot = 0 },
            new InputState(),
            new InputBuffer { BufferFrames = 5 },
            new Velocity(),
            new MovementDelta(),
            new FractionalPositionRemainder(),
            new GroundedState { IsGrounded = true });

        // Player moves right with velocity 2, platform moves right at 1
        player.Set(new Velocity(2, 0));

        engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });

        // Platform moved to 101
        Assert.Equal(new Position(101, 200), platform.Get<Position>());
        // Player walked 2 and platform carried 1 -> moved 3 to 123
        Assert.Equal(new Position(123, 170), player.Get<Position>());
    }
}
