using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Generators.Shared;
public static class CompilationProviderExtensions
{
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
        this IncrementalValueProvider<Compilation> compilationProvider,
        string fullyQualifiedMetadataName,
        Func<ISymbol, CancellationToken, bool> predicate,
        Func<GeneratorSyntaxCollectInfoContext, CancellationToken, T> transform)
    {
        return compilationProvider.SelectMany((compilation, cancellationToken) =>
            GetAttributeContextsFromAllAssemblies(
                compilation,
                fullyQualifiedMetadataName,
                predicate,
                transform,
                cancellationToken));
    }

    public static IEnumerable<T> ForAttributeWithMetadataName<T>(
        this Compilation compilation,
        string fullyQualifiedMetadataName,
        Func<ISymbol, CancellationToken, bool> predicate,
        Func<GeneratorSyntaxCollectInfoContext, CancellationToken, T> transform)
    {
        return GetAttributeContextsFromAllAssemblies(
                compilation,
                fullyQualifiedMetadataName,
                predicate,
                transform, CancellationToken.None);
    }

    public static IEnumerable<T> FindByAttributeMetadataName<T>(this Compilation compilation
        , string fullyQualifiedMetadataName
        , Func<GeneratorSyntaxCollectInfoContext, T> transform
        , bool isInherited = false)
    {
        return InternalGetAllSymbols<T>(compilation
            , compilation.GlobalNamespace
            , fullyQualifiedMetadataName
            , transform
            , isInherited);

        static IEnumerable<TI> InternalGetAllSymbols<TI>(Compilation compilation
            , INamespaceSymbol global
            , string targetAttribute
            , Func<GeneratorSyntaxCollectInfoContext, TI> transform
            , bool isInherited)
        {
            foreach (var symbol in global.GetMembers())
            {
                if (symbol is INamespaceSymbol n)
                {
                    foreach (var item in InternalGetAllSymbols<TI>(compilation
                        , n
                        , targetAttribute
                        , transform
                        , isInherited))
                    {
                        yield return item;
                    }
                }
                else if (symbol is INamedTypeSymbol target)
                {
                    if (target.GetAttribute(targetAttribute, out var a, isInherited))
                    {
                        yield return CreateAttributeContext<TI>(a!, target, compilation, transform);
                    }
                }
            }
        }
    }


    private static IEnumerable<T> GetAttributeContextsFromAllAssemblies<T>(
        Compilation compilation,
        string fullyQualifiedMetadataName,
        Func<ISymbol, CancellationToken, bool> predicate,
        Func<GeneratorSyntaxCollectInfoContext, CancellationToken, T> transform,
        CancellationToken cancellationToken)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
        if (attributeSymbol == null)
            yield break;

        var processedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        // 处理当前程序集
        foreach (var context in GetAttributeContextsFromAssembly(
            compilation.Assembly,
            compilation,
            true,
            cancellationToken))
        {
            yield return context;
        }
        processedAssemblies.Add(compilation.Assembly);

        // 处理引用程序集
        foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (processedAssemblies.Contains(referencedAssembly))
                continue;

            foreach (var context in GetAttributeContextsFromAssembly(
                referencedAssembly,
                compilation,
                false,
                cancellationToken))
            {
                yield return context;
            }

            processedAssemblies.Add(referencedAssembly);
        }

        IEnumerable<T> GetAttributeContextsFromAssembly(
            IAssemblySymbol assembly,
            Compilation compilation,
            bool hasSourceCode,
            CancellationToken cancellationToken)
        {
            foreach (var module in assembly.Modules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var context in GetAttributeContextsFromNamespace(
                    module.GlobalNamespace,
                    compilation,
                    hasSourceCode,
                    cancellationToken))
                {
                    yield return context;
                }
            }
        }

        IEnumerable<T> GetAttributeContextsFromNamespace(
            INamespaceSymbol namespaceSymbol,
            Compilation compilation,
            bool hasSourceCode,
            CancellationToken cancellationToken)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var context in GetAttributeContextsFromType(
                    type,
                    compilation,
                    hasSourceCode,
                    cancellationToken))
                {
                    yield return context;
                }
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var context in GetAttributeContextsFromNamespace(
                    nestedNamespace,
                    compilation,
                    hasSourceCode,
                    cancellationToken))
                {
                    yield return context;
                }
            }
        }

        IEnumerable<T> GetAttributeContextsFromType(
            INamedTypeSymbol type,
            Compilation compilation,
            bool hasSourceCode,
            CancellationToken cancellationToken)
        {
            // 检查类型属性
            if (predicate(type, cancellationToken))
            {
                foreach (var attributeData in type.GetAttributes())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol))
                    {
                        var context = CreateAttributeContext(
                            attributeData,
                            type,
                            compilation,
                            hasSourceCode,
                            transform,
                            cancellationToken);
                        if (context != null)
                            yield return context;
                    }
                }
            }

            // 检查成员属性
            foreach (var member in type.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (predicate(member, cancellationToken))
                {
                    foreach (var attributeData in member.GetAttributes())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeSymbol))
                        {
                            var context = CreateAttributeContext(
                                attributeData,
                                member,
                                compilation,
                                hasSourceCode,
                                transform,
                                cancellationToken);
                            if (context != null)
                                yield return context;
                        }
                    }
                }
            }

            // 处理嵌套类型
            foreach (var nestedType in type.GetTypeMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var context in GetAttributeContextsFromType(
                    nestedType,
                    compilation,
                    hasSourceCode,
                    cancellationToken))
                {
                    yield return context;
                }
            }
        }
    }

    private static T? CreateAttributeContext<T>(
        AttributeData attributeData,
        ISymbol targetSymbol,
        Compilation compilation,
        bool hasSourceCode,
        Func<GeneratorSyntaxCollectInfoContext, CancellationToken, T> transform,
        CancellationToken cancellationToken)
    {
        SyntaxNode? targetSyntax = null;
        SemanticModel? semanticModel = null;
        AttributeSyntax? attributeSyntax = null;

        // 只有当前程序集才有语法
        if (hasSourceCode)
        {
            // 获取目标语法
            var targetSyntaxReference = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (targetSyntaxReference != null)
            {
                targetSyntax = targetSyntaxReference.GetSyntax(cancellationToken);
                semanticModel = compilation.GetSemanticModel(targetSyntax.SyntaxTree);
            }

            // 获取属性语法
            var attributeSyntaxReference = attributeData.ApplicationSyntaxReference;
            if (attributeSyntaxReference != null)
            {
                attributeSyntax = attributeSyntaxReference.GetSyntax(cancellationToken) as AttributeSyntax;
            }
        }

        // 获取位置信息
        var location = targetSyntax?.GetLocation() ?? targetSymbol.Locations.FirstOrDefault();

        var context = new GeneratorSyntaxCollectInfoContext(
            targetSyntax,
            targetSymbol,
            semanticModel,
            attributeData,
            attributeSyntax,
            location);

        return transform(context, cancellationToken);
    }

    private static T CreateAttributeContext<T>(
        AttributeData attributeData,
        ISymbol targetSymbol,
        Compilation compilation,
        Func<GeneratorSyntaxCollectInfoContext, T> transform)
    {
        SyntaxNode? targetSyntax = null;
        SemanticModel? semanticModel = null;
        AttributeSyntax? attributeSyntax = null;

        // 获取目标语法
        var targetSyntaxReference = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (targetSyntaxReference != null)
        {
            targetSyntax = targetSyntaxReference.GetSyntax();
            semanticModel = compilation.GetSemanticModel(targetSyntax.SyntaxTree);
        }

        // 获取属性语法
        var attributeSyntaxReference = attributeData.ApplicationSyntaxReference;
        if (attributeSyntaxReference != null)
        {
            attributeSyntax = attributeSyntaxReference.GetSyntax() as AttributeSyntax;
        }


        // 获取位置信息
        var location = targetSyntax?.GetLocation() ?? targetSymbol.Locations.FirstOrDefault();

        var context = new GeneratorSyntaxCollectInfoContext(
            targetSyntax,
            targetSymbol,
            semanticModel,
            attributeData,
            attributeSyntax,
            location);

        return transform(context);
    }
}

// 增强的 GeneratorAttributeSyntaxContext
public readonly struct GeneratorSyntaxCollectInfoContext(
    SyntaxNode? targetNode,
    ISymbol targetSymbol,
    SemanticModel? semanticModel,
    AttributeData attributeData,
    AttributeSyntax? attributeSyntax,
    Location? location)
{
    public SyntaxNode? TargetNode { get; } = targetNode;
    public ISymbol TargetSymbol { get; } = targetSymbol;
    public SemanticModel? SemanticModel { get; } = semanticModel;
    public AttributeData AttributeData { get; } = attributeData;
    public AttributeSyntax? AttributeSyntax { get; } = attributeSyntax;
    public Location? Location { get; } = location;

    public readonly Location GetDiagnosticLocation()
    {
        return Location ?? TargetSymbol.Locations.FirstOrDefault() ?? Microsoft.CodeAnalysis.Location.None;
    }

    //public readonly void ReportDiagnostic(SourceProductionContext context, DiagnosticDescriptor descriptor, params object[] args)
    //{
    //    var diagnostic = Diagnostic.Create(descriptor, GetDiagnosticLocation(), args);
    //    context.ReportDiagnostic(diagnostic);
    //}
}