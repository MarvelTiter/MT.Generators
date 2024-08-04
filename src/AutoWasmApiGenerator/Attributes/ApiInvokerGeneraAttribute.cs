using System;
using System.Linq;

namespace AutoWasmApiGenerator
{
    /// <summary>
    /// For <see cref="HttpServiceInvokerGenerator"/> Generator
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class ApiInvokerGeneraAttribute : Attribute
    {
        public Type[] Attributes { get; set; }

        public ApiInvokerGeneraAttribute(params Type[] attributes)
        {
            Attributes = attributes;
        }
    }
}
