using System;
using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Components.EntityComponents.Collision;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Crouch;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
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

        Assert.Equal(new OffsetAabbCollider
        {
            Width = 20,
            Height = 26,
            OffsetX = -12,
            OffsetY = 10
        }, player.Get<OffsetAabbCollider>());

        player.Set(new InputState());
        system.Update(2);

        Assert.Equal(new OffsetAabbCollider { Width = 8, Height = 36 },
            player.Get<OffsetAabbCollider>());
        Assert.False(player.Get<CrouchState>().IsCrouched);
    }

    [Fact]
    public void AirbornePlayersCannotCrouch()
    {
        using var world = World.Create();
        var player = CreatePlayer(world);
        player.Set(new GroundedState { IsGrounded = false });
        player.Set(new InputState { IsCrouchedPressed = true });

        new CrouchSystem(world).Update(1);

        Assert.False(player.Get<CrouchState>().IsCrouched);
        Assert.Equal(new OffsetAabbCollider { Width = 8, Height = 36 },
            player.Get<OffsetAabbCollider>());
    }

    [Fact]
    public void ALowCeilingPreventsStandingUp()
    {
        using var world = World.Create();
        var player = CreatePlayer(world);
        var system = new CrouchSystem(world);

        player.Set(new InputState { IsCrouchedPressed = true });
        system.Update(1);
        world.Create(
            new Position(0, 0),
            new SolidBody(),
            new AabbCollider { Width = 40, Height = 10 });

        player.Set(new InputState());
        system.Update(2);

        Assert.True(player.Get<CrouchState>().IsCrouched);
        Assert.Equal(26, player.Get<OffsetAabbCollider>().Height);
    }

    private static Entity CreatePlayer(World world) => world.Create(
        new Position(0, 0),
        new OffsetAabbCollider { Width = 8, Height = 36 },
        new InputState(),
        CrouchConfigForTests,
        new GroundedState { IsGrounded = true },
        new CrouchState());

    private static CrouchConfig CrouchConfigForTests => new(
        new OffsetAabbCollider { Width = 8, Height = 36 });
}

public class LayerToggleSystemTests
{
    [Fact]
    public void AReleaseTogglesOnceAndConsumesTheBufferedRelease()
    {
        using var world = World.Create();
        var player = CreateLayerPlayer(world);
        var system = new LayerToggleSystem(world);

        system.Update(1);
        system.Update(2);

        Assert.Equal(Layer.GameplayLayer1, player.Get<Layer>());
        Assert.True(player.Get<InputBuffer>().LastTogglePressHandled);
    }

    [Fact]
    public void NoToggleOccursWithoutAPress()
    {
        using var world = World.Create();
        var player = world.Create(
            new Position(0, 0),
            new OffsetAabbCollider { Width = 2, Height = 2 },
            new InputBuffer { BufferFrames = 5 },
            Layer.GameplayLayer0);

        new LayerToggleSystem(world).Update(1);

        Assert.Equal(Layer.GameplayLayer0, player.Get<Layer>());
    }

    [Fact]
    public void ToggleMovesThePlayerToTheFirstFreeOffsetInTheNewLayer()
    {
        using var world = World.Create();
        var player = CreateLayerPlayer(world, new Position(10, 20));
        world.Create(
            new Position(7, 17),
            new AabbCollider { Width = 6, Height = 6 },
            new SolidBody(),
            Layer.GameplayLayer1);

        new LayerToggleSystem(world).Update(1);

        Assert.Equal(Layer.GameplayLayer1, player.Get<Layer>());
        Assert.Equal(new Position(13, 20), player.Get<Position>());
    }

    [Fact]
    public void ToggleRemainsBufferedWhenEveryOffsetIsBlocked()
    {
        using var world = World.Create();
        var player = CreateLayerPlayer(world);
        world.Create(
            new Position(-10, -10),
            new AabbCollider { Width = 30, Height = 30 },
            new SolidBody(),
            Layer.GameplayLayer1);

        new LayerToggleSystem(world).Update(1);

        Assert.Equal(Layer.GameplayLayer0, player.Get<Layer>());
        Assert.False(player.Get<InputBuffer>().LastTogglePressHandled);
    }

    private static Entity CreateLayerPlayer(World world, Position position = default) => world.Create(
        position,
        new OffsetAabbCollider { Width = 2, Height = 2 },
        new InputBuffer
        {
            BufferFrames = 5,
            LastTogglePressedFrame = 1,
            LastTogglePressHandled = false
        },
        Layer.GameplayLayer0);
}

internal interface IEntityChangeMultiComponent
{
    void Apply(Entity entity);
}

internal sealed class EntityAction : IEntityChangeMultiComponent
{
    private readonly Action<Entity> _apply;

    public EntityAction(Action<Entity> apply)
    {
        _apply = apply;
    }

    public void Apply(Entity entity) => _apply(entity);
}
