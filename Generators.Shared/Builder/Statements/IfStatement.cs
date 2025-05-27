using System.Collections.Generic;

namespace Generators.Shared.Builder;

internal class IfStatement : Statement
{
    //internal static IfStatement Default(Node parent) => new IfStatement() { Parent = parent };
    internal static IfStatement Default => new IfStatement();
    public string? Condition { get; set; }
    public List<Statement> IfContents { get; set; } = [];
    public List<Statement> ElseContents { get; set; } = [];

    public override string ToString()
    {
        return
$$"""
{{Indent}}if ({{Condition}})
{{Indent}}{
{{string.Join("\n", IfContents)}}
{{Indent}}}
""";
    }
}
