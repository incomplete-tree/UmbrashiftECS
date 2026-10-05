using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Crouch;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Services;

namespace UmbrashiftECS.GameLogic.Systems;

public partial class CrouchSystem(World world) : BaseSystem<World, uint>(world)
{
    [Query]
    public void CrouchWhenPressed(
        in Entity entity,
        in InputState inputState,
        ref CrouchState crouchState)
    {
        if (entity.TryGet<DashState>(out var dashState) && dashState.IsDashing)
        {
            return;
        }
        
        if (inputState.IsCrouchedPressed)
        {
            crouchState.IsCrouched = true;
            return;
        }
        else
        {
            crouchState.IsCrouched = false;
        }
    }
    [Query]
    public void ForceCrouchedInAir(in Entity entity, in GroundedState groundedState, ref CrouchState crouchState)
    {
        if (!groundedState.IsGrounded)
        {
            crouchState.IsCrouched = false;
        }
    }

    [Query]
    public void ForceCrouchedInSmallSpaces(in Entity entity, ref CrouchState crouchState)
    {
        if (crouchState.IsCrouched) return;

        if (CollisionService.IsCollidingWith<SolidBody>(entity))
        {
            crouchState.IsCrouched = true;
        }
    }

    [Query]
    public void ApplyCrouchChanges(in Entity entity, ref CrouchState crouchState, in CrouchConfig crouchConfig)
    {
        if (crouchState.IsCrouched && !crouchState.WasCrouched)
            crouchConfig.ToCrouched.Apply(entity);
        
        if (!crouchState.IsCrouched && crouchState.WasCrouched)
            crouchConfig.ToUncrouched.Apply(entity);

        crouchState.WasCrouched = crouchState.IsCrouched;
    }
}
