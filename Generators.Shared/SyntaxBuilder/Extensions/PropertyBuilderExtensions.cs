using System;
using System.Collections.Generic;

namespace Generators.Shared.SyntaxBuilder;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder PropertyName(this PropertyBuilder builder, string name)
    {
        builder.Name = name;
        return builder;
    }

    public static PropertyBuilder Readonly(this PropertyBuilder builder)
    {
        builder.CanRead = true;
        builder.CanWrite = false;
        return builder;
    }

    public static PropertyBuilder Writeonly(this PropertyBuilder builder)
    {
        builder.CanRead = false;
        builder.CanWrite = true;
        return builder;
    }

    /// <summary>表达式体属性，如 <c>public int Value =&gt; _value;</c>。</summary>
    public static PropertyBuilder Lambda(this PropertyBuilder builder, string body)
    {
        builder.IsLambdaBody = true;
        builder.Readonly();
        builder.InitializeWith(body);
        return builder;
    }

    /// <summary>带后端字段的完整属性，默认字段名是属性名首字母小写。</summary>
    public static PropertyBuilder Full(this PropertyBuilder builder, string? fieldName = null)
    {
        builder.Full = true;
        if (builder.Name is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        fieldName ??= $"{char.ToLowerInvariant(builder.Name[0])}{builder.Name.Substring(1)}";
        builder.FieldName = fieldName;
        return builder;
    }

    public static PropertyBuilder Get(this PropertyBuilder builder, Func<PropertyBuilder, IEnumerable<Statement>> body)
    {
        foreach (var item in body.Invoke(builder))
        {
            item.Parent = builder;
            builder.GetBody.Add(item);
        }
        return builder;
    }

    public static PropertyBuilder Set(this PropertyBuilder builder, Func<PropertyBuilder, IEnumerable<Statement>> body)
    {
        foreach (var item in body.Invoke(builder))
        {
            item.Parent = builder;
            builder.SetBody.Add(item);
        }
        return builder;
    }

    public static PropertyBuilder GetLambda(this PropertyBuilder builder, string body)
    {
        builder.Getter = $"get => {body}{(body.EndsWith(";") ? "" : ";")}";
        return builder;
    }

    public static PropertyBuilder SetLambda(this PropertyBuilder builder, string body)
    {
        builder.Setter = $"set => {body}{(body.EndsWith(";") ? "" : ";")}";
        return builder;
    }

    public static PropertyBuilder ExplicitFor(this PropertyBuilder builder, string interfaceType)
    {
        builder.IsExplicit = true;
        builder.ExplicitType = interfaceType;
        return builder;
    }
}
