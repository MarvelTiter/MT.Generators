# AutoWasmApiGenerator

## 介绍

本项目是一个增量生成器，用于生成BlazorServer项目的WebApi接口，以便在BlazorAuto模式下使用。

> https://www.cnblogs.com/marvelTitile/p/18360614

## 使用

### 项目结构

+ `Server`端项目
+ `Client`端项目
+ `Shared`双端共用的项目，一般是共用的页面、实体模型、等等

### 使用步骤
1. 在Server项目中引用AutoWasmApiGenerator项目
	```csharp
	<PackageReference Include="AutoWasmApiGenerator" Version="0.0.*" />
	```
2. 在Shared或Client项目中添加接口
	```csharp
	[WebController]
	public interface ITestService
	{
		[WebMethod(Method = WebMethod.Post)] // 默认指定为Post
		Task<bool> LogAsync(string message, string path, CancellationToken token);
	}
	```
3. 在Server项目中任意一个文件标注`WebControllerAssemblyAttribute`, 生成的文件为`TestServiceController`
	```csharp
	[assembly: AutoWasmApiGenerator.WebControllerAssembly]
	```
4. 在Client项目中或生成控制器调用类的项目中的任意一个文件标注`ApiInvokerAssemblyAttribute`, 生成的文件为`TestServiceApiInvoker`
	```csharp
	[assembly: AutoWasmApiGenerator.ApiInvokerAssembly]
	```
5. 在Client项目中注册
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

标注服务接口，根据接口生成控制器

### ApiInvokerGenerateAttribute

标注服务接口，根据接口生成调用类

### WebMethodAttribute

标注方法，指定请求方式

```csharp
[WebMethod(Method = WebMethod.Post)]
Task<bool> LogAsync(string message);
```

#### 属性

| 名称           | 类型      | 说明                   |
| -------------- | -------- | ---------------------- |
| Method         | [WebMethod] | 指定请求方法，默认为Post |
| Route          | string?  | 指定Action路由，null时为方法名称 |
| AllowAnonymous | bool     | 是否支持匿名访问，会覆盖Authorize设置 |
| Authorize      | bool     | 是否需要授权           |

### WebMethod

可能的值
+ Get
+ Post
+ Put
+ Delete

### WebMethodParameterBindingAttribute

标注参数，指定参数绑定方式

```csharp
[WebMethod(Method = WebMethod.Post)]
Task<bool> Log3Async([WebMethodParameterBinding(BindingType.FromBody)] string message, [WebMethodParameterBinding(BindingType.FromQuery)] string path,[WebMethodParameterBinding(BindingType.Ignore)] CancellationToken token);
```

#### 属性

| 名称           | 类型      | 说明                   |
| ------------- | -------- | ---------------------- |
| Type          | [BindingType ] | 参数绑定类型 |

#### BindingType
+ Ignore 忽略
+ FromQuery 从查询字符串中获取值。
+ FromRoute 从路由数据中获取值。
+ FromForm 从发布的表单域中获取值。
+ FromBody 从请求正文中获取值。
+ FromHeader 从 HTTP 标头中获取值。
+ FromServices 从服务容器中获取值。

