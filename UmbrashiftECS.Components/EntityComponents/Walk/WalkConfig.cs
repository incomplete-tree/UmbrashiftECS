namespace UmbrashiftECS.Components.EntityComponents.Walk;

public record struct WalkConfig
{
    public float SpeedOnGround; // added every frame.
    public float SpeedInAir;
}