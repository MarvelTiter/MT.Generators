﻿using Microsoft.CodeAnalysis;
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
                        category: typeof(ApiInvokerGenerator).FullName!,
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
                        category: typeof(ApiInvokerGenerator).FullName!,
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
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 仅支持异步方法
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00005(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00005",
                        title: "仅支持异步方法",
                        messageFormat: "仅支持异步方法",
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 路由参数
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00006(Location? location, string? symbolString = null) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00006",
                        title: "路由中未包含路由参数",
                        messageFormat: $"路由中未包含路由参数({symbolString})",
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 不能同时设置[FromBody]和[FromForm]
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00007(Location? location, string? symbolString = null) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00007",
                        title: "不能同时设置[FromBody]和[FromForm]",
                        messageFormat: $"不能同时设置[FromBody]和[FromForm]({symbolString})",
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 不能设置多个[FromBody]
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00008(Location? location, string? symbolString = null) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00008",
                        title: "不能设置多个[FromBody]",
                        messageFormat: $"不能设置多个[FromBody]({symbolString})",
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 暂不支持的返回值类型
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00009(Location? location, string? symbolString = null) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00009",
                        title: "暂不支持的返回值类型",
                        messageFormat: $"暂不支持的返回值类型({symbolString})",
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// 不支持的元组属性类型
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic WAG00010(Location? location, string? message = null) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "WAG00010",
                        title: "不支持的元组属性类型",
                        messageFormat: $"不支持的元组属性类型({message})",
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);
    }
}
