using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class DashCollisionSystem(World world) : BaseSystem<World, uint>(world)
{
    [Event]
    public void StopDashOnCollision(in ActorHitWallEvent @event)
    {
        if (@event.Actor.TryGet<DashState>(out var state) && state.IsDashing)
        {
            state.IsDashing = false;
            if (!@event.Actor.Get<DashConfig>().PreserveVelocityAfterCollision)
            {
                @event.Actor.Set<Velocity>(default);
            }
        }
    }
}
