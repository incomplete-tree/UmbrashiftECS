using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.Components.EntityComponents.Walk;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class FrictionSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void ApplyFriction(
        in Entity entity,
        in GroundedState groundedState,
        in FrictionConfig frictionConfig,
        ref Velocity velocity)
    {
        if (!groundedState.IsGrounded) return;

        var platformVelocity = 0f;
        var platformFriction = 1f;
        var foundPlatform = false;
        foreach (var platform in CollisionService.GetCollidingEntities<SolidBody>(entity, offsetY: 1))
        {
            if (!foundPlatform)
            {
                foundPlatform = true;
                if (platform.TryGet<FrictionConfig>(out var frictionConfigOnPlatform))
                    platformFriction = frictionConfigOnPlatform.Friction;
            }

            if (!platform.TryGet<MovementDelta>(out var movementDelta)) continue;

            platformVelocity = movementDelta.X;
            platformFriction = platform.TryGet<FrictionConfig>(out var movingPlatformFriction)
                ? movingPlatformFriction.Friction
                : 1f;
            break;
        }

        velocity.X = platformVelocity +
                     (velocity.X - platformVelocity) * frictionConfig.Friction * platformFriction;
    }
}
