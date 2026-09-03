using System;
using System.Collections.Generic;
using System.Text;

namespace Generators.Models
{
    internal class AttributeInfo(string name)
    {
        public readonly record struct AttributeArgumentInfo(object Value, string? Name = null);
        public string Name { get; set; } = name;
        public List<AttributeArgumentInfo>? Arguments { get; set; }
        public void AddArgument(params IEnumerable<(object value, string? name)> arguments)
        {
            Arguments ??= [];
            foreach (var (value, name) in arguments)
            {
                Arguments.Add(new AttributeArgumentInfo(value, name));
            }
        }
    }
}
