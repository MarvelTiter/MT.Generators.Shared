using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// foreach 语句。
/// <see cref="LoopContent"/> 沿用 "T item in source" 的紧凑写法，
/// 内部先整体解析，再替换循环体，保证类型、变量名、表达式都是真实语法节点。
/// </summary>
internal class ForeachStatement : Statement
{
    public static ForeachStatement Default => new ForeachStatement();

    public List<Statement> Contents { get; set; } = [];

    public string? LoopContent { get; set; }

    public override StatementSyntax? ToStatementSyntax()
    {
        var body = SyntaxKit.Block(Contents);
        var text = (LoopContent ?? string.Empty).Trim();

        var parsed = SyntaxFactory.ParseStatement($"foreach({text}) {{ }}", 0, null, true) as ForEachStatementSyntax;
        if (parsed is not null && !parsed.ContainsDiagnostics && !parsed.ContainsSkippedText)
        {
            return parsed
                .WithExpression(parsed.Expression.WithoutTrivia())
                .WithStatement(body);
        }
        return Fallback(text, body);
    }

    private static StatementSyntax Fallback(string text, BlockSyntax body)
    {
        const string InKeyword = " in ";
        var index = text.IndexOf(InKeyword);
        if (index <= 0)
        {
            return SyntaxFactory.ForEachStatement(
                SyntaxKit.ParseType("var"),
                SyntaxFactory.Identifier("item"),
                SyntaxKit.ParseExpression(text),
                body);
        }
        var left = text.Substring(0, index).Trim();
        var right = text.Substring(index + InKeyword.Length).Trim();
        var split = left.LastIndexOf(' ');
        var type = split > 0 ? left.Substring(0, split) : left;
        var name = split > 0 ? left.Substring(split + 1).Trim() : "item";
        return SyntaxFactory.ForEachStatement(
            SyntaxKit.ParseType(type),
            SyntaxFactory.Identifier(name),
            SyntaxKit.ParseExpression(right),
            body);
    }
}
