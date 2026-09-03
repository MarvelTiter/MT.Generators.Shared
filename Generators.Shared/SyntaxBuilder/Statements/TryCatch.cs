using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>try / catch / finally 语句。</summary>
internal class TryCatch : Statement
{
    public static TryCatch Default => new TryCatch();

    public List<Statement> Body { get; set; } = [];

    public List<Statement> Finally { get; set; } = [];

    public List<TryCatchException> Catchs { get; set; } = [];

    public override StatementSyntax? ToStatementSyntax()
    {
        if (Finally.Count == 0 && Catchs.Count == 0)
        {
            throw new Exception("catch块和finally块不能同时为空");
        }
        FinallyClauseSyntax? finallyClause = Finally.Count > 0
            ? SyntaxFactory.FinallyClause(SyntaxKit.Block(Finally))
            : null;
        // TryStatement(attributeLists, block, catches, finally)
        return SyntaxFactory.TryStatement(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxKit.Block(Body),
            SyntaxFactory.List(Catchs.Select(c => c.ToCatchClause())),
            finallyClause);
    }
}

/// <summary>catch 子句。</summary>
internal class TryCatchException : Statement
{
    public static TryCatchException Default => new TryCatchException();

    /// <summary>异常声明原文，如 <c>Exception ex</c>、<c>Exception</c>；为空表示捕获全部。</summary>
    public string? Exception { get; set; }

    public List<Statement> Body { get; set; } = [];

    internal CatchClauseSyntax ToCatchClause()
    {
        // CatchClause(declaration, filter, block)
        return SyntaxFactory.CatchClause(BuildDeclaration(Exception), null, SyntaxKit.Block(Body));
    }

    /// <summary>
    /// 解析异常声明。整体解析一段 try/catch 再取出声明节点，
    /// 能够正确处理带变量名、泛型异常类型、仅类型名等写法。
    /// </summary>
    private static CatchDeclarationSyntax? BuildDeclaration(string? exception)
    {
        var text = (exception ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return null;
        }
        var parsed = SyntaxFactory.ParseStatement($"try {{ }} catch({text}) {{ }}", 0, null, true) as TryStatementSyntax;
        var declaration = parsed?.Catches.FirstOrDefault()?.Declaration;
        if (declaration is not null && !declaration.ContainsDiagnostics)
        {
            return declaration.WithoutTrivia();
        }
        return SyntaxFactory.CatchDeclaration(SyntaxKit.ParseType(text));
    }
}
