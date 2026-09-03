using Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 语法节点种类，对应于 Roslyn 语法树的成员类别。
/// 作用与 <see cref="Builder.NodeType"/> 一致，用于容器内成员的分组排序。
/// </summary>
internal enum SyntaxNodeKind
{
    Unit,
    NameSpace,
    Class,
    Interface,
    Record,
    Constructor,
    Field,
    Property,
    Method,
    Event,
    Statement
}

/// <summary>
/// 所有构造器的基类。
/// <para>
/// 与旧 Builder 的区别：<see cref="Build"/> 产出的是 Roslyn 语法节点
/// （由 <see cref="SyntaxKit"/> 基于 SyntaxFactory 构造），而不是拼出来的字符串；
/// 缩进、换行等格式统一交给 NormalizeWhitespace 处理，因此不再需要 Indent / Level 这类手工缩进状态。
/// </para>
/// </summary>
internal abstract class Node
{
    public abstract SyntaxNodeKind Kind { get; }

    public Node? Parent { get; set; }

    /// <summary>构造当前节点对应的 Roslyn 语法节点。</summary>
    public abstract SyntaxNode? Build();

    /// <summary>
    /// 构造当前节点对应的全部语法节点。
    /// 默认只有一个，但像「带字段的完整属性」这类成员会同时产出字段和属性两个节点。
    /// </summary>
    public virtual IEnumerable<SyntaxNode> BuildNodes()
    {
        var node = Build();
        if (node is not null)
        {
            yield return node;
        }
    }

    public override string ToString() => Build()?.NormalizeWhitespace().ToFullString() ?? string.Empty;
}

/// <summary>可以容纳成员的容器（命名空间、类型、编译单元等）。</summary>
internal abstract class TypeBuilder : Node
{
    public List<Node> Members { get; } = [];

    /// <summary>按 Roslyn 语法节点的类型取出所有成员。</summary>
    protected IEnumerable<MemberDeclarationSyntax> BuildMembers()
    {
        return Members.SelectMany(m => m.BuildNodes()).OfType<MemberDeclarationSyntax>();
    }
}

internal abstract class TypeBuilder<T> : TypeBuilder where T : TypeBuilder, new()
{
    public static T Default => new T();
}

/// <summary>带名字、修饰符、特性、泛型的成员。</summary>
internal abstract class MemberBuilder : TypeBuilder
{
    public string? Name { get; set; }
    public string? Modifiers { get; set; }
    public string? MemberType { get; set; }
    public string? Initialization { get; set; }
    public IList<AttributeInfo> Attributes { get; } = [];
    public bool IsGeneric { get; set; }
    public bool IsOverride { get; set; }
    public IList<TypeParameterInfo> TypeArguments { get; set; } = [];

    protected SyntaxList<AttributeListSyntax> AttributeLists => SyntaxKit.Attributes(Attributes);

    protected TypeParameterListSyntax? TypeParameters => SyntaxKit.TypeParameterList(TypeArguments);

    protected SyntaxList<TypeParameterConstraintClauseSyntax> TypeConstraints =>
        SyntaxKit.ConstraintClauses(TypeArguments);

    protected SyntaxTokenList ModifierList => SyntaxKit.Modifiers(Modifiers, IsOverride ? "override" : null);

    /// <summary>泛型参数声明部分，如 <c>&lt;T, TResult&gt;</c>。</summary>
    protected string Types => TypeArguments.Count == 0
        ? string.Empty
        : $"<{string.Join(", ", TypeArguments.Select(t => t.Name))}>";

    protected ExplicitInterfaceSpecifierSyntax? ExplicitSpecifier =>
        string.IsNullOrWhiteSpace(ExplicitType)
            ? null
            : SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxKit.ParseName(ExplicitType));

    /// <summary>显式接口实现时的接口类型，设置后不会输出访问修饰符。</summary>
    public bool IsExplicit { get; set; }

    public string? ExplicitType { get; set; }
}

internal abstract class MemberBuilder<T> : MemberBuilder where T : MemberBuilder, new()
{
    public static T Default => new T();
}

/// <summary>带参数列表与方法体的方法类成员（方法、构造函数）。</summary>
internal abstract class MethodBase : MemberBuilder
{
    public IList<string> Parameters { get; set; } = [];
    public IList<Statement> Body { get; set; } = [];

    protected ParameterListSyntax ParameterList => SyntaxKit.ParameterList(Parameters);

    protected BlockSyntax MethodBlock => SyntaxKit.Block(Body);
}

internal abstract class MethodBase<T> : MethodBase where T : MethodBase, new()
{
    public static T Default => new T();
}
