using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Crouch;
using UmbrashiftECS.GameLogic.EntityComponents.Dash;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.EntityComponents.PlayerInput;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class CrouchSystemTests
{
    [Fact]
    public void CrouchingChangesTheColliderAndStandingRestoresIt()
    {
        using var world = World.Create();
        var player = CreatePlayer(world);
        var system = new CrouchSystem(world);
        player.Set(new InputState { IsCrouchedPressed = true });

        system.Update(1);
        Assert.Equal(20, player.Get<OffsetAabbCollider>().Width);
        Assert.Equal(26, player.Get<OffsetAabbCollider>().Height);
        Assert.Equal(-12, player.Get<OffsetAabbCollider>().OffsetX);
        Assert.Equal(10, player.Get<OffsetAabbCollider>().OffsetY);

        player.Set(new InputState());
        system.Update(2);
        Assert.Equal(8, player.Get<OffsetAabbCollider>().Width);
        Assert.Equal(36, player.Get<OffsetAabbCollider>().Height);
        Assert.False(player.Get<CrouchState>().IsCrouched);
    }

    [Fact]
    public void CrouchingRemainsWhenThereIsNoStandingClearance()
    {
        using var world = World.Create();
        var player = CreatePlayer(world);
        world.Create(new Position(0, 0), new SolidBody(), new AabbCollider { Width = 20, Height = 10 });
        var system = new CrouchSystem(world);
        player.Set(new InputState { IsCrouchedPressed = true });
        system.Update(1);
        player.Set(new InputState());
        system.Update(2);

        Assert.True(player.Get<CrouchState>().IsCrouched);
        Assert.Equal(20, player.Get<OffsetAabbCollider>().Width);
    }

    private static Entity CreatePlayer(World world) => world.Create(
        new Position(0, 0),
        new OffsetAabbCollider { Width = 8, Height = 36 },
        new InputState(),
        CrouchConfig.Default,
        new GroundedState { IsGrounded = true },
        new CrouchState());
}

public class LayerToggleSystemTests
{
    [Fact]
    public void ToggleChangesTheGameplayLayerOncePerPress()
    {
        using var world = World.Create();
        var player = world.Create(
            new Position(0, 0),
            new InputBuffer { LastTogglePressedFrame = 1 },
            Layer.GameplayLayer0);
        var system = new LayerToggleSystem(world);

        system.Update(1);
        system.Update(2);

        Assert.Equal(Layer.GameplayLayer1, player.Get<Layer>());
        Assert.True(player.Get<InputBuffer>().LastTogglePressHandled);
    }

    [Fact]
    public void AnExpiredTogglePressIsIgnored()
    {
        using var world = World.Create();
        var player = world.Create(
            new Position(0, 0),
            new InputBuffer { LastTogglePressedFrame = 1 },
            Layer.GameplayLayer0);

        new LayerToggleSystem(world).Update(InputBuffer.BufferFrames + 1);

        Assert.Equal(Layer.GameplayLayer0, player.Get<Layer>());
        Assert.False(player.Get<InputBuffer>().LastTogglePressHandled);
    }

    [Fact]
    public void LayerToggleCanPushThePlayerThreePixelsOutOfTheNewLayer()
    {
        using var world = World.Create();
        var player = world.Create(
            new Position(0, 0),
            new OffsetAabbCollider { Width = 2, Height = 2 },
            new InputBuffer { LastTogglePressedFrame = 1 },
            Layer.GameplayLayer0);
        world.Create(
            new Position(-3, -3),
            new AabbCollider { Width = 6, Height = 12 },
            new SolidBody(),
            Layer.GameplayLayer1);

        new LayerToggleSystem(world).Update(1);

        Assert.Equal(Layer.GameplayLayer1, player.Get<Layer>());
        Assert.Equal(new Position(3, 0), player.Get<Position>());
    }
}
