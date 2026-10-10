using System.Collections.Generic;
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
    [Query]
    public void CollideSolids(
        in Entity entity,
        in SolidBody solid,
        in Position position,
        in FractionalPositionRemainder fractionalPositionRemainder,
        in MovementDelta movementDelta
    )
    {
        if (movementDelta == new MovementDelta()) return;
        var pixelsMoveX = (int)(position.X + fractionalPositionRemainder.X + movementDelta.X) - position.X;
        var pixelsMoveY = (int)(position.Y + fractionalPositionRemainder.Y + movementDelta.Y) - position.Y;

        var affectedActors = new HashSet<Entity>();

        if (pixelsMoveX != 0 || pixelsMoveY != 0)
        {
            var collidingActors = CollisionService.GetCollidingEntities<ActorBody>(entity, pixelsMoveX, pixelsMoveY);
            foreach (var actor in collidingActors)
            {
                affectedActors.Add(actor);
            }
        }

        // Actors riding on top of the solid
        var ridingActors = CollisionService.GetCollidingEntities<ActorBody>(entity, 0, -1);
        foreach (var actor in ridingActors)
        {
            affectedActors.Add(actor);
        }

        foreach (var actor in affectedActors)
        {
            if (!actor.TryGet<MovementDelta>(out var actorMovementDelta)) actor.Add<MovementDelta>();
            actorMovementDelta.X += movementDelta.X;
            if (movementDelta.Y < 0)
            {
                actorMovementDelta.Y += movementDelta.Y;
            }
            actor.Set(actorMovementDelta);
        }
    }
}