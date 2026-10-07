using System.Linq.Expressions;

namespace UmbrashiftECS.Components;

public abstract record EntityExpression;

public record ConstantExpression(object? Value) : EntityExpression;

public record GetFieldExpression(string ComponentName, string FieldName) : EntityExpression;


public abstract record ComponentStatement;

public record NullStatement() : ComponentStatement;

public record IfElseStatement(
    EntityExpression Condition,
    ComponentStatement IfConditionPasses,
    ComponentStatement Else)
    : ComponentStatement;

public record SetFieldStatement(string ComponentName, string FieldName, EntityExpression Value) : ComponentStatement;