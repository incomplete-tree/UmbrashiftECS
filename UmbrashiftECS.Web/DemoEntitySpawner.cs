using System;
using Arch.Core;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.Components.EntityComponents.Rendering;
using UmbrashiftECS.Components.EntityComponents.Respawn;
using UmbrashiftECS.Components.EntityComponents.Walk;
using UmbrashiftECS.GameLogic.EntityComponents;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.Web;

public class DemoEntitySpawner(World world)
{
    public static readonly Guid PlayerSpawnPointId = Guid.Parse("5a7d0c73-9f2b-4f45-8bb9-2a7bbd08d2cb");
    public static readonly Position PlayerSpawnPosition = new(96, 120);

    public void SpawnAll()
    {
        SpawnBackground();
        SpawnMapBounds(640, 640);
        SpawnPlayerSpawnPoint();
        SpawnPlayer();

        // Shared floor (no Layer component: solid on both layers)
        SpawnPlatform(0, 316, 640, 44);

        // Layer 0 platforms (Blue tint)
        SpawnPlatform(72, 258, 126, 18, Layer.GameplayLayer0);
        SpawnPlatform(360, 216, 120, 18, Layer.GameplayLayer0);
        SpawnPlatform(120, 150, 110, 18, Layer.GameplayLayer0);

        // Layer 1 platforms (Purple tint)
        SpawnPlatform(220, 264, 126, 18, Layer.GameplayLayer1);
        SpawnPlatform(260, 176, 116, 18, Layer.GameplayLayer1);
        SpawnPlatform(474, 250, 110, 18, Layer.GameplayLayer1);
        SpawnPlatform(440, 120, 120, 18, Layer.GameplayLayer1);

        // Crates on specific layers and a shared crate
        SpawnCrate(new Position(130, 40), Layer.GameplayLayer0);
        SpawnCrate(new Position(280, 40), Layer.GameplayLayer1);
        SpawnCrate(new Position(480, 20));

        // One-way platform (Jump-through from below, solid from above)
        SpawnOneWayPlatform(340, 265, 110, 14, Direction4.Up);

        // Moving platform (oscillates horizontally across the central gap)
        SpawnMovingPlatform(190, 204, 72, 14, 1.0f, [new Position(190, 204), new Position(310, 204)]);
    }

    public Entity SpawnPlayer()
    {
        return world.Create(
            PlayerSpawnPosition,
            new ActorBody(),
            new DiesOutOfBounds(),
            new ActiveRespawnPoint { ActiveRespawnPointId = PlayerSpawnPointId },
            new AabbCollider { Width = 22, Height = 30 },
            new InputBinding { InputBindingSlot = 0 },
            new InputState(),
            new InputBuffer { BufferFrames = 5 },
            new Velocity(),
            new MovementDelta(),
            new FractionalPositionRemainder(),
            new FacingDirection { Value = 1 },
            new GroundedState(),
            new JumpConfig
            {
                InitialJumpSpeed = -8.5f,
                JumpDistance = 48,
                JumpDuration = 16,
                JumpHeight = 64,
                CoyoteTimeFrames = 5
            },
            new GravityConfig
            {
                GravityInPixelsPerFrameSquared = 0.45f,
                ChangeGravityWhenJumpHeld = true,
                JumpHeldModifier = 0.5f
            },
            new DashConfig(Distance: 48, Duration: 6, Amount: 1, FramesTillRefill: 10),
            new DashState { AmountRemaining = 1 },
            new WalkConfig { SpeedInAir = 0.1f, SpeedOnGround = 0.3f },
            new FrictionConfig { Friction = 0.9f },
            Layer.GameplayLayer0,
            new ActiveLayerController(),
            new ColorRenderer(System.Drawing.Color.FromArgb(255, 208, 92)));
    }

    public Entity SpawnPlayerSpawnPoint()
    {
        return world.Create(
            PlayerSpawnPosition,
            new PlayerSpawnPoint { Id = PlayerSpawnPointId });
    }

    public Entity SpawnPlatform(int x, int y, int width, int height, Layer? layer = null, System.Drawing.Color? color = null)
    {
        var platformColor = color ?? (layer switch
        {
            Layer.GameplayLayer0 => System.Drawing.Color.FromArgb(91, 120, 190),  // Steel Blue
            Layer.GameplayLayer1 => System.Drawing.Color.FromArgb(170, 95, 195), // Purple / Violet
            _ => System.Drawing.Color.FromArgb(91, 111, 151)                    // Neutral Floor
        });

        if (layer.HasValue)
        {
            return world.Create(
                new Position(x, y),
                new AabbCollider { Width = width, Height = height },
                new SolidBody(),
                layer.Value,
                new ColorRenderer(platformColor));
        }

        return world.Create(
            new Position(x, y),
            new AabbCollider { Width = width, Height = height },
            new SolidBody(),
            new ColorRenderer(platformColor));
    }

