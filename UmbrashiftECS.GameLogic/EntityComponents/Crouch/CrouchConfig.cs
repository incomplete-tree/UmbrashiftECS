namespace UmbrashiftECS.GameLogic.EntityComponents.Crouch;

public record struct CrouchConfig(IEntityChangeMultiComponent ToCrouched, IEntityChangeMultiComponent ToUncrouched);
