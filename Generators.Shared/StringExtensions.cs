using System.Collections.Generic;

namespace Generators.Shared;

internal static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static string Join(this IEnumerable<string> strings, string separator)
    {
        return string.Join(separator, strings);
    }
}