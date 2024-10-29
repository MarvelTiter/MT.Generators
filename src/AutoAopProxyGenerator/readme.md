# AutoAopProxyGenerator

## 介绍

本项目是一个增量生成器，用于生成AOP代理类

> https://www.cnblogs.com/marvelTitile/p/18382803

## 使用

1. 在接口或者方法中添加`AddAspectHandlerAttribute`，并指定`AspectType`，可重复添加不同的`AspectType`
2. 在实现类中添加`GenAspectProxyAttribute`，指示生成器为该类型生成代理类，生成的代理类名为`{实现类}GeneratedProxy`
3. 在容器中配置`AutoAopProxyServiceProviderFactory`，目前仅支持官方的IOC容器
```chsarp
builder.Host.UseServiceProviderFactory(new AutoAopProxyServiceProviderFactory());
```

## 相关特性介绍

### AddAspectHandlerAttribute

标注接口/方法，添加AOP处理程序

#### 属性

| 名称           | 类型      | 说明                   |
| -------------- | -------- | ---------------------- |
| AspectType     | Type		| 切面处理类型		 |
| SelfOnly          | bool  | 是否只适用于当前类型的方法，当设置为true时，不适用于继承而来的方法，默认为false |

### GenAspectProxyAttribute

标注类型，指示生成器为该类型生成代理类

### IgnoreAspectAttribute

标注方法，忽略AOP处理

#### 构造函数参数 `params Type[] ignoreTypes`

指定忽略的类型，默认忽略全部
