; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WAG00001 | AutoWasmApiGenerator.ControllerGenerator | Error | 继承多个接口需要指定接口标注[WebControllerAttribute]
WAG00002 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 无法为该类型生成WebApi调用类，缺少接口
WAG00003 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 方法参数过多
WAG00004 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 控制器（controller）不能包含泛型
WAG00005 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 仅支持异步方法
WAG00006 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 路由中未包含路由参数({0})
WAG00007 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 不能同时设置[FromBody]和[FromForm]({0})
WAG00008 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 不能设置多个[FromBody]({0})
WAG00009 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 暂不支持的返回值类型({0})
WAG00010 | AutoWasmApiGenerator.HttpServiceInvokerGenerator | Error | 生成服务调用器过程中发生错误: {0}
