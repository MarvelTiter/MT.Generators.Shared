using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>switch 语句。</summary>
internal class SwitchStatement : Statement
{
    public SwitchStatement(string switchValue)
    {
        SwitchValue = switchValue;
    }

    public SwitchStatement()
    {
    }

    public static SwitchStatement Default => new SwitchStatement();

    public string? SwitchValue { get; set; }

    public List<SwitchCaseStatement> SwitchCases { get; set; } = [];

    public DefaultCaseStatement? DefaultCase { get; set; }

    public override StatementSyntax? ToStatementSyntax()
    {
        List<SwitchSectionSyntax> sections = [];
        foreach (var item in SwitchCases)
        {
            sections.Add(item.ToSection());
        }
        if (DefaultCase is not null)
        {
            sections.Add(DefaultCase.ToSection());
        }
        return SyntaxFactory.SwitchStatement(
            SyntaxKit.ParseExpression(SwitchValue),
            SyntaxFactory.List(sections));
    }
}

/// <summary>case 分支。</summary>
internal class SwitchCaseStatement : Statement
{
    public string? Condition { get; set; }

    public List<Statement> Action { get; set; } = [];

    /// <summary>是否在分支末尾追加 break。</summary>
    public bool IsBreak { get; set; }

    internal SwitchSectionSyntax ToSection()
    {
        var labels = SyntaxFactory.SingletonList<SwitchLabelSyntax>(BuildLabel(Condition));
        var statements = new List<StatementSyntax>(SyntaxKit.Statements(Action));
        if (IsBreak)
        {
            statements.Add(SyntaxFactory.BreakStatement());
        }
        return SyntaxFactory.SwitchSection(labels, SyntaxFactory.List(statements));
    }

    /// <summary>
    /// 解析 case 标签。整体解析一段 switch 再取出标签节点，
    /// 这样常量、模式匹配（如 <c>int i</c>、<c>null</c>）都能支持。
    /// </summary>
    private static CaseSwitchLabelSyntax BuildLabel(string? condition)
    {
        var text = (condition ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return SyntaxFactory.CaseSwitchLabel(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        }
        var parsed = SyntaxFactory.ParseStatement($"switch(1){{case {text}: break;}}", 0, null, true) as SwitchStatementSyntax;
        var label = parsed?.Sections.FirstOrDefault()?.Labels.OfType<CaseSwitchLabelSyntax>().FirstOrDefault();
        if (label is not null && parsed is not null && !parsed.ContainsDiagnostics)
        {
            return label.WithoutTrivia();
        }
        return SyntaxFactory.CaseSwitchLabel(SyntaxKit.ParseExpression(text));
    }
}

/// <summary>default 分支。</summary>
internal class DefaultCaseStatement : Statement
{
    public string? Condition { get; set; }

    public string? Action { get; set; }

    internal SwitchSectionSyntax ToSection()
    {
        List<StatementSyntax> statements = [];
        var statement = SyntaxKit.ParseStatement(Action);
        if (statement is not null)
        {
            statements.Add(statement);
        }
        return SyntaxFactory.SwitchSection(
            SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()),
            SyntaxFactory.List(statements));
    }

    public static implicit operator DefaultCaseStatement(string action) => new DefaultCaseStatement { Action = action };
}
