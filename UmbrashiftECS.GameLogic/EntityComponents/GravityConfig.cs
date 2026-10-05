namespace UmbrashiftECS.GameLogic.EntityComponents;

public record struct GravityConfig
{
    public float GravityInPixelsPerFrameSquared;

    public bool ChangeGravityWhenJumpHeld;
    public float JumpHeldModifier;
}