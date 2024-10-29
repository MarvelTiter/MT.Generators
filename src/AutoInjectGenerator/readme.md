# AutoInjectGenerator

## 介绍

本项目是一个增量生成器，用于生成自动注入方法

> https://www.cnblogs.com/marvelTitile/p/18360380

## 使用

1. 实现类添加`AutoInjectAttribute`
2. 新建一个静态分部类，添加`AutoInjectContextAttribute`，并提供一个静态分部方法
```csharp
[AutoInjectContext]
public static partial class AutoInjectContext
{
    public static partial void YourMethodName(this IServiceCollection services);
}
```
3. 调用方法
```csharp
services.YourMethodName();
```

## 相关特性介绍

### AutoInjectAttribute

标注实现类，生成服务注入代码

#### 属性

| 名称           | 类型      | 说明                   |
| -------------- | -------- | ---------------------- |
| LifeTime | [InjectLifeTime] | 注入周期	|
| ServiceType | Type | 服务类型，默认为类的第一个接口/自身 |
|Group|string|分组，默认为空|
|ServiceKey|object|服务键，默认为null|
|IsTry|bool|是否使用TryAdd，默认为false|

### InjectLifeTime

可能的值
+ Transient
+ Scoped(默认值)
+ Singleton

### AutoInjectContextAttribute

指示生成调用方法
