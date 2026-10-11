using System;
using System.Diagnostics;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;

namespace UmbrashiftECS.Rendering.Services;

public static class CameraService
{
    public const float AspectRatio = 16f / 9f;
    public const float AspectRatioTolerance = 0.01f;
    
    public const float DefaultSmoothingFactor = 0.1f;
    public const int LookAheadDistance = 32;
    public const float DefaultWidth = 640f;
    public const float DefaultHeight = 360f;

    private static float _posX, _posY, _width = DefaultWidth, _height = DefaultHeight;

    private static float _velX, _velY, _velW, _velH;

    private static int _targetPosX, _targetPosY, _targetWidth, _targetHeight;
    /// <summary>
    /// Converts a world position vector to screen space.
    /// </summary>
    public static Vector2 WorldToScreen(Vector2 worldPos)
    {
        var transform = GetTransformation();
        return Vector2.Transform(worldPos, transform);
    }

    /// <summary>
    /// Converts a screen position vector to world space.
    /// </summary>
    public static Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Matrix4x4.Invert(GetTransformation(), out var inverse);
        return Vector2.Transform(screenPos, inverse);
    }
    
    private static void CalculateTargetPosition(
        int controllerWorldX,
        int controllerWorldY,
        sbyte lastMovingDirection)
    {
        _targetPosX = controllerWorldX + (lastMovingDirection * LookAheadDistance);

        var screenY = WorldToScreen(new Vector2(controllerWorldX, controllerWorldY)).Y;

        if (screenY is < DefaultHeight * 0.25f or > DefaultHeight * 0.70f)
        {
            _targetPosY = controllerWorldY;
        }
    }

    private static void ClampToLevelBox(in AabbCollider controllerHitbox, in int controllerX, in int controllerY)
    {
        // TODO
    }
    
    private static void SmoothVelocity()
    {
        // TODO
    }

    /// <summary>
    /// Computes 2D camera view matrix for SpriteBatch rendering.
    /// </summary>
    public static Matrix4x4 GetTransformation()
    {
        var zoomX = DefaultWidth / _width;
        var zoomY = DefaultHeight / _height;

        return Matrix4x4.CreateTranslation(-_posX, -_posY, 0f)
             * Matrix4x4.CreateScale(zoomX, zoomY, 1f)
             * Matrix4x4.CreateTranslation(DefaultWidth/2, DefaultHeight/2, 0f);
    }
    
    public static Matrix4x4 Update(World world)
    {
        var cameraQuery = new QueryDescription().WithAll<CameraController, Position, FacingDirection>();
        Entity cameraControllerEntity = default;
        Position cameraControllerPos = default;
        FacingDirection facingDirection = default;
        foreach (var chunk in world.Query(cameraQuery).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                cameraControllerEntity = chunk.Entity(index);
                cameraControllerPos = chunk.Get<Position>(index);
                facingDirection = chunk.Get<FacingDirection>(index);
                break;
            }
        }
        
        if (true)
        {
            CalculateTargetPosition(cameraControllerPos.X, cameraControllerPos.Y, facingDirection.Value);

            // ClampToLevelBox();
            
            // TODO remove
            _posX = _targetPosX;
            _posY = _targetPosY;
        }

        return GetTransformation();
    }
}