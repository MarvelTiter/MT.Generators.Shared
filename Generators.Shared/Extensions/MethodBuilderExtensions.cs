using Generators.Shared.Builder;
using Generators.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Generators.Shared;

internal static class E
{
    public static string ToCSharpKeyword(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.NotApplicable => "",
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => throw new NotSupportedException($"Accessibility '{accessibility}' is not supported in C#"),
        };
    }
}
internal static class MethodBuilderExtensions
{
    public static MethodBuilder Partial(this MethodBuilder builder, IMethodSymbol method)
    {
        var access = method.DeclaredAccessibility.ToCSharpKeyword();
        var staticString = method.IsStatic ? " static " : " ";
        string[] parameters = [
                .. method.Parameters.Select((p, i) =>
                    $"{(i == 0 && method.IsExtensionMethod ? "this " : "")}{p.Type.ToDisplayString()} {p.Name}")];
        //method.Parameters();
        return builder.MethodName(method.Name)
            .Modifiers($"{access}{staticString}partial")
            .AddParameter(parameters)
            .ReturnType(method.ReturnType.ToDisplayString())
            .Async(method.IsAsync);
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

    //public static T AddBody<T>(this T builder, params string[] body) where T : MethodBase
    //{
    //    foreach (var item in body)
    //    {
    //        builder.Body.Add(item);
    //    }
    //    return builder;
    //}

    public static T AddBody<T>(this T builder, params Statement[] body) where T : MethodBase
    {
        foreach (var item in body)
        {
            item.Parent = builder;
            builder.Body.Add(item);
        }
        return builder;
    }

    public static T AddParameter<T>(this T builder, params string[] parameters)
        where T : MethodBase
    {
        foreach (var item in parameters)
        {
            builder.Parameters.Add(item);
        }
        return builder;
    }


    #region switch
    public static T AddSwitchStatement<T>(this T builder, string switchValue, Action<SwitchStatement> action) where T : MethodBase
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
        switchStatement.SwitchCases.Add(new SwitchCaseStatement { Condition = condition, Action = [ret], Parent = switchStatement.Parent });
        return switchStatement;
    }

    public static SwitchStatement AddReturnCase(this SwitchStatement switchStatement, string condition, params Statement[] statements)
    {
        statements.ForEach(s => s.Parent = switchStatement);
        switchStatement.SwitchCases.Add(new SwitchCaseStatement { Condition = condition, Action = [.. statements], Parent = switchStatement.Parent });
        return switchStatement;
    }

    public static SwitchStatement AddBreakCase(this SwitchStatement switchStatement, string condition, Statement action)
    {
        action.Parent = switchStatement;
        switchStatement.SwitchCases.Add(new SwitchCaseStatement { Condition = condition, Action = [action], IsBreak = true, Parent = switchStatement.Parent });
        return switchStatement;
    }

    public static SwitchStatement AddBreakCase(this SwitchStatement switchStatement, string condition, params Statement[] action)
    {
        action.ForEach(s => s.Parent = switchStatement);
        switchStatement.SwitchCases.Add(new SwitchCaseStatement { Condition = condition, Action = [.. action], IsBreak = true, Parent = switchStatement.Parent });
        return switchStatement;
    }

    public static SwitchStatement AddDefaultCase(this SwitchStatement switchStatement, string action)
    {
        switchStatement.DefaultCase = new DefaultCaseStatement { Action = action, Parent = switchStatement.Parent };
        return switchStatement;
    }
    #endregion

    #region if
    public static T AddIfStatement<T>(this T builder, string condition, Action<IfStatement> action) where T : MethodBase
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
        var lf = LocalFunction.Default;
        lf.Parent = builder;
        action.Invoke(lf);
        builder.AddBody(lf);
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

    public static ForeachStatement Foreach(this ForeachStatement builder, string loop)
    {
        builder.LoopContent = loop;
        return builder;
    }

    public static ForeachStatement AddStatements(this ForeachStatement builder, params Statement[] statements)
    {
        statements.ForEach(s => s.Parent = builder);
        builder.Contents.AddRange(statements);
        return builder;
    }

    #endregion

    #region try-catch

    public static T AddTryCatch<T>(this T builder, Action<TryCatch> action) where T : MethodBase
    {
        var trycatch = TryCatch.Default;
        trycatch.Parent = builder;
        action.Invoke(trycatch);
        builder.AddBody(trycatch);
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
        catchStatement.Parent = tryCatch.Parent;
        action.Invoke(catchStatement);
        tryCatch.Catchs.Add(catchStatement);
        return tryCatch;
    }
    public static TryCatch AddCatch(this TryCatch tryCatch, string? exception, params Statement[] statements)
    {
        var catchStatement = TryCatchException.Default;
        catchStatement.Exception = exception;
        catchStatement.Parent = tryCatch.Parent;
        foreach (var item in statements)
        {
            item.Parent = catchStatement;
            catchStatement.Body.Add(item);
        }
        tryCatch.Catchs.Add(catchStatement);
        return tryCatch;
    }

    public static TryCatchException AddBody(this TryCatchException tce, params Statement[] statements)
    {
        foreach (var item in statements)
        {
            item.Parent = tce;
            tce.Body.Add(item);
        }
        return tce;
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
