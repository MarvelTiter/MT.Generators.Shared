using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 字段构造器。
/// <see cref="MemberBuilder.MemberType"/> 以 event 开头时（如 <c>event Action?</c>）会自动生成事件字段。
/// </summary>
internal class FieldBuilder : MemberBuilder<FieldBuilder>
{
    private const string EventPrefix = "event ";

    public FieldBuilder()
    {
        Modifiers = "private readonly";
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Field;

    public override SyntaxNode? Build()
    {
        var typeText = (MemberType ?? string.Empty).Trim();
        var isEvent = typeText.StartsWith(EventPrefix, System.StringComparison.Ordinal);
        if (isEvent)
        {
            typeText = typeText.Substring(EventPrefix.Length).Trim();
        }
        var declarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(Name ?? string.Empty));
        if (!string.IsNullOrWhiteSpace(Initialization))
        {
            declarator = declarator.WithInitializer(
                SyntaxFactory.EqualsValueClause(SyntaxKit.ParseExpression(Initialization)));
        }
        var declaration = SyntaxFactory.VariableDeclaration(
            SyntaxKit.ParseType(typeText),
            SyntaxFactory.SingletonSeparatedList(declarator));

        return isEvent
            ? SyntaxFactory.EventFieldDeclaration(AttributeLists, ModifierList, declaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            : SyntaxFactory.FieldDeclaration(AttributeLists, ModifierList, declaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }
}

/// <summary>
/// 属性构造器，支持自动属性、表达式体属性、以及带后端字段的完整属性。
/// </summary>
internal class PropertyBuilder : MemberBuilder<PropertyBuilder>
{
    public PropertyBuilder()
    {
        Modifiers = "public";
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Property;

    public bool CanRead { get; set; } = true;

    public bool CanWrite { get; set; } = true;

    /// <summary>自定义 get 访问器原文，如 <c>get =&gt; _value;</c>。</summary>
    public string? Getter { get; set; }

    /// <summary>自定义 set 访问器原文，如 <c>set =&gt; _value = value;</c>。</summary>
    public string? Setter { get; set; }

    public bool IsLambdaBody { get; set; }

    /// <summary>是否为带后端字段的完整属性。</summary>
    public bool Full { get; set; }

    public string? FieldName { get; set; }

    public List<Statement> GetBody { get; set; } = [];

    public List<Statement> SetBody { get; set; } = [];

    public override IEnumerable<SyntaxNode> BuildNodes()
    {
        if (Full)
        {
            yield return BuildBackingField();
        }
        yield return BuildProperty();
    }

    public override SyntaxNode? Build() => BuildProperty();

    private FieldDeclarationSyntax BuildBackingField()
    {
        var declarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(FieldName ?? string.Empty));
        if (!string.IsNullOrWhiteSpace(Initialization))
        {
            declarator = declarator.WithInitializer(
                SyntaxFactory.EqualsValueClause(SyntaxKit.ParseExpression(Initialization)));
        }
        return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxKit.ParseType(MemberType),
                SyntaxFactory.SingletonSeparatedList(declarator)))
            .WithModifiers(SyntaxKit.Modifiers("private"))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private PropertyDeclarationSyntax BuildProperty()
    {
        var attributeLists = IsExplicit ? default : AttributeLists;
        var modifiers = IsExplicit ? default : ModifierList;
        var explicitSpecifier = IsExplicit ? ExplicitSpecifier : null;
        var identifier = SyntaxFactory.Identifier(Name ?? string.Empty);
        var type = SyntaxKit.ParseType(MemberType);

        if (IsLambdaBody)
        {
            // PropertyDeclaration(
            //   attributeLists, modifiers, type, explicitInterfaceSpecifier, identifier,
            //   accessorList, expressionBody, initializer, semicolonToken)
            return SyntaxFactory.PropertyDeclaration(
                attributeLists,
                modifiers,
                type,
                explicitSpecifier,
                identifier,
                null,
                SyntaxFactory.ArrowExpressionClause(SyntaxKit.ParseExpression(Initialization)),
                null,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        var initializer = string.IsNullOrWhiteSpace(Initialization) || Full
            ? null
            : SyntaxFactory.EqualsValueClause(SyntaxKit.ParseExpression(Initialization));

        return SyntaxFactory.PropertyDeclaration(
            attributeLists,
            modifiers,
            type,
            explicitSpecifier,
            identifier,
            BuildAccessorList(),
            null,
            initializer);
    }

    private AccessorListSyntax? BuildAccessorList()
    {
        if (Full)
        {
            List<AccessorDeclarationSyntax> fullAccessors = [];
            if (CanRead)
            {
                fullAccessors.Add(SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration, SyntaxKit.Block(GetBody)));
            }
            if (CanWrite)
            {
                fullAccessors.Add(SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.SetAccessorDeclaration, SyntaxKit.Block(SetBody)));
            }
            return fullAccessors.Count == 0 ? null : SyntaxFactory.AccessorList(SyntaxFactory.List(fullAccessors));
        }

        if (!string.IsNullOrWhiteSpace(Getter) || !string.IsNullOrWhiteSpace(Setter))
        {
            // 通过整体解析还原访问器，get => x; / set { ... } 等任意写法都能支持
            var parsed = SyntaxFactory.ParseMemberDeclaration(
                $"{MemberType} __Property__ {{ {Getter} {Setter} }}") as PropertyDeclarationSyntax;
            if (parsed?.AccessorList is { } accessorList && !parsed.ContainsDiagnostics)
            {
                return accessorList.WithoutTrivia();
            }
        }

        List<AccessorDeclarationSyntax> accessors = [];
        if (CanRead)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }
        if (CanWrite)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }
        return accessors.Count == 0 ? null : SyntaxFactory.AccessorList(SyntaxFactory.List(accessors));
    }
}
