using Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Generators.Shared.SyntaxBuilder;

/// <summary>
/// 基于 <c>Microsoft.CodeAnalysis.CSharp.SyntaxFactory</c> 的语法工具箱。
/// <para>
/// builder 负责描述「要生成什么结构」，本类负责把零散的字符串片段
/// （类型名、表达式、参数声明、语句原文……）转换成合法的 Roslyn 语法节点，
/// 从而取代原先拼字符串的做法：缩进、换行等格式统一交给 NormalizeWhitespace 处理。
/// </para>
/// </summary>
internal static class SyntaxKit
{
    private static readonly char[] WhiteSpaces = [' ', '\t', '\r', '\n'];

    #region 名称 / 类型 / 表达式

    /// <summary>解析类型名，null 或空时返回 void。</summary>
    public static TypeSyntax ParseType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return PredefinedType(Token(SyntaxKind.VoidKeyword));
        }
        // ParseTypeName(text, offset, consumeFullText)
        return ParseTypeName(type!.Trim(), 0, true);
    }

    /// <summary>解析命名空间 / 特性等的名字，支持 global:: 前缀与泛型。</summary>
    public static NameSyntax ParseName(string? name)
    {
        var text = (name ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return IdentifierName(string.Empty);
        }
        // ParseName(text, offset, consumeFullText)
        return SyntaxFactory.ParseName(text, 0, true) ?? IdentifierName(text);
    }

    /// <summary>解析表达式，容忍末尾多余的分号。</summary>
    public static ExpressionSyntax ParseExpression(string? expression)
    {
        var text = (expression ?? string.Empty).Trim().TrimEnd(';');
        if (text.Length == 0)
        {
            return IdentifierName(string.Empty);
        }
        // ParseExpression(text, offset, options, consumeFullText)
        return SyntaxFactory.ParseExpression(text, 0, null, true) ?? IdentifierName(text);
    }

    #endregion

    #region 修饰符

    private static readonly Dictionary<string, SyntaxKind> ModifierKeywords = new(StringComparer.Ordinal)
    {
        ["public"] = SyntaxKind.PublicKeyword,
        ["private"] = SyntaxKind.PrivateKeyword,
        ["protected"] = SyntaxKind.ProtectedKeyword,
        ["internal"] = SyntaxKind.InternalKeyword,
        ["file"] = SyntaxKind.FileKeyword,
        ["static"] = SyntaxKind.StaticKeyword,
        ["readonly"] = SyntaxKind.ReadOnlyKeyword,
        ["const"] = SyntaxKind.ConstKeyword,
        ["partial"] = SyntaxKind.PartialKeyword,
        ["virtual"] = SyntaxKind.VirtualKeyword,
        ["override"] = SyntaxKind.OverrideKeyword,
        ["abstract"] = SyntaxKind.AbstractKeyword,
        ["sealed"] = SyntaxKind.SealedKeyword,
        ["async"] = SyntaxKind.AsyncKeyword,
        ["extern"] = SyntaxKind.ExternKeyword,
        ["new"] = SyntaxKind.NewKeyword,
        ["unsafe"] = SyntaxKind.UnsafeKeyword,
        ["volatile"] = SyntaxKind.VolatileKeyword,
        ["required"] = SyntaxKind.RequiredKeyword,
        ["ref"] = SyntaxKind.RefKeyword,
        ["scoped"] = SyntaxKind.ScopedKeyword,
        ["event"] = SyntaxKind.EventKeyword,
    };

    /// <summary>
    /// 把 "public partial"、"private readonly" 之类的修饰符文本转换为 token 列表，重复修饰符自动去重。
    /// </summary>
    public static SyntaxTokenList Modifiers(params string?[] modifiers)
    {
        List<SyntaxToken> tokens = [];
        foreach (var modifier in modifiers)
        {
            if (string.IsNullOrWhiteSpace(modifier))
            {
                continue;
            }
            foreach (var word in modifier!.Split(WhiteSpaces, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!ModifierKeywords.TryGetValue(word, out SyntaxKind kind))
                {
                    continue;
                }
                if (tokens.Any(t => t.IsKind(kind)))
                {
                    continue;
                }
                tokens.Add(Token(kind));
            }
        }
        return TokenList(tokens);
    }

    #endregion

    #region 特性

    /// <summary>把特性文本列表转换为特性列表节点，每个特性独立成行。</summary>
    public static SyntaxList<AttributeListSyntax> Attributes(IEnumerable<AttributeInfo> attributes)
    {
        List<AttributeListSyntax> lists = [];
        foreach (AttributeInfo attribute in attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Name))
            {
                continue;
            }
            lists.Add(AttributeList(attribute));
        }
        
        return List(lists);
    }

    /// <summary>解析单条特性，支持 "[X]"、"X"、"X(a = 1)"、"global::A.B.C(\"...\")" 等写法。</summary>
    public static AttributeListSyntax AttributeList(AttributeInfo attribute)
    {
        AttributeListSyntax? parsed = TryParseAttribute(attribute);
        if (parsed is not null)
        {
            return parsed;
        }
        // 兜底：当作不带参数的特性名
        return SyntaxFactory.AttributeList(
            SingletonSeparatedList(Attribute(ParseName(attribute.Name))));
    }

    private static AttributeListSyntax? TryParseAttribute(AttributeInfo attributeInfo)
    {
        var name = attributeInfo.Name;
        IEnumerable<AttributeInfo.AttributeArgumentInfo>? arguments = attributeInfo.Arguments;
        NameSyntax typeName = ParseName(name);
        if (typeName.ContainsDiagnostics)
        {
            return null;
        }
        if (arguments is null)
        {
            return SyntaxFactory.AttributeList(
                SingletonSeparatedList(Attribute(typeName)));
        }
        IEnumerable<AttributeArgumentSyntax> alist = arguments.Select(arg =>
         {
             if (arg.Name is null)
             {
                 return AttributeArgument(ParseValue(arg.Value));
             }
             else
             {
                 return AttributeArgument(ParseValue(arg.Value))
                     .WithNameEquals(NameEquals(IdentifierName(arg.Name)));
             }
         });
        AttributeArgumentListSyntax argumentList = AttributeArgumentList(SeparatedList(alist));
        return SyntaxFactory.AttributeList(
            SingletonSeparatedList(Attribute(typeName, argumentList.WithoutTrivia())));

        static ExpressionSyntax ParseValue(object value) => value switch
        {
            null => LiteralExpression(SyntaxKind.NullLiteralExpression),
            char c => LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal(c)),
            string s => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s)),
            int i => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)),
            bool b => LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
            uint u => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(u)),
            long l => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(l)),
            ulong ul => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(ul)),
            float f => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(f)),
            double d => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(d)),
            decimal m => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(m)),
            ITypeSymbol t => TypeOfExpression(ParseTypeName(t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))),
            Enum e => ParseEnumExpression(e),
            System.Collections.IEnumerable enumerable when value is not string => BuildArrayInitializer(enumerable),
            _ => throw new NotSupportedException($"Unsupported attribute argument type: {value.GetType()}")
        };

        static ExpressionSyntax ParseEnumExpression(Enum enumValue)
        {
            Type enumType = enumValue.GetType();
            IdentifierNameSyntax typeName = IdentifierName(enumType.Name);

            // 获取所有设置了标志位的成员名
            var flags = Enum.GetValues(enumType).Cast<Enum>()
                .Where(f => !f.Equals(default) && enumValue.HasFlag(f))
                .Select(f => (ExpressionSyntax)MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    typeName,
                    IdentifierName(f.ToString())))
                .ToList();

            if (flags.Count == 0)
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    typeName,
                    IdentifierName("0")); // default 值

            // 用 | 连接多个标志
            return flags.Aggregate((a, b) => BinaryExpression(SyntaxKind.BitwiseOrExpression, a, b));
        }

        static ExpressionSyntax BuildArrayInitializer(System.Collections.IEnumerable enumerable)
        {
            var elements = new List<ExpressionSyntax>();
            foreach (var item in enumerable)
                elements.Add(ParseValue(item)); // 递归处理每个元素

            return ArrayCreationExpression(
                ArrayType(
                    PredefinedType(Token(SyntaxKind.ObjectKeyword)))
                .WithRankSpecifiers(
                    SingletonList(
                        ArrayRankSpecifier(
                            SingletonSeparatedList<ExpressionSyntax>(
                                OmittedArraySizeExpression())))))
            .WithInitializer(
                InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(elements)));
        }
    }

    #endregion

    #region 类型参数与约束

    public static TypeParameterListSyntax? TypeParameterList(IEnumerable<TypeParameterInfo>? typeArguments)
    {
        TypeParameterInfo[] items = typeArguments?.Where(t => !string.IsNullOrWhiteSpace(t.Name)).ToArray() ?? [];
        if (items.Length == 0)
        {
            return null;
        }
        return SyntaxFactory.TypeParameterList(
            SeparatedList(items.Select(t => TypeParameter(t.Name))));
    }

    public static SyntaxList<TypeParameterConstraintClauseSyntax> ConstraintClauses(IEnumerable<TypeParameterInfo>? typeArguments)
    {
        List<TypeParameterConstraintClauseSyntax> clauses = [];
        foreach (TypeParameterInfo item in typeArguments ?? [])
        {
            TypeParameterConstraintSyntax[] constraints = (item.Constraints ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.Ordinal)
                .Select(ToConstraint)
                .ToArray();
            if (constraints.Length == 0)
            {
                continue;
            }
            clauses.Add(TypeParameterConstraintClause(
                IdentifierName(item.Name),
                SeparatedList(constraints)));
        }
        return List(clauses);
    }

    /// <summary>把约束文本转换为约束节点。</summary>
    private static TypeParameterConstraintSyntax ToConstraint(string constraint)
    {
        switch (constraint)
        {
            case "class":
                return ClassOrStructConstraint(SyntaxKind.ClassConstraint);
            case "struct":
                return ClassOrStructConstraint(SyntaxKind.StructConstraint);
            case "new()":
                return ConstructorConstraint();
            default:
                // notnull、unmanaged 以及普通类型约束都走类型约束
                return TypeConstraint(ParseType(constraint));
        }
    }

    #endregion

    #region 参数列表

    /// <summary>
    /// 解析参数声明列表，支持 <c>this</c> 扩展方法参数、默认值、ref/out/params 等写法。
    /// </summary>
    public static ParameterListSyntax ParameterList(IEnumerable<string>? parameters)
    {
        var items = parameters?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? [];
        if (items.Length == 0)
        {
            return SyntaxFactory.ParameterList(SeparatedList<ParameterSyntax>());
        }
        // ParseParameterList(text, offset, options, consumeFullText)
        ParameterListSyntax? parsed = ParseParameterList($"({string.Join(", ", items)})", 0, null, true);
        if (parsed is not null
            && !parsed.ContainsDiagnostics
            && !parsed.ContainsSkippedText
            && parsed.Parameters.Count == items.Length)
        {
            return parsed.WithoutTrivia();
        }
        return SyntaxFactory.ParameterList(SeparatedList(items.Select(FallbackParameter)));
    }

    private static ParameterSyntax FallbackParameter(string parameter)
    {
        var text = parameter.Trim();
        SyntaxTokenList modifiers = default;
        if (text.StartsWith("this "))
        {
            modifiers = TokenList(Token(SyntaxKind.ThisKeyword));
            text = text.Substring("this ".Length).Trim();
        }
        var index = text.LastIndexOf(' ');
        if (index <= 0)
        {
            return Parameter(Identifier(text)).WithModifiers(modifiers);
        }
        // Parameter(attributeLists, modifiers, type, identifier, default)
        return Parameter(
            List<AttributeListSyntax>(),
            modifiers,
            ParseType(text.Substring(0, index)),
            Identifier(text.Substring(index + 1)),
            null);
    }

    #endregion

    #region using 指令

    /// <summary>
    /// 解析 using 指令，兼容 "using X;"、"using static X;"、"using Alias = X;" 等写法，并自动去重。
    /// </summary>
    public static SyntaxList<UsingDirectiveSyntax> Usings(IEnumerable<string>? usings, IEnumerable<UsingDirectiveSyntax>? usingDirectives)
    {
        List<UsingDirectiveSyntax> directives = [];
        HashSet<string> added = new(StringComparer.Ordinal);
        foreach (var item in usings ?? [])
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }
            var text = item.Trim().TrimEnd(';').Trim();
            UsingDirectiveSyntax directive = UsingDirective(ParseName(text));
            if (!added.Add(directive.ToString()))
            {
                continue;
            }
            directives.Add(directive);
        }
        foreach (UsingDirectiveSyntax item in usingDirectives ?? [])
        {
            directives.Add(item);
        }
        return List(directives);
    }

    #endregion

    #region 语句

    public static BlockSyntax Block(IEnumerable<Statement>? statements) => SyntaxFactory.Block(Statements(statements));

    public static SyntaxList<StatementSyntax> Statements(IEnumerable<Statement>? statements)
    {
        List<StatementSyntax> list = [];
        foreach (Statement item in statements ?? [])
        {
            var statement = item?.ToStatementSyntax();
            if (statement is not null)
            {
                list.Add(statement);
            }
        }
        return List(list);
    }

    /// <summary>
    /// 把一段原文解析为语句。
    /// 优先按单条语句解析；解析不了时（例如一次写了多句）退化为代码块；
    /// 预处理指令与注释则作为 trivia 挂在空语句上。
    /// </summary>
    public static StatementSyntax? ParseStatement(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }
        var text = (content ?? string.Empty).Trim();

        if (text.StartsWith("#"))
        {
            return EmptyStatement().WithLeadingTrivia(ParseLeadingTrivia(text));
        }
        if (text.StartsWith("//"))
        {
            return EmptyStatement().WithLeadingTrivia(Comment(text));
        }

        // ParseStatement(text, offset, options, consumeFullText)
        foreach (var candidate in new[] { text, text + ";" })
        {
            StatementSyntax? statement = SyntaxFactory.ParseStatement(candidate, 0, null, true);
            if (statement is not null && !statement.ContainsDiagnostics && !statement.ContainsSkippedText)
            {
                return statement;
            }
        }

        var block = SyntaxFactory.ParseStatement($"{{\n{text}\n}}", 0, null, true) as BlockSyntax;
        if (block is not null
            && !block.ContainsDiagnostics
            && !block.ContainsSkippedText
            && block.Statements.Count > 0)
        {
            return block;
        }
        return SyntaxFactory.ParseStatement(text, 0, null, true);
    }

    #endregion

    #region 文档注释与文件头

    /// <summary>在节点首个 token 前插入文档注释（如 /// &lt;inheritdoc/&gt;）。</summary>
    public static T WithDocComment<T>(T node, string comment) where T : SyntaxNode
    {
        SyntaxToken token = node.GetFirstToken(true);
        SyntaxTriviaList trivia = TriviaList(Comment(comment), CarriageReturnLineFeed)
            .AddRange(token.LeadingTrivia);
        return (T)node.ReplaceToken(token, token.WithLeadingTrivia(trivia));
    }

    /// <summary>
    /// 生成文件头注释与编译指令：
    /// <c>// &lt;auto-generated/&gt;</c>、<c>#pragma warning disable</c>、<c>#nullable enable</c>。
    /// </summary>
    public static SyntaxTriviaList FileHeader()
    {
        return TriviaList(
            Comment("// <auto-generated/>"),
            CarriageReturnLineFeed,
            Trivia(PragmaWarningDirectiveTrivia(
                Token(SyntaxKind.DisableKeyword), true)),
            CarriageReturnLineFeed,
            Trivia(NullableDirectiveTrivia(
                Token(SyntaxKind.EnableKeyword), true)),
            CarriageReturnLineFeed);
    }

    /// <summary>把文件头挂到节点的第一个 token 上。</summary>
    public static T WithFileHeader<T>(T node) where T : SyntaxNode
    {
        SyntaxToken token = node.GetFirstToken(true);
        SyntaxTriviaList trivia = FileHeader().AddRange(token.LeadingTrivia);
        return (T)node.ReplaceToken(token, token.WithLeadingTrivia(trivia));
    }

    #endregion
}
