using System.Diagnostics.Contracts;

namespace UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

public struct InputBuffer
{
    public uint BufferFrames = 5;

    public uint LastDashPressedFrame;
    public bool LastDashPressHandled = true;

    public uint LastDashReleasedFrame;
    public bool LastDashReleaseHandled = true;
    
    public uint LastJumpPressedFrame;
    public bool LastJumpPressHandled = true;

    public uint LastJumpReleasedFrame;
    public bool LastJumpReleaseHandled = true;
    
    public uint LastAttackPressedFrame;
    public bool LastAttackPressHandled = true;

    public uint LastAttackReleasedFrame;
    public bool LastAttackReleaseHandled = true;
    
    public uint LastTogglePressedFrame;
    public bool LastTogglePressHandled = true;

    public uint LastToggleReleasedFrame;
    public bool LastToggleReleaseHandled = true;

    public InputBuffer()
    {
    }

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
