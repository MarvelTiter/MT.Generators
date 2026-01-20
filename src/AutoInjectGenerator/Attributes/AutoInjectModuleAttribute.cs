using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInjectGenerator;

/// <summary>
/// Specifies that the attributed class is an auto-injectable module for dependency injection frameworks.
/// </summary>
/// <remarks>Apply this attribute to a class to indicate that it should be automatically discovered and registered
/// as a module by compatible dependency injection containers. This attribute is typically used in frameworks that
/// support convention-based module registration.</remarks>
[AttributeUsage(AttributeTargets.Class)]
public class AutoInjectModuleAttribute : Attribute
{
}
