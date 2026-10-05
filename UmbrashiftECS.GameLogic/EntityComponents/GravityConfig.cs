namespace UmbrashiftECS.GameLogic.EntityComponents;

public record struct GravityConfig
{
    public float GravityInPixelsPerFrameSquared;
    public float TerminalVelocity;
}