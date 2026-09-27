namespace UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

public class InputBuffer
{
    public uint LastDashPressedFrame;
    public bool LastDashPressHandled;

    public uint LastDashReleasedFrame;
    public bool LastDashReleaseHandled;
    
    public uint LastJumpPressedFrame;
    public bool LastJumpPressHandled;

    public uint LastJumpReleasedFrame;
    public bool LastJumpReleaseHandled;
    
    public uint LastAttackPressedFrame;
    public bool LastAttackPressHandled;

    public uint LastAttackReleasedFrame;
    public bool LastAttackReleaseHandled;
    
    public uint LastTogglePressedFrame;
    public bool LastTogglePressHandled;

    public uint LastToggleReleasedFrame;
    public bool LastToggleReleaseHandled;
}
