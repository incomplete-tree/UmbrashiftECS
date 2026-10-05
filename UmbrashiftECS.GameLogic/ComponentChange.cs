using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;

namespace UmbrashiftECS.GameLogic;

public struct EntityChangeSingleComponent : IEntityChangeMultiComponent
{
    public readonly List<IEntityChangeSingleComponent> Changes = new List<IEntityChangeSingleComponent>();

    public EntityChangeSingleComponent()
    {
    }

    public void Apply(Entity entity)
    {
        foreach (var change in Changes)
        {
            change.Apply(entity);
        }
    }
}

/// <summary>
/// Can be applied on an entity to change it.
/// </summary>
public struct EntityChangeSingleComponent<T> : IEntityChangeSingleComponent
{
    private readonly Func<T, T> _apply;

    public EntityChangeSingleComponent(Func<T, T> apply)
    {
        _apply = apply;
    }

    public void Apply(Entity entity)
    {
        if (entity.TryGet<T>(out var component))
        {
            entity.Set(_apply(component));
        }
    }
}

public interface IEntityChangeSingleComponent
{
    public void Apply(Entity entity);
}

public interface IEntityChangeMultiComponent
{
    public void Apply(Entity entity);
}
