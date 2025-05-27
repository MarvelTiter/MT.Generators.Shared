using System.Collections.Generic;

namespace Generators.Shared.Builder;

internal class SwitchStatement : Statement
{
    public SwitchStatement(string switchValue)
    {
        SwitchValue = switchValue;
    }
    public SwitchStatement()
    {

    }
    //public static SwitchStatement Default(Node parent) => new SwitchStatement() { Parent = parent };
    public static SwitchStatement Default => new SwitchStatement();

    public string? SwitchValue { get; set; }
    public List<SwitchCaseStatement> SwitchCases { get; set; } = [];
    public DefaultCaseStatement? DefaultCase { get; set; }
    public override string ToString()
    {
        return
$$"""
{{Indent}}switch ({{SwitchValue}})
{{Indent}}{
{{string.Join("\n", SwitchCases)}}
{{DefaultCase}}
{{Indent}}}
""";
    }
}

internal class SwitchCaseStatement : Statement
{
    public string? Condition { get; set; }
    public List<Statement> Action { get; set; } = [];
    public bool IsBreak { get; set; }
    public override string ToString()
    {
        Action.ForEach(a => a.IndentFixed = 1);
        if (IsBreak)
        {
            return
$"""
{Indent}    case {Condition}:
{string.Join("\n", Action)}
{Indent}        break;
""";
        }
        else
        {
            return
$"""
{Indent}    case {Condition}:
{string.Join("\n", Action)}
""";
        }
    }
}



internal class DefaultCaseStatement : Statement
{
    public string? Condition { get; set; }
    public string? Action { get; set; }
    public override string ToString()
    {
        return
$"""
{Indent}    default:
{Indent}        {Action};
""";
    }
    public static implicit operator DefaultCaseStatement(string action) => new DefaultCaseStatement { Action = action };
}
