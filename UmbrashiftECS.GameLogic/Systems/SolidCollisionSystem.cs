using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class SolidCollisionSystem(World world) : BaseSystem<World, uint>(world)
{
    private static readonly QueryDescription ActorQuery = new QueryDescription()
        .WithAll<ActorBody, Position>()
        .WithAny<AabbCollider, OffsetAabbCollider>();

    [Query]
    public void CollideSolids(
        in Entity entity,
        in SolidBody _,
        in Position position,
        in FractionalPositionRemainder fractionalPositionRemainder,
        in MovementDelta movementDelta
    )
    {
        if (movementDelta == new MovementDelta()) return;

        var newPositionX = movementDelta.X + position.X + fractionalPositionRemainder.X;
        var newPositionY = movementDelta.Y + position.Y + fractionalPositionRemainder.Y;
        var targetSolidX = (int)newPositionX;
        var targetSolidY = (int)newPositionY;
        var pixelsMoveX = targetSolidX - position.X;
        var pixelsMoveY = targetSolidY - position.Y;

        if (pixelsMoveX == 0 && pixelsMoveY == 0)
        {
            return;
        }

        if (!CollisionService.TryGetBounds(entity, out int solidLeft, out int solidTop, out int solidRight, out int solidBottom))
        {
            return;
        }

        var targetSolidLeft = solidLeft + pixelsMoveX;
        var targetSolidRight = solidRight + pixelsMoveX;
        var targetSolidTop = solidTop + pixelsMoveY;
        var targetSolidBottom = solidBottom + pixelsMoveY;

        
        // Actors riding on top of this solid BEFORE it moves are carried along
        var ridingActors = CollisionService.GetCollidingEntities<ActorBody>(entity, 0, -1).ToHashSet();
        foreach (var ridingActor in ridingActors)
        {
            if (!ridingActor.TryGet<MovementDelta>(out var actorDelta)) actorDelta = new MovementDelta();
            actorDelta.X += movementDelta.X;
            actorDelta.Y += movementDelta.Y;
            ridingActor.Set(actorDelta);
        }

        // Resolve pushes
        foreach (var chunk in world.Query(in ActorQuery).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                var actor = chunk.Entity(index);

                if (!CollisionService.TryGetBounds(actor, out int actorLeft, out int actorTop, out int actorRight, out int actorBottom))
                    continue;

                bool isRiding = ridingActors.Contains(actor);

                var actorPos = actor.Get<Position>();
                var actorRemainder = actor.TryGet<FractionalPositionRemainder>(out var rem) ? rem : default;
                if (!actor.TryGet<MovementDelta>(out var actorDelta)) actorDelta = new MovementDelta();

                var actorTargetX = (int)(actorPos.X + actorDelta.X + actorRemainder.X);
                var actorPixelsMoveX = actorTargetX - actorPos.X;
                var actorTargetLeft = actorLeft + actorPixelsMoveX;
                var actorTargetRight = actorRight + actorPixelsMoveX;

                var actorTargetY = (int)(actorPos.Y + actorDelta.Y + actorRemainder.Y);
                var actorPixelsMoveY = actorTargetY - actorPos.Y;
                var actorTargetTop = actorTop + actorPixelsMoveY;
                var actorTargetBottom = actorBottom + actorPixelsMoveY;

                // Check vertical overlap at target positions
                bool verticalOverlap = actorTargetTop < targetSolidBottom && actorTargetBottom > targetSolidTop;

                // Resolve X collision / push
                if (pixelsMoveX < 0 && verticalOverlap)
                {
                    // Solid moving left: pushes actor on the left if actorTargetRight > targetSolidLeft
                    if (actorLeft < solidLeft && actorTargetRight > targetSolidLeft)
                    {
                        var newTargetActorRight = targetSolidLeft;
                        var newTargetActorLeft = newTargetActorRight - (actorRight - actorLeft);
                        var newTargetActorX = newTargetActorLeft - (actorLeft - actorPos.X);
                        actorDelta.X = (newTargetActorX - actorPos.X) - actorRemainder.X;

                        if (actor.TryGet<Velocity>(out var actorVel) && actorVel.X > 0)
                        {
                            actorVel.X = 0;
                            actor.Set(actorVel);
                        }
                        actor.Set(actorDelta);

                        actorTargetLeft = newTargetActorLeft;
                        actorTargetRight = newTargetActorRight;
                    }
                }
                else if (pixelsMoveX > 0 && verticalOverlap)
                {
                    // Solid moving right: pushes actor on the right if actorTargetLeft < targetSolidRight
                    if (actorRight > solidRight && actorTargetLeft < targetSolidRight)
                    {
                        var newTargetActorLeft = targetSolidRight;
                        var newTargetActorX = newTargetActorLeft - (actorLeft - actorPos.X);
                        actorDelta.X = (newTargetActorX - actorPos.X) - actorRemainder.X;

                        if (actor.TryGet<Velocity>(out var actorVel) && actorVel.X < 0)
                        {
                            actorVel.X = 0;
                            actor.Set(actorVel);
                        }
                        actor.Set(actorDelta);

                        actorTargetLeft = newTargetActorLeft;
                        actorTargetRight = newTargetActorLeft + (actorRight - actorLeft);
                    }
                }

                // Resolve Y collision / push
                bool horizontalOverlap = actorTargetLeft < targetSolidRight && actorTargetRight > targetSolidLeft;
                if (!isRiding)
                {
                    if (pixelsMoveY < 0 && horizontalOverlap)
                    {
                        // Solid moving up: pushes actor above if actorTargetBottom > targetSolidTop
                        if (actorTop < solidTop && actorTargetBottom > targetSolidTop)
                        {
                            var newTargetActorBottom = targetSolidTop;
                            var newTargetActorTop = newTargetActorBottom - (actorBottom - actorTop);
                            var newTargetActorY = newTargetActorTop - (actorTop - actorPos.Y);
                            actorDelta.Y = (newTargetActorY - actorPos.Y) - actorRemainder.Y;

                            if (actor.TryGet<Velocity>(out var actorVel) && actorVel.Y > 0)
                            {
                                actorVel.Y = 0;
                                actor.Set(actorVel);
                            }
                            actor.Set(actorDelta);
                        }
                    }
                    else if (pixelsMoveY > 0 && horizontalOverlap)
                    {
                        // Solid moving down: pushes actor below if actorTargetTop < targetSolidBottom
                        if (actorBottom > solidBottom && actorTargetTop < targetSolidBottom)
                        {
                            var newTargetActorTop = targetSolidBottom;
                            var newTargetActorY = newTargetActorTop - (actorTop - actorPos.Y);
                            actorDelta.Y = (newTargetActorY - actorPos.Y) - actorRemainder.Y;

                            if (actor.TryGet<Velocity>(out var actorVel) && actorVel.Y < 0)
                            {
                                actorVel.Y = 0;
                                actor.Set(actorVel);
                            }
                            actor.Set(actorDelta);
                        }
                    }
                }
            }
        }
    }
}