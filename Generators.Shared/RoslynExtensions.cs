using Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Generators.Shared;

internal static class RoslynExtensions
{
    /// <summary>
    /// 获取指定了名称的参数的值
    /// </summary>
    /// <param name="a"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static object? GetNamedValue(this AttributeData? a, string key)
    {
        if (a == null) return null;
        var namedValue = a.NamedArguments.FirstOrDefault(t => t.Key == key).Value;
        if (namedValue.IsNull) return null;
        return namedValue.Value;
    }
    /// <summary>
    /// 获取指定了名称的参数的值
    /// </summary>
    /// <param name="a"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static object?[] GetNamedValues(this AttributeData? a, string key)
    {
        if (a == null) return [];
        var namedValue = a.NamedArguments.FirstOrDefault(t => t.Key == key).Value;
        if (namedValue.IsNull) return [];
        return [.. namedValue.Values.Select(v => v.Value)];
    }
    /// <summary>
    /// 获取指定了名称的参数的值
    /// </summary>
    /// <param name="a"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool GetNamedValue(this AttributeData? a, string key, out object? value)
    {
        var t = a.GetNamedValue(key);
        value = t;
        return t != null;
    }
    public static bool GetNamedValue<T>(this AttributeData? a, string key, out T? value)
    {
        var b = GetNamedValue(a, key, out var obj);
        if (b)
        {
            value = (T)obj!;
        }
        else
        {
            value = default;
        }
        return b;
    }
    /// <summary>
    /// 获取指定索引的构造函数参数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool GetConstructorValue(this AttributeData a, int index, out object? value)
    {
        if (a.ConstructorArguments.Length <= index)
        {
            value = null;
            return false;
        }
        value = a.ConstructorArguments[index].Value;
        return true;
    }
    /// <summary>
    /// 获取指定索引的构造函数参数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="index"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public static bool GetConstructorValues(this AttributeData a, int index, out object?[] values)
    {
        if (a.ConstructorArguments.Length <= index)
        {
            values = [];
            return false;
        }
        values = [.. a.ConstructorArguments[index].Values.Select(v => v.Value)];
        return true;
    }
    /// <summary>
    /// 根据名称获取attribute的值
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="fullName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static bool GetAttribute(this ISymbol? symbol, string fullName, out AttributeData? data, bool isInherited = false)
    {
        data = symbol?.GetAttributes(fullName, isInherited).FirstOrDefault();
        return data != null;
    }
    /// <summary>
    /// 根据名称获取attribute的值
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="fullName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static IEnumerable<AttributeData> GetAttributes(this ISymbol? symbol, string fullName, bool isInherited = false)
    {
        foreach (var item in symbol?.GetAttributes() ?? [])
        {
            if (item.AttributeClass?.ToDisplayString() == fullName)
            {
                yield return item;
            }
            if (isInherited && CheckBaseAttribute(item.AttributeClass, fullName))
            {
                yield return item;
            }
        }
    }
    /// <summary>
    /// 根据名称判定是否拥有某个attribute
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="fullName"></param>
    /// <param name="isInherited">是否检查继承关系</param>
    /// <returns></returns>
    public static bool HasAttribute(this ISymbol? symbol, string fullName, bool isInherited = false)
    {
        foreach (var item in symbol?.GetAttributes() ?? [])
        {
            if (item.AttributeClass?.ToDisplayString() == fullName)
            {
                return true;
            }
            if (isInherited && CheckBaseAttribute(item.AttributeClass, fullName))
            {
                return true;
            }
        }
        return false;
        //return symbol?.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == fullName) == true;

    }
    static bool CheckBaseAttribute(INamedTypeSymbol? attributeType, string targetAttribute)
    {
        if (attributeType is null) return false;
        if (attributeType.BaseType is null) return false;
        var parent = attributeType.BaseType;
        var parentType = parent?.ToDisplayString();
        if (parentType == "System.Attribute")
        {
            return false;
        }
        if (parentType == targetAttribute)
        {
            return true;
        }
        return CheckBaseAttribute(parent?.BaseType, targetAttribute);
        //return false;
    }

    public static bool HasInterface(this ITypeSymbol? symbol, string fullName)
    {
        return symbol?.Interfaces.Any(i => i.ToDisplayString() == fullName) == true;
    }

    public static bool HasInterfaceAll(this ITypeSymbol? symbol, string fullName)
    {
        return symbol?.AllInterfaces.Any(i => i.ToDisplayString() == fullName) == true;
    }
    /// <summary>
    /// 获取方法符号
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static IEnumerable<IMethodSymbol> GetMethods(this INamedTypeSymbol? symbol, Func<IMethodSymbol, bool>? predicate = null)
    {
        predicate ??= _ => true;
        foreach (var item in symbol?.GetMembers() ?? [])
        {
            if (item.Kind == SymbolKind.Method
                && item is IMethodSymbol method
                && predicate(method))
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// 获取属性符号
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static IEnumerable<IPropertySymbol> GetProperties(this INamedTypeSymbol? symbol)
    {
        foreach (var item in symbol?.GetMembers() ?? [])
        {
            if (item.Kind == SymbolKind.Property && item is IPropertySymbol p)
            {
                yield return p;
            }
        }
    }

    public static bool CheckDisableGenerator(this AnalyzerConfigOptionsProvider options, string key)
    {
        return options.GlobalOptions.TryGetValue($"build_property.{key}", out var value) && !string.IsNullOrEmpty(value);
    }

    public static string[] GetTargetUsings(this GeneratorAttributeSyntaxContext source)
    {
        if (source.TargetNode is
            {
                Parent: NamespaceDeclarationSyntax
                {
                    Usings: var nu,
                    Parent: CompilationUnitSyntax
                    {
                        Usings: var cnu
                    }
                }
            }
            )
        {
            UsingDirectiveSyntax[] arr = [.. nu, .. cnu];
            return arr.Select(a => a.ToFullString().Replace("\n", "")).ToArray();
        }

        return [];
    }

    public static string[] GetTargetUsings(this ISymbol symbol)
    {
        List<UsingDirectiveSyntax> usings = [];
        foreach (var item in symbol.DeclaringSyntaxReferences)
        {
            var syntax = item.GetSyntax();
            if (syntax is
                {
                    Parent: NamespaceDeclarationSyntax
                    {
                        Usings: var nu1,
                        Parent: CompilationUnitSyntax
                        {
                            Usings: var cnu1
                        }
                    }
                })
            {
                usings.AddRange(nu1);
                usings.AddRange(cnu1);
            }
            else if (syntax is
            {
                Parent: FileScopedNamespaceDeclarationSyntax
                {
                    Usings: var nu2,
                    Parent: CompilationUnitSyntax
                    {
                        Usings: var cnu2
                    }
                }
            })
            {
                usings.AddRange(nu2);
                usings.AddRange(cnu2);
            }
        }
        return usings.Select(a => a.ToFullString().Replace("\n", "")).ToArray();
    }

    public static IEnumerable<INamedTypeSymbol> GetAllSymbols(this Compilation compilation, string fullName, bool isInherited = false)
    {
        return InternalGetAllSymbols(compilation.GlobalNamespace, fullName, isInherited);

        static IEnumerable<INamedTypeSymbol> InternalGetAllSymbols(INamespaceSymbol global, string targetAttribute, bool isInherited)
        {
            foreach (var symbol in global.GetMembers())
            {
                if (symbol is INamespaceSymbol n)
                {
                    foreach (var item in InternalGetAllSymbols(n, targetAttribute, isInherited))
                    {
                        //if (item.HasAttribute(AutoInject))
                        yield return item;
                    }
                }
                else if (symbol is INamedTypeSymbol target)
                {
                    if (target.HasAttribute(targetAttribute, isInherited))
                        yield return target;
                }
            }
        }
    }
    public static IEnumerable<TReturn> GetAllMembers<TReturn>(this INamespaceSymbol namespaceSymbol
        , Func<ISymbol, bool> predicate)
        where TReturn : ISymbol
    {
        foreach (var item in InternalGetAllSymbols(namespaceSymbol))
        {
            if (predicate(item) && item is TReturn t)
            {
                yield return t;
            }
        }

        static IEnumerable<ISymbol> InternalGetAllSymbols(INamespaceSymbol global)
        {
            foreach (var symbol in global.GetMembers())
            {
                if (symbol is INamespaceSymbol n)
                {
                    foreach (var item in InternalGetAllSymbols(n))
                    {
                        //if (item.HasAttribute(AutoInject))
                        yield return item;
                    }
                }
                else if (symbol is INamedTypeSymbol t)
                {
                    yield return symbol;
                    foreach (var item in t.GetMembers())
                    {
                        yield return item;
                    }
                }
            }
        }
    }


    public static Location? TryGetLocation(this ISymbol symbol)
    {
        var syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        return syntaxReference?.GetSyntax()?.GetLocation()
                            ?? symbol.Locations.FirstOrDefault();
    }

    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol, Func<INamedTypeSymbol, bool> baseTypeCheck)
    {
        if (symbol.BaseType is not null
            && symbol.BaseType.SpecialType != SpecialType.System_Object
            && baseTypeCheck.Invoke(symbol.BaseType))
        {
            foreach (var item in symbol.BaseType.GetAllMembers(baseTypeCheck))
            {
                yield return item;
            }
        }
        foreach (var item in symbol.GetMembers())
        {
            yield return item;
        }
    }

    public static string FormatClassName(this INamedTypeSymbol symbol, bool full = false)
    {
        var meta = symbol.MetadataName;
        if (meta.IndexOf('`') > -1)
        {
            meta = meta.Substring(0, meta.IndexOf('`'));
        }
        if (symbol.TypeKind == TypeKind.Interface && meta.StartsWith("I"))
        {
            meta = meta.Substring(1);
        }
        if (full)
        {
            var np = symbol.ContainingNamespace.ToDisplayString().Replace(".", "_");
            return $"{np}_{meta}";
        }
        return meta;
    }

    public static string SafeMetaName(this ISymbol symbol)
    {
        var meta = symbol.MetadataName;
        if (meta.IndexOf('`') > -1)
        {
            meta = meta.Substring(0, meta.IndexOf('`'));
        }
        if (meta.Contains("."))
        {
            meta = meta.Replace(".", "_");
        }
        return meta;
    }

    public static string FormatFileName(this INamedTypeSymbol symbol)
    {
        var meta = symbol.MetadataName;
        if (symbol.TypeKind == TypeKind.Interface && meta.StartsWith("I"))
        {
            meta = meta.Substring(1);
        }
        var np = symbol.ContainingNamespace.ToDisplayString().Replace('.', '_');
        return $"{np}_{meta}";
    }

    public static IEnumerable<(IMethodSymbol Symbol, AttributeData? AttrData)> GetAllMethodWithAttribute(this INamedTypeSymbol interfaceSymbol, string fullName, INamedTypeSymbol? classSymbol = null)
    {
        var all = interfaceSymbol.AllInterfaces.Insert(0, interfaceSymbol);
        foreach (var m in all)
        {
            foreach (var item in m.GetMembers().Where(m => m is IMethodSymbol).Cast<IMethodSymbol>())
            {
                if (item.MethodKind == MethodKind.Constructor)
                {
                    continue;
                }

                var classMethod = classSymbol?.GetMembers().FirstOrDefault(m => m.Name == item.Name);

                if (!item.GetAttribute(fullName, out var a))
                {
                    if (!classMethod.GetAttribute(fullName, out a))
                    {
                        a = null;
                    }
                }
                var method = m.IsGenericType ? item.ConstructedFrom : item;
                yield return (method, a);
            }
        }
    }
    /// <summary>
    /// 获取直接继承的接口列表
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static IEnumerable<INamedTypeSymbol> GetInterfaces(this ITypeSymbol symbol)
    {
        var dis = symbol.Interfaces;
        if (dis.Length == 0) yield break;
        if (dis.Length == 1)
        {
            yield return dis[0];
            yield break;
        }
        yield return dis[0];
        for (int i = 1; i < dis.Length; i++)
        {
            var iface = dis[i];
            if (!InheriBefore(iface, symbol, i))
            {
                yield return iface;
            }
        }

        static bool InheriBefore(INamedTypeSymbol target, ITypeSymbol owner, int last)
        {
            for (int i = 0; i < last; i++)
            {
                var iface = owner.Interfaces[i];
                if (iface.Interfaces.Contains(target))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static ITypeSymbol GetElementType(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.HasInterfaceAll("System.Collections.IEnumerable") && typeSymbol.SpecialType == SpecialType.None)
        {
            if (typeSymbol is IArrayTypeSymbol a)
            {
                return a.ElementType;
            }
            return typeSymbol.GetGenericTypes().First();
        }
        return typeSymbol;
    }

    public static IEnumerable<ITypeSymbol> GetGenericTypes(this ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol { IsGenericType: true, TypeArguments: var types })
        {
            foreach (var t in types)
            {
                yield return t;
            }
        }
        //else
        //{
        //    yield return symbol;
        //}
    }

    public static (bool IsTask, bool HasReturn, ITypeSymbol ReturnType) GetReturnTypeInfo(this IMethodSymbol method)
    {
        var typeSymbol = method.ReturnType;
        var isTask = IsTask(typeSymbol);
        var hasReturn = HasReturn(isTask, typeSymbol) && !method.ReturnsVoid;
        var returnType = GetRealReturnType(isTask, hasReturn, typeSymbol);
        return (isTask, hasReturn, returnType);

        static bool IsTask(ITypeSymbol t) => t is INamedTypeSymbol nt && nt.ConstructedFrom?.ToDisplayString()?.StartsWith("System.Threading.Tasks.Task") == true;
        static bool HasReturn(bool isTask, ITypeSymbol t)
        {
            if (!isTask) return t.SpecialType != SpecialType.System_Void;
            return t is INamedTypeSymbol nt && nt.IsGenericType;
        }
        static ITypeSymbol GetRealReturnType(bool isTask, bool hasReturn, ITypeSymbol t)
        {
            if (isTask)
            {
                // 具有异步返回值，说明返回 Task<T>，提取出T
                if (hasReturn)
                {
                    return t.GetGenericTypes().FirstOrDefault();
                }
            }
            return t;
        }
    }

    /// <summary>
    /// new xxx()
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static string New(this ITypeSymbol symbol) => $"new {symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()}()";

    /// <summary>
    /// 判断两个方法的签名是否一致
    /// </summary>
    /// <param name="method"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool AreMethodsSignatureEqual(this IMethodSymbol method, IMethodSymbol other, bool checkReturnType = true)
    {
        // 快速引用比较
        if (SymbolEqualityComparer.Default.Equals(method, other))
            return true;

        // 检查方法名
        if (method.Name != other.Name)
            return false;

        // 检查返回类型
        if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, other.ReturnType) && checkReturnType)
            return false;

        // 检查参数
        if (!AreParametersEqual(method.Parameters, other.Parameters))
            return false;

        // 检查泛型约束
        if (!AreGenericConstraintsEqual(method, other))
            return false;

        return true;



        static bool AreParametersEqual(ImmutableArray<IParameterSymbol> params1, ImmutableArray<IParameterSymbol> params2)
        {
            if (params1.Length != params2.Length)
                return false;

            for (int i = 0; i < params1.Length; i++)
            {
                var param1 = params1[i];
                var param2 = params2[i];

                if (param1.RefKind != param2.RefKind ||
                    param1.IsParams != param2.IsParams ||
                    !SymbolEqualityComparer.Default.Equals(param1.Type, param2.Type) ||
                    !AreParametersDefaultValuesEqual(param1, param2))
                {
                    return false;
                }
            }

            return true;
        }

        static bool AreParametersDefaultValuesEqual(IParameterSymbol param1, IParameterSymbol param2)
        {
            if (param1.HasExplicitDefaultValue != param2.HasExplicitDefaultValue)
                return false;

            if (param1.HasExplicitDefaultValue)
            {
                if (!Equals(param1.ExplicitDefaultValue, param2.ExplicitDefaultValue))
                    return false;
            }
            return true;
        }

        static bool AreGenericConstraintsEqual(IMethodSymbol method1, IMethodSymbol method2)
        {
            if (method1.TypeParameters.Length != method2.TypeParameters.Length)
                return false;

            for (int i = 0; i < method1.TypeParameters.Length; i++)
            {
                ITypeParameterSymbol typeParam1 = method1.TypeParameters[i];
                ITypeParameterSymbol typeParam2 = method2.TypeParameters[i];

                // 检查约束类型（如 where T : IComparable）
                if (!ImmutableArrayExtensions.SequenceEqual(typeParam1.ConstraintTypes,
                    typeParam2.ConstraintTypes, SymbolEqualityComparer.Default))
                    return false;

                // 检查特殊约束（如 class, struct, new()）
                if (typeParam1.HasReferenceTypeConstraint != typeParam2.HasReferenceTypeConstraint ||
                    typeParam1.HasValueTypeConstraint != typeParam2.HasValueTypeConstraint ||
                    typeParam1.HasConstructorConstraint != typeParam2.HasConstructorConstraint)
                    return false;
            }

            return true;
        }
    }


    public static IEnumerable<TypeParameterInfo> GetTypeParameters(this ISymbol symbol)
    {
        IEnumerable<ITypeParameterSymbol> tpc = [];
        if (symbol is IMethodSymbol method)
        {
            tpc = method.TypeParameters;
        }
        else if (symbol is INamedTypeSymbol typeSymbol)
        {
            tpc = typeSymbol.TypeParameters;
        }
        else
        {
            yield break;
        }

        foreach (var tp in tpc)
        {
            List<string> cs = tp.ConstraintTypes.Select(t => t.ToDisplayString()).ToList();
            tp.HasNotNullConstraint.IsTrueThen(() => cs.Add("notnull"));
            tp.HasReferenceTypeConstraint.IsTrueThen(() => cs.Add("class"));
            tp.HasUnmanagedTypeConstraint.IsTrueThen(() => cs.Add("unmanaged "));
            tp.HasValueTypeConstraint.IsTrueThen(() => cs.Add("struct"));
            tp.HasConstructorConstraint.IsTrueThen(() => cs.Add("new()"));
            yield return new(tp.Name, [.. cs]);
        }
    }

    private static void IsTrueThen(this bool value, Action action)
    {
        if (value)
        {
            action();
        }
    }
}
