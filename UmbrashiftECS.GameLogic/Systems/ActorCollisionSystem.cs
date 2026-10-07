using System;
using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class ActorCollisionSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void CheckActorCollision(in Entity entity, in ActorBody _, ref MovementDelta movementDelta, ref Velocity velocity)
    {
        var position = entity.Get<Position>();
        var remainder = entity.TryGet<FractionalPositionRemainder>(out var value)
            ? value
            : default;

        var collidedX = CheckX(entity, position.X, remainder.X, ref movementDelta.X);
        var collidedY = CheckY(entity, position.Y, remainder.Y, ref movementDelta.Y);

        if (collidedY || collidedX)
        {
            var hitWallEvent = new ActorHitWallEvent(entity, collidedX, collidedY);
            velocity = new Velocity();
            // EventBus.Send(in hitWallEvent);
        }
    }

    private bool CheckX(in Entity actor, int position, float remainder, ref float amount)
    {
        var targetPosition = (int)(amount + position + remainder);
        var integerAmount = targetPosition - position;

        if (ClampX(actor, ref integerAmount))
        {
            amount = integerAmount - remainder;
            return true;
        }

        return false;
    }

    private bool ClampX(in Entity actor, ref int amount)
    {
        if (!CollisionService.IsCollidingWith<SolidBody>(actor, offsetX: amount))
        {
            return false;
        }

        var direction = Math.Sign(amount);
        for (var checkedAmount = 0; checkedAmount != amount; checkedAmount += direction)
        {
            if (CollisionService.IsCollidingWith<SolidBody>(actor, offsetX: checkedAmount + direction))
            {
                amount = checkedAmount;
                return true;
            }
        }

        return true;
    }
    
    private bool CheckY(in Entity actor, int position, float remainder, ref float amount)
    {
        var targetPosition = (int)(amount + position + remainder);
        var integerAmount = targetPosition - position;

        if (ClampY(actor, ref integerAmount))
        {
            amount = integerAmount - remainder;
            return true;
        }

        return false;
    }

    private bool ClampY(in Entity actor, ref int amount)
    {
        if (!CollisionService.IsCollidingWith<SolidBody>(actor, offsetY: amount))
        {
            return false;
        }

        var direction = Math.Sign(amount);
        for (var checkedAmount = 0; checkedAmount != amount; checkedAmount += direction)
        {
            if (CollisionService.IsCollidingWith<SolidBody>(actor, offsetY: checkedAmount + direction))
            {
                amount = checkedAmount;
                return true;
            }
        }

        return true;
    }
}

public record struct ActorHitWallEvent(Entity Actor, bool OnXAxis, bool OnYAxis);
