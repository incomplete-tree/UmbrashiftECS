using System;
using System.Numerics;
using Arch.Core;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.Rendering;
using UmbrashiftECS.Rendering.Services;
using Xunit;

namespace UmbrashiftECS.UnitTests;

public class CameraTests
{
    // ==========================================
    // 1. Target Position Calculation
    // ==========================================

    [Fact]
    public void HorizontalLookAhead_MovingRight_OffsetsTargetByPlus32()
    {
        var playerX = 100f;
        var playerY = 180f;
        var direction = 1;

        var (targetX, _) = CameraService.CalculateTargetPosition(
            playerX, playerY, direction,
            currentCameraCenterY: 180f,
            currentCameraHeight: 360f,
            currentTargetY: 180f);

        Assert.Equal(132f, targetX);
    }

    [Fact]
    public void HorizontalLookAhead_MovingLeft_OffsetsTargetByMinus32()
    {
        var playerX = 100f;
        var playerY = 180f;
        var direction = -1;

        var (targetX, _) = CameraService.CalculateTargetPosition(
            playerX, playerY, direction,
            currentCameraCenterY: 180f,
            currentCameraHeight: 360f,
            currentTargetY: 180f);

        Assert.Equal(68f, targetX);
    }

    [Fact]
    public void VerticalDeadzone_PlayerWithinRange_TargetYRemainsUnchanged()
    {
        // Viewport height = 360. Deadzone: 25% (90) to 70% (252).
        // Camera at Y = 180, height = 360.
        // Screen Y for world Y is world Y (since camera is centered at 180 and height is 360).
        var currentTargetY = 180f;

        // Player at world Y = 150 -> screen Y = 150 (between 90 and 252)
        var (_, targetY1) = CameraService.CalculateTargetPosition(
            controllerWorldX: 100f,
            playerWorldY: 150f,
            lastMovingDirection: 1,
            currentCameraCenterY: 180f,
            currentCameraHeight: 360f,
            currentTargetY: currentTargetY,
            viewportHeight: 360f);

        Assert.Equal(180f, targetY1);

        // Player at exact 25% boundary: 90 -> within range, unchanged
        var (_, targetY2) = CameraService.CalculateTargetPosition(
            controllerWorldX: 100f,
            playerWorldY: 90f,
            lastMovingDirection: 1,
            currentCameraCenterY: 180f,
            currentCameraHeight: 360f,
            currentTargetY: currentTargetY,
            viewportHeight: 360f);

        Assert.Equal(180f, targetY2);

        // Player at exact 70% boundary: 252 -> within range, unchanged
        var (_, targetY3) = CameraService.CalculateTargetPosition(
            controllerWorldX: 100f,
            playerWorldY: 252f,
            lastMovingDirection: 1,
            currentCameraCenterY: 180f,
            currentCameraHeight: 360f,
            currentTargetY: currentTargetY,
            viewportHeight: 360f);

        Assert.Equal(180f, targetY3);
    }

    [Fact]
    public void VerticalDeadzone_PlayerBreaksTopBoundary_TargetYInstantlyMatchesPlayerY()
    {
        // 25% of 360 is 90. Player at Y = 80 breaks the top boundary (80 < 90).
        var currentTargetY = 180f;

        var (_, targetY) = CameraService.CalculateTargetPosition(
            controllerWorldX: 100f,
            playerWorldY: 80f,
            lastMovingDirection: 1,
            currentCameraCenterY: 180f,
            currentCameraHeight: 360f,
            currentTargetY: currentTargetY,
            viewportHeight: 360f);

        Assert.Equal(80f, targetY);
    }

    [Fact]
    public void VerticalDeadzone_PlayerBreaksBottomBoundary_TargetYInstantlyMatchesPlayerY()
    {
        // 70% of 360 is 252. Player at Y = 260 breaks the bottom boundary (260 > 252).
        var currentTargetY = 180f;

        var (_, targetY) = CameraService.CalculateTargetPosition(
            controllerWorldX: 100f,
            playerWorldY: 260f,
            lastMovingDirection: 1,
            currentCameraCenterY: 180f,
            currentCameraHeight: 360f,
            currentTargetY: currentTargetY,
            viewportHeight: 360f);

        Assert.Equal(260f, targetY);
    }

    [Fact]
    public void WorldToScreenSpace_ConvertsCoordinatesCorrectly()
    {
        // Camera centered at (320, 180), size 640x360
        var camera = new CameraController(320, 180, 640, 360);

        var screenTopLeft = CameraService.WorldToScreen(new Vector2(0, 0), camera);
        Assert.Equal(0f, screenTopLeft.X);
        Assert.Equal(0f, screenTopLeft.Y);

        var screenCenter = CameraService.WorldToScreen(new Vector2(320, 180), camera);
        Assert.Equal(320f, screenCenter.X);
        Assert.Equal(180f, screenCenter.Y);

        var screenBottomRight = CameraService.WorldToScreen(new Vector2(640, 360), camera);
        Assert.Equal(640f, screenBottomRight.X);
        Assert.Equal(360f, screenBottomRight.Y);
    }

    // ==========================================
    // 2. Room Boundary Clamping
    // ==========================================

    [Fact]
    public void RoomBoundaryClamping_Preserves16By9AspectRatio_WhenRoomIsSmaller()
    {
        // Room width = 320, room height = 360
        // ratioW = 320 / 640 = 0.5
        // ratioH = 360 / 360 = 1.0
        // Min ratio = 0.5
        // Clamped W = 640 * 0.5 = 320, Clamped H = 360 * 0.5 = 180
        var (_, _, clampedW, clampedH) = CameraService.ClampToRoom(
            targetX: 160f,
            targetY: 180f,
            roomMinX: 0f,
            roomMinY: 0f,
            roomMaxX: 320f,
            roomMaxY: 360f);

        Assert.Equal(320f, clampedW);
        Assert.Equal(180f, clampedH);

        var ratio = clampedW / clampedH;
        Assert.True(MathF.Abs(ratio - 1.77777f) <= 0.01f);
    }

    [Fact]
    public void RoomBoundaryClamping_ClampsCenterCoordinatesWithinRoomEdges()
    {
        // Room: (0, 0) to (1000, 360)
        // ratioW = 1000/640 = 1.5625, ratioH = 360/360 = 1.0. Smaller ratio = 1.0.
        // View size: 640x360. Half width = 320, Half height = 180.
        // X range: max(0 + 320, min(X, 1000 - 320)) => [320, 680].
        // Y range: max(0 + 180, min(Y, 360 - 180)) => [180, 180].

        // Too far left (targetX = 100)
        var (clampedX1, clampedY1, _, _) = CameraService.ClampToRoom(
            targetX: 100f, targetY: 180f,
            roomMinX: 0f, roomMinY: 0f, roomMaxX: 1000f, roomMaxY: 360f);
        Assert.Equal(320f, clampedX1);
        Assert.Equal(180f, clampedY1);

        // Too far right (targetX = 900)
        var (clampedX2, clampedY2, _, _) = CameraService.ClampToRoom(
            targetX: 900f, targetY: 180f,
            roomMinX: 0f, roomMinY: 0f, roomMaxX: 1000f, roomMaxY: 360f);
        Assert.Equal(680f, clampedX2);
        Assert.Equal(180f, clampedY2);

        // Valid middle position (targetX = 500)
        var (clampedX3, clampedY3, _, _) = CameraService.ClampToRoom(
            targetX: 500f, targetY: 180f,
            roomMinX: 0f, roomMinY: 0f, roomMaxX: 1000f, roomMaxY: 360f);
        Assert.Equal(500f, clampedX3);
        Assert.Equal(180f, clampedY3);
    }

    // ==========================================
    // 3. Motion Interpolation (Smoothing)
    // ==========================================

    [Fact]
    public void MotionInterpolation_CriticallyDampedSpring_MatchesExactFormula()
    {
        // Formula:
        // v_new = v_current + (P_target - P_current) * (f * f) - 2 * f * v_current
        // P_new = P_current + v_new
        // With f = 0.1:
        // Frame 1: P_0 = 0, v_0 = 0, Target = 100
        // v_1 = 0 + (100 - 0) * 0.01 - 2 * 0.1 * 0 = 1.0
        // P_1 = 0 + 1.0 = 1.0
        var (p1, v1) = CameraService.StepSpring(0f, 0f, 100f, 0.1f);
        Assert.Equal(1.0f, v1, 4);
        Assert.Equal(1.0f, p1, 4);

        // Frame 2:
        // v_2 = 1.0 + (100 - 1.0) * 0.01 - 2 * 0.1 * 1.0 = 1.0 + 0.99 - 0.2 = 1.79
        // P_2 = 1.0 + 1.79 = 2.79
        var (p2, v2) = CameraService.StepSpring(p1, v1, 100f, 0.1f);
        Assert.Equal(1.79f, v2, 4);
        Assert.Equal(2.79f, p2, 4);
    }

    [Fact]
    public void ApplySmoothing_GlidesCameraTowardsTargetWithoutSnapping()
    {
        var camera = new CameraController(0, 0, 640, 360);

        CameraService.ApplySmoothing(ref camera, targetX: 100f, targetY: 50f, targetW: 640f, targetH: 360f, smoothingFactor: 0.1f);

        // Must not snap directly to 100 or 50
        Assert.True(camera.X > 0f && camera.X < 100f);
        Assert.True(camera.Y > 0f && camera.Y < 50f);
        Assert.Equal(1.0f, camera.X, 4);
        Assert.Equal(0.5f, camera.Y, 4);
    }

    [Fact]
    public void AspectRatioCheck_ThrowsDebugFailure_WhenDeviationExceedsTolerance()
    {
        // Ratio = 800 / 360 = 2.222, deviates from 1.77777 by ~0.44 > 0.01 threshold
        Assert.ThrowsAny<Exception>(() =>
        {
            CameraService.CheckAspectRatio(800f, 360f);
        });

        Assert.ThrowsAny<Exception>(() =>
        {
            CameraService.CheckAspectRatio(500f, 360f);
        });
    }

    [Fact]
    public void AspectRatioCheck_Passes_WhenWithinTolerance()
    {
        // 640 / 360 = 1.7777778, |1.7777778 - 1.77777| < 0.0001 <= 0.01
        var ex = Record.Exception(() => CameraService.CheckAspectRatio(640f, 360f));
        Assert.Null(ex);

        // Scaled by 0.5: 320 / 180 = 1.7777778
        var ex2 = Record.Exception(() => CameraService.CheckAspectRatio(320f, 180f));
        Assert.Null(ex2);
    }

    // ==========================================
    // 4. Full Follow Logic & ECS Integration
    // ==========================================

    [Fact]
    public void Follow_UpdatesTargetClampsAndSmooths()
    {
        var camera = new CameraController(320, 180, 640, 360);
        camera.TargetY = 180f;

        // Player moves right to (400, 180) inside room (0, 0) to (1000, 360)
        if (!camera.Initialized)
        {
            camera.Initialized = true;
            camera.TargetY = playerWorldY;
            if (camera.Width <= 0f) camera.Width = CameraService.DefaultWidth;
            if (camera.Height <= 0f) camera.Height = CameraService.DefaultHeight;
            if (camera.ViewportWidth <= 0f) camera.ViewportWidth = CameraService.DefaultWidth;
            if (camera.ViewportHeight <= 0f) camera.ViewportHeight = CameraService.DefaultHeight;
        }

        if (movingDirection.HasValue && movingDirection.Value != 0)
        {
            camera.LastMovingDirection = movingDirection.Value > 0 ? 1 : -1;
        }

        // 1. Target Position Calculation
        var (targetX, targetY) = CameraService.CalculateTargetPosition(
            playerWorldX,
            playerWorldY,
            camera.LastMovingDirection,
            camera.Y,
            camera.Height,
            camera.TargetY,
            camera.ViewportHeight > 0f ? camera.ViewportHeight : CameraService.DefaultHeight);

        camera.TargetY = targetY;

        var targetW = CameraService.DefaultWidth;
        var targetH = CameraService.DefaultHeight;

        // 2. Room Boundary Clamping
        if (roomBounds.HasValue)
        {
            var room = roomBounds.Value;
            (targetX, targetY, targetW, targetH) = CameraService.ClampToLevelBox(
                targetX,
                targetY,
                room.minX,
                room.minY,
                room.maxX,
                room.maxY,
                CameraService.DefaultWidth,
                CameraService.DefaultHeight);
        }

        // 3. Motion Interpolation (Smoothing)
        ApplySmoothing(ref camera, targetX, targetY, targetW, targetH, smoothingFactor);

        // Target X should be playerX (400) + lookahead (32) = 432
        // Camera glides from 320 towards 432:
        // v_new = 0 + (432 - 320) * 0.01 = 1.12
        // X_new = 320 + 1.12 = 321.12
        Assert.Equal(321.12f, camera.X, 3);
        Assert.Equal(180f, camera.Y);
    }

