using System;
using Arch.Core;
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
using UmbrashiftECS.Rendering.EntityComponents;

namespace UmbrashiftECS.MonoGame;

public class DemoEntitySpawner(World world)
{
    public static readonly Guid PlayerSpawnPointId = Guid.Parse("5a7d0c73-9f2b-4f45-8bb9-2a7bbd08d2cb");
    public static readonly Position PlayerSpawnPosition = new(96, 120);

    public void SpawnAll(int mapWidth = 640, int mapHeight = 360)
    {
        SpawnMapBounds(mapWidth, mapHeight);
        SpawnPlayerSpawnPoint();
        SpawnPlayer();

        SpawnPlatform(0, 316, 640, 44);
        SpawnPlatform(72, 258, 126, 18);
        SpawnPlatform(286, 226, 116, 18);
        SpawnPlatform(474, 274, 108, 18);

        SpawnCrate(new Position(248, 40));
        SpawnCrate(new Position(438, 20));
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
            new WalkConfig { SpeedInAir = 0.2f, SpeedOnGround = 1 },
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

    public Entity SpawnPlatform(int x, int y, int width, int height)
    {
        return world.Create(
            new Position(x, y),
            new AabbCollider { Width = width, Height = height },
            new SolidBody(),
            new ColorRenderer(System.Drawing.Color.FromArgb(91, 111, 151)));
    }

    public Entity SpawnMapBounds(int width, int height)
    {
        return world.Create(
            new Position(0, 0),
            new AabbCollider { Width = width, Height = height },
            new MapBounds());
    }

    public Entity SpawnCrate(Position position)
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
            new ColorRenderer(System.Drawing.Color.FromArgb(231, 120, 89)));
    }
}
