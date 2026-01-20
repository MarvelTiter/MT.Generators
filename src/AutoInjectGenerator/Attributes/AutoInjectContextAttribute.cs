using System;

namespace AutoInjectGenerator;

/// <summary>
/// 指定自动注册方法生成的位置
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoInjectContextAttribute : Attribute
{
    ///// <summary>
    ///// 由于生成器的限制，生成注入上下文的项目中同时使用了AutoInjectEntryGenerator和AutoInjectModuleGenerator时，AutoInjectEntryGenerator无法发现当前程序集的模块注入方法，因此新增此参数强制包含当前程序集的模块注入方法
    ///// </summary>
    //public bool ContainSelf { get; set; }
}
