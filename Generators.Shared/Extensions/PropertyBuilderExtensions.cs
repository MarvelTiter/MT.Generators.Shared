using Generators.Shared.Builder;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Generators.Shared;

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

    public static PropertyBuilder Lambda(this PropertyBuilder builder, string body)
    {
        builder.IsLambdaBody = true;
        builder.Readonly();
        builder.InitializeWith(body);
        return builder;
    }

    public static PropertyBuilder Full(this PropertyBuilder builder, string? fieldName = null)
    {
        builder.Full = true;
        if (builder.Name is null) throw new System.ArgumentNullException();
        fieldName ??= $"{builder.Name[0].ToString().ToLower()}{builder.Name.Substring(1)}";
        builder.FieldName = fieldName;
        return builder;
    }
    public static PropertyBuilder Get(this PropertyBuilder builder, Func<PropertyBuilder, IEnumerable<Statement>> body)
    {
        var b = body.Invoke(builder);
        foreach (var item in b)
        {
            item.Parent = builder;
            item.IndentFixed = 1;
            builder.GetBody.Add(item);
        }
        return builder;
    }

    public static PropertyBuilder Set(this PropertyBuilder builder, Func<PropertyBuilder, IEnumerable<Statement>> body)
    {
        var b = body.Invoke(builder);
        foreach (var item in b)
        {
            item.Parent = builder;
            item.IndentFixed = 1;
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
}
