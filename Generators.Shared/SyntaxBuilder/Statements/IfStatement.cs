using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>if 语句，支持 else 分支（旧 Builder 会丢弃 else 内容，这里补上）。</summary>
internal class IfStatement : Statement
{
    public static IfStatement Default => new IfStatement();

    public string? Condition { get; set; }

    public List<Statement> IfContents { get; set; } = [];

    public List<Statement> ElseContents { get; set; } = [];

    public override StatementSyntax? ToStatementSyntax()
    {
        ElseClauseSyntax? elseClause = ElseContents.Count > 0
            ? SyntaxFactory.ElseClause(SyntaxKit.Block(ElseContents))
            : null;
        return SyntaxFactory.IfStatement(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxKit.ParseExpression(Condition),
            SyntaxKit.Block(IfContents),
            elseClause);
    }
}
