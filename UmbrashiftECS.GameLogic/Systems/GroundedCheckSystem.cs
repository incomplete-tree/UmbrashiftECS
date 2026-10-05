using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class GroundedCheckSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    [Any<AabbCollider, OffsetAabbCollider>]
    public void UpdateGrounded([Data] uint currentFrame, ref GroundedState groundedState, in Entity entity)
    {
        if (CollisionService.IsCollidingWith<SolidBody>(entity, 0, 1))
        {
            groundedState.IsGrounded = true;
            groundedState.LastGroundedTime = currentFrame;
            return;
        }

        groundedState.IsGrounded = false;
    }
}