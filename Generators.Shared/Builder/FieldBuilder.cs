using System;
using System.Collections.Generic;
using System.Text;

namespace Generators.Shared.Builder;

internal class FieldBuilder : MemberBuilder<FieldBuilder>
{
    public FieldBuilder()
    {
        Modifiers = "private readonly";
    }
    public override NodeType Type => NodeType.Field;
    //public override string Indent => "        ";
    string InitStatement => string.IsNullOrEmpty(Initialization) ? "" : $" = {Initialization}";
    public override string ToString()
    {
        var l = this.Level;
        return $"""
            {Indent}{Modifiers} {MemberType} {Name}{InitStatement};
            """;
    }
}

internal class PropertyBuilder : MemberBuilder<PropertyBuilder>
{
    public PropertyBuilder()
    {
        Modifiers = "public";
    }
    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = true;
    public override NodeType Type => NodeType.Property;
    //public override string Indent => "        ";
    public string? Getter { get; set; }
    public string? Setter { get; set; }
    public bool IsLambdaBody { get; set; }
    string InitStatement => string.IsNullOrEmpty(Initialization) ? "" : $" = {Initialization};";
    string FieldInit => string.IsNullOrEmpty(Initialization) ? ";" : $" = {Initialization};";
    public bool Full { get; set; }
    public string? FieldName { get; set; }
    public List<Statement> GetBody { get; set; } = [];
    public List<Statement> SetBody { get; set; } = [];

    string Get => Getter ?? (CanRead ? "get;" : "");
    string Set => Setter ?? (CanWrite ? "set;" : "");
    public override string ToString()
    {
        if (Full)
        {
            return $$"""
                {{Indent}}private {{MemberType}} {{FieldName}}{{FieldInit}}
                {{AttributeList}}
                {{Indent}}{{Modifiers}} {{MemberType}} {{Name}}
                {{Indent}}{
                {{Indent}}    get
                {{Indent}}    {
                {{string.Join("\n", GetBody)}}
                {{Indent}}    }
                {{Indent}}    set
                {{Indent}}    {
                {{string.Join("\n", SetBody)}} 
                {{Indent}}    }
                {{Indent}}}
                """;
        }
        else
        {

            if (IsLambdaBody)
            {
                return $$"""
                {{AttributeList}}
                {{Indent}}{{Modifiers}} {{MemberType}} {{Name}} => {{Initialization}};
                """;
            }
            else
            {
                return $$"""
                {{AttributeList}}
                {{Indent}}{{Modifiers}} {{MemberType}} {{Name}} { {{Get}} {{Set}} }{{InitStatement}}
                """;
            }
        }
    }
}
