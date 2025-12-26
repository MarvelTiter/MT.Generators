using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGenMapperGenerator;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TSource"></typeparam>
/// <typeparam name="TTarget"></typeparam>
public readonly struct MappingContext<TSource, TTarget>(TSource source, TTarget target)
    where TSource : class
    where TTarget : class
{
    /// <summary>
    /// 源数据
    /// </summary>
    public TSource Source { get; } = source;
    /// <summary>
    /// 目标数据
    /// </summary>
    public TTarget Target { get; } = target;
}
//internal class MyClass
//{
//    public int Age { get; set; }
//    public string Name { get; set; } = string.Empty;
//}
//internal class Test
//{
//    public void MapTest(MappingContext<MyClass, MyClass> context)
//    {
//        context.Target.Age = context.Source.Age + 1;
//        context.Target.Name = context.Source.Name + "!";
//    }
//}
