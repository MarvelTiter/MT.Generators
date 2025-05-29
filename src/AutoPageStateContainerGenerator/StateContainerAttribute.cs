using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPageStateContainerGenerator;

/// <summary>
/// 标记需要生成数据的容器的组件
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StateContainerAttribute : Attribute
{

}