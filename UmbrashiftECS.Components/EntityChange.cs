using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace UmbrashiftECS.Components;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConstantExpression), "constant")]
[JsonDerivedType(typeof(GetFieldExpression), "getField")]
public abstract record EntityExpression;

public record ConstantExpression(ConstantValue Value) : EntityExpression;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(IntValue), "int")]
[JsonDerivedType(typeof(FloatValue), "float")]
[JsonDerivedType(typeof(BoolValue), "bool")]
[JsonDerivedType(typeof(StringValue), "string")]
[JsonDerivedType(typeof(NullValue), "null")]
public abstract record ConstantValue;

public sealed record IntValue(int Value) : ConstantValue;
public sealed record FloatValue(float Value) : ConstantValue;
public sealed record BoolValue(bool Value) : ConstantValue;
public sealed record StringValue(string Value) : ConstantValue;
public sealed record NullValue() : ConstantValue;


public record GetFieldExpression(string ComponentName, string FieldName) : EntityExpression;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NullStatement), "null")]
[JsonDerivedType(typeof(IfElseStatement), "ifElse")]
[JsonDerivedType(typeof(SetFieldStatement), "setField")]
[JsonDerivedType(typeof(SequenceStatement), "sequence")]
[JsonDerivedType(typeof(AddComponentStatement), "addComponent")]
public abstract record ComponentStatement;

public record NullStatement() : ComponentStatement;

public record IfElseStatement(
    EntityExpression Condition,
    ComponentStatement IfConditionPasses,
    ComponentStatement Else)
    : ComponentStatement;

public record SetFieldStatement(string ComponentName, string FieldName, EntityExpression Value) : ComponentStatement;

public record SequenceStatement(params ComponentStatement[] Statements) : ComponentStatement
{
    public virtual bool Equals(SequenceStatement? other) =>
        other is not null
        && base.Equals(other)
        && Statements.SequenceEqual(other.Statements);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var componentStatement in Statements)
        {
            hash.Add(componentStatement);
        }

        return hash.ToHashCode();
    }
}

public record AddComponentStatement(string ComponentName) : ComponentStatement;