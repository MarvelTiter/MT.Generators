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
    }
}
