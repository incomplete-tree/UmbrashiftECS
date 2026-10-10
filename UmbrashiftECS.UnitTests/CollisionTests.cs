using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.Services;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class CollisionServiceTests
{
    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(2, 0, false)]
    [InlineData(0, 2, false)]
    [InlineData(2, 2, false)]
    public void IsColliding_Aabbs_ReturnsExpectedResult(int secondX, int secondY, bool expected)
    {
        using var world = World.Create();
        var first = CollisionTestEntities.Aabb(world, new Position(0, 0), 2, 2);
        var second = CollisionTestEntities.Aabb(world, new Position(secondX, secondY), 2, 2);

        Assert.Equal(expected, CollisionService.IsColliding(first, second));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsColliding_AabbAndOffsetAabb_ReturnsExpectedResultInEitherOrder(bool reverse)
    {
        using var world = World.Create();
        var aabb = CollisionTestEntities.Aabb(world, new Position(0, 0), 2, 2);
        var offsetAabb = CollisionTestEntities.OffsetAabb(world, new Position(0, 0), 2, 2, 1, 1);

        var result = reverse
            ? CollisionService.IsColliding(offsetAabb, aabb)
            : CollisionService.IsColliding(aabb, offsetAabb);

        Assert.True(result);
    }

    [Theory]
    [InlineData(2, 2, -1, -1, true)]
    [InlineData(3, 3, 0, 0, false)]
    public void IsColliding_OffsetAabbs_ReturnsExpectedResult(
        int secondX,
        int secondY,
        int secondOffsetX,
        int secondOffsetY,
        bool expected)
    {
        using var world = World.Create();
        var first = CollisionTestEntities.OffsetAabb(world, new Position(0, 0), 2, 2, 1, 1);
        var second = CollisionTestEntities.OffsetAabb(
            world,
            new Position(secondX, secondY),
            2,
            2,
            secondOffsetX,
            secondOffsetY);

        Assert.Equal(expected, CollisionService.IsColliding(first, second));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void IsColliding_AppliesOffsetToFirstEntity(int offsetX, bool expected)
    {
        using var world = World.Create();
        var first = CollisionTestEntities.Aabb(world, new Position(0, 0), 2, 2);
        var second = CollisionTestEntities.Aabb(world, new Position(3, 0), 1, 2);

        Assert.Equal(expected, CollisionService.IsColliding(first, second, offsetX: offsetX));
    }

    [Theory]
    [InlineData(Layer.GameplayLayer0, Layer.GameplayLayer0, true)]
    [InlineData(Layer.GameplayLayer0, Layer.GameplayLayer1, false)]
    public void IsColliding_Layers_ReturnExpectedResult(Layer firstLayer, Layer secondLayer, bool expected)
    {
        using var world = World.Create();
        var first = CollisionTestEntities.Aabb(world, new Position(0, 0), 2, 2, firstLayer);
        var second = CollisionTestEntities.Aabb(world, new Position(1, 1), 2, 2, secondLayer);

        Assert.Equal(expected, CollisionService.IsColliding(first, second));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void IsCollidingWith_FiltersByBodyAndAppliesOffset(int offsetX, bool expected)
    {
        using var world = World.Create();
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 2, 2);
        CollisionTestEntities.Solid(world, new Position(3, 0), 1, 2);
        CollisionTestEntities.Aabb(world, new Position(0, 0), 2, 2);

        Assert.Equal(expected, CollisionService.IsCollidingWith<SolidBody>(actor, offsetX: offsetX));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(6, true)]
    public void IsCollidingWith_SupportsOffsetActor(int offsetX, bool expected)
    {
        using var world = World.Create();
        var actor = CollisionTestEntities.Actor(world, new Position(0, 0), 2, 2);
        CollisionTestEntities.Solid(world, new Position(5, 0), 5, 2);

        Assert.Equal(expected, CollisionService.IsCollidingWith<SolidBody>(actor, offsetX: offsetX));
    }
}

public class ActorCollisionSystemTests
{
    [Theory]
    [InlineData(0, 5f, 0.25f, 5, 5, 2.75f, 3)]
    [InlineData(10, -5f, 0.25f, 3, 5, -2.25f, 8)]
    public void CheckActorCollision_ClampsXAndMovementSystemClearsRemainder(
        int actorX,
        float movementX,
        float remainderX,
        int solidX,
        int solidWidth,
        float expectedMovementX,
        int expectedPositionX)
    {
        using var world = World.Create();
        var actor = CollisionTestEntities.MovingActor(
            world,
            new Position(actorX, 0),
            new FractionalPositionRemainder { X = remainderX },
            new MovementDelta { X = movementX });
        CollisionTestEntities.Solid(world, new Position(solidX, 0), solidWidth, 2);
        var originalPosition = actor.Get<Position>();
        var originalRemainder = actor.Get<FractionalPositionRemainder>();

        new ActorCollisionSystem(world).Update(1);

        Assert.Equal(expectedMovementX, actor.Get<MovementDelta>().X, 5);
        Assert.Equal(originalPosition, actor.Get<Position>());
        Assert.Equal(originalRemainder, actor.Get<FractionalPositionRemainder>());

        new MovementSystem(world).Update(1);

        Assert.Equal(new Position(expectedPositionX, 0), actor.Get<Position>());
        Assert.Equal(0f, actor.Get<FractionalPositionRemainder>().X, 5);
    }

    [Theory]
    [InlineData(0, 5f, 0.25f, 5, 5, 2.75f, 3)]
    [InlineData(10, -5f, 0.25f, 3, 5, -2.25f, 8)]
    public void CheckActorCollision_ClampsYAndMovementSystemClearsRemainder(
        int actorY,
        float movementY,
        float remainderY,
        int solidY,
        int solidHeight,
        float expectedMovementY,
        int expectedPositionY)
    {
        using var world = World.Create();
        var actor = CollisionTestEntities.MovingActor(
            world,
            new Position(0, actorY),
            new FractionalPositionRemainder { Y = remainderY },
            new MovementDelta { Y = movementY });
        CollisionTestEntities.Solid(world, new Position(0, solidY), 2, solidHeight);
        var originalPosition = actor.Get<Position>();
        var originalRemainder = actor.Get<FractionalPositionRemainder>();

        new ActorCollisionSystem(world).Update(1);

        Assert.Equal(expectedMovementY, actor.Get<MovementDelta>().Y, 5);
        Assert.Equal(originalPosition, actor.Get<Position>());
        Assert.Equal(originalRemainder, actor.Get<FractionalPositionRemainder>());

        new MovementSystem(world).Update(1);

        Assert.Equal(new Position(0, expectedPositionY), actor.Get<Position>());
        Assert.Equal(0f, actor.Get<FractionalPositionRemainder>().Y, 5);
    }

    [Theory]
    [InlineData(0.5f, 0.25f, 0.75f)]
    [InlineData(-0.5f, 0.25f, -0.25f)]
    public void CheckActorCollision_LeavesUnobstructedFractionalMovement(
        float movementX,
        float remainderX,
        float expectedRemainderX)
    {
        using var world = World.Create();
        var actor = CollisionTestEntities.MovingActor(
            world,
            new Position(0, 0),
            new FractionalPositionRemainder { X = remainderX },
            new MovementDelta { X = movementX });
        var originalPosition = actor.Get<Position>();
        var originalRemainder = actor.Get<FractionalPositionRemainder>();

        new ActorCollisionSystem(world).Update(1);

        Assert.Equal(movementX, actor.Get<MovementDelta>().X, 5);
        Assert.Equal(originalPosition, actor.Get<Position>());
        Assert.Equal(originalRemainder, actor.Get<FractionalPositionRemainder>());

        new MovementSystem(world).Update(1);

        Assert.Equal(new Position(0, 0), actor.Get<Position>());
        Assert.Equal(expectedRemainderX, actor.Get<FractionalPositionRemainder>().X, 5);
    }
}

internal static class CollisionTestEntities
{
    public static Entity Aabb(World world, Position position, int width, int height, Layer? layer = null)
    {
        var entity = world.Create(position, new AabbCollider { Width = width, Height = height });
        if (layer.HasValue)
        {
            entity.Add(layer.Value);
        }

        return entity;
    }

    public static Entity OffsetAabb(
        World world,
        Position position,
        int width,
        int height,
        int offsetX,
        int offsetY)
    {
        return world.Create(
            position,
            new OffsetAabbCollider
            {
                Width = width,
                Height = height,
                OffsetX = offsetX,
                OffsetY = offsetY
            });
    }

    public static Entity Actor(World world, Position position, int width, int height)
    {
        return world.Create(
            position,
            new ActorBody(),
            new OffsetAabbCollider
            {
                Width = width,
                Height = height
            });
    }

    public static Entity MovingActor(
        World world,
        Position position,
        FractionalPositionRemainder remainder,
        MovementDelta movementDelta)
    {
        return world.Create(
            position,
            new ActorBody(),
            new OffsetAabbCollider { Width = 2, Height = 2 },
            new Velocity(),
            remainder,
            movementDelta);
    }

    public static Entity Solid(World world, Position position, int width, int height)
    {
        return world.Create(
            position,
            new SolidBody(),
            new AabbCollider { Width = width, Height = height });
    }

    public static Entity OneWay(
        World world,
        Position position,
        int width,
        int height,
        Direction4 direction = Direction4.Up,
        bool withSolidBody = false)
    {
        return withSolidBody
            ? world.Create(
                position,
                new SolidBody(),
                new OneWayCollision(direction),
                new AabbCollider { Width = width, Height = height })
            : world.Create(
                position,
                new OneWayCollision(direction),
                new AabbCollider { Width = width, Height = height });
    }
}

public class OneWayCollisionTests
{
    [Fact]
    public void Up_CollidesWhenFallingFromAbove()
    {
        using var world = World.Create();
        // Platform at (0, 10), 10x10. Top edge is Y=10.
        var platform = CollisionTestEntities.OneWay(world, new Position(0, 10), 10, 10, Direction4.Up);
        // Actor at (0, 0), 10x10. Bottom is Y=10 (initial bottom <= platform top).
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        // Moving down by 2 -> target bottom = 12 > 10.
        Assert.True(CollisionService.IsColliding(actor, platform, offsetY: 2));
    }

    [Fact]
    public void Up_AllowsPassingThroughFromBelow()
    {
        using var world = World.Create();
        var platform = CollisionTestEntities.OneWay(world, new Position(0, 10), 10, 10, Direction4.Up);
        // Actor at (0, 20), 10x10. Moving UP by 5 into platform (target Y=15, top=15 < bottom=20).
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 20), 10, 10);

        Assert.False(CollisionService.IsColliding(actor, platform, offsetY: -5));
    }

    [Fact]
    public void Up_AllowsPassingThroughFromLeftAndRight()
    {
        using var world = World.Create();
        var platform = CollisionTestEntities.OneWay(world, new Position(10, 10), 10, 10, Direction4.Up);

        // Actor at (0, 10), moving right into platform
        var actorLeft = CollisionTestEntities.Aabb(world, new Position(0, 10), 10, 10);
        Assert.False(CollisionService.IsColliding(actorLeft, platform, offsetX: 5));

        // Actor at (20, 10), moving left into platform
        var actorRight = CollisionTestEntities.Aabb(world, new Position(20, 10), 10, 10);
        Assert.False(CollisionService.IsColliding(actorRight, platform, offsetX: -5));
    }

    [Fact]
    public void Up_HalfwayThroughFromBelow_CanStillGoBackDown()
    {
        using var world = World.Create();
        // Platform at (0, 10), 10x10. Top edge is Y=10, bottom edge is Y=20.
        var platform = CollisionTestEntities.OneWay(world, new Position(0, 10), 10, 10, Direction4.Up);
        // Actor at (0, 5), 10x10. Top is 5 (above platform top), bottom is 15 (inside platform).
        // Actor jumped from below and is halfway through!
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 5), 10, 10);

        // Actor falls back down by 2 (going back):
        Assert.False(CollisionService.IsColliding(actor, platform, offsetY: 2));
    }

    [Fact]
    public void Up_OnlyWhenFullyThrough_CantGoBackDown()
    {
        using var world = World.Create();
        // Platform at (0, 10), 10x10. Top edge is Y=10.
        var platform = CollisionTestEntities.OneWay(world, new Position(0, 10), 10, 10, Direction4.Up);
        // Actor at (0, 0), 10x10. Bottom is 10 (fully through: bottom <= platform top).
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        // Actor tries to go back down by 2:
        Assert.True(CollisionService.IsColliding(actor, platform, offsetY: 2));
    }

    [Fact]
    public void Up_ActorOnPlatform_CanJumpUp()
    {
        using var world = World.Create();
        var platform = CollisionTestEntities.OneWay(world, new Position(0, 10), 10, 10, Direction4.Up);
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        // Jumping up (negative Y)
        Assert.False(CollisionService.IsColliding(actor, platform, offsetY: -5));
    }

    [Fact]
    public void Up_ActorOnPlatform_CanWalkHorizontally()
    {
        using var world = World.Create();
        var platform = CollisionTestEntities.OneWay(world, new Position(0, 10), 20, 10, Direction4.Up);
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        // Walking horizontally across the top
        Assert.False(CollisionService.IsColliding(actor, platform, offsetX: 5));
    }

    [Fact]
    public void Down_CollidesWhenMovingUpFromBelow()
    {
        using var world = World.Create();
        // Obstacle at (0, 0), 10x10. Bottom edge is Y=10.
        var obstacle = CollisionTestEntities.OneWay(world, new Position(0, 0), 10, 10, Direction4.Down);
        // Actor at (0, 10), 10x10. Top is Y=10 (initial top >= obstacle bottom).
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 10), 10, 10);

        // Moving UP by 2 -> target top = 8 < 10.
        Assert.True(CollisionService.IsColliding(actor, obstacle, offsetY: -2));
    }

    [Fact]
    public void Down_AllowsPassingFromAboveAndSides()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(10, 10), 10, 10, Direction4.Down);

        // From above moving down
        var actorAbove = CollisionTestEntities.Aabb(world, new Position(10, 0), 10, 10);
        Assert.False(CollisionService.IsColliding(actorAbove, obstacle, offsetY: 5));

        // From left moving right
        var actorLeft = CollisionTestEntities.Aabb(world, new Position(0, 10), 10, 10);
        Assert.False(CollisionService.IsColliding(actorLeft, obstacle, offsetX: 5));
    }

    [Fact]
    public void Down_HalfwayThroughFromAbove_CanGoBackUp()
    {
        using var world = World.Create();
        // Obstacle at (0, 0), 10x10. Bottom edge is Y=10.
        var obstacle = CollisionTestEntities.OneWay(world, new Position(0, 0), 10, 10, Direction4.Down);
        // Actor at (0, 5), 10x10. Top is 5 (inside obstacle), bottom is 15 (below obstacle). Halfway through!
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 5), 10, 10);

        // Moving back UP by 2:
        Assert.False(CollisionService.IsColliding(actor, obstacle, offsetY: -2));
    }

    [Fact]
    public void Down_OnlyWhenFullyThrough_CantGoBackUp()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(0, 0), 10, 10, Direction4.Down);
        // Actor at (0, 10), 10x10. Top is 10 (fully through: top >= obstacle bottom).
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 10), 10, 10);

        // Moving back UP by 2:
        Assert.True(CollisionService.IsColliding(actor, obstacle, offsetY: -2));
    }

    [Fact]
    public void Left_CollidesWhenMovingRightFromLeft()
    {
        using var world = World.Create();
        // Obstacle at (10, 0), 10x10. Left edge is X=10.
        var obstacle = CollisionTestEntities.OneWay(world, new Position(10, 0), 10, 10, Direction4.Left);
        // Actor at (0, 0), 10x10. Right is X=10 (initial right <= obstacle left).
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        // Moving right by 2 -> target right = 12 > 10.
        Assert.True(CollisionService.IsColliding(actor, obstacle, offsetX: 2));
    }

    [Fact]
    public void Left_AllowsPassingFromRightAndVertically()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(10, 10), 10, 10, Direction4.Left);

        // From right moving left
        var actorRight = CollisionTestEntities.Aabb(world, new Position(20, 10), 10, 10);
        Assert.False(CollisionService.IsColliding(actorRight, obstacle, offsetX: -5));

        // From above moving down
        var actorAbove = CollisionTestEntities.Aabb(world, new Position(10, 0), 10, 10);
        Assert.False(CollisionService.IsColliding(actorAbove, obstacle, offsetY: 5));
    }

    [Fact]
    public void Left_HalfwayThroughFromRight_CanGoBackRight()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(10, 0), 10, 10, Direction4.Left);
        // Actor at (5, 0), 10x10. Left is 5, right is 15. Halfway through!
        var actor = CollisionTestEntities.Aabb(world, new Position(5, 0), 10, 10);

        // Moving back right by 2:
        Assert.False(CollisionService.IsColliding(actor, obstacle, offsetX: 2));
    }

    [Fact]
    public void Left_OnlyWhenFullyThrough_CantGoBackRight()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(10, 0), 10, 10, Direction4.Left);
        // Actor at (0, 0), 10x10. Right is 10 (fully through: right <= obstacle left).
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        // Moving back right by 2:
        Assert.True(CollisionService.IsColliding(actor, obstacle, offsetX: 2));
    }

    [Fact]
    public void Right_CollidesWhenMovingLeftFromRight()
    {
        using var world = World.Create();
        // Obstacle at (0, 0), 10x10. Right edge is X=10.
        var obstacle = CollisionTestEntities.OneWay(world, new Position(0, 0), 10, 10, Direction4.Right);
        // Actor at (10, 0), 10x10. Left is X=10 (initial left >= obstacle right).
        var actor = CollisionTestEntities.Aabb(world, new Position(10, 0), 10, 10);

        // Moving left by 2 -> target left = 8 < 10.
        Assert.True(CollisionService.IsColliding(actor, obstacle, offsetX: -2));
    }

    [Fact]
    public void Right_AllowsPassingFromLeftAndVertically()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(10, 10), 10, 10, Direction4.Right);

        // From left moving right
        var actorLeft = CollisionTestEntities.Aabb(world, new Position(0, 10), 10, 10);
        Assert.False(CollisionService.IsColliding(actorLeft, obstacle, offsetX: 5));

        // From above moving down
        var actorAbove = CollisionTestEntities.Aabb(world, new Position(10, 0), 10, 10);
        Assert.False(CollisionService.IsColliding(actorAbove, obstacle, offsetY: 5));
    }

    [Fact]
    public void Right_HalfwayThroughFromLeft_CanGoBackLeft()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(0, 0), 10, 10, Direction4.Right);
        // Actor at (5, 0), 10x10. Left is 5, right is 15. Halfway through!
        var actor = CollisionTestEntities.Aabb(world, new Position(5, 0), 10, 10);

        // Moving back left by 2:
        Assert.False(CollisionService.IsColliding(actor, obstacle, offsetX: -2));
    }

    [Fact]
    public void Right_OnlyWhenFullyThrough_CantGoBackLeft()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(0, 0), 10, 10, Direction4.Right);
        // Actor at (10, 0), 10x10. Left is 10 (fully through: left >= obstacle right).
        var actor = CollisionTestEntities.Aabb(world, new Position(10, 0), 10, 10);

        // Moving back left by 2:
        Assert.True(CollisionService.IsColliding(actor, obstacle, offsetX: -2));
    }

    [Fact]
    public void None_NeverCollides()
    {
        using var world = World.Create();
        var obstacle = CollisionTestEntities.OneWay(world, new Position(0, 0), 10, 10, Direction4.None);
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        Assert.False(CollisionService.IsColliding(actor, obstacle, offsetY: 1));
        Assert.False(CollisionService.IsColliding(actor, obstacle, offsetY: -1));
        Assert.False(CollisionService.IsColliding(actor, obstacle, offsetX: 1));
        Assert.False(CollisionService.IsColliding(actor, obstacle, offsetX: -1));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsCollidingWith_DetectsOneWayPlatformWithOrWithoutSolidBody(bool withSolidBody)
    {
        using var world = World.Create();
        CollisionTestEntities.OneWay(world, new Position(0, 10), 10, 10, Direction4.Up, withSolidBody);
        var actor = CollisionTestEntities.Aabb(world, new Position(0, 0), 10, 10);

        Assert.True(CollisionService.IsCollidingWith<SolidBody>(actor, offsetY: 2));
    }
}

