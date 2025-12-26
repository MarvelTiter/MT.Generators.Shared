using Microsoft.CodeAnalysis;
using System;
using System.Reflection;

internal class DiagnosticCall<T>
{
    public static Diagnostic GetDiagnostic<TG>(string name, Location? location, string? message = null, Exception? exception = null)
    {
        var method = typeof(T).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
        if (method is not null)
        {
            return (Diagnostic)method.Invoke(null, [location]);
        }
        else
        {
            return Diagnostic.Create(new DiagnosticDescriptor(
                id: name,
                title: message ?? exception?.Message ?? "Î´Öª´íÎó",
                messageFormat: message ?? exception?.Message ?? "Î´Öª´íÎó",
                category: typeof(TG).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true), location);
        }
    }
}