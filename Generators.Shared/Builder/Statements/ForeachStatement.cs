using System.Collections.Generic;

namespace Generators.Shared.Builder;

internal class ForeachStatement : Statement
{
    public static ForeachStatement Default => new ForeachStatement();
    public List<Statement> Contents { get; set; } = [];
    public string? LoopContent { get; set; }
    public override string ToString()
    {
        return
$$"""
{{Indent}}foreach({{LoopContent}})
{{Indent}}{
{{string.Join("\n", Contents)}}
{{Indent}}}
""";
    }
}
