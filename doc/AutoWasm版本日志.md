# 版本功能更新记录

## v0.1.2

- ⚡️移除`ApiInvokerGenerateAttribute`, 统一使用`WebControllerAttribute`作为标识
- ⚡️新增`WebMethodNotSupportedAttribute`, 用在方法上, 指示是否生成对应WebApi
- ⚡️`ApiInvokeNotSupportedAttribute`新增可用在类上, 不生成Api调用类
- ⚡️返回值增加元组类型支持, WebController中将元组转为匿名类型, Api调用类中还原为元组类型
- 🐞修改参数默认传参方式, 自定义类使用body传参, 其他如string, int 等等默认使用query传参
