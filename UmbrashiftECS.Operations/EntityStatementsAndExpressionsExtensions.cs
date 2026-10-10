using System.Collections.Concurrent;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using UmbrashiftECS.Components;

namespace UmbrashiftECS.Operations;

public static class EntityStatementsAndExpressionsExtensions
{
    private static readonly ConcurrentDictionary<string, Type> ComponentTypes = new();
    private static readonly ConcurrentDictionary<(Type ComponentType, string FieldName), FieldInfo> Fields = new();

    public static object? Evaluate(this EntityExpression expression, Entity entity)
    {
        return expression switch
        {
            ConstantExpression constant =>
                constant.Value,

            GetFieldExpression getField =>
                GetField(entity, getField.ComponentName, getField.FieldName),

            _ => throw new NotSupportedException(
                $"Unsupported entity expression: {expression.GetType().Name}")
        };
    }

    public static void Execute(this ComponentStatement statement, Entity entity)
    {
        switch (statement)
        {
            case NullStatement:
                return;

            case IfElseStatement ifElse:
                var conditionResult = ifElse.Condition.Evaluate(entity);
                if (conditionResult is not bool conditionResultBool)
                    throw new InvalidOperationException("If else condition must always return a boolean.");
                if (conditionResultBool)
                    ifElse.IfConditionPasses.Execute(entity);
                else
                    ifElse.Else.Execute(entity);

                return;
            
            case SetFieldStatement setField:
                ExecuteSetField(setField, entity);
                return;
            
            case SequenceStatement sequenceStatement:
                foreach (var partOfSeqStatement in sequenceStatement.Statements)
                {
                    partOfSeqStatement.Execute(entity);
                }

                return;
            default:
                throw new NotImplementedException();
                return;
        }
    }

    private static void ExecuteSetField(
        SetFieldStatement statement,
        Entity entity)
    {
        var componentType = ResolveComponentType(statement.ComponentName);
        var field = ResolveField(componentType, statement.FieldName);

        var component = GetComponent(entity, componentType);

        field.SetValue(
            component,
            statement.Value.Evaluate(entity));

        SetComponent(entity, componentType, component);
    }

    private static object? GetField(
        Entity entity,
        string componentName,
        string fieldName)
    {
        var componentType = ResolveComponentType(componentName);
        var field = ResolveField(componentType, fieldName);

        var component = GetComponent(entity, componentType);

        return field.GetValue(component);
    }

    private static Type ResolveComponentType(string name) // TODO to registry, remove reflection entirely.
    {
        return ComponentTypes.GetOrAdd(name, static name =>
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var orderedAssemblies = assemblies
                .Where(a => a.GetName().Name?.StartsWith("Umbrashift") == true)
                .Concat(assemblies.Where(a => a.GetName().Name?.StartsWith("Umbrashift") != true));

            foreach (var assembly in orderedAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == name || type.FullName == name)
                        return type;
                }
            }

            throw new InvalidOperationException(
                $"Could not find component type '{name}'.");
        });
    }

    private static FieldInfo ResolveField(Type componentType, string fieldName) // TODO remove reflection
    {
        return Fields.GetOrAdd(
            (componentType, fieldName),
            static key =>
                key.ComponentType.GetField(
                    key.FieldName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                ?? key.ComponentType.GetField(
                    $"<{key.FieldName}>k__BackingField",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    $"Component '{key.ComponentType.FullName}' " +
                    $"does not contain field '{key.FieldName}'."));
    }

    private static object? GetComponent(Entity entity, Type componentType)
    {
        return entity.Get(componentType);
    }

    private static void SetComponent(
        Entity entity,
        Type componentType,
        object component)
    {
        entity.Set(component);
    }
}