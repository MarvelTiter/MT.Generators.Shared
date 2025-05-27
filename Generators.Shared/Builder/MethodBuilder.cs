using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generators.Shared.Builder;

internal class MethodBuilder : MethodBase<MethodBuilder>
{
    public MethodBuilder()
    {
        Modifiers = "public";
    }
    public override NodeType Type => NodeType.Method;
    public bool IsLambdaBody { get; set; }
    public bool IsAsync { get; set; }
    string Async => IsAsync ? " async " : " ";
    public string? ReturnType { get; set; } = "void";
    public string ConstructedMethodName => $"{Name}{Types}";
    public bool IsExplicit { get; set; }
    public string? ExplicitType { get; set; }
    string? InternalModifiers => IsExplicit ? "" : Modifiers;
    string? InternalExplicit => IsExplicit ? $"{ExplicitType}." : "";
    public override string ToString()
    {
        if (IsLambdaBody)
            return
$$"""
{{AttributeList}}
{{Indent}}{{InternalModifiers}}{{Async}}{{ReturnType}} {{InternalExplicit}}{{Name}}{{Types}}({{string.Join(", ", Parameters)}}){{TypeConstraints}}
{{Indent}}  => {{Body.FirstOrDefault()?.ToString().Trim()}}
""";
        else
            return
$$"""
{{AttributeList}}
{{Indent}}{{InternalModifiers}}{{Async}}{{ReturnType}} {{InternalExplicit}}{{Name}}{{Types}}({{string.Join(", ", Parameters)}}){{TypeConstraints}}
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
""";
    }
}
