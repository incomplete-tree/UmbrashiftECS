using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;

namespace UmbrashiftECS.GameLogic.Services;

public static partial class CollisionService
{
    public static bool IsColliding(in Entity entity1, in Entity entity2, int offsetX = 0, int offsetY = 0)
    {
        if (entity1.TryGet<Layer>(out var layer1) && entity2.TryGet<Layer>(out var layer2) && layer1 != layer2)
        {
            return false;
        }

        if (!entity1.TryGet<Position>(out var pos1) || !entity2.TryGet<Position>(out var pos2))
        {
            Debug.Fail("no position!");
            return false;
        }

        pos1 = new Position(pos1.X + offsetX, pos1.Y + offsetY);

        if (entity1.TryGet<AabbCollider>(out var aabb1))
        {
            if (entity2.TryGet<AabbCollider>(out var aabb2))
                return AabbToAabb(aabb1, pos1, aabb2, pos2);

            if (entity2.TryGet<OffsetAabbCollider>(out var offsetAabb2))
                return AabbToOffsetAabb(aabb1, pos1, offsetAabb2, pos2);

            return false;
        }

        if (entity1.TryGet<OffsetAabbCollider>(out var offsetAabb1))
        {
            if (entity2.TryGet<AabbCollider>(out var aabb2))
                return AabbToOffsetAabb(aabb2, pos2, offsetAabb1, pos1);
            if (entity2.TryGet<OffsetAabbCollider>(out var offsetAabb2))
                return OffsetAabbToOffsetAabb(offsetAabb1, pos1, offsetAabb2, pos2);

            return false;
        }

        return false;
    }
    
    private static bool AabbToAabb(in AabbCollider aabb1, in Position pos1, in AabbCollider aabb2, in Position pos2) =>
        pos1.X < pos2.X + aabb2.Width && pos1.X + aabb1.Width > pos2.X &&
        pos1.Y < pos2.Y + aabb2.Height && pos1.Y + aabb1.Height > pos2.Y;

    private static bool AabbToOffsetAabb(in AabbCollider aabb1, in Position pos1, in OffsetAabbCollider aabb2, in Position pos2) =>
        pos1.X < pos2.X + aabb2.OffsetX + aabb2.Width && pos1.X + aabb1.Width > pos2.X + aabb2.OffsetX &&
        pos1.Y < pos2.Y + aabb2.OffsetY + aabb2.Height && pos1.Y + aabb1.Height > pos2.Y + aabb2.OffsetY;

    private static bool OffsetAabbToOffsetAabb(in OffsetAabbCollider aabb1, in Position pos1, in OffsetAabbCollider aabb2, in Position pos2) =>
        pos1.X + aabb1.OffsetX < pos2.X + aabb2.OffsetX + aabb2.Width &&
        pos1.X + aabb1.OffsetX + aabb1.Width > pos2.X + aabb2.OffsetX &&
        pos1.Y + aabb1.OffsetY < pos2.Y + aabb2.OffsetY + aabb2.Height &&
        pos1.Y + aabb1.OffsetY + aabb1.Height > pos2.Y + aabb2.OffsetY;


    public static QueryDescription GetSolidQueryDescription<TBody>() =>
        new QueryDescription().WithAll<TBody,Position>().WithAny<AabbCollider, OffsetAabbCollider>();
    
    public static bool IsCollidingWith<TBody>(in Entity entity, int offsetX=0, int offsetY=0)
    {
        foreach (var chunk in World.Worlds[entity.WorldId].Query(GetSolidQueryDescription<TBody>()).GetChunkIterator())
        {
            foreach (var index in chunk)
            {
                var otherEntity = chunk.Entity(index);
                if (IsColliding(entity, otherEntity, offsetX, offsetY)) return true;
            }
        }
        return false;
    }
    //
    // [Query]
    // private static void GetCollidingEntities<TBody>(
    //     [Data] in Entity entity,
    //     [Data] ref List<Entity> result,
    //     in Entity otherEntity,
    //     in TBody _,
    //     [Data] in int offsetX = 0,
    //     [Data] in int offsetY = 0)
    // {
    //     if (IsColliding(entity, otherEntity, offsetX, offsetY))
    //     {
    //         result.Add(otherEntity);
    //     }
    // }
}
