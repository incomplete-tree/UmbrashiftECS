using UmbrashiftECS.GameLogic.EntityComponents.Collision;

namespace UmbrashiftECS.GameLogic.EntityComponents.Crouch;

public record struct CrouchConfig(
    IEntityChangeMultiComponent ToCrouched,
    IEntityChangeMultiComponent ToUncrouched,
    OffsetAabbCollider StandingCollider);
