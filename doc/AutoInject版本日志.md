# 版本功能更新记录

## v0.1.2
- 🛠`AutoInjectAttribute.LifeTime`修改为`InjectLifeTime`类型

## v0.1.1
- 🛠重构生成逻辑, 修复部分bug
- 🛠新增自定义`ServiceDescriptor` -> `AutoInjectServiceDescriptor`, 包含工厂模式下的真实返回类型

## v0.1.0
- ⚡️新增`AutoInjectSelfAttribute`, 表示注入自身类型, 当类型未实现或继承其他类型时, 效果等同于`AutoInjectAttribute`
