using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 语句。既可以直接从字符串隐式转换（原文交给 <see cref="SyntaxKit.ParseStatement"/> 解析成语法节点），
/// 也可以作为 if / switch / try-catch / foreach / 本地函数等结构化语句的基类。
/// </summary>
internal class Statement : Node
{
    public string Content { get; private set; } = string.Empty;

    public override SyntaxNodeKind Kind => SyntaxNodeKind.Statement;

    public static implicit operator Statement(string content) => new Statement { Content = content };

    /// <summary>把当前语句构造成 <see cref="StatementSyntax"/>；空内容返回 null。</summary>
    public virtual StatementSyntax? ToStatementSyntax() => SyntaxKit.ParseStatement(Content);

    public override SyntaxNode? Build() => ToStatementSyntax();

    /// <summary>把内容当成表达式使用（表达式体成员、lambda 语句等场景）。</summary>
    public ExpressionSyntax ToExpression() => SyntaxKit.ParseExpression(Content);
}
