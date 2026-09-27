using System;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class ActorCollisionSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void CheckActorCollision(in Entity entity, in ActorBody _, ref MovementDelta movementDelta)
    {
        var position = entity.Get<Position>();
        var remainder = entity.TryGet<FractionalPositionRemainder>(out var value)
            ? value
            : default;

        CheckX(entity, position.X, remainder.X, ref movementDelta.X);
        MoveY(entity, position.Y, remainder.Y, ref movementDelta.Y);
    }
    
    public void CheckX(in Entity actor, ref int amount)
    {
        ClampX(actor, ref amount);
    }

    private void CheckX(in Entity actor, int position, float remainder, ref float amount)
    {
        var targetPosition = (int)(amount + position + remainder);
        var integerAmount = targetPosition - position;

        if (ClampX(actor, ref integerAmount))
        {
            amount = integerAmount - remainder;
        }
    }

    private bool ClampX(in Entity actor, ref int amount)
    {
        if (!CollisionService.IsCollidingWith<SolidBody>(World, actor, offsetX: amount))
        {
            return false;
        }

        var direction = Math.Sign(amount);
        for (var checkedAmount = 0; checkedAmount != amount; checkedAmount += direction)
        {
            if (CollisionService.IsCollidingWith<SolidBody>(World, actor, offsetX: checkedAmount + direction))
            {
                amount = checkedAmount;
                return true;
            }
        }

        return true;
    }
    
    public void MoveY(in Entity actor, ref int amount)
    {
        ClampY(actor, ref amount);
    }

    private void MoveY(in Entity actor, int position, float remainder, ref float amount)
    {
        var targetPosition = (int)(amount + position + remainder);
        var integerAmount = targetPosition - position;

        if (ClampY(actor, ref integerAmount))
        {
            amount = integerAmount - remainder;
        }
    }

    private bool ClampY(in Entity actor, ref int amount)
    {
        if (!CollisionService.IsCollidingWith<SolidBody>(World, actor, offsetY: amount))
        {
            return false;
        }

        var direction = Math.Sign(amount);
        for (var checkedAmount = 0; checkedAmount != amount; checkedAmount += direction)
        {
            if (CollisionService.IsCollidingWith<SolidBody>(World, actor, offsetY: checkedAmount + direction))
            {
                amount = checkedAmount;
                return true;
            }
        }

        return true;
    }
}

public record struct ActorHitWallEvent(Entity Actor, Entity Solid);
