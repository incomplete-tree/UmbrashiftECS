using System;
using Arch.Core;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class MoveBetweenPointsSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void UpdateMovers(ref MoveBetweenPoints moveBetweenPoints,
        ref MovementDelta movementDelta,
        in Position currentPosition, in FractionalPositionRemainder fractionalPositionRemainder)
    {
        var nextPoint = moveBetweenPoints.Points[moveBetweenPoints.NextPointIndex];

        if (currentPosition == nextPoint)
        {
            moveBetweenPoints.NextPointIndex++;
            moveBetweenPoints.NextPointIndex %= moveBetweenPoints.Points.Length;
            UpdateMovers(ref moveBetweenPoints, ref movementDelta, in currentPosition, in fractionalPositionRemainder);
            return;
        }

        float currentPosX = currentPosition.X + fractionalPositionRemainder.X;
        float currentPosY = currentPosition.Y + fractionalPositionRemainder.Y;
        
        var deltaX = nextPoint.X - currentPosX;
        var deltaY = nextPoint.Y - currentPosY;
        var distanceSquared = deltaX * deltaX + deltaY * deltaY;
        if (distanceSquared <= moveBetweenPoints.Speed * moveBetweenPoints.Speed)
        {
            movementDelta = new MovementDelta() { X = deltaX, Y = deltaY };
            return;
        }

        var distance = MathF.Sqrt(distanceSquared);
        var ratio = moveBetweenPoints.Speed / distance;
        movementDelta = new MovementDelta() { X = deltaX * ratio, Y = deltaY * ratio };
    }
}