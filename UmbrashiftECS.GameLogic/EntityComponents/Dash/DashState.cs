namespace UmbrashiftECS.GameLogic.EntityComponents.Dash;

public struct DashState
{
    public int AmountRemaining;
    public int TimeRemaining;
    public float DirectionX;
    public float DirectionY;
    public bool IsDashing;
}