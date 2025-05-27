namespace Generators.Shared.Builder;

internal class ConstructorBuilder : MethodBase<ConstructorBuilder>
{
    public ConstructorBuilder()
    {
        Modifiers = "public";
    }
    public override NodeType Type => NodeType.Constructor;
    public override string ToString()
    {
        return
$$"""
{{Indent}}{{Modifiers}} {{Name}}({{string.Join(", ", Parameters)}})
{{Indent}}{
{{string.Join("\n", Body)}}
{{Indent}}}
""";
    }
}
