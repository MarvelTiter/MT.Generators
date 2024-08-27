using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoAopProxyGenerator
{
    internal class DiagnosticDefinitions
    {
        /// <summary>
        /// AutoAopProxyGenerator.AddAspectHandlerAttribute.AspectType不能为null
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AAPG00001(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AAPG00001",
                        title: "AutoAopProxyGenerator.AddAspectHandlerAttribute.AspectType不能为null",
                        messageFormat: "AutoAopProxyGenerator.AddAspectHandlerAttribute.AspectType不能为null",
                        category: typeof(AutoAopProxyClassGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);

        /// <summary>
        /// AutoAopProxyGenerator.AddAspectHandlerAttribute.AspectType不实现IAspectHandler
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Diagnostic AAPG00002(Location? location) => Diagnostic.Create(new DiagnosticDescriptor(
                        id: "AAPG00002",
                        title: "AutoAopProxyGenerator.AddAspectHandlerAttribute.AspectType不实现IAspectHandler",
                        messageFormat: "AutoAopProxyGenerator.AddAspectHandlerAttribute.AspectType不实现IAspectHandler",
                        category: typeof(AutoAopProxyClassGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true), location);
    }
}
