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
            if (entity.TryGet(change.ComponentType, out object component))
            {
                component = change.Apply(component);
                entity.Set(component);
            }
        }
    }
}

/// <summary>
/// Can be applied on an entity to change it.
/// </summary>
public struct EntityChangeSingleComponent<T> : IEntityChangeSingleComponent
{
    public EntityChangeSingleComponent(Func<T, T> apply)
    {
        ComponentType = typeof(T);
        Apply = (component) => apply((T)component);
    }
    public Func<object, object> Apply { get; set; }

    public Type ComponentType { get; set; }
}

public interface IEntityChangeSingleComponent
{
    public Func<object, object> Apply { get;}
    public Type ComponentType { get;}
}

public interface IEntityChangeMultiComponent
{
    public void Apply(Entity entity);
}