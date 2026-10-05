using System.Diagnostics.Contracts;

namespace UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

public struct InputBuffer
{
    public uint BufferFrames;

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
    
    [Pure]
    public bool IsBuffered(uint currentFrame, uint actionFrame, bool handled = false)
    {
        if (handled) return false;

        if (currentFrame - actionFrame < BufferFrames)
            return true;

        return false;
    }

    public bool ConsumeActionIfPressed(uint currentFrame, uint actionFrame, ref bool handled)
    {
        if (handled) return false;

        if (currentFrame - actionFrame < BufferFrames)
        {
            handled = true;
            return true;
        }

        return false;
    }
}
