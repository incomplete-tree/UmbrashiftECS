using UmbrashiftECS.Components.EntityComponents.Collision;

namespace UmbrashiftECS.Components.EntityComponents;

public record struct CrouchConfig(
    OffsetAabbCollider StandingCollider ); // for pre-stand collision check.

// TODO add a dynamic events sysstem so we can change hitbox size when crouching.
