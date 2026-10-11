namespace UmbrashiftECS.GameLogic.EntityComponents.Movement;

public record struct FacingDirection
{
    public sbyte Value;

    public FacingDirection(sbyte value = 1)
    {
        Value = value;
    }
}
