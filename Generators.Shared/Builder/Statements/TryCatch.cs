using System.Collections.Generic;

namespace Generators.Shared.Builder;

internal class TryCatch : Statement
{
    public static TryCatch Default => new TryCatch();
    public List<Statement> Body { get; set; } = [];
    public List<Statement> Finally { get; set; } = [];
    public List<TryCatchException> Catchs { get; set; } = [];
    public override string ToString()
    {
        if (Finally.Count == 0 && Catchs.Count == 0) throw new System.Exception("catch块和finally块不能同时为空");
        Catchs.ForEach(c => c.Parent = Parent);
        if (Finally.Count > 0)
            return
$$"""
{{Indent}}try
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
{{string.Join("\n", Catchs)}}
{{Indent}}finally
{{Indent}}{
{{string.Join("\n", Finally)}}
{{Indent}}}
""";
        else
            return
$$"""
{{Indent}}try
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
{{string.Join("\n", Catchs)}}
""";
    }
}




internal class TryCatchException : Statement
{
    public static TryCatchException Default => new TryCatchException();
    public string? Exception { get; set; }
    public List<Statement> Body { get; set; } = [];
    string ExceptionStr => string.IsNullOrEmpty(Exception) ? "" : $"({Exception})";
    public override string ToString()
    {
        return
$$"""
{{Indent}}catch{{ExceptionStr}}
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
""";
    }
}
