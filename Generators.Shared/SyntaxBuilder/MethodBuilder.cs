using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 方法构造器，支持普通方法体与表达式体（lambda）两种形态。
/// </summary>
internal class MethodBuilder : MethodBase<MethodBuilder>
{
    public MethodBuilder()
    {
        Modifiers = "public";
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Method;

    public bool IsLambdaBody { get; set; }

    public bool IsAsync { get; set; }

    public string? ReturnType { get; set; } = "void";

    /// <summary>带泛型参数的方法名，如 <c>Get&lt;T&gt;</c>。</summary>
    public string ConstructedMethodName => $"{Name}{Types}";

    public override SyntaxNode? Build()
    {
        var attributeLists = IsExplicit ? SyntaxFactory.List<AttributeListSyntax>() : AttributeLists;
        var modifiers = IsExplicit
            ? SyntaxFactory.TokenList()
            : SyntaxKit.Modifiers(Modifiers, IsOverride ? "override" : null, IsAsync ? "async" : null);
        var returnType = SyntaxKit.ParseType(ReturnType ?? "void");
        var identifier = SyntaxFactory.Identifier(Name ?? string.Empty);
        var explicitSpecifier = IsExplicit ? ExplicitSpecifier : null;

        // MethodDeclaration(
        //   attributeLists, modifiers, returnType, explicitInterfaceSpecifier, identifier,
        //   typeParameterList, parameterList, constraintClauses, body, expressionBody[, semicolonToken])
        if (IsLambdaBody)
        {
            return SyntaxFactory.MethodDeclaration(
                attributeLists,
                modifiers,
                returnType,
                explicitSpecifier,
                identifier,
                TypeParameters,
                ParameterList,
                TypeConstraints,
                null,
                SyntaxFactory.ArrowExpressionClause(LambdaExpression()),
                SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        return SyntaxFactory.MethodDeclaration(
            attributeLists,
            modifiers,
            returnType,
            explicitSpecifier,
            identifier,
            TypeParameters,
            ParameterList,
            TypeConstraints,
            MethodBlock,
            null);
    }

    private ExpressionSyntax LambdaExpression()
    {
        var statement = Body.FirstOrDefault();
        return statement is null
            ? SyntaxKit.ParseExpression(string.Empty)
            : statement.ToExpression();
    }
}
