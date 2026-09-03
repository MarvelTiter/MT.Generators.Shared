using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 类型构造器（class / record）。
/// 成员按字段 → 事件 → 构造函数 → 属性 → 方法的顺序输出。
/// </summary>
internal class ClassBuilder : MemberBuilder<ClassBuilder>
{
    public ClassBuilder()
    {
        Modifiers = "public partial";
    }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Class;

    public string? BaseType { get; set; }

    /// <summary>类型关键字：class（默认）、record。</summary>
    public string ClassType { get; set; } = "class";

    public IList<string> Interfaces { get; } = [];

    private BaseListSyntax? BaseList
    {
        get
        {
            string?[] all = [BaseType, .. Interfaces];
            var bases = all.Where(b => !string.IsNullOrWhiteSpace(b)).ToArray();
            if (bases.Length == 0)
            {
                return null;
            }
            return SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(
                bases.Select(b => (BaseTypeSyntax)SyntaxFactory.SimpleBaseType(SyntaxKit.ParseType(b)))));
        }
    }

    private IEnumerable<MemberDeclarationSyntax> OrderedMembers()
    {
        var known = new[]
        {
            SyntaxNodeKind.Field,
            SyntaxNodeKind.Event,
            SyntaxNodeKind.Constructor,
            SyntaxNodeKind.Property,
            SyntaxNodeKind.Method,
        };
        foreach (var kind in known)
        {
            foreach (var member in Members.Where(m => m.Kind == kind).SelectMany(m => m.BuildNodes()).OfType<MemberDeclarationSyntax>())
            {
                yield return member;
            }
        }
        // 兜底：嵌套类型等未分类成员
        foreach (var member in Members.Where(m => !known.Contains(m.Kind)).SelectMany(m => m.BuildNodes()).OfType<MemberDeclarationSyntax>())
        {
            yield return member;
        }
    }

    public override SyntaxNode? Build()
    {
        var members = SyntaxFactory.List(OrderedMembers());
        var attributeLists = AttributeLists;
        var modifiers = ModifierList;
        var identifier = SyntaxFactory.Identifier(Name ?? string.Empty);

        MemberDeclarationSyntax declaration;
        if (string.Equals(ClassType, "record", StringComparison.Ordinal))
        {
            // RecordDeclaration(
            //   kind, attributeLists, modifiers, keyword, identifier,
            //   typeParameterList, parameterList, baseList, constraintClauses, members)
            declaration = SyntaxFactory.RecordDeclaration(
                SyntaxKind.RecordDeclaration,
                attributeLists,
                modifiers,
                SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                identifier,
                TypeParameters,
                null,
                BaseList,
                TypeConstraints,
                members);
        }
        else
        {
            // ClassDeclaration(
            //   attributeLists, modifiers, identifier,
            //   typeParameterList, baseList, constraintClauses, members)
            declaration = SyntaxFactory.ClassDeclaration(
                attributeLists,
                modifiers,
                identifier,
                TypeParameters,
                BaseList,
                TypeConstraints,
                members);
        }
        return SyntaxKit.WithDocComment(declaration, "/// <inheritdoc/>");
    }
}
