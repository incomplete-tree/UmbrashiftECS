using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.Operations;

namespace UmbrashiftECS.GameLogic.Services;

public static partial class CollisionService
{
    public static bool IsColliding(in Entity entity1, in Entity entity2, int offsetX = 0, int offsetY = 0)
    {
        if (entity1.TryGet<Layer>(out var layer1) && entity2.TryGet<Layer>(out var layer2) && layer1 != layer2)
        {
            return false;
        }

        if (!TryGetBounds(entity1, out var initLeft1, out var initTop1, out var initRight1, out var initBottom1) ||
            !TryGetBounds(entity2, out var left2, out var top2, out var right2, out var bottom2))
        {
            return false;
        }

        var targetLeft1 = initLeft1 + offsetX;
        var targetTop1 = initTop1 + offsetY;
        var targetRight1 = initRight1 + offsetX;
        var targetBottom1 = initBottom1 + offsetY;

        var hasOneWay1 = entity1.TryGet<OneWayCollision>(out var oneWay1);
        var hasOneWay2 = entity2.TryGet<OneWayCollision>(out var oneWay2);

        if (hasOneWay2 && hasOneWay1)
        {
            return CheckOneWay(oneWay2.DirectionToCollide,
                       left2, top2, right2, bottom2,
                       initLeft1, initTop1, initRight1, initBottom1,
                       targetLeft1, targetTop1, targetRight1, targetBottom1,
                       offsetX, offsetY) ||
                   CheckOneWay(oneWay1.DirectionToCollide,
                       initLeft1, initTop1, initRight1, initBottom1,
                       left2, top2, right2, bottom2,
                       left2 - offsetX, top2 - offsetY, right2 - offsetX, bottom2 - offsetY,
                       -offsetX, -offsetY);
        }

        if (hasOneWay2)
        {
            return CheckOneWay(oneWay2.DirectionToCollide,
                left2, top2, right2, bottom2,
                initLeft1, initTop1, initRight1, initBottom1,
                targetLeft1, targetTop1, targetRight1, targetBottom1,
                offsetX, offsetY);
        }

        if (hasOneWay1)
        {
            return CheckOneWay(oneWay1.DirectionToCollide,
                initLeft1, initTop1, initRight1, initBottom1,
                left2, top2, right2, bottom2,
                left2 - offsetX, top2 - offsetY, right2 - offsetX, bottom2 - offsetY,
                -offsetX, -offsetY);
        }

        return AabbOverlaps(targetLeft1, targetTop1, targetRight1, targetBottom1, left2, top2, right2, bottom2);
    }

    private static bool TryGetBounds(
        in Entity entity,
        out int left,
        out int top,
        out int right,
        out int bottom,
        int offsetX = 0,
        int offsetY = 0)
    {
        left = top = right = bottom = 0;
        if (!entity.TryGet<Position>(out var pos))
        {
            return false;
        }

        if (entity.TryGet<OffsetAabbCollider>(out var offsetAabb))
        {
            left = pos.X + offsetAabb.OffsetX + offsetX;
            top = pos.Y + offsetAabb.OffsetY + offsetY;
            right = left + offsetAabb.Width;
            bottom = top + offsetAabb.Height;
            return true;
        }

        if (entity.TryGet<AabbCollider>(out var aabb))
        {
            left = pos.X + offsetX;
            top = pos.Y + offsetY;
            right = left + aabb.Width;
            bottom = top + aabb.Height;
            return true;
        }

        return false;
    }

    private static bool AabbOverlaps(
        int left1, int top1, int right1, int bottom1,
        int left2, int top2, int right2, int bottom2) =>
        left1 < right2 && right1 > left2 && top1 < bottom2 && bottom1 > top2;

    private static bool CheckOneWay(
        Direction4 direction,
        int obsLeft, int obsTop, int obsRight, int obsBottom,
        int actorInitLeft, int actorInitTop, int actorInitRight, int actorInitBottom,
        int actorTargetLeft, int actorTargetTop, int actorTargetRight, int actorTargetBottom,
        int relOffsetX, int relOffsetY)
    {
        return direction switch
        {
            Direction4.Up =>
                relOffsetY > 0 &&
                actorInitBottom <= obsTop &&
                actorTargetBottom > obsTop &&
                Math.Max(actorInitRight, actorTargetRight) > obsLeft &&
                Math.Min(actorInitLeft, actorTargetLeft) < obsRight,

            Direction4.Down =>
                relOffsetY < 0 &&
                actorInitTop >= obsBottom &&
                actorTargetTop < obsBottom &&
                Math.Max(actorInitRight, actorTargetRight) > obsLeft &&
                Math.Min(actorInitLeft, actorTargetLeft) < obsRight,

            Direction4.Left =>
                relOffsetX > 0 &&
                actorInitRight <= obsLeft &&
                actorTargetRight > obsLeft &&
                Math.Max(actorInitBottom, actorTargetBottom) > obsTop &&
                Math.Min(actorInitTop, actorTargetTop) < obsBottom,

            Direction4.Right =>
                relOffsetX < 0 &&
                actorInitLeft >= obsRight &&
                actorTargetLeft < obsRight &&
                Math.Max(actorInitBottom, actorTargetBottom) > obsTop &&
                Math.Min(actorInitTop, actorTargetTop) < obsBottom,

            _ => false
        };
    }

