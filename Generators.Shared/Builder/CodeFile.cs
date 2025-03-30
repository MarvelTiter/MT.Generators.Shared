using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Generators.Shared.Builder;

internal class CodeFile : TypeBuilder
{
    private CodeFile(string filename) { FileName = filename; }
    public static CodeFile New(string filename) => new(filename);
    public override NodeType Type => NodeType.Unit;
    public string FileName { get; set; }
    public IList<string> Usings { get; } = [];
    public IList<string> FileHeader { get; } = [];

    public CodeFile AddFileHeader(params string[] header)
    {
        foreach (var h in header)
        {
            FileHeader.Add(h);
        }
        return this;
    }

    public CodeFile AddUsings(params string[] usings)
    {
        foreach (var us in usings)
        {
            Usings.Add(us);
        }
        return this;
    }

    public override string ToString()
    {
        return
$"""
{string.Join("\n", FileHeader)}

{string.Join("\n", Usings)}

{string.Join("\n\n", Members.Select(m => m.ToString()))}
"""
            ;
    }
}
