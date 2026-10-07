using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
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
}
