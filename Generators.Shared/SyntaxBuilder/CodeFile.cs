using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 一个完整的源文件（编译单元）：using 指令 + 命名空间/类型成员 + 文件头。
/// </summary>
internal class CodeFile : TypeBuilder
{
    private CodeFile(string filename)
    {
        FileName = filename;
    }

    public static CodeFile New(string filename) => new(filename);

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Unit;

    public string FileName { get; set; }

    public IList<string> Usings { get; } = [];

    public CodeFile AddUsings(params string[] usings)
    {
        foreach (var item in usings)
        {
            Usings.Add(item);
        }
        return this;
    }
    private List<UsingDirectiveSyntax> UsingDirectives { get; set; } = [];
    public CodeFile AddUsings(params List<UsingDirectiveSyntax> usings)
    {
        UsingDirectives.AddRange(usings);
        return this;
    }

    public override SyntaxNode? Build()
    {
        var unit = SyntaxFactory.CompilationUnit(
            externs: default,
            usings: SyntaxKit.Usings(Usings, UsingDirectives),
            attributeLists: default,
            members: SyntaxFactory.List(BuildMembers()))
            .WithEndOfFileToken(SyntaxFactory.Token(SyntaxKind.EndOfFileToken));
        return SyntaxKit.WithFileHeader(unit);
    }
}
