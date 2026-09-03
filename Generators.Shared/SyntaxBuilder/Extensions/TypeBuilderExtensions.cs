using Generators.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Generators.Shared.SyntaxBuilder;

internal static class TypeBuilderExtensions
{
    /// <summary>把整个文件加入编译，等价于 context.AddSource(fileName, file.ToString())。</summary>
    public static void AddSource(this SourceProductionContext context, CodeFile? codeFile)
    {
        if (codeFile is null)
        {
            return;
        }
        context.AddSource(codeFile.FileName, codeFile.ToString());
    }

    public static T AddMembers<T>(this T builder, params Node[] members) where T : TypeBuilder
    {
        foreach (var item in members)
        {
            item.Parent = builder;
            builder.Members.Add(item);
        }
        return builder;
    }

    public static T Modifiers<T>(this T builder, string modifiers) where T : MemberBuilder
    {
        builder.Modifiers = modifiers;
        return builder;
    }

    public static T Attribute<T>(this T builder, params AttributeInfo[] attributes) where T : MemberBuilder
    {
        foreach (var attribute in attributes)
        {
            builder.Attributes.Add(attribute);
        }
        return builder;
    }

    public static T Attribute<T>(this T builder, string attribute, Action<AttributeInfo>? config = null)
        where T : MemberBuilder
    {
        var attrInfo = new AttributeInfo(attribute);
        config?.Invoke(attrInfo);
        builder.Attributes.Add(attrInfo);
        return builder;
    }

    public static T AttributeIf<T>(this T builder, bool condition, params AttributeInfo[] attributes)
        where T : MemberBuilder
    {
        if (condition)
        {
            builder.Attribute(attributes);
        }
        return builder;
    }

    public static T InitializeWith<T>(this T builder, string statement) where T : MemberBuilder
    {
        builder.Initialization = statement;
        return builder;
    }

    public static T AddGeneratedCodeAttribute<T>(this T builder, Type generatorType) where T : MemberBuilder
    {
        return builder.Attribute(
            $"""global::System.CodeDom.Compiler.GeneratedCode("{generatorType.FullName}", "{generatorType.Assembly.GetName().Version}")""");
    }

    public static FieldBuilder MemberType(this FieldBuilder builder, string type)
    {
        builder.MemberType = type;
        return builder;
    }

    public static FieldBuilder MemberType(this FieldBuilder builder, string type, params TypeParameterInfo[] typeParameters)
    {
        builder.MemberType = typeParameters.Length > 0
            ? $"{type}<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>"
            : type;
        return builder;
    }

    public static PropertyBuilder MemberType(this PropertyBuilder builder, string type)
    {
        builder.MemberType = type;
        return builder;
    }

    public static PropertyBuilder MemberType(this PropertyBuilder builder, string type, params TypeParameterInfo[] typeParameters)
    {
        builder.MemberType = typeParameters.Length > 0
            ? $"{type}<{string.Join(", ", typeParameters.Select(tp => tp.Name))}>"
            : type;
        return builder;
    }

    public static FieldBuilder FieldName(this FieldBuilder builder, string name)
    {
        builder.Name = name;
        return builder;
    }
}