public class ActorCollisionSystemOneWayTests
{
    [Fact]
    public void ActorLandsOnOneWayPlatformFromAbove()
    {
        using var world = World.Create();
        var actor = CollisionTestEntities.MovingActor(
            world,
            new Position(0, 0),
            new FractionalPositionRemainder(),
            new MovementDelta { Y = 5 }); // Actor is 2x2 at (0, 0), bottom = 2.
        // One-way platform at (0, 4), 10x10. Top is 4.
        CollisionTestEntities.OneWay(world, new Position(0, 4), 10, 10, Direction4.Up, withSolidBody: true);

        new ActorCollisionSystem(world).Update(1);

        // Movement should be clamped to 2 (position 0 + 2 = 2, so actor bottom = 4 = platform top).
        Assert.Equal(2f, actor.Get<MovementDelta>().Y, 5);

        new MovementSystem(world).Update(1);

        Assert.Equal(new Position(0, 2), actor.Get<Position>());
    }

    [Fact]
    public void ActorCanJumpThroughOneWayPlatformFromBelow()
    {
        using var world = World.Create();
        var actor = CollisionTestEntities.MovingActor(
            world,
            new Position(0, 15),
            new FractionalPositionRemainder(),
            new MovementDelta { Y = -10 }); // Moving UP through platform at Y=4..14
        CollisionTestEntities.OneWay(world, new Position(0, 4), 10, 10, Direction4.Up, withSolidBody: true);

        new ActorCollisionSystem(world).Update(1);

        // Movement should not be clamped!
        Assert.Equal(-10f, actor.Get<MovementDelta>().Y, 5);

        new MovementSystem(world).Update(1);

        Assert.Equal(new Position(0, 5), actor.Get<Position>());
    }

