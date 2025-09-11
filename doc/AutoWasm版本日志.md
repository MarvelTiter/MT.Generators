# 版本功能更新记录

## v0.1.4
- ⚡️为生成的Controller和对应的Controller调用类添加`GeneratedByAutoWasmApiGeneratorAttribute`标识
- ⚡️添加`AddGeneratedApiInvokerServices`扩展方法，用于反射注册生成器生成的Controller调用类

## v0.1.3
- ⚡️生成API调用类时添加异常处理
- ⚡️API调用类添加切面入口`IGeneratedApiInvokeDelegatingHandler`
- ⚡️API调用类返回值优化，当返回自定义类时，将尝试构造该返回值，并从中寻找类似success的属性和类似message的属性，并将异常信息赋值给message，这两个标记可以通过`ApiInvokerAssemblyAttribute`新增的参数辅助查找
```csharp
// 假设接口统一返回 QueryResult
public class QueryResult
{
	public bool Success { get; set; }
	public string Message { get; set; }
}
```

## v0.1.2

- ⚡️移除`ApiInvokerGenerateAttribute`, 统一使用`WebControllerAttribute`作为标识
- ⚡️新增`WebMethodNotSupportedAttribute`, 用在方法上, 指示是否生成对应WebApi
- ⚡️`ApiInvokeNotSupportedAttribute`新增可用在类上, 不生成Api调用类
- ⚡️返回值增加元组类型支持, WebController中将元组转为匿名类型, Api调用类中还原为元组类型
- 🐞修改参数默认传参方式, 自定义类使用body传参, 其他如string, int 等等默认使用query传参
