using Generators.Shared.Builder;
using Generators.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Generators.Shared;

internal static class MethodBuilderExtensions
{
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
        switchStatement.SwitchCases.Add(new SwitchCaseStatement { Condition = condition, Action = $"return {returnItem}", Parent = switchStatement.Parent });
        return switchStatement;
    }

    public static SwitchStatement AddBreakCase(this SwitchStatement switchStatement, string condition, string action)
    {
        switchStatement.SwitchCases.Add(new SwitchCaseStatement { Condition = condition, Action = action, IsBreak = true, Parent = switchStatement.Parent });
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
}
