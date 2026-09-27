namespace UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

public class InputBuffer
{
    public uint LastDashPressedFrame;
    public bool LastDashPressHandled;
    
    public uint LastJumpPressedFrame;
    public bool LastJumpPressHandled;
    
    public uint LastAttackPressedFrame;
    public bool LastAttackPressHandled;
    
    public uint LastTogglePressedFrame;
    public bool LastTogglePressHandled;
}