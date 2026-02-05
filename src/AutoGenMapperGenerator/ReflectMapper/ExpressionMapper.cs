using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static AutoGenMapperGenerator.ReflectMapper.ExpressionHelper;
namespace AutoGenMapperGenerator.ReflectMapper;

internal static partial class ExpressionMapper<
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
TSource,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
TTarget>
{
    private static readonly Func<TSource, TTarget> mapFunc;

    public static TTarget Map(TSource source) => mapFunc(source);
    static ExpressionMapper()
    {
        var sourceParam = Expression.Parameter(typeof(TSource), "source");

        // 获取目标类型的属性信息
        var targetPropsDict = typeof(TTarget)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name);

        // 获取源类型的属性信息 (用于自动映射)
        var sourcePropsDict = typeof(TSource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name);

        NewExpression? targetNew;
        var bindings = new List<MemberBinding>();
        // --- 阶段 1: 处理 Profile (如果存在) ---
        if (MapperOptions.Instance.TryGetProfile<TSource, TTarget>(out var profile))
        {
            // 获取 Profile 中定义的所有配置
            var configurations = profile.GetConfigurations();
            var ctorParams = profile.GetConstructorParameters();
            var ignores = profile.GetIgnoreMembers();
            foreach (var item in ignores)
            {
                sourcePropsDict.Remove(item);
            }
            if (ctorParams.Count > 0)
            {
                targetNew = CreateNewWithParameters(sourceParam, ctorParams, name =>
                {
                    // 优化: 从自动映射字典中移除已处理的属性，防止后面重复处理
                    sourcePropsDict.Remove(name);
                });
            }
            else
            {
                targetNew = Expression.New(typeof(TTarget));
            }
            foreach (var config in configurations)
            {
                if (config.SourceExpression == null || config.DestinationExpression == null) continue;

                // 1. 解析目标表达式 (例如: t => t.TargetName)
                var targetMember = GetMemberInfoFromLambda(config.DestinationExpression);
                if (targetMember == null) continue;

                // 2. 转换源表达式：将 Profile 中定义的表达式转换为基于当前 sourceParam 的表达式
                // 例如：将 (s) => s.A + s.B 转换为基于当前上下文的表达式
                var sourceExpressionBody = config.SourceExpression.Body;

                // 关键步骤：替换表达式中的参数
                var modifiedSourceBody = ReplaceParameter(config.SourceExpression.Parameters[0], sourceParam, sourceExpressionBody);

                // 4. 创建绑定
                var bind = Expression.Bind(targetMember, modifiedSourceBody);
                bindings.Add(bind);

                // 优化: 从自动映射字典中移除已处理的属性，防止后面重复处理
                targetPropsDict.Remove(targetMember.Name);
            }
        }
        else
        {
            targetNew = Expression.New(typeof(TTarget));
        }

        // 剩下的属性按默认规则处理 (名称匹配)
        foreach (var targetProp in targetPropsDict.Values)
        {
            // 检查源对象中是否存在同名属性
            if (!sourcePropsDict.TryGetValue(targetProp.Name, out var sourceProp))
            {
                continue;
            }
            var sourceType = sourceProp.PropertyType;
            var targetType = targetProp.PropertyType;
            var sp = IsComplexType(sourceType);
            var tp = IsComplexType(targetType);
            Expression convertedSource;
            if ((sp.IsEnumerable || tp.IsEnumerable) && !(sp.IsDictionary || tp.IsDictionary)) // 排除字典
            {
                convertedSource = HandleCollectionConversion(
                    Expression.MakeMemberAccess(sourceParam, sourceProp),
                    sourceType,
                    targetType);
            }
            else if (sp.IsComplex || tp.IsComplex)
            {
                var sourceAccess = Expression.MakeMemberAccess(sourceParam, sourceProp);
                if (sp.IsDictionary && tp.IsDictionary)
                {
                    throw new InvalidOperationException("目标类型和数据源类型都是字典");
                }
                if (sp.IsDictionary)
                {
                    var method = typeof(ExpressionMapper<>).MakeGenericType(targetType).GetMethod("MapFromDictionary", [typeof(IDictionary<string, object?>)])!;
                    convertedSource = Expression.Call(method, sourceAccess);
                }
                else if (tp.IsDictionary)
                {
                    var method = typeof(ExpressionMapper<>).MakeGenericType(sourceType).GetMethod("MapToDictionary", [sourceType])!;
                    convertedSource = Expression.Call(method, sourceAccess);
                }
                else
                {
                    var method = typeof(ExpressionMapper<,>).MakeGenericType(sourceType, targetType).GetMethod("Map", [sourceType])!;
                    convertedSource = Expression.Call(method, sourceAccess);
                }
                convertedSource = Expression.Condition(Expression.Equal(sourceAccess, Expression.Default(sourceProp.PropertyType)),
                    Expression.Default(targetProp.PropertyType),
                    convertedSource);
            }
            else
            {
                var sourceAccess = Expression.MakeMemberAccess(sourceParam, sourceProp);
                convertedSource = GetConversionExpression(
                   sourceProp.PropertyType,
                   sourceAccess,
                   targetProp.PropertyType, System.Globalization.CultureInfo.CurrentCulture);
            }

            var bind = Expression.Bind(targetProp, convertedSource);
            bindings.Add(bind);
        }

        // --- 构建表达式树 ---
        var memberInit = Expression.MemberInit(targetNew, bindings);
        var lambda = Expression.Lambda<Func<TSource, TTarget>>(memberInit, sourceParam);
        mapFunc = lambda.Compile();
    }

    private static NewExpression CreateNewWithParameters(ParameterExpression source
        , IReadOnlyList<(Type, string)> parameters, Action<string> each)
    {
        var ctorInfo = typeof(TTarget).GetConstructors().FirstOrDefault(FindConstructor) ?? throw new InvalidOperationException("No matching constructor found for the specified parameters.");

#pragma warning disable IL2026
        List<Expression> cps = [];
        foreach (var item in parameters)
        {
            cps.Add(Expression.Property(source, item.Item2));
            each.Invoke(item.Item2);
        }
        return Expression.New(ctorInfo, cps);
#pragma warning restore IL2026 

        bool FindConstructor(ConstructorInfo ctor)
        {
            var pp = ctor.GetParameters();
            if (pp.Length != parameters.Count) return false;
            for (int i = 0; i < pp.Length; i++)
            {
                if (pp[i].ParameterType != parameters[i].Item1) return false;
            }
            return true;
        }
    }

    private static Expression HandleCollectionConversion(Expression sourceExpression, Type sourceType, Type targetType)
    {
        // 获取元素类型
        // 例如：List<Source> -> elementTye = Source
        var sourceElementType = GetEnumerableElementType(sourceType);
        var targetElementType = GetEnumerableElementType(targetType);

        if (sourceElementType == null || targetElementType == null)
        {
            // 如果无法获取元素类型，尝试直接转换（可能是 object[] 转 List<object> 等简单情况）
            return Expression.Convert(sourceExpression, targetType);
        }

        // --- 构建 Select 表达式 ---
        // 1. 创建参数: TSourceElement item
        var itemParam = Expression.Parameter(sourceElementType, "item");

        // 2. 构建转换逻辑: 将 item 转换为目标元素类型
        Expression conversionExpr;

        // 如果元素是简单类型或不需要特殊处理，直接使用 Convert
        // 如果元素是复杂类型，使用 ExpressionMapper<TSourceElement, TTargetElement>.Map(item)
        var elementSp = IsComplexType(sourceElementType);
        var elementTp = IsComplexType(targetElementType);
        if (elementSp.IsComplex || elementTp.IsComplex)
        {
            var mapperType = typeof(ExpressionMapper<,>).MakeGenericType(sourceElementType, targetElementType);
            var mapMethod = mapperType.GetMethod("Map", BindingFlags.Public | BindingFlags.Static);
            conversionExpr = Expression.Call(mapMethod, itemParam);
        }
        else if (elementSp.IsDictionary)
        {
            var method = typeof(ExpressionMapper<>).MakeGenericType(targetType).GetMethod("MapFromDictionary", [typeof(IDictionary<string, object?>)])!;
            conversionExpr = Expression.Call(method, itemParam);
        }
        else if (elementTp.IsDictionary)
        {
            var method = typeof(ExpressionMapper<>).MakeGenericType(sourceType).GetMethod("MapToDictionary", [sourceType])!;
            conversionExpr = Expression.Call(method, itemParam);
        }
        else
        {
            conversionExpr = Expression.Convert(itemParam, targetElementType);
        }

        // 3. 构建 Lambda: item => conversionExpr
        var selector = Expression.Lambda(conversionExpr, itemParam);

        // --- 构建 LINQ 查询 ---
        // 这里使用 Enumerable.Select 和 Enumerable.ToList
        Expression result;
        try
        {
            // 1. 调用 Select: sourceExpression.AsQueryable().Select(selector)
            // 注意：对于 IEnumerable<T>，直接使用 Enumerable.Select
            var selectMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .MakeGenericMethod(sourceElementType, targetElementType);

            var selectCall = Expression.Call(selectMethod, sourceExpression, selector);

            // 2. 调用 ToList: Select(...).ToList()
            // 这里需要根据目标类型决定是 ToList 还是 ToArray
            if (targetType.IsArray)
            {
                // 如果目标是数组，调用 ToArray()
                var toArrayMethod = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(targetElementType);
                result = Expression.Call(toArrayMethod, selectCall);
            }
            else
            {
                // 如果目标是 List<T> 或 IEnumerable<T>，调用 ToList()
                targetType = typeof(List<>).MakeGenericType(targetElementType);
                var toListMethod = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(targetElementType);
                result = Expression.Call(toListMethod, selectCall);
            }
        }
        catch (Exception ex)
        {
            // 如果 Linq 方法调用失败（例如方法未找到），回退到直接转换
            result = Expression.Convert(sourceExpression, targetType);
        }
        return Expression.Condition(Expression.Equal(sourceExpression, Expression.Default(sourceType))
            , Expression.Default(targetType)
            , result);


        static Type? GetEnumerableElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            foreach (var iface in type.GetInterfaces().Concat([type]))
            {
                if (iface.IsGenericType)
                {
                    var def = iface.GetGenericTypeDefinition();
                    if (def == typeof(IEnumerable<>) || def == typeof(ICollection<>) || def == typeof(IList<>))
                    {
                        return iface.GetGenericArguments()[0];
                    }
                }
            }

            return null;
        }
    }


    private static Expression ReplaceParameter(ParameterExpression oldParam, ParameterExpression newParam, Expression expression)
    {

        var visitor = new ParameterReplaceVisitor(oldParam, newParam);
        return visitor.Visit(expression);
    }
    class ParameterReplaceVisitor(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            // 如果节点是我们要替换的旧参数，返回新参数
            return node == oldParameter ? newParameter : base.VisitParameter(node);
        }
    }
}
