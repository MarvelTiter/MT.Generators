using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenMapperGenerator.ReflectMapper;
internal static class ExpressionMapper<TEntity>
{
    private static readonly Func<IDictionary<string, object?>, TEntity> mapFromDictionaryFunc;
    private static readonly Func<TEntity, IDictionary<string, object?>> mapToDictionaryFunc;
    public static TEntity MapFromDictionary(IDictionary<string, object?> dict) => mapFromDictionaryFunc(dict);
    public static IDictionary<string, object?> MapToDictionary(TEntity entity) => mapToDictionaryFunc(entity);
    static ExpressionMapper()
    {
        mapFromDictionaryFunc = CreateMapFromDelegate();
        mapToDictionaryFunc = CreateMapToDelegate();
    }

    private static Func<TEntity, IDictionary<string, object?>> CreateMapToDelegate()
    {
        // 参数: TEntity entity
        var entityParam = Expression.Parameter(typeof(TEntity), "entity");

        // 1. 创建新字典: new Dictionary<string, object>()
        var dictionaryType = typeof(Dictionary<string, object?>);
        var newDict = Expression.New(dictionaryType);

        // 2. 获取 TEntity 的所有属性
        var properties = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        // 3. 构建初始化器列表
        var initializers = new List<ElementInit>();

        foreach (var prop in properties)
        {
            // 字典的 Add 方法
            var addMethod = dictionaryType.GetMethod("Add")!;

            // Key: 属性名
            var keyExpr = Expression.Constant(prop.Name);

            // Value: entity.Property (需要转换为 object)
            var valueExpr = Expression.MakeMemberAccess(entityParam, prop);
            var convertedValue = Expression.Convert(valueExpr, typeof(object));

            // 构建 Add 调用: .Add("PropertyName", (object)entity.Property)
            var elementInit = Expression.ElementInit(addMethod, keyExpr, convertedValue);
            initializers.Add(elementInit);
        }

        // 4. 构建集合初始化表达式
        var initExpr = Expression.ListInit(newDict, initializers);

        // 5. 构建 Lambda
        var lambda = Expression.Lambda<Func<TEntity, IDictionary<string, object?>>>(initExpr, entityParam);

        return lambda.Compile();
    }

    private static readonly MethodInfo tryGetValueMethod = typeof(DictionaryExtensions).GetMethod("TryGetValue")!;
    private static Func<IDictionary<string, object?>, TEntity> CreateMapFromDelegate()
    {
        var dictParam = Expression.Parameter(typeof(IDictionary<string, object?>), "dict");

        // 创建新实体
        var newExpr = Expression.New(typeof(TEntity));
        var properties = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);
        var bindings = new List<MemberBinding>();
        // 1. 声明一个局部变量用于接收值: out object resultValue
        ParameterExpression? outVariable = Expression.Variable(typeof(object), "value");
        var assign = Expression.Assign(outVariable, Expression.Constant(null, typeof(object)));
        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;
            //var (IsComplex, IsDictionary, IsEnumerable) = ExpressionHelper.IsComplexType(propType);

            var keyConst = Expression.Constant(prop.Name);
            // 2. 调用 TryGetValue 方法
            var genericTryGetValueMethod = tryGetValueMethod.MakeGenericMethod(prop.PropertyType);
            var tryGetValueCall = Expression.Call(genericTryGetValueMethod, dictParam, keyConst, outVariable);
            //var tryGetValueCall = Expression.Call(dictParam, tryGetValueMethod, keyConst, outVariable);

            // 3. 构建转换逻辑：如果 TryGetValue 返回 true，则转换 outVariable；否则使用默认值
            //var conversionExpr = ExpressionHelper.GetConversionExpression(typeof(object), outVariable, propType, CultureInfo.CurrentCulture);
            var conversionExpr = BuildConversionFromDictionaryValue(outVariable, propType);

            // 4. 组合条件表达式: dict.TryGetValue(key, out value) ? Convert(value) : default(TProp)
            var conditionExpr = Expression.Condition(
                tryGetValueCall, // 条件: TryGetValue 的结果
                conversionExpr, // 成功: 转换值
                Expression.Default(propType) // 失败: 默认值
            );

            //var block = Expression.Block([outVariable], assign, conditionExpr);
            var bind = Expression.Bind(prop, conditionExpr);
            bindings.Add(bind);
        }

        var memberInit = Expression.MemberInit(newExpr, bindings);
        var block = Expression.Block([outVariable], assign, memberInit);
        var lambda = Expression.Lambda<Func<IDictionary<string, object?>, TEntity>>(block, dictParam);
        return lambda.Compile();

        static Expression BuildConversionFromDictionaryValue(Expression sourceExpr, Type targetType)
        {
            // 如果目标类型是 object 或 dynamic，直接返回
            if (targetType == typeof(object))
            {
                return sourceExpr;
            }
            
            // 处理可空类型
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            var conversionTargetType = underlyingType ?? targetType;

            // 如果是枚举
            if (conversionTargetType.IsEnum)
            {
                // 调用 Enum.Parse
                var parseMethod = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) })!;
                var toStringCall = Expression.Call(sourceExpr, typeof(object).GetMethod("ToString")!);
                var callExpr = Expression.Call(
                    parseMethod,
                    Expression.Constant(conversionTargetType),
                    toStringCall,
                    Expression.Constant(true)
                );
                return Expression.Convert(callExpr, targetType);
            }
            return Expression.Convert(sourceExpr, targetType);
        }
    }
}
