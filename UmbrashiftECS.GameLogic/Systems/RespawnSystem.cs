using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Respawn;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class RespawnSystem(World world) : BaseSystem<World, uint>(world)
{
    private static readonly QueryDescription SpawnPointQuery = new QueryDescription()
        .WithAll<PlayerSpawnPoint, Position>();

    [Query]
    public void Respawn(
        in Entity entity,
        in Dead _,
        in ActiveRespawnPoint activeRespawnPoint,
        ref Position position)
    {
        if (!TryGetSpawnPosition(activeRespawnPoint.ActiveRespawnPointId, out var spawnPosition)) return;

        position = spawnPosition;
        ResetMovementState(entity);
        entity.Remove<Dead>();
    }

    private bool TryGetSpawnPosition(Guid id, out Position position)
    {
        foreach (var chunk in World.Query(SpawnPointQuery).GetChunkIterator())
        {
            var refs = chunk.GetFirst<PlayerSpawnPoint, Position>();
            foreach (var index in chunk)
            {
                ref var spawnPoint = ref Unsafe.Add(ref refs.t0, index);
                if (spawnPoint.Id != id) continue;

                position = Unsafe.Add(ref refs.t1, index);
                return true;
            }
        }

        position = default;
        return false;
    }

    private static void ResetMovementState(in Entity entity)
    {
        if (entity.Has<Velocity>()) entity.Set(new Velocity());
        if (entity.Has<MovementDelta>()) entity.Set(new MovementDelta());
        if (entity.Has<FractionalPositionRemainder>()) entity.Set(new FractionalPositionRemainder());
        if (entity.Has<GroundedState>()) entity.Set(new GroundedState());

        if (entity.TryGet<DashState>(out var dashState))
        {
            dashState.IsDashing = false;
            dashState.TimeRemaining = 0;
            dashState.DirectionX = 0;
            dashState.DirectionY = 0;
            entity.Set(dashState);
        }
    }
}
