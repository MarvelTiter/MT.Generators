# AutoWasmApiGenerator

## ����

����Ŀ��һ����������������������BlazorServer��Ŀ��WebApi�ӿڣ��Ա���BlazorAutoģʽ��ʹ�á�

> https://www.cnblogs.com/marvelTitile/p/18360614

## ʹ��

### ��Ŀ�ṹ

+ `Server`����Ŀ
+ `Client`����Ŀ
+ `Shared`˫�˹��õ���Ŀ��һ���ǹ��õ�ҳ�桢ʵ��ģ�͡��ȵ�

### ʹ�ò���
1. ��Server��Ŀ������AutoWasmApiGenerator��Ŀ
	```csharp
	<PackageReference Include="AutoWasmApiGenerator" Version="0.0.*" />
	```
2. ��Shared��Client��Ŀ����ӽӿ�
	```csharp
	[WebController]
	public interface ITestService
	{
		[WebMethod(Method = WebMethod.Post)] // Ĭ��ָ��ΪPost
		Task<bool> LogAsync(string message, string path, CancellationToken token);
	}
	```
3. ��Server��Ŀ������һ���ļ���ע`WebControllerAssemblyAttribute`, ���ɵ��ļ�Ϊ`TestServiceController`
	```csharp
	[assembly: AutoWasmApiGenerator.WebControllerAssembly]
	```
4. ��Client��Ŀ�л����ɿ��������������Ŀ�е�����һ���ļ���ע`ApiInvokerAssemblyAttribute`, ���ɵ��ļ�Ϊ`TestServiceApiInvoker`
	```csharp
	[assembly: AutoWasmApiGenerator.ApiInvokerAssembly]
	```
5. ��Client��Ŀ��ע��
	```csharp
	builder.Services.AddScoped<ITestService, TestServiceApiInvoker>();
	```
6. ͨ��`ITestService`���÷���
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

## ������Խ���

### WebControllerAttribute

��ע����ӿڣ����ݽӿ����ɿ�����

### ApiInvokerGenerateAttribute

��ע����ӿڣ����ݽӿ����ɵ�����

### WebMethodAttribute

��ע������ָ������ʽ

```csharp
[WebMethod(Method = WebMethod.Post)]
Task<bool> LogAsync(string message);
```

#### ����

| ����           | ����      | ˵��                   |
| -------------- | -------- | ---------------------- |
| Method         | [WebMethod] | ָ�����󷽷���Ĭ��ΪPost |
| Route          | string?  | ָ��Action·�ɣ�nullʱΪ�������� |
| AllowAnonymous | bool     | �Ƿ�֧���������ʣ��Ḳ��Authorize���� |
| Authorize      | bool     | �Ƿ���Ҫ��Ȩ           |

### WebMethod

���ܵ�ֵ
+ Get
+ Post
+ Put
+ Delete

### WebMethodParameterBindingAttribute

��ע������ָ�������󶨷�ʽ

```csharp
[WebMethod(Method = WebMethod.Post)]
Task<bool> Log3Async([WebMethodParameterBinding(BindingType.FromBody)] string message, [WebMethodParameterBinding(BindingType.FromQuery)] string path,[WebMethodParameterBinding(BindingType.Ignore)] CancellationToken token);
```

#### ����

| ����           | ����      | ˵��                   |
| ------------- | -------- | ---------------------- |
| Type          | [BindingType ] | ���������� |

#### BindingType
+ Ignore ����
+ FromQuery �Ӳ�ѯ�ַ����л�ȡֵ��
+ FromRoute ��·�������л�ȡֵ��
+ FromForm �ӷ����ı����л�ȡֵ��
+ FromBody �����������л�ȡֵ��
+ FromHeader �� HTTP ��ͷ�л�ȡֵ��
+ FromServices �ӷ��������л�ȡֵ��

