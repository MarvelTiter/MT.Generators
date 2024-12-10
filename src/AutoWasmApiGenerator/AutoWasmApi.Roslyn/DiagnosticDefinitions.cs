using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
// ReSharper disable InconsistentNaming

namespace AutoWasmApiGenerator
{
    internal class DiagnosticDefinitions
    {
        private static readonly DiagnosticDescriptor Wag00001DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00001",
                title: "继承多个接口需要指定接口标注[WebControllerAttribute]",
                messageFormat: "继承多个接口需要指定接口标注[WebControllerAttribute]",
                category: typeof(ControllerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00002DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00002",
                title: "无法为该类型生成WebApi调用类，缺少接口",
                messageFormat: "无法为该类型生成WebApi调用类，缺少接口",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00003DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00003",
                title: "方法参数过多",
                messageFormat: "方法参数过多",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00004DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00004",
                title: "控制器（controller）不能包含泛型",
                messageFormat: "控制器（controller）不能包含泛型",
                category: typeof(ControllerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00005DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00005",
                title: "仅支持异步方法",
                messageFormat: "仅支持异步方法",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00006DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00006",
                title: "路由中未包含路由参数",
                messageFormat: "路由中未包含路由参数({0})",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00007DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00007",
                title: "不能同时设置[FromBody]和[FromForm]",
                messageFormat: "不能同时设置[FromBody]和[FromForm]({0})",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00008DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00008",
                title: "不能设置多个[FromBody]",
                messageFormat: "不能设置多个[FromBody]({0})",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);


        private static readonly DiagnosticDescriptor Wag00009DiagDescriptor = new DiagnosticDescriptor(
                id: "WAG00009",
                title: "暂不支持的返回值类型",
                messageFormat: "暂不支持的返回值类型({0})",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor Wag00010DiagDescriptor = new(
                id: "WAG00010",
                title: "生成错误",
                messageFormat: "生成过程中发生错误: {0}",
                category: typeof(HttpServiceInvokerGenerator).FullName!,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

        private static readonly DiagnosticDescriptor Wag00011DiagDescriptor = new(
            id: "WAG00011",
            title: "属性依赖错误",
            messageFormat: "AutoWasmApiGenerator.ApiInvokerGenerateAttribute需要同时设置AutoWasmApiGenerator.WebControllerAttribute",
            category: typeof(HttpServiceInvokerGenerator).FullName!,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        /// <summary>
        /// 继承多个接口需要指定接口标注[WebControllerAttribute]
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00001(Location? location)
        {
            return Diagnostic.Create(Wag00001DiagDescriptor, location);
        }

        /// <summary>
        /// 无法为该类型生成WebApi调用类，缺少接口
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00002(Location? location)
        {
            return Diagnostic.Create(Wag00002DiagDescriptor, location);
        }

        /// <summary>
        /// 方法参数过多
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00003(Location? location)
        {
            return Diagnostic.Create(Wag00003DiagDescriptor, location);
        }

        /// <summary>
        /// 控制器（controller）不能包含泛型
        /// </summary>
        /// <param name="location"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static Diagnostic WAG00004(Location? location, string? symbolString = null)
        {
            return Diagnostic.Create(Wag00004DiagDescriptor, location, symbolString);
        }

        /// <summary>
        /// 仅支持异步方法
        /// </summary>
        /// <param name="location"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static Diagnostic WAG00005(Location? location, string? symbolString = null)
        {
            return Diagnostic.Create(Wag00005DiagDescriptor, location, symbolString);
        }

        /// <summary>
        /// 路由中未包含路由参数
        /// </summary>
        /// <param name="location"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static Diagnostic WAG00006(Location? location, string? symbolString = null)
        {
            return Diagnostic.Create(Wag00006DiagDescriptor, location, symbolString);
        }

        /// <summary>
        /// 不能同时设置[FromBody]和[FromForm]
        /// </summary>
        /// <param name="location"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static Diagnostic WAG00007(Location? location, string? symbolString = null)
        {
            return Diagnostic.Create(Wag00007DiagDescriptor, location, symbolString);
        }

        /// <summary>
        /// 不能设置多个[FromBody]
        /// </summary>
        /// <param name="location"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static Diagnostic WAG00008(Location? location, string? symbolString = null)
        {
            return Diagnostic.Create(Wag00008DiagDescriptor, location, symbolString);
        }

        /// <summary>
        /// 暂不支持的返回值类型
        /// </summary>
        /// <param name="location"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static Diagnostic WAG00009(Location? location, string? symbolString = null)
        {
            return Diagnostic.Create(Wag00009DiagDescriptor, location, symbolString);
        }

        /// <summary>
        /// 生成错误
        /// </summary>
        /// <param name="location"></param>
        /// <param name="symbolString"></param>
        /// <returns></returns>
        public static Diagnostic WAG00010(Location? location, string symbolString)
        {
            return Diagnostic.Create(Wag00010DiagDescriptor, location, symbolString);
        }

        /// <summary>
        /// 属性依赖错误
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00011(Location? location)
        {
            return Diagnostic.Create(Wag00011DiagDescriptor, location);
        }
    }
}