    [Fact]
    public void ECS_CameraServiceUpdate_LocatesPlayerAndUpdatesCamera()
    {
        using var world = World.Create();

        // Spawn player with Player tag, Position, FacingDirection further right (500, 180)
        world.Create(
            new Player(),
            new Position(500, 180),
            new AabbCollider { Width = 22, Height = 30 },
            new FacingDirection { Value = 1 });

        // Spawn level boundary
        world.Create(
            new LevelBoundary(),
            new Position(0, 0),
            new AabbCollider { Width = 1280, Height = 360 });

        // First update should create and update camera
        var matrix1 = CameraService.Update(world);
        // Player at 500, lookahead +32 => target 532. Camera glides from 320 towards 532, so camera.X > 320.
        // View matrix is not Identity because camera center is not at default (320, 180)
        Assert.NotEqual(Matrix4x4.Identity, matrix1);

        // Query camera to check state
        var cameraQuery = new QueryDescription().WithAll<CameraController>();
        var foundCamera = false;
        foreach (var chunk in world.Query(cameraQuery).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                var cam = chunk.GetFirst<CameraController>();
                foundCamera = true;
                Assert.True(cam.Initialized);
                Assert.Equal(1, cam.LastMovingDirection);
                Assert.True(cam.X > 320f);
            }
        }
        Assert.True(foundCamera);
    }

    [Fact]
    public void ViewMatrix_CorrectlyMapsWorldToScreen()
    {
        var camera = new CameraController(320, 180, 640, 360);
        var matrix = CameraService.GetTransformation(camera);

        // World (0, 0) should map to screen (0, 0)
        var worldOrigin = new Vector4(0f, 0f, 0f, 1f);
        var screenOrigin = Vector4.Transform(worldOrigin, matrix);
        Assert.Equal(0f, screenOrigin.X, 4);
        Assert.Equal(0f, screenOrigin.Y, 4);

        // World (320, 180) should map to screen center (320, 180)
        var worldCenter = new Vector4(320f, 180f, 0f, 1f);
        var screenCenter = Vector4.Transform(worldCenter, matrix);
        Assert.Equal(320f, screenCenter.X, 4);
        Assert.Equal(180f, screenCenter.Y, 4);
    }

    [Fact]
    public void HorizontalLookAhead_MovingDirectionZero_MaintainsLastMovingDirection()
    {
        var camera = new CameraController(320, 180, 640, 360)
        {
            LastMovingDirection = -1,
            TargetY = 180f
        };

        // When movingDirection passed is null, it should keep -1 and lookahead should be -32
        if (!camera.Initialized)
        {
            camera.Initialized = true;
            camera.TargetY = playerWorldY;
            if (camera.Width <= 0f) camera.Width = CameraService.DefaultWidth;
            if (camera.Height <= 0f) camera.Height = CameraService.DefaultHeight;
            if (camera.ViewportWidth <= 0f) camera.ViewportWidth = CameraService.DefaultWidth;
            if (camera.ViewportHeight <= 0f) camera.ViewportHeight = CameraService.DefaultHeight;
        }

        if (movingDirection.HasValue && movingDirection.Value != 0)
        {
            camera.LastMovingDirection = movingDirection.Value > 0 ? 1 : -1;
        }

        // 1. Target Position Calculation
        var (targetX, targetY) = CameraService.CalculateTargetPosition(
            playerWorldX,
            playerWorldY,
            camera.LastMovingDirection,
            camera.Y,
            camera.Height,
            camera.TargetY,
            camera.ViewportHeight > 0f ? camera.ViewportHeight : CameraService.DefaultHeight);

        camera.TargetY = targetY;

        var targetW = CameraService.DefaultWidth;
        var targetH = CameraService.DefaultHeight;

        // 2. Room Boundary Clamping
        if (roomBounds.HasValue)
        {
            var room = roomBounds.Value;
            (targetX, targetY, targetW, targetH) = CameraService.ClampToLevelBox(
                targetX,
                targetY,
                room.minX,
                room.minY,
                room.maxX,
                room.maxY,
                CameraService.DefaultWidth,
                CameraService.DefaultHeight);
        }

        // 3. Motion Interpolation (Smoothing)
        ApplySmoothing(ref camera, targetX, targetY, targetW, targetH, smoothingFactor);

        Assert.Equal(-1, camera.LastMovingDirection);
        // Target X = 200 - 32 = 168.
        // Gliding from 320 to 168 should move left (camera.X < 320)
        Assert.True(camera.X < 320f);
    }

    [Fact]
    public void RoomBoundaryClamping_WhenRoomIsTallerThan16By9_ScalesBasedOnWidth()
    {
        // Room: 320 wide x 720 tall.
        // ratioW = 320 / 640 = 0.5
        // ratioH = 720 / 360 = 2.0
        // Min ratio = 0.5 (governed by width)
        // W = 640 * 0.5 = 320, H = 360 * 0.5 = 180
        var (_, _, clampedW, clampedH) = CameraService.ClampToRoom(
            targetX: 160f, targetY: 360f,
            roomMinX: 0f, roomMinY: 0f, roomMaxX: 320f, roomMaxY: 720f);

        Assert.Equal(320f, clampedW);
        Assert.Equal(180f, clampedH);
    }

    [Fact]
    public void RoomBoundaryClamping_WhenRoomIsWiderThan16By9_ScalesBasedOnHeight()
    {
        // Room: 1280 wide x 180 tall.
        // ratioW = 1280 / 640 = 2.0
        // ratioH = 180 / 360 = 0.5
        // Min ratio = 0.5 (governed by height)
        // W = 640 * 0.5 = 320, H = 360 * 0.5 = 180
        var (_, _, clampedW, clampedH) = CameraService.ClampToRoom(
            targetX: 640f, targetY: 90f,
            roomMinX: 0f, roomMinY: 0f, roomMaxX: 1280f, roomMaxY: 180f);

        Assert.Equal(320f, clampedW);
        Assert.Equal(180f, clampedH);
    }

    [Fact]
    public void RoomBoundaryClamping_NegativeRoomCoordinates_ClampsCorrectly()
    {
        // Room: (-500, -300) to (500, 300). Width = 1000, Height = 600.
        // ratioW = 1000 / 640 = 1.5625, ratioH = 600 / 360 = 1.6666...
        // Smaller ratio = 1.5625.
        // W = 640 * 1.5625 = 1000, H = 360 * 1.5625 = 562.5.
        // X center clamp: max(-500 + 500, min(X, 500 - 500)) = 0.
        var (clampedX, clampedY, clampedW, clampedH) = CameraService.ClampToRoom(
            targetX: -300f, targetY: 0f,
            roomMinX: -500f, roomMinY: -300f, roomMaxX: 500f, roomMaxY: 300f);

        Assert.Equal(0f, clampedX);
        Assert.Equal(1000f, clampedW);
        Assert.Equal(562.5f, clampedH);
        Assert.True(MathF.Abs(clampedW / clampedH - 1.77777f) <= 0.01f);
    }

    [Fact]
    public void SpringInterpolation_OverMultipleFrames_SmoothlyConvergesToTarget()
    {
        var pos = 0f;
        var vel = 0f;
        var target = 100f;

        // Run 200 frames of spring update with f = 0.1
        for (int frame = 0; frame < 200; frame++)
        {
            (pos, vel) = CameraService.StepSpring(pos, vel, target, 0.1f);
        }

        // After 200 frames, critically damped spring has converged near target with near-zero velocity
        Assert.True(MathF.Abs(pos - target) < 0.01f);
        Assert.True(MathF.Abs(vel) < 0.01f);
    }

    [Fact]
    public void ScreenToWorldSpace_InvertsWorldToScreenSpace()
    {
        var camera = new CameraController(450, 220, 640, 360);

        var originalWorld = new Vector2(512, 175);
        var screen = CameraService.WorldToScreen(originalWorld, camera);
        var roundTripWorld = CameraService.ScreenToWorld(screen, camera);

        Assert.Equal(originalWorld.X, roundTripWorld.X, 4);
        Assert.Equal(originalWorld.Y, roundTripWorld.Y, 4);
    }

    [Fact]
    public void MultipleRooms_ChoosesRoomContainingPlayerCenter()
    {
        using var world = World.Create();

        // Player at (100, 100), width 20, height 20 -> center at (110, 110)
        var player = world.Create(
            new Player(),
            new Position(100, 100),
            new AabbCollider { Width = 20, Height = 20 });

        // Room 1: (0, 0) to (640, 360) - contains player center
        var room1 = world.Create(
            new LevelBoundary(),
            new Position(0, 0),
            new AabbCollider { Width = 640, Height = 360 });

        // Room 2: (600, 0) to (1240, 360) - adjacent room overlapping player edge if extended
        var room2 = world.Create(
            new LevelBoundary(),
            new Position(600, 0),
            new AabbCollider { Width = 640, Height = 360 });

        var found = CameraService.TryFindIntersectingRoom(world, player, out var roomBounds);
        Assert.True(found);
        Assert.Equal(0f, roomBounds.minX);
        Assert.Equal(640f, roomBounds.maxX);
    }

    [Fact]
    public void NoIntersectingRoom_FollowDoesNotClampToRoom()
    {
        var camera = new CameraController(0, 0, 640, 360)
        {
            TargetY = 0f
        };

        // When roomBounds is null, target view is default 640x360 and target center is unconstrained
        if (!camera.Initialized)
        {
            camera.Initialized = true;
            camera.TargetY = playerWorldY;
            if (camera.Width <= 0f) camera.Width = CameraService.DefaultWidth;
            if (camera.Height <= 0f) camera.Height = CameraService.DefaultHeight;
            if (camera.ViewportWidth <= 0f) camera.ViewportWidth = CameraService.DefaultWidth;
            if (camera.ViewportHeight <= 0f) camera.ViewportHeight = CameraService.DefaultHeight;
        }

        if (movingDirection.HasValue && movingDirection.Value != 0)
        {
            camera.LastMovingDirection = movingDirection.Value > 0 ? 1 : -1;
        }

        // 1. Target Position Calculation
        var (targetX, targetY) = CameraService.CalculateTargetPosition(
            playerWorldX,
            playerWorldY,
            camera.LastMovingDirection,
            camera.Y,
            camera.Height,
            camera.TargetY,
            camera.ViewportHeight > 0f ? camera.ViewportHeight : CameraService.DefaultHeight);

        camera.TargetY = targetY;

        var targetW = CameraService.DefaultWidth;
        var targetH = CameraService.DefaultHeight;

        // 2. Room Boundary Clamping
        if (roomBounds.HasValue)
        {
            var room = roomBounds.Value;
            (targetX, targetY, targetW, targetH) = CameraService.ClampToLevelBox(
                targetX,
                targetY,
                room.minX,
                room.minY,
                room.maxX,
                room.maxY,
                CameraService.DefaultWidth,
                CameraService.DefaultHeight);
        }

        // 3. Motion Interpolation (Smoothing)
        ApplySmoothing(ref camera, targetX, targetY, targetW, targetH, smoothingFactor);

        // Target was 2000 + 32 = 2032. Camera moved towards it.
        Assert.True(camera.X > 0f);
        Assert.Equal(640f, camera.Width, 2);
        Assert.Equal(360f, camera.Height, 2);
    }
}
