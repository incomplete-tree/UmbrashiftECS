namespace UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

public record struct InputState
{
    public Direction8 ArrowsDirection;
    
    public bool IsDashPressed;
    
    public bool IsJumpPressed;
    
    public bool IsAttackPressed;

    public bool IsTogglePressed;
    
    public bool IsCrouchedPressed;
}