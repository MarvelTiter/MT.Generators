using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInjectGenerator
{
    internal class DiagnosticDefinitions
    {
        /// <summary>
        /// 未提供一个公开的静态的分部方法( public static partial )
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AIG00001(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AIG00001",
                        title: "未提供一个公开的静态的分部方法( public static partial )",
                        messageFormat: "未提供一个公开的静态的分部方法( public static partial )",
                        category: typeof(AutoInjectContextGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 配置冲突，Include和Exclude包含相同的值
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AIG00002(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AIG00002",
                        title: "配置冲突，Include和Exclude包含相同的值",
                        messageFormat: "配置冲突，Include和Exclude包含相同的值",
                        category: typeof(AutoInjectContextGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);
        /// <summary>
        /// 类型错误
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AIG00003(string serviceType, string implType,Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AIG00003",
                        title: "类型错误",
                        messageFormat: $"{implType}不能作为{serviceType}的实现注入",
                        category: typeof(AutoInjectContextGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 多次注入，相同的分组中使用了不同的生命周期
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AIG00004(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AIG00004",
                        title: "多次注入，相同的分组中使用了不同的生命周期",
                        messageFormat: "多次注入，相同的分组中使用了不同的生命周期",
                        category: typeof(AutoInjectContextGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 不能同时自定义Factory和Instance
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AIG00005(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AIG00005",
                        title: "不能同时自定义Factory和Instance",
                        messageFormat: "不能同时自定义Factory和Instance",
                        category: typeof(AutoInjectContextGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 未在目标类中中找到自定义Factory或者自定义Instance
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AIG00006(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AIG00006",
                        title: "未在目标类中中找到自定义Factory或者自定义Instance",
                        messageFormat: "未在目标类中中找到自定义Factory或者自定义Instance",
                        category: typeof(AutoInjectContextGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 相同分组中的相同生命周期中，只能有一个实现注入
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AIG00007(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AIG00007",
                        title: "相同分组中的相同生命周期中，只能有一个实现注入",
                        messageFormat: "相同分组中的相同生命周期中，只能有一个实现注入",
                        category: typeof(AutoInjectContextGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);
    }
}
