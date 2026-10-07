namespace UmbrashiftECS.Components.EntityComponents;

public record struct DashConfig(int Distance, int Duration, int Amount, int FramesTillRefill)
{
    public float Speed => Distance / (float)Duration;
}