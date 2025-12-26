using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using AutoGenMapperGenerator;

namespace AutoGenMapperGenerator
{
    internal class DiagnosticDefinitions
    {
        /// <summary>
        /// 需要实现 IAutoMap 接口
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00001(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00001",
                        title: "需要实现 IAutoMap 接口",
                        messageFormat: "需要实现 IAutoMap 接口",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// MapBetweenAttribute的Target不能为空
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00002(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00002",
                        title: "MapBetweenAttribute的Target不能为空",
                        messageFormat: "MapBetweenAttribute的Target不能为空",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 无法找到自定义的映射方法，请检查是否定义在自己的类中
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00003(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00003",
                        title: "无法找到自定义的映射方法，请检查是否定义在自己的类中",
                        messageFormat: "无法找到自定义的映射方法，请检查是否定义在自己的类中",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 该MapBetweenAttribute的构造只能用于属性/方法
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00004(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00004",
                        title: "该MapBetweenAttribute的构造只能用于属性/方法",
                        messageFormat: "该MapBetweenAttribute的构造只能用于属性/方法",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 该MapBetweenAttribute的构造只能用于类
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00005(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00005",
                        title: "该MapBetweenAttribute的构造只能用于类",
                        messageFormat: "该MapBetweenAttribute的构造只能用于类",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 定义了单对多或者多对单的映射，但是没有提供对应的处理方法
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00006(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00006",
                        title: "定义了单对多或者多对单的映射，但是没有提供对应的处理方法",
                        messageFormat: "定义了单对多或者多对单的映射，但是没有提供对应的处理方法",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 自定义映射处理方法的参数个数或类型不匹配
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00007(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00007",
                        title: "自定义映射处理方法的参数个数或类型不匹配",
                        messageFormat: "自定义映射处理方法的参数个数或类型不匹配",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 自定义映射处理方法的返回值元组元素类型不匹配
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00008(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00008",
                        title: "自定义映射处理方法的返回值元组元素类型不匹配",
                        messageFormat: "自定义映射处理方法的返回值元组元素类型不匹配",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 自定义映射处理方法的返回值类型不匹配，请使用元组类型
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00009(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00009",
                        title: "自定义映射处理方法的返回值类型不匹配，请使用元组类型",
                        messageFormat: "自定义映射处理方法的返回值类型不匹配，请使用元组类型",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 自定义映射处理方法的返回值类型元素数量不匹配
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00010(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AGM00010",
                        title: "自定义映射处理方法的返回值类型元素数量不匹配",
                        messageFormat: "自定义映射处理方法的返回值类型元素数量不匹配",
                        category: typeof(AutoMapperGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);
        /// <summary>
        /// 目标类型没有提供可用的构造函数
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00011(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
            id: "AGM00011",
            title: "目标类型没有提供可用的构造函数",
            messageFormat: "目标类型没有提供可用的构造函数",
            category: typeof(AutoMapperGenerator).FullName!,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true), location);
        /// <summary>
        /// 未提供无参构造函数，可能无法支持部分MapFrom操作
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00012(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
            id: "AGM00012",
            title: "未提供无参构造函数，可能无法支持部分MapFrom操作",
            messageFormat: "未提供无参构造函数，可能无法支持部分MapFrom操作",
            category: typeof(AutoMapperGenerator).FullName!,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true), location);

        /// <summary>
        /// 自动匹配构造函数参数失败
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AGM00013(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
            id: "AGM00013",
            title: "自动匹配构造函数参数失败",
            messageFormat: "自动匹配构造函数参数失败",
            category: typeof(AutoMapperGenerator).FullName!,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true), location);
    }
}
