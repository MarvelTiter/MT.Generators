using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoWasmApiGenerator
{
    internal class DiagnosticDefinitions
    {
        /// <summary>
        /// 继承多个接口需要指定接口标注[WebControllerAttribute]
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00001(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00001",
                        title: "继承多个接口需要指定接口标注[WebControllerAttribute]",
                        messageFormat: "继承多个接口需要指定接口标注[WebControllerAttribute]",
                        category: typeof(ControllerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 无法为该类型生成WebApi调用类，缺少接口
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00002(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00002",
                        title: "无法为该类型生成WebApi调用类，缺少接口",
                        messageFormat: "无法为该类型生成WebApi调用类，缺少接口",
                        category: typeof(HttpServiceInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 方法参数过多
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00003(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00003",
                        title: "方法参数过多",
                        messageFormat: "方法参数过多",
                        category: typeof(HttpServiceInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 控制器（controller）不能包含泛型
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00004(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00004",
                        title: "控制器（controller）不能包含泛型",
                        messageFormat: "控制器（controller）不能包含泛型",
                        category: typeof(HttpServiceInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);
    }
}
