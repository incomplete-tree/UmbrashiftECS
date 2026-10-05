using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.GameLogic;
using UmbrashiftECS.GameLogic.EntityComponents.Basic;

namespace UmbrashiftECS.UnitTests;

public class ComponentChangeTests
{
    [Fact]
    public void AppliesAChangeToAValueTypeComponent()
    {
        using var world = World.Create();
        var entity = world.Create(new Position(2, 3));
        var changes = new EntityChangeSingleComponent();
        changes.Changes.Add(new EntityChangeSingleComponent<Position>(position =>
            new Position(position.X + 1, position.Y - 1)));

        changes.Apply(entity);

        Assert.Equal(new Position(3, 2), entity.Get<Position>());
    }
}