    public Entity SpawnOneWayPlatform(
        int x,
        int y,
        int width,
        int height,
        Direction4 direction = Direction4.Up,
        Layer? layer = null,
        System.Drawing.Color? color = null)
    {
        var platformColor = color ?? (layer switch
        {
            Layer.GameplayLayer0 => System.Drawing.Color.FromArgb(70, 190, 160),
            Layer.GameplayLayer1 => System.Drawing.Color.FromArgb(190, 100, 210),
            _ => System.Drawing.Color.FromArgb(80, 200, 120) // Mint/emerald green for one-way
        });

        if (layer.HasValue)
        {
            return world.Create(
                new Position(x, y),
                new AabbCollider { Width = width, Height = height },
                new SolidBody(),
                new OneWayCollision(direction),
                layer.Value,
                new ColorRenderer(platformColor));
        }

        return world.Create(
            new Position(x, y),
            new AabbCollider { Width = width, Height = height },
            new SolidBody(),
            new OneWayCollision(direction),
            new ColorRenderer(platformColor));
    }

    public Entity SpawnMovingPlatform(
        int x,
        int y,
        int width,
        int height,
        float speed,
        Position[] waypoints,
        Layer? layer = null,
        System.Drawing.Color? color = null)
    {
        var startPosition = new Position(x, y);
        var points = waypoints;
        if (points.Length == 1)
        {
            points = [startPosition, points[0]];
        }
        else if (points.Length == 0)
        {
            throw new ArgumentException("Moving platform must have at least one target waypoint.", nameof(waypoints));
        }

        var platformColor = color ?? (layer switch
        {
            Layer.GameplayLayer0 => System.Drawing.Color.FromArgb(91, 140, 210),
            Layer.GameplayLayer1 => System.Drawing.Color.FromArgb(185, 105, 210),
            _ => System.Drawing.Color.FromArgb(220, 160, 60)
        });

        if (layer.HasValue)
        {
            return world.Create(
                startPosition,
                new AabbCollider { Width = width, Height = height },
                new SolidBody(),
                new MovementDelta(),
                new FractionalPositionRemainder(),
                new MoveBetweenPoints(speed, points),
                layer.Value,
                new ColorRenderer(platformColor));
        }

        return world.Create(
            startPosition,
            new AabbCollider { Width = width, Height = height },
            new SolidBody(),
            new MovementDelta(),
            new FractionalPositionRemainder(),
            new MoveBetweenPoints(speed, points),
            new ColorRenderer(platformColor));
    }

    public Entity SpawnBackground(string texturePath = "Content/background.png")
    {
        return world.Create(
            new Position(0, 0),
            new BasicSpriteRenderer(texturePath));
    }

    public Entity SpawnMapBounds(int width, int height)
    {
        return world.Create(
            new Position(0, -280),
            new AabbCollider { Width = width, Height = height },
            new MapBounds());
    }

    public Entity SpawnCrate(Position position, Layer? layer = null)
    {
        var crateColor = layer switch
        {
            Layer.GameplayLayer0 => System.Drawing.Color.FromArgb(235, 110, 85),
            Layer.GameplayLayer1 => System.Drawing.Color.FromArgb(225, 80, 140),
            _ => System.Drawing.Color.FromArgb(231, 120, 89)
        };

        if (layer.HasValue)
        {
            return world.Create(
                position,
                new ActorBody(),
                new AabbCollider { Width = 18, Height = 18 },
                new InputState(),
                new Velocity(),
                new MovementDelta(),
                new FractionalPositionRemainder(),
                new GravityConfig { GravityInPixelsPerFrameSquared = 0.35f },
                layer.Value,
                new ColorRenderer(crateColor));
        }

        return world.Create(
            position,
            new ActorBody(),
            new AabbCollider { Width = 18, Height = 18 },
            new InputState(),
            new Velocity(),
            new MovementDelta(),
            new FractionalPositionRemainder(),
            new GravityConfig { GravityInPixelsPerFrameSquared = 0.35f },
            new ColorRenderer(crateColor));
    }
}
