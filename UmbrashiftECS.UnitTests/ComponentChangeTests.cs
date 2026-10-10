using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components;
using UmbrashiftECS.Components.EntityComponents.Basic;
using UmbrashiftECS.Operations;

namespace UmbrashiftECS.UnitTests;

public class ComponentChangeTests
{
    [Fact]
    public void AppliesAChangeToAValueTypeComponent()
    {
        using var world = World.Create();
        var entity = world.Create(new Position(2, 3));
        var statement = new SetFieldStatement(nameof(Position), nameof(Position.X), new ConstantExpression(3));
        statement.Execute(entity);

        Assert.Equal(new Position(3, 3), entity.Get<Position>());
    }
}