    [Fact]
    public void GroundedCheckSystem_RecognizesOneWayPlatformWhenStandingOnTop()
    {
        using var world = World.Create();
        // Actor 2x2 at (0, 2), bottom is 4.
        var actor = world.Create(
            new Position(0, 2),
            new OffsetAabbCollider { Width = 2, Height = 2 },
            new GroundedState());
        // Platform top is 4.
        CollisionTestEntities.OneWay(world, new Position(0, 4), 10, 10, Direction4.Up, withSolidBody: false);

        new GroundedCheckSystem(world).Update(1);

        Assert.True(actor.Get<GroundedState>().IsGrounded);
    }

    [Fact]
    public void GroundedCheckSystem_DoesNotGroundActorWhileHalfwayThrough()
    {
        using var world = World.Create();
        // Actor 2x2 at (0, 3), bottom is 5 (inside platform Y=4..14).
        var actor = world.Create(
            new Position(0, 3),
            new OffsetAabbCollider { Width = 2, Height = 2 },
            new GroundedState());
        CollisionTestEntities.OneWay(world, new Position(0, 4), 10, 10, Direction4.Up, withSolidBody: false);

        new GroundedCheckSystem(world).Update(1);

        Assert.False(actor.Get<GroundedState>().IsGrounded);
    }
}
