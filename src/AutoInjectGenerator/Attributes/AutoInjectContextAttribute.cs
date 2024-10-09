using System;

namespace AutoInjectGenerator;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoInjectContextAttribute : Attribute
{
}
