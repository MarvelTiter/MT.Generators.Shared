using Generators.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Generators.Shared.SyntaxBuilder;

internal static class MethodBuilderExtensions
{
    /// <summary>依据方法符号生成 partial 方法签名。</summary>
    public static MethodBuilder Partial(this MethodBuilder builder, IMethodSymbol method)
    {
        var access = ToAccessibilityKeyword(method.DeclaredAccessibility);
        var staticString = method.IsStatic ? " static " : " ";
        string[] parameters = [
            .. method.Parameters.Select((p, i) =>
                $"{(i == 0 && method.IsExtensionMethod ? "this " : "")}{p.Type.ToDisplayString()} {p.Name}")];
        return builder.MethodName(method.Name)
            .Modifiers($"{access}{staticString}partial")
            .AddParameter(parameters)
            .ReturnType(method.ReturnType.ToDisplayString())
            .Async(method.IsAsync);
    }

    private static string ToAccessibilityKeyword(Accessibility declaredAccessibility)
    {
        return declaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.Private => "private",
            _ => throw new ArgumentOutOfRangeException(nameof(declaredAccessibility), declaredAccessibility, null)
        };
    }

    public static T MethodName<T>(this T builder, string name) where T : MethodBase
    {
        builder.Name = name;
        return builder;
    }

    public static T Generic<T>(this T builder, params TypeParameterInfo[] types) where T : MemberBuilder
    {
        if (types.Length > 0)
        {
            builder.IsGeneric = true;
            foreach (var item in types)
            {
                builder.TypeArguments.Add(item);
            }
        }
        return builder;
    }

    public static MethodBuilder ReturnType(this MethodBuilder builder, string returnType)
    {
        builder.ReturnType = returnType;
        return builder;
    }

    public static MethodBuilder Async(this MethodBuilder builder, bool isAsync = true)
    {
        builder.IsAsync = isAsync;
        return builder;
    }

    /// <summary>表达式体方法，如 <c>public int Get() =&gt; _value;</c>。</summary>
    public static MethodBuilder Lambda(this MethodBuilder builder, string body)
    {
        builder.IsLambdaBody = true;
        builder.Body.Add(body);
        return builder;
    }

    public static MethodBuilder ExplicitFor(this MethodBuilder builder, string interfaceType)
    {
        builder.IsExplicit = true;
        builder.ExplicitType = interfaceType;
        return builder;
    }

    public static T AddBody<T>(this T builder, params Statement[] body) where T : MethodBase
    {
        foreach (var item in body)
        {
            item.Parent = builder;
            builder.Body.Add(item);
        }
        return builder;
    }

    public static T AddParameter<T>(this T builder, params string[] parameters) where T : MethodBase
    {
        foreach (var item in parameters)
        {
            builder.Parameters.Add(item);
        }
        return builder;
    }

    #region switch

    public static T AddSwitchStatement<T>(this T builder, string switchValue, Action<SwitchStatement> action)
        where T : MethodBase
    {
        var switchStatement = SwitchStatement.Default.Switch(switchValue);
        switchStatement.Parent = builder;
        action.Invoke(switchStatement);
        builder.AddBody(switchStatement);
        return builder;
    }

    public static SwitchStatement Switch(this SwitchStatement switchStatement, string switchValue)
    {
        switchStatement.SwitchValue = switchValue;
        return switchStatement;
    }

    public static SwitchStatement AddReturnCase(this SwitchStatement switchStatement, string condition, string returnItem)
    {
        Statement ret = $"return {returnItem}";
        ret.Parent = switchStatement;
        switchStatement.SwitchCases.Add(new SwitchCaseStatement
        {
            Condition = condition,
            Action = [ret],
            Parent = switchStatement.Parent
        });
        return switchStatement;
    }

    public static SwitchStatement AddReturnCase(this SwitchStatement switchStatement, string condition, params Statement[] statements)
    {
        foreach (var item in statements)
        {
            item.Parent = switchStatement;
        }
        switchStatement.SwitchCases.Add(new SwitchCaseStatement
        {
            Condition = condition,
            Action = [.. statements],
            Parent = switchStatement.Parent
        });
        return switchStatement;
    }

    public static SwitchStatement AddBreakCase(this SwitchStatement switchStatement, string condition, Statement action)
    {
        action.Parent = switchStatement;
        switchStatement.SwitchCases.Add(new SwitchCaseStatement
        {
            Condition = condition,
            Action = [action],
            IsBreak = true,
            Parent = switchStatement.Parent
        });
        return switchStatement;
    }

    public static SwitchStatement AddBreakCase(this SwitchStatement switchStatement, string condition, params Statement[] action)
    {
        foreach (var item in action)
        {
            item.Parent = switchStatement;
        }
        switchStatement.SwitchCases.Add(new SwitchCaseStatement
        {
            Condition = condition,
            Action = [.. action],
            IsBreak = true,
            Parent = switchStatement.Parent
        });
        return switchStatement;
    }

    public static SwitchStatement AddDefaultCase(this SwitchStatement switchStatement, string action)
    {
        switchStatement.DefaultCase = new DefaultCaseStatement { Action = action, Parent = switchStatement.Parent };
        return switchStatement;
    }

    #endregion

    #region if

    public static T AddIfStatement<T>(this T builder, string condition, Action<IfStatement> action)
        where T : MethodBase
    {
        var ifs = IfStatement.Default.If(condition);
        ifs.Parent = builder;
        action.Invoke(ifs);
        builder.AddBody(ifs);
        return builder;
    }

    public static IfStatement If(this IfStatement ifStatement, string condition)
    {
        ifStatement.Condition = condition;
        return ifStatement;
    }

    public static IfStatement AddStatement(this IfStatement ifStatement, params Statement[] statements)
    {
        foreach (var statement in statements)
        {
            statement.Parent = ifStatement;
            ifStatement.IfContents.Add(statement);
        }
        return ifStatement;
    }

    #endregion

    #region LocalFunction

    public static T AddLocalFunction<T>(this T builder, Action<LocalFunction> action) where T : MethodBase
    {
        var localFunction = LocalFunction.Default;
        localFunction.Parent = builder;
        action.Invoke(localFunction);
        builder.AddBody(localFunction);
        return builder;
    }

    public static LocalFunction MethodName(this LocalFunction localFunction, string name)
    {
        localFunction.Name = name;
        return localFunction;
    }

    public static LocalFunction Async(this LocalFunction localFunction, bool isAsync = true)
    {
        localFunction.IsAsync = isAsync;
        return localFunction;
    }

    public static LocalFunction Return(this LocalFunction localFunction, string returnType)
    {
        localFunction.ReturnType = returnType;
        return localFunction;
    }

    public static LocalFunction AddParameters(this LocalFunction localFunction, params string[] parameters)
    {
        foreach (var parameter in parameters)
        {
            localFunction.Parameters.Add(parameter);
        }
        return localFunction;
    }

    public static LocalFunction AddBody(this LocalFunction localFunction, params Statement[] body)
    {
        foreach (var item in body)
        {
            item.Parent = localFunction;
            localFunction.Body.Add(item);
        }
        return localFunction;
    }

    #endregion

    #region foreach

    public static T AddForeachStatement<T>(this T builder, string loopContent, Action<ForeachStatement> action)
        where T : MethodBase
    {
        var loop = ForeachStatement.Default.Foreach(loopContent);
        loop.Parent = builder;
        action.Invoke(loop);
        builder.AddBody(loop);
        return builder;
    }

    public static ForeachStatement Foreach(this ForeachStatement builder, string loop)
    {
        builder.LoopContent = loop;
        return builder;
    }

    public static ForeachStatement AddStatements(this ForeachStatement builder, params Statement[] statements)
    {
        foreach (var item in statements)
        {
            item.Parent = builder;
        }
        builder.Contents.AddRange(statements);
        return builder;
    }

    #endregion

    #region try-catch

    public static T AddTryCatch<T>(this T builder, Action<TryCatch> action) where T : MethodBase
    {
        var tryCatch = TryCatch.Default;
        tryCatch.Parent = builder;
        action.Invoke(tryCatch);
        builder.AddBody(tryCatch);
        return builder;
    }

    public static TryCatch AddBody(this TryCatch tryCatch, params Statement[] statements)
    {
        foreach (var item in statements)
        {
            item.Parent = tryCatch;
            tryCatch.Body.Add(item);
        }
        return tryCatch;
    }

    public static TryCatch AddCatch(this TryCatch tryCatch, Action<TryCatchException> action)
    {
        var catchStatement = TryCatchException.Default;
        catchStatement.Parent = tryCatch;
        action.Invoke(catchStatement);
        tryCatch.Catchs.Add(catchStatement);
        return tryCatch;
    }

    public static TryCatch AddCatch(this TryCatch tryCatch, string? exception, params Statement[] statements)
    {
        var catchStatement = TryCatchException.Default;
        catchStatement.Exception = exception;
        catchStatement.Parent = tryCatch;
        foreach (var item in statements)
        {
            item.Parent = catchStatement;
            catchStatement.Body.Add(item);
        }
        tryCatch.Catchs.Add(catchStatement);
        return tryCatch;
    }

    public static TryCatchException AddBody(this TryCatchException exception, params Statement[] statements)
    {
        foreach (var item in statements)
        {
            item.Parent = exception;
            exception.Body.Add(item);
        }
        return exception;
    }

    public static TryCatch AddFinally(this TryCatch tryCatch, params Statement[] statements)
    {
        foreach (var item in statements)
        {
            item.Parent = tryCatch;
            tryCatch.Finally.Add(item);
        }
        return tryCatch;
    }

    #endregion
}
