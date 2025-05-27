using System.Collections.Generic;

namespace Generators.Shared.Builder;

internal class LocalFunction : Statement
{
    //public static LocalFunction Default(Node parent) => new LocalFunction() { Parent = parent };
    public static LocalFunction Default => new LocalFunction();
    public string? ReturnType { get; set; }
    public string? Name { get; set; }
    public bool IsAsync { get; set; }
    string Async => IsAsync ? "async " : "";
    public List<string> Parameters { get; set; } = [];
    public List<Statement> Body { get; set; } = [];
    public override string ToString()
    {
        return
$$"""
{{Indent}}{{Async}}{{ReturnType}} {{Name}}({{string.Join(", ", Parameters)}})
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
""";
    }
}
