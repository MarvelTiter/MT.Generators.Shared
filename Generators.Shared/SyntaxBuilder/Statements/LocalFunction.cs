using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>本地函数。</summary>
internal class LocalFunction : Statement
{
    public static LocalFunction Default => new LocalFunction();

    public string? ReturnType { get; set; }

    public string? Name { get; set; }

    public bool IsAsync { get; set; }

    public List<string> Parameters { get; set; } = [];

    public List<Statement> Body { get; set; } = [];

    public override StatementSyntax? ToStatementSyntax()
    {
        // LocalFunctionStatement(
        //   attributeLists, modifiers, returnType, identifier,
        //   typeParameterList, parameterList, constraintClauses, body, expressionBody)
        return SyntaxFactory.LocalFunctionStatement(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxKit.Modifiers(IsAsync ? "async" : null),
            SyntaxKit.ParseType(ReturnType ?? "void"),
            SyntaxFactory.Identifier(Name ?? string.Empty),
            null,
            SyntaxKit.ParameterList(Parameters),
            SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
            SyntaxKit.Block(Body),
            null);
    }
}
