using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Collision;
using UmbrashiftECS.GameLogic.EntityComponents.Jump;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class GroundedCheckSystemTests
{
    [Fact]
    public void GroundedStateClearsAfterLeavingTheGround()
    {
        using var world = World.Create();
        var player = world.Create(
            new Position(0, 0),
            new OffsetAabbCollider { Width = 2, Height = 2 },
            new GroundedState());
        var ground = world.Create(
            new Position(0, 2),
            new AabbCollider { Width = 4, Height = 2 },
            new SolidBody());
        var system = new GroundedCheckSystem(world);

        system.Update(1);
        Assert.True(player.Get<GroundedState>().IsGrounded);

        world.Destroy(ground);
        system.Update(2);

        Assert.False(player.Get<GroundedState>().IsGrounded);
    }
}
