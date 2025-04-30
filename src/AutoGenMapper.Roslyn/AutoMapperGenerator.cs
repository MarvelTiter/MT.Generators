using System;
using System.Collections.Generic;
using System.Linq;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Generators.Shared;
using System.Diagnostics;

namespace AutoGenMapperGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class AutoMapperGenerator : IIncrementalGenerator
    {
        internal const string GenMapperAttributeFullName = "AutoGenMapperGenerator.GenMapperAttribute";
        internal const string GenMapFromAttributeFullName = "AutoGenMapperGenerator.MapFromAttribute";
        internal const string GenMapToAttributeFullName = "AutoGenMapperGenerator.MapToAttribute";
        internal const string GenMapableInterface = "AutoGenMapperGenerator.IAutoMap";
        internal const string GenMapIgnoreAttribute = "AutoGenMapperGenerator.MapIgnoreAttribute";
        internal const string GenMapBetweenAttributeFullName = "AutoGenMapperGenerator.MapBetweenAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var map = context.SyntaxProvider.ForAttributeWithMetadataName(GenMapperAttributeFullName
                , static (node, _) => node is ClassDeclarationSyntax
                , static (source, _) => source);

            context.RegisterSourceOutput(map, static (context, source) =>
            {
                var file = CreateCodeFile(context, source);
#if DEBUG
                var ss = file?.ToString();
#endif
                context.AddSource(file);
            });
        }

        private static CodeFile? CreateCodeFile(SourceProductionContext context, GeneratorAttributeSyntaxContext gasc)
        {
            var source = (INamedTypeSymbol)gasc.TargetSymbol;
            var hasDefaultCtor = source.Constructors.Any(c => c.Parameters.Length == 0);
            if (!hasDefaultCtor)
            {
                context.ReportDiagnostic(DiagnosticDefinitions.AGM00011(source.Locations.FirstOrDefault()));
            }
            var ns = NamespaceBuilder.Default.Namespace(source.ContainingNamespace.ToDisplayString());

            var typeInfos = source.GetAttributes(GenMapperAttributeFullName).Select(a => CollectTypeInfos(source, a)).ToArray();
            var errors = typeInfos.Where(r => r.Item2 is not null).Select(r => r.Item2).ToArray();
            if (errors.Length > 0)
            {
                foreach (var e in errors)
                {
                    context.ReportDiagnostic(e!);
                }

                return null;
            }

            GenMapperContext[] ctxs = [.. typeInfos.Where(r => r.Item1 is not null).Select(r => r.Item1)];
            var cb = ClassBuilder.Default.Modifiers("partial").ClassName(source.Name)
                .Interface(GenMapableInterface)
                .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator));
            List<MethodBuilder> methods = [];
            foreach (var ctx in ctxs)
            {
                var m = BuildAutoMapClass.GenerateMapToMethod(ctx);
                methods.Add(m);
                var f = BuildAutoMapClass.GenerateMapFromMethod(ctx);
                methods.Add(f);
            }
            
            //TODO 添加对Dictionary的支持
            

            var im = BuildAutoMapClass.GenerateInterfaceMethod(ctxs);
            methods.AddRange(im);
            cb.AddMembers([.. methods]);


            return CodeFile.New($"{source.FormatFileName()}.AutoMap.g.cs")
                //.AddUsings("using System.Linq;")
                //.AddUsings("using AutoGenMapperGenerator;")
                .AddUsings(source.GetTargetUsings())
                .AddMembers(ns.AddMembers(cb));
        }

        private static (GenMapperContext, Diagnostic?) CollectTypeInfos(INamedTypeSymbol source, AttributeData a)
        {
            var context = new GenMapperContext() { SourceType = source };
            if (!(a.GetNamedValue("TargetType", out var t) && t is INamedTypeSymbol target))
            {
                target = (a.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol) ?? source;
            }

            context.TargetType = target;

            var sourceProperties = source.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>().ToArray();
            var targetProperties = target.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>().ToArray();

            var mapInfos = new List<MapInfo>();

            // 定义在类上的MapBetween
            var topRules = GetSpecificDatas(source, target);
            if (topRules.Error is not null)
            {
                return (context, topRules.Error);
            }

            foreach (var item in topRules.AttrDatas)
            {
                var mi = new MapInfo() { Position = DeclarePosition.Class };
                var type = CheckMapType(item);
                item.GetConstructorValue(0, out var tar);
                switch (type)
                {
                    case MappingType.SingleToSingle:
                        {
                            item.GetConstructorValue(1, out var sp);
                            item.GetConstructorValue(2, out var tp);
                            mi.SourceName = [sp!.ToString()];
                            mi.SourceProp = [.. sourceProperties.Where(p => mi.SourceName.Contains(p.Name))];
                            mi.TargetName = [tp!.ToString()];
                            mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                            mi.MappingType = MappingType.SingleToSingle;
                            break;
                        }
                    case MappingType.SingleToMulti:
                        {
                            item.GetConstructorValue(1, out var sp);
                            item.GetConstructorValues(2, out var tps);
                            mi.SourceName = [sp!.ToString()];
                            mi.SourceProp = [.. sourceProperties.Where(p => mi.SourceName.Contains(p.Name))];
                            mi.TargetName = [.. tps.Select(tp => tp!.ToString())];
                            mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                            mi.MappingType = MappingType.SingleToMulti;
                            break;
                        }
                    case MappingType.MultiToSingle:
                        {
                            item.GetConstructorValues(1, out var sps);
                            item.GetConstructorValue(2, out var tp);
                            mi.SourceName = [.. sps.Select(sp => sp!.ToString())];
                            mi.SourceProp = [.. sourceProperties.Where(p => mi.SourceName.Contains(p.Name))];
                            mi.TargetName = [tp!.ToString()];
                            mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                            mi.MappingType = MappingType.MultiToSingle;
                            break;
                        }
                    default:
                        throw new Exception();
                }

                if (item.GetNamedValue("By", out var by))
                {
                    var methodName = by!.ToString();
                    var error = HandleByMethod(source, methodName, mi);
                    if (error is not null)
                    {
                        return (context, error);
                    }
                }

                if (type != MappingType.SingleToSingle && mi.ForwardBy is null)
                {
                    return (context, DiagnosticDefinitions.AGM00006(source.Locations.FirstOrDefault()));
                }

                mapInfos.Add(mi);
            }

            foreach (var sp in sourceProperties)
            {
                if (sp.IsReadOnly || sp.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                var mi = new MapInfo()
                {
                    SourceName = [sp.Name],
                    SourceProp = [sp],
                    MappingType = MappingType.SingleToSingle
                };
                // 属性上可能有多个Attribute，只找出跟当前Target一样的Target的Attribute
                var AttrData = GetSpecificData(sp, target);
                if (AttrData is not null)
                {
                    if (AttrData.ConstructorArguments.Length > 2)
                    {
                        return (context, DiagnosticDefinitions.AGM00005(source.Locations.FirstOrDefault()));
                    }

                    AttrData.GetConstructorValue(1, out var tarName);
                    // 已经在Class上定义了MapBetween，就跳过
                    if (mapInfos.Any(m => m.Position == DeclarePosition.Class
                                          && m.SourceName.Contains(sp.Name)
                                          && m.TargetName.Contains(tarName!.ToString())))
                    {
                        continue;
                    }

                    mi.TargetName = [tarName!.ToString()];
                    mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                    if (AttrData.GetNamedValue("By", out var by))
                    {
                        var methodName = by!.ToString();
                        var error = HandleByMethod(source, methodName, mi);
                        if (error is not null)
                        {
                            return (context, error);
                        }
                    }
                }
                else
                {
                    var tp = targetProperties.FirstOrDefault(tp => tp.Name == sp.Name);
                    if (tp is null)
                    {
                        continue;
                    }

                    mi.TargetName = [tp.Name];
                    mi.TargetProp = [tp];
                }

                mapInfos.Add(mi);
            }

            if (a.ConstructorArguments.Length > 1)
            {
                // 指定了构造函数参数
                context.ConstructorParameters =
                    [.. a.ConstructorArguments[1].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString())];
            }
            else
            {
                // 尝试自动获取构造函数参数
                var ctorSymbol = context.TargetType.GetMethods().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length > 0);
                ctorSymbol ??= context.TargetType.GetMethods().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor);
                if (ctorSymbol is null)
                {
                    return (context, DiagnosticDefinitions.AGM00011(target.Locations.FirstOrDefault()));
                }
                string[] ps = ctorSymbol.Parameters.Select(p => source.GetAllMembers(_ => true).FirstOrDefault(s => string.Equals(p.Name, s.Name, StringComparison.OrdinalIgnoreCase))?.Name).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (ps.Length != ctorSymbol.Parameters.Length)
                {
                    return (context, DiagnosticDefinitions.AGM00013(source.Locations.FirstOrDefault()));
                }
                context.ConstructorParameters = ps;
            }

            context.Maps = mapInfos;
            return (context, null);


            static AttributeData? GetSpecificData(IPropertySymbol property, INamedTypeSymbol matchSymbol)
            {
                var attrs = property.GetAttributes(GenMapBetweenAttributeFullName);
                foreach (var item in attrs)
                {
                    item.GetConstructorValue(0, out var t);
                    var symbol = (INamedTypeSymbol)t!;
                    if (EqualityComparer<INamedTypeSymbol>.Default.Equals(symbol, matchSymbol))
                    {
                        return item;
                    }
                }

                return null;
            }

            static (AttributeData[] AttrDatas, Diagnostic? Error) GetSpecificDatas(INamedTypeSymbol type, INamedTypeSymbol matchSymbol)
            {
                var attrs = type.GetAttributes(GenMapBetweenAttributeFullName);
                List<AttributeData> result = [];
                foreach (var item in attrs)
                {
                    if (item.ConstructorArguments.Length == 2)
                    {
                        return ([], DiagnosticDefinitions.AGM00004(type.Locations.FirstOrDefault()));
                    }

                    item.GetConstructorValue(0, out var t);
                    var symbol = (INamedTypeSymbol)t!;
                    if (EqualityComparer<INamedTypeSymbol>.Default.Equals(symbol, matchSymbol))
                    {
                        result.Add(item);
                    }
                }

                return ([.. result], null);
            }

            static MappingType CheckMapType(AttributeData data)
            {
                var s = data.ConstructorArguments[1];
                var t = data.ConstructorArguments[2];
                if (s.Type is IArrayTypeSymbol)
                {
                    return MappingType.MultiToSingle;
                }
                else if (t.Type is IArrayTypeSymbol)
                {
                    return MappingType.SingleToMulti;
                }
                else
                {
                    return MappingType.SingleToSingle;
                }
            }
        }

        private static bool CheckMappingMethodReturnType(IMethodSymbol? method
            , Location? location
            , ITypeSymbol[] paramTypes
            , ITypeSymbol[] returnTypes
            , out Diagnostic? error)
        {
            if (method is null)
            {
                // 自定义处理方法的参数个数不匹配
                error = DiagnosticDefinitions.AGM00007(location);
                return false;
            }
             location = method.Locations.FirstOrDefault();
            // 检查返回值
            if (returnTypes.Length > 1)
            {
                // 一对多的情况，返回值需要是object[]类型
                if (!method.ReturnType.IsTupleType)
                {
                    error = DiagnosticDefinitions.AGM00009(location);
                    return false;
                }
                if (method.ReturnType is not INamedTypeSymbol tuple)
                {
                    error = DiagnosticDefinitions.AGM00009(location);
                    return false;
                }
                var tupleElement = tuple.TupleElements;
                if (tupleElement.Length != returnTypes.Length)
                {
                    error = DiagnosticDefinitions.AGM00010(location);
                    return false;
                }
                for (int i = 0; i < tupleElement.Length; i++)
                {
                    var te = tupleElement[i].Type;
                    if (!EqualityComparer<ITypeSymbol>.Default.Equals(te, returnTypes[i]))
                    {
                        error = DiagnosticDefinitions.AGM00008(location);
                        return false;
                    }
                }
            }
            else
            {
                var returnType = returnTypes[0];
                if (!EqualityComparer<ITypeSymbol>.Default.Equals(method.ReturnType, returnType))
                {
                    error = DiagnosticDefinitions.AGM00009(location);
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool CheckParameters(IMethodSymbol method, ITypeSymbol[] paramTypes)
        {
            // 检查参数类型
            if (method.Parameters.Length != paramTypes.Length) return false;
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var mpt = method.Parameters[i].Type;
                if (!EqualityComparer<ITypeSymbol>.Default.Equals(mpt, paramTypes[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static Diagnostic? HandleByMethod(INamedTypeSymbol source
            , string methodName
            , MapInfo mi)
        {
            var methods = source.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Method && i.Name == methodName).Cast<IMethodSymbol>().ToArray();
            if (methods.Length == 0)
            {
                return DiagnosticDefinitions.AGM00003(source.Locations.FirstOrDefault());
            }

            var spts = mi.SourceProp.Select(p => p.Type).ToArray();
            var tpts = mi.TargetProp.Select(p => p.Type).ToArray();

            #region 正向映射

            var forward = methods.FirstOrDefault(m => CheckParameters(m, spts));
            if (!CheckMappingMethodReturnType(forward
                    , source.Locations.FirstOrDefault()
                    , spts
                    , tpts
                    , out var error))
            {
                return error;
            }

            mi.ForwardBy = forward;

            #endregion

            #region 反向映射

            var reverse = methods.FirstOrDefault(m => CheckParameters(m, tpts));
            if (CheckMappingMethodReturnType(reverse
                    , source.Locations.FirstOrDefault()
                    , tpts
                    , spts
                    , out error))
            {
                mi.CanReverse = true;
                mi.ReverseBy = reverse;
            }
            else if (reverse != null)
            {
                return error;
            }

            #endregion

            return null;
        }
    }
}