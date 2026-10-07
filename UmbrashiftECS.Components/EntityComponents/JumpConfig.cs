namespace UmbrashiftECS.Components.EntityComponents;

public record struct JumpConfig
{
    public float InitialJumpSpeed;
    public int JumpDistance;
    public int JumpDuration;
    public int JumpHeight;
    public bool AllowAirJump;
    public uint CoyoteTimeFrames;
 }