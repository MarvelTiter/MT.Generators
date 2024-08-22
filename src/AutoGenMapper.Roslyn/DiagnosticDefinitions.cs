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
    }
}
