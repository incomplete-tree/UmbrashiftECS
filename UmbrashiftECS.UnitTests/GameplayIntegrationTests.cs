using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.UnitTests;

public class GameplayIntegrationTests
{
    [Fact]
    public void EngineRunsTheVelocityAndMovementPipeline()
    {
        using var engine = new Engine();
        engine.Initialize();
        var player = engine.MainWorld.Create(
            new Position(0, 0),
            new InputBinding { InputBindingSlot = 0 },
            new InputState(),
            new InputBuffer { BufferFrames = 5 },
            new Velocity(2, 0),
            new MovementDelta(),
            new FractionalPositionRemainder());

        engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });

        Assert.Equal(new Position(2, 0), player.Get<Position>());
    }

    [Fact]
    public void EngineAppliesAStartedDashOnThePressFrame()
    {
        using var engine = new Engine();
        engine.Initialize();
        var player = engine.MainWorld.Create(
            new Position(0, 0),
            new ActorBody(),
            new InputBinding { InputBindingSlot = 0 },
            new InputState(),
            new InputBuffer { BufferFrames = 5 },
            new FacingDirection { Value = 1 },
            new DashConfig(8, 4, 1, 2),
            new DashState { AmountRemaining = 1 },
            new GroundedState(),
            new Velocity(),
            new MovementDelta(),
            new FractionalPositionRemainder());

        engine.Update(new PlayerInputStateDTO
        {
            InputBindingSlot = 0,
            ArrowsDirection = Direction8.Right,
            IsDashPressed = true
        });

        Assert.Equal(new Position(2, 0), player.Get<Position>());
        Assert.True(player.Get<DashState>().IsDashing);
    }
}
