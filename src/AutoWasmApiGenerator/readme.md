# AutoWasmApiGenerator

## 介绍

本项目是一个增量生成器，用于生成 BlazorServer 项目的 WebApi 接口，以便在 BlazorAuto 模式下使用。

> https://www.cnblogs.com/marvelTitile/p/18360614

## 使用

### 项目结构

- `Server`端项目
- `Client`端项目
- `Shared`双端共用的项目，一般是共用的页面、实体模型、等等

### 使用步骤

1. 在 Server 项目中引用 AutoWasmApiGenerator 项目
   ```csharp
   <PackageReference Include="AutoWasmApiGenerator" Version="0.0.*" />
   ```
2. 在 Shared 或 Client 项目中添加接口
   ```csharp
   [WebController]
   public interface ITestService
   {
   	[WebMethod(Method = WebMethod.Post)] // 默认指定为Post
   	Task<bool> LogAsync(string message, string path, CancellationToken token);
   }
   ```
3. 在 Server 项目中任意一个文件标注`WebControllerAssemblyAttribute`, 生成的文件为`TestServiceController`
   ```csharp
   [assembly: AutoWasmApiGenerator.WebControllerAssembly]
   ```
4. 在 Client 项目中或生成控制器调用类的项目中的任意一个文件标注`ApiInvokerAssemblyAttribute`, 生成的文件为`TestServiceApiInvoker`
   ```csharp
   [assembly: AutoWasmApiGenerator.ApiInvokerAssembly]
   ```
5. 在 Client 项目中注册
   ```csharp
   builder.Services.AddScoped<ITestService, TestServiceApiInvoker>();
   ```
6. 通过`ITestService`调用服务
   ```csharp
   @code
   {
   	[Inject] public ITestService TestService { get; set; }
   	private async Task LogAsync()
   	{
   		await TestService.LogAsync("Hello World", "path", CancellationToken.None);
   	}
   }
   ```

## 相关特性介绍

### WebControllerAttribute

标注服务接口，根据接口生成控制器和调用类

### WebMethodNotSupportedAttribute

标注方法，表示不生成对应的 WebApi

### ApiInvokeNotSupportedAttribute

标注类，表示不生成调用类；标注方法， 表示不生成调用方法（调用该方法，将抛出`NotSupportedException`）

### WebMethodAttribute

标注方法，指定请求方式

```csharp
[WebMethod(Method = WebMethod.Post)]
Task<bool> LogAsync(string message);
```

#### 属性

| 名称           | 类型                    | 说明                                    |
| -------------- | ----------------------- | --------------------------------------- |
| Method         | [WebMethod](#webmethod) | 指定请求方法，默认为 Post               |
| Route          | string?                 | 指定 Action 路由，null 时为方法名称     |
| AllowAnonymous | bool                    | 是否支持匿名访问，会覆盖 Authorize 设置 |
| Authorize      | bool                    | 是否需要授权                            |

### WebMethod

可能的值

- Get
- Post
- Put
- Delete

### WebMethodParameterBindingAttribute

标注参数，指定参数绑定方式

```csharp
[WebMethod(Method = WebMethod.Post)]
Task<bool> Log3Async([WebMethodParameterBinding(BindingType.FromBody)] string message, [WebMethodParameterBinding(BindingType.FromQuery)] string path,[WebMethodParameterBinding(BindingType.Ignore)] CancellationToken token);
```

#### 属性

| 名称 | 类型                        | 说明         |
| ---- | --------------------------- | ------------ |
| Type | [BindingType](#bindingtype) | 参数绑定类型 |

#### BindingType

- Ignore 忽略
- FromQuery 从查询字符串中获取值。
- FromRoute 从路由数据中获取值。
- FromForm 从发布的表单域中获取值。
- FromBody 从请求正文中获取值。
- FromHeader 从 HTTP 标头中获取值。
- FromServices 从服务容器中获取值。

## 可注入服务

### IGeneratedApiInvokeDelegatingHandler

接口调用类的切面入口

- Task BeforeSendAsync([SendContext](#sendcontext) context);
- Task AfterSendAsync([SendContext](#sendcontext) context);
- Task OnExceptionAsync([ExceptionContext](#exceptioncontext) context);

使用示例

```csharp
[AutoInject(Group = "WASM", ServiceType = typeof(IGeneratedApiInvokeDelegatingHandler))]
public class GeneratedApiHandler(ILogger<GeneratedApiHandler> logger, IUIService ui) : GeneratedApiInvokeDelegatingHandler
{
    public override Task BeforeSendAsync(SendContext context)
    {
        logger.LogDebug("before request {Message}", context.TargetMethod);
        return base.BeforeSendAsync(context);
    }
    public override Task OnExceptionAsync(ExceptionContext context)
    {
        logger.LogDebug("请求发生异常: {Message}", context.Exception.Message);
        if (context.SendContext.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            context.Handled = true;
            ui.Error("用户过期，请重新登录");
        }
        return Task.CompletedTask;
    }
}
```

#### SendContext

| 名称         | 类型                 | 说明                    |
| ------------ | -------------------- | ----------------------- |
| TargetType   | Type                 | 接口类型                |
| TargetMethod | string               | 方法名称                |
| Parameters   | object?[]?           | 方法参数                |
| ReturnType   | Type?                | 方法返回值              |
| Request      | HttpRequestMessage   | HttpClient 的请求上下文 |
| Response     | HttpResponseMessage? | HttpClient 的响应上下文 |

#### ExceptionContext

| 名称        | 类型                        | 说明                               |
| ----------- | --------------------------- | ---------------------------------- |
| SendContext | [SendContext](#sendcontext) | 请求上下文                         |
| Exception   | Exception                   | 异常                               |
| Handled     | bool                        | 异常是否已经处理，未处理则抛出异常 |

## 异常返回值处理

### AddAutoWasmErrorResultHandler

```csharp
// 当接口调用类发生异常时，若异常已经处理，需要返回自定义的失败结果时，可根据返回类型进行自定义
builder.Services.AddAutoWasmErrorResultHandler(config =>
{
    config.CreateErrorResult<QueryResult>(context =>
    {
        return new QueryResult() { IsSuccess = false, Message = context.Exception.Message };
    });
});
```

### 按约束创建返回值

如果是返回统一的自定义返回值，并且该类型拥有无参构造函数，拥有一个类似`success`的`bool`类型的属性和一个类似`message`或者`msg`的`string`类型的属性，将尝试 new 一个示例并为这两个属性赋值

#### 自定义属性查找功能

如果未按约束命名，可通过`ApiInvokerAssemblyAttribute`上的属性辅助查找

| 名称        | 说明                                   |
| ----------- | -------------------------------------- |
| SuccessFlag | 如果有多个值，用 逗号、空格、分号 隔开 |
| MessageFlag | 如果有多个值，用 逗号、空格、分号 隔开 |
