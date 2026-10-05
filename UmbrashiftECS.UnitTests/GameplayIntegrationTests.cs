using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Crouch;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;

namespace UmbrashiftECS.UnitTests;

public class GameplayIntegrationTests
{
    private static Entity CreatePlayer(World world, Position position, Layer layer = Layer.GameplayLayer0)
    {
        return world.Create(
            position,
            new ActorBody(),
            new OffsetAabbCollider { Width = 8, Height = 36 },
            layer,
            new InputBinding { InputBindingSlot = 0 },
            new InputState(),
            new InputBuffer(),
            new FacingDirection(1),
            JumpConfig.Default,
            new GroundedState(),
            Gravity.PlayerDefault,
            CrouchConfig.Default,
            new CrouchState(),
            DashConfig.Default,
            new DashState { AmountRemaining = 1 },
            new Velocity(),
            new MovementDelta(),
            new FractionalPositionRemainder());
    }

    [Fact]
    public void Engine_CanCrouchAndUncrouch()
    {
        using var engine = new Engine();
        engine.Initialize();
        var player = CreatePlayer(engine.MainWorld, new Position(0, 0));
        // Ground
        engine.MainWorld.Create(
            new Position(0, 36),
            new SolidBody(),
            new AabbCollider { Width = 32, Height = 16 });

        // Update 1: standing on ground
        engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });
        Assert.True(player.Get<GroundedState>().IsGrounded);
        Assert.False(player.Get<CrouchState>().IsCrouched);
        Assert.Equal(8, player.Get<OffsetAabbCollider>().Width);

        // Update 2: press crouch
        engine.Update(new PlayerInputStateDTO
        {
            InputBindingSlot = 0,
            IsCrouchedPressed = true
        });
        Assert.True(player.Get<CrouchState>().IsCrouched);
        Assert.Equal(20, player.Get<OffsetAabbCollider>().Width);
        Assert.Equal(26, player.Get<OffsetAabbCollider>().Height);
        Assert.Equal(-12, player.Get<OffsetAabbCollider>().OffsetX);
        Assert.Equal(10, player.Get<OffsetAabbCollider>().OffsetY);

        // Update 3: release crouch
        engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });
        Assert.False(player.Get<CrouchState>().IsCrouched);
        Assert.Equal(8, player.Get<OffsetAabbCollider>().Width);
        Assert.Equal(36, player.Get<OffsetAabbCollider>().Height);
    }

    [Fact]
    public void Engine_CanJumpWhenGrounded()
    {
        using var engine = new Engine();
        engine.Initialize();
        var player = CreatePlayer(engine.MainWorld, new Position(0, 0));
        engine.MainWorld.Create(
            new Position(0, 36),
            new SolidBody(),
            new AabbCollider { Width = 32, Height = 16 });

        // Update 1: establish grounded
        engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });
        Assert.True(player.Get<GroundedState>().IsGrounded);

        // Update 2: jump
        engine.Update(new PlayerInputStateDTO
        {
            InputBindingSlot = 0,
            IsJumpPressed = true
        });

        Assert.Equal(-JumpConfig.Default.InitialJumpSpeed, player.Get<Velocity>().Y, 5);
        Assert.True(player.Get<MovementDelta>().Y < 0);
        Assert.True(player.Get<Position>().Y < 0);
    }

    [Fact]
    public void Engine_CanDashHorizontally()
    {
        using var engine = new Engine();
        engine.Initialize();
        var player = CreatePlayer(engine.MainWorld, new Position(0, 0));
        engine.MainWorld.Create(
            new Position(0, 36),
            new SolidBody(),
            new AabbCollider { Width = 320, Height = 16 });

        // Update 1: ground player to have dash available
        engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });
        Assert.Equal(1, player.Get<DashState>().AmountRemaining);

        // Update 2: press dash to the right
        engine.Update(new PlayerInputStateDTO
        {
            InputBindingSlot = 0,
            ArrowsDirection = Direction8.Right,
            IsDashPressed = true
        });

        Assert.True(player.Get<DashState>().IsDashing);
        Assert.Equal(DashConfig.Default.Speed, player.Get<Velocity>().X, 5);
        Assert.True(player.Get<Position>().X > 0);
    }

    [Fact]
    public void Engine_CanSwitchLayers()
    {
        using var engine = new Engine();
        engine.Initialize();
        var player = CreatePlayer(engine.MainWorld, new Position(0, 0), Layer.GameplayLayer0);

        // Update 1: press toggle
        engine.Update(new PlayerInputStateDTO
        {
            InputBindingSlot = 0,
            IsTogglePressed = true
        });

        Assert.Equal(Layer.GameplayLayer1, player.Get<Layer>());

        // Update 2: release toggle
        engine.Update(new PlayerInputStateDTO { InputBindingSlot = 0 });
        Assert.Equal(Layer.GameplayLayer1, player.Get<Layer>());

        // Update 3: press toggle again to switch back
        engine.Update(new PlayerInputStateDTO
        {
            InputBindingSlot = 0,
            IsTogglePressed = true
        });

        Assert.Equal(Layer.GameplayLayer0, player.Get<Layer>());
    }
}
