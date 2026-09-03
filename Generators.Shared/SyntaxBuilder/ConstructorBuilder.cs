using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>构造函数构造器。</summary>
internal class ConstructorBuilder : MethodBase<ConstructorBuilder>
{
    public ConstructorBuilder()
    {
        Modifiers = "public";
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Constructor;

    public override SyntaxNode? Build()
    {
        // ConstructorDeclaration(
        //   attributeLists, modifiers, identifier, parameterList, initializer, body, expressionBody)
        return SyntaxFactory.ConstructorDeclaration(
            AttributeLists,
            ModifierList,
            SyntaxFactory.Identifier(Name ?? string.Empty),
            ParameterList,
            null,
            MethodBlock,
            null);
    }
}
