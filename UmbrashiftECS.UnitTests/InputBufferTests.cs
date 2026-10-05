using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.UnitTests;

public class InputBufferTests
{
    private const uint BufferFrames = 5;

    [Theory]
    [InlineData(1u, 2u, false)]
    [InlineData(5u, 1u, true)]
    [InlineData(6u, 1u, false)]
    public void OnlyPastUnconsumedFramesAreBuffered(uint currentFrame, uint actionFrame, bool expected)
    {
        var buffer = new InputBuffer { BufferFrames = BufferFrames };

        Assert.Equal(expected, buffer.IsBuffered(currentFrame, actionFrame));
    }

    [Fact]
    public void ConsumingAnActionMarksItHandledOnce()
    {
        var buffer = new InputBuffer { BufferFrames = BufferFrames };
        var handled = false;

        Assert.True(buffer.ConsumeActionIfPressed(2, 2, ref handled));
        Assert.True(handled);
        Assert.False(buffer.ConsumeActionIfPressed(3, 2, ref handled));
    }
}
