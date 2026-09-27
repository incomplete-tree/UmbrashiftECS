using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic.EntityComponents.Actors;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;
using UmbrashiftECS.GameLogic.EntityComponents.Movement;
using UmbrashiftECS.GameLogic.Systems;

namespace UmbrashiftECS.UnitTests;

public class MovementSystemTests
{
    [Fact]
    public void FirstMovementAddsAndUsesFractionalRemainder()
    {
        using var world = World.Create();
        var entity = world.Create(
            new Position(0, 0),
            new MovementDelta { X = 0.5f },
            new FractionalPositionRemainder());
        var system = new MovementSystem(world);

        system.Update(1);
        system.Update(2);

        Assert.Equal(new Position(1, 0), entity.Get<Position>());
        Assert.True(entity.Has<FractionalPositionRemainder>());
    }
}
