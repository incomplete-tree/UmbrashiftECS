using Microsoft.Xna.Framework.Input;
using UmbrashiftECS.Components;
using UmbrashiftECS.GameLogic;

namespace UmbrashiftECS.MonoGame;

public class InputCapturer
{
    // TODO un-hardcode bindings

    public PlayerInputStateDTO GetKeyboardInput()
    {
        var keyboardState = Keyboard.GetState();

        var horizontal = (keyboardState.IsKeyDown(Keys.Right) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.Left) ? 1 : 0);
        var vertical = (keyboardState.IsKeyDown(Keys.Down) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.Up) ? 1 : 0);

        var dir = Direction8.FromAxes(horizontal, vertical);

        return new PlayerInputStateDTO
        {
            InputBindingSlot = 0,
            ArrowsDirection = dir,
            IsDashPressed = keyboardState.IsKeyDown(Keys.C),
            IsJumpPressed = keyboardState.IsKeyDown(Keys.Space),
            IsAttackPressed = keyboardState.IsKeyDown(Keys.X),
            IsTogglePressed = keyboardState.IsKeyDown(Keys.Z),
            IsCrouchedPressed = keyboardState.IsKeyDown(Keys.Down)
        };
    }
}