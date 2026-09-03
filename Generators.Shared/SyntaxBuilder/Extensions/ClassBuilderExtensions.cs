namespace Generators.Shared.SyntaxBuilder;

internal static class ClassBuilderExtensions
{
    public static ClassBuilder ClassName(this ClassBuilder builder, string className)
    {
        builder.Name = className;
        return builder;
    }

    public static ClassBuilder MakeRecord(this ClassBuilder builder)
    {
        builder.ClassType = "record";
        return builder;
    }

    public static ClassBuilder BaseType(this ClassBuilder builder, string baseType)
    {
        builder.BaseType = baseType;
        return builder;
    }

    public static ClassBuilder Interface(this ClassBuilder builder, params string[] interfaces)
    {
        foreach (var item in interfaces)
        {
            builder.Interfaces.Add(item);
        }
        return builder;
    }

    public static ClassBuilder InterfaceIf(this ClassBuilder builder, bool condition, params string[] interfaces)
    {
        if (!condition)
        {
            return builder;
        }
        foreach (var item in interfaces)
        {
            builder.Interfaces.Add(item);
        }
        return builder;
    }
}
