namespace Generators.Shared.Builder;

internal class Statement : Node
{
    //public override string Indent => "            ";

    public string Content { get; private set; } = string.Empty;

    public override NodeType Type => NodeType.Statement;


    public static implicit operator Statement(string content) => new Statement() { Content = content };

    public override string ToString()
    {
        //if (!Content.Trim().EndsWith(";"))
        //{
        //    return $"{Indent}{Content};";
        //}
        return $"{Indent}{Content}{AttachSemicolon()}";
    }

    private string AttachSemicolon()
    {
        if (string.IsNullOrWhiteSpace(Content))
            return string.Empty;
        if (Content.Trim().EndsWith(";"))
            return string.Empty;
        if (Content.StartsWith("if"))
            return string.Empty;
        if (Content.StartsWith("{"))
            return string.Empty;
        if (Content.EndsWith("}"))
            return string.Empty;
        if (Content.StartsWith("#"))
            return string.Empty;
        if (Content.StartsWith("//"))
            return string.Empty;
        return ";";

    }
}
