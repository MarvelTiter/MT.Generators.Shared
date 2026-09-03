using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 命名空间构造器，支持文件作用域命名空间（<c>namespace X;</c>）与块级命名空间。
/// <para>
/// 文件头（<c>// &lt;auto-generated/&gt;</c>、<c>#pragma warning disable</c>、<c>#nullable enable</c>）
/// 由 <see cref="CodeFile"/> 统一负责；只有当命名空间被单独输出时才会补上。
/// </para>
/// </summary>
internal class NamespaceBuilder : TypeBuilder<NamespaceBuilder>
{
    public override SyntaxNodeKind Kind => SyntaxNodeKind.NameSpace;

    public string? Namespace { get; set; }

    public bool IsFileScoped { get; set; }

    public override SyntaxNode? Build()
    {
        var members = SyntaxFactory.List(BuildMembers());
        var name = SyntaxKit.ParseName(Namespace);

        MemberDeclarationSyntax declaration = IsFileScoped
            ? SyntaxFactory.FileScopedNamespaceDeclaration(
                attributeLists: default,
                modifiers: default,
                name,
                externs: default,
                usings: default,
                members)
            : SyntaxFactory.NamespaceDeclaration(
                attributeLists: default,
                modifiers: default,
                name,
                externs: default,
                usings: default,
                members);

        // 单独输出命名空间时才补文件头，避免与 CodeFile 重复
        return Parent is CodeFile ? declaration : SyntaxKit.WithFileHeader(declaration);
    }
}
