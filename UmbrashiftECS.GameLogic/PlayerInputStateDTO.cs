using UmbrashiftECS.Components;

namespace UmbrashiftECS.GameLogic;

public record struct PlayerInputStateDTO
{
    public int InputBindingSlot;
    public Direction8 ArrowsDirection;

    public bool IsDashPressed;

    public bool IsJumpPressed;

    public bool IsAttackPressed;

    public bool IsTogglePressed;

    public bool IsCrouchedPressed;
}