    public static QueryDescription GetSolidQueryDescription<TBody>() =>
        new QueryDescription().WithAll<TBody, Position>().WithAny<AabbCollider, OffsetAabbCollider>();

    public static QueryDescription GetOneWayQueryDescription() =>
        new QueryDescription().WithAll<OneWayCollision, Position>().WithAny<AabbCollider, OffsetAabbCollider>();

    public static bool IsCollidingWith<TBody>(in Entity entity, int offsetX = 0, int offsetY = 0)
    {
        var world = World.Worlds[entity.WorldId];
        foreach (var chunk in world.Query(GetSolidQueryDescription<TBody>()).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                var otherEntity = chunk.Entity(index);
                if (otherEntity == entity) continue;
                if (IsColliding(entity, otherEntity, offsetX, offsetY)) return true;
            }
        }

        if (typeof(TBody) == typeof(SolidBody))
        {
            foreach (var chunk in world.Query(GetOneWayQueryDescription()).GetChunkIterator())
            {
                foreach (var index in chunk)
                {
                    var otherEntity = chunk.Entity(index);
                    if (otherEntity == entity) continue;
                    if (otherEntity.Has<SolidBody>()) continue;
                    if (IsColliding(entity, otherEntity, offsetX, offsetY)) return true;
                }
            }
        }

        return false;
    }

    public static bool IsCollidingWith<TBody>(in Entity entity, in OffsetAabbCollider collider)
    {
        var world = World.Worlds[entity.WorldId];
        foreach (var chunk in world.Query(GetSolidQueryDescription<TBody>()).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                var otherEntity = chunk.Entity(index);
                if (otherEntity == entity) continue;
                if (IsColliding(collider, entity, otherEntity)) return true;
            }
        }

        if (typeof(TBody) == typeof(SolidBody))
        {
            foreach (var chunk in world.Query(GetOneWayQueryDescription()).GetChunkIterator())
            {
                foreach (var index in chunk)
                {
                    var otherEntity = chunk.Entity(index);
                    if (otherEntity == entity) continue;
                    if (otherEntity.Has<SolidBody>()) continue;
                    if (IsColliding(collider, entity, otherEntity)) return true;
                }
            }
        }

        return false;
    }

    private static bool IsColliding(
        in OffsetAabbCollider collider,
        in Entity entity1,
        in Entity entity2)
    {
        if (entity1.TryGet<Layer>(out var layer1) && entity2.TryGet<Layer>(out var layer2) && layer1 != layer2)
        {
            return false;
        }

        if (!entity1.TryGet<Position>(out var pos1) ||
            !TryGetBounds(entity2, out var left2, out var top2, out var right2, out var bottom2))
        {
            return false;
        }

        var targetLeft1 = pos1.X + collider.OffsetX;
        var targetTop1 = pos1.Y + collider.OffsetY;
        var targetRight1 = targetLeft1 + collider.Width;
        var targetBottom1 = targetTop1 + collider.Height;

        if (entity2.TryGet<OneWayCollision>(out var oneWay2))
        {
            if (TryGetBounds(entity1, out var initLeft1, out var initTop1, out var initRight1, out var initBottom1))
            {
                var relOffsetX = targetLeft1 - initLeft1;
                var relOffsetY = targetBottom1 - initTop1;
                return CheckOneWay(oneWay2.DirectionToCollide,
                    left2, top2, right2, bottom2,
                    initLeft1, initTop1, initRight1, initBottom1,
                    targetLeft1, targetTop1, targetRight1, targetBottom1,
                    relOffsetX, relOffsetY);
            }

            return false;
        }

        return AabbOverlaps(targetLeft1, targetTop1, targetRight1, targetBottom1, left2, top2, right2, bottom2);
    }

    public static List<Entity> GetCollidingEntities<TBody>(
        Entity entity,
        int offsetX = 0,
        int offsetY = 0)
    {
        var result = new List<Entity>();
        var world = World.Worlds[entity.WorldId];
        var queryDesc = new QueryDescription().WithAll<TBody, Position>().WithAny<AabbCollider, OffsetAabbCollider>();

        foreach (var chunk in world.Query(in queryDesc).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                var otherEntity = chunk.Entity(index);
                if (otherEntity == entity) continue;
                if (IsColliding(entity, otherEntity, offsetX, offsetY))
                {
                    result.Add(otherEntity);
                }
            }
        }
        return result;
    }
}
