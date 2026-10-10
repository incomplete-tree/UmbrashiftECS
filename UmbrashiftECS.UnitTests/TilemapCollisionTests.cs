using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.Components.EntityComponents.Tilemap;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Services;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class TilemapCollisionTests
{
    [Fact]
    public void IsColliding_ActorOverlappingSolidTile_ReturnsTrue()
    {
        using var world = World.Create();
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 });

        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1 // Solid tile at (0, 0)
            }));

        Assert.True(CollisionService.IsColliding(actor, tilemap));
        Assert.True(CollisionService.IsColliding(tilemap, actor));
    }

    [Fact]
    public void IsColliding_ActorOverlappingEmptyOrMissingTile_ReturnsFalse()
    {
        using var world = World.Create();
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 });

        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(1, 0)] = 1, // Solid tile at (16, 0), not overlapping actor at (0, 0)
                [(0, 0)] = 0  // 0 is empty
            }));

        Assert.False(CollisionService.IsColliding(actor, tilemap));
        Assert.False(CollisionService.IsColliding(tilemap, actor));
    }

    [Fact]
    public void IsColliding_NegativeTileId_DoesNotCollide()
    {
        using var world = World.Create();
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 });

        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = -1 // <0 is rendered same as >0 but doesn't collide
            }));

        Assert.False(CollisionService.IsColliding(actor, tilemap));
    }

    [Fact]
    public void IsColliding_ActorFlushAgainstTileEdge_DoesNotCollide()
    {
        using var world = World.Create();
        // Actor at X: 0..16, tile at X: 16..32
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 });

        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(1, 0)] = 1 // tile at X=16..32
            }));

        Assert.False(CollisionService.IsColliding(actor, tilemap));
    }

    [Fact]
    public void IsColliding_ActorPenetratingTileByOnePixel_ReturnsTrue()
    {
        using var world = World.Create();
        // Actor at X: 1..17, tile at X: 16..32 -> 1 pixel overlap
        var actor = world.Create(
            new Position(1, 0),
            new AabbCollider { Width = 16, Height = 16 });

        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(1, 0)] = 1
            }));

        Assert.True(CollisionService.IsColliding(actor, tilemap));
    }

    [Fact]
    public void IsColliding_TilemapAtWorldOffset_CorrectlyOffsetsTiles()
    {
        using var world = World.Create();
        // Tilemap at (100, 200). Tile (0, 0) is at world pos (100, 200).
        var tilemap = world.Create(
            new Position(100, 200),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1
            }));

        var actorMissing = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 });
        Assert.False(CollisionService.IsColliding(actorMissing, tilemap));

        var actorHitting = world.Create(
            new Position(105, 205),
            new AabbCollider { Width = 16, Height = 16 });
        Assert.True(CollisionService.IsColliding(actorHitting, tilemap));
    }

    [Fact]
    public void IsColliding_NegativeTileCoordinates_HandledCorrectly()
    {
        using var world = World.Create();
        // Tilemap at (0, 0). Tile (-1, -1) is at (-16, -16) to (0, 0).
        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(-1, -1)] = 1
            }));

        var actorInside = world.Create(
            new Position(-10, -10),
            new AabbCollider { Width = 8, Height = 8 });
        Assert.True(CollisionService.IsColliding(actorInside, tilemap));

        var actorOutside = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 8, Height = 8 });
        Assert.False(CollisionService.IsColliding(actorOutside, tilemap));
    }

    [Fact]
    public void IsColliding_CustomTileDimensions_HandledCorrectly()
    {
        using var world = World.Create();
        // Tilemap with 32x32 tiles
        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(32, 32),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(1, 1)] = 1 // tile at (32..64, 32..64)
            }));

        var actorAt20 = world.Create(
            new Position(20, 20),
            new AabbCollider { Width = 10, Height = 10 });
        Assert.False(CollisionService.IsColliding(actorAt20, tilemap));

        var actorAt35 = world.Create(
            new Position(35, 35),
            new AabbCollider { Width = 10, Height = 10 });
        Assert.True(CollisionService.IsColliding(actorAt35, tilemap));
    }

    [Fact]
    public void IsColliding_WithLayers_RespectsGameplayLayers()
    {
        using var world = World.Create();
        var tilemapL0 = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            Layer.GameplayLayer0,
            new TilemapData(new Dictionary<(int X, int Y), int> { [(0, 0)] = 1 }));

        var actorL1 = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            Layer.GameplayLayer1);

        var actorL0 = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            Layer.GameplayLayer0);

        Assert.False(CollisionService.IsColliding(actorL1, tilemapL0));
        Assert.True(CollisionService.IsColliding(actorL0, tilemapL0));
    }

    [Fact]
    public void IsColliding_SharedTilemapWithoutLayer_CollidesOnBothLayers()
    {
        using var world = World.Create();
        // Shared tilemap without Layer component
        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int> { [(0, 0)] = 1 }));

        var actorL0 = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            Layer.GameplayLayer0);

        var actorL1 = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            Layer.GameplayLayer1);

        Assert.True(CollisionService.IsColliding(actorL0, tilemap));
        Assert.True(CollisionService.IsColliding(actorL1, tilemap));
    }

    [Fact]
    public void IsColliding_TwoTilemaps_DetectsOverlap()
    {
        using var world = World.Create();
        var map1 = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(1, 0)] = 1
            }));

        var map2 = world.Create(
            new Position(16, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1 // (16 + 0*16, 0) = (16, 0), overlaps map1's (1, 0) tile!
            }));

        Assert.True(CollisionService.IsColliding(map1, map2));

        var map3 = world.Create(
            new Position(48, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1
            }));

        Assert.False(CollisionService.IsColliding(map1, map3));
    }

    [Fact]
    public void IsCollidingWith_SolidBody_QueriesTilemapCorrectly()
    {
        using var world = World.Create();
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            new ActorBody());

        var solidTilemap = world.Create(
            new Position(0, 0),
            new SolidBody(),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(2, 0)] = 1 // tile at X=32..48
            }));

        Assert.False(CollisionService.IsCollidingWith<SolidBody>(actor));
        Assert.True(CollisionService.IsCollidingWith<SolidBody>(actor, offsetX: 20));
    }

    [Fact]
    public void IsCollidingWith_OffsetAabbCollider_ChecksTilemap()
    {
        using var world = World.Create();
        var actor = world.Create(
            new Position(0, 0),
            new OffsetAabbCollider { Width = 8, Height = 16 });

        world.Create(
            new Position(0, 0),
            new SolidBody(),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1
            }));

        var standingCollider = new OffsetAabbCollider { Width = 16, Height = 32 };
        Assert.True(CollisionService.IsCollidingWith<SolidBody>(actor, standingCollider));

        var distantCollider = new OffsetAabbCollider { OffsetX = 50, Width = 16, Height = 32 };
        Assert.False(CollisionService.IsCollidingWith<SolidBody>(actor, distantCollider));
    }

    [Fact]
    public void ActorCollisionSystem_ActorWalkingIntoTilemapWall_IsClamped()
    {
        using var world = World.Create();
        // Actor at X=0, width=16. Floor at Y=16. Wall at X=32 (tile 2, 0).
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            new ActorBody(),
            new Velocity(),
            new FractionalPositionRemainder(),
            new MovementDelta { X = 20 }); // Attempts to move right by 20 -> right edge would reach 36

        world.Create(
            new Position(0, 0),
            new SolidBody(),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(2, 0)] = 1 // Wall at X=32..48
            }));

        new ActorCollisionSystem(world).Update(1);

        // Actor right edge was 16, wall starts at 32 -> maximum allowed move is 16
        Assert.Equal(16f, actor.Get<MovementDelta>().X, 5);

        new MovementSystem(world).Update(1);
        Assert.Equal(new Position(16, 0), actor.Get<Position>());
    }

    [Fact]
    public void GroundedCheckSystem_RecognizesTilemapFloor()
    {
        using var world = World.Create();
        // Actor at (0, 0), width=16, height=16. Floor at (0, 16).
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            new GroundedState());

        world.Create(
            new Position(0, 0),
            new SolidBody(),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 1)] = 1 // Floor tile at Y=16..32
            }));

        new GroundedCheckSystem(world).Update(1);

        Assert.True(actor.Get<GroundedState>().IsGrounded);
    }

    [Fact]
    public void TryGetBounds_ReturnsOuterBoundsOfSolidTiles()
    {
        using var world = World.Create();
        var tilemap = world.Create(
            new Position(10, 20),
            new TilemapCollider(16, 16),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1,
                [(2, 3)] = 1,
                [(1, 1)] = -1 // Non-solid tile, shouldn't expand bounds
            }));

        var hasBounds = CollisionService.TryGetBounds(tilemap, out int left, out int top, out int right, out int bottom);
        Assert.True(hasBounds);
        Assert.Equal(10, left);                         // 10 + 0 * 16
        Assert.Equal(20, top);                          // 20 + 0 * 16
        Assert.Equal(10 + (2 + 1) * 16, right);         // 10 + 3 * 16 = 58
        Assert.Equal(20 + (3 + 1) * 16, bottom);        // 20 + 4 * 16 = 84
    }

    [Fact]
    public void TryGetBounds_EmptyTilemap_ReturnsFalse()
    {
        using var world = World.Create();
        var tilemap = world.Create(
            new Position(0, 0),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 0,
                [(1, 1)] = -1
            }));

        var hasBounds = CollisionService.TryGetBounds(tilemap, out _, out _, out _, out _);
        Assert.False(hasBounds);
    }

    [Fact]
    public void GetCollidingEntities_FindsCollidingTilemap()
    {
        using var world = World.Create();
        var actor = world.Create(
            new Position(0, 0),
            new AabbCollider { Width = 16, Height = 16 },
            new ActorBody());

        var tilemap = world.Create(
            new Position(0, 0),
            new SolidBody(),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1
            }));

        var colliding = CollisionService.GetCollidingEntities<SolidBody>(actor);
        Assert.Single(colliding);
        Assert.Equal(tilemap, colliding[0]);
    }

    [Fact]
    public void OneWayTilemap_AllowsPassingThroughInAllowedDirection()
    {
        using var world = World.Create();
        // OneWay platform pointing Up: actor passing from below (moving up) should not collide
        var actor = world.Create(
            new Position(0, 15),
            new OffsetAabbCollider { Width = 2, Height = 2 },
            new ActorBody(),
            new Velocity(),
            new FractionalPositionRemainder(),
            new MovementDelta { Y = -10 }); // moving up through platform at Y=0..16

        world.Create(
            new Position(0, 0),
            new SolidBody(),
            new OneWayCollision(Direction4.Up),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1
            }));

        new ActorCollisionSystem(world).Update(1);

        // Should not be clamped!
        Assert.Equal(-10f, actor.Get<MovementDelta>().Y, 5);
    }

    [Fact]
    public void OneWayTilemap_BlocksPassingFromOppositeDirection()
    {
        using var world = World.Create();
        // OneWay platform pointing Up: actor falling from above (moving down) should collide and clamp
        var actor = world.Create(
            new Position(0, -10),
            new AabbCollider { Width = 10, Height = 10 },
            new ActorBody(),
            new Velocity(),
            new FractionalPositionRemainder(),
            new MovementDelta { Y = 10 }); // target Y = 0 (touching/intersecting platform at Y = 0..16)

        world.Create(
            new Position(0, 0),
            new SolidBody(),
            new OneWayCollision(Direction4.Up),
            new TilemapCollider(),
            new TilemapData(new Dictionary<(int X, int Y), int>
            {
                [(0, 0)] = 1
            }));

        new ActorCollisionSystem(world).Update(1);

        // Movement should be clamped so actor lands exactly on top of the platform (at Y = -10, delta clamped to 0)
        Assert.Equal(0f, actor.Get<MovementDelta>().Y);
    }

    [Fact]
    public void MultiTilePlatform_StopsActorFalling()
    {
        using var world = World.Create();
        var actor = world.Create(
            new Position(50, 0),
            new AabbCollider { Width = 22, Height = 30 },
            new ActorBody(),
            new Velocity(),
            new FractionalPositionRemainder(),
            new MovementDelta { Y = 40 }); // target Y = 40 (inside platform at Y = 32..48)

        // Platform of 8x1 tiles at (0, 32)
        var tileData = new Dictionary<(int X, int Y), int>();
        for (var tx = 0; tx < 8; tx++)
        {
            tileData[(tx, 0)] = 1;
        }

        world.Create(
            new Position(0, 32),
            new SolidBody(),
            new TilemapCollider(16, 16),
            new TilemapData(tileData));

        new ActorCollisionSystem(world).Update(1);

        // Platform top is at 32. Actor height is 30. Actor should clamp at Y = 2 (delta = 2)
        Assert.Equal(2f, actor.Get<MovementDelta>().Y);
    }
}
