using System;
using System.Linq;

namespace AutoWasmApiGenerator
{
    /// <summary>
    /// For <see cref="HttpServiceInvokerGenerator"/> Generator
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class ApiInvokerGenerateAttribute : Attribute
    {
        public Type[] Attributes { get; set; }

        public ApiInvokerGenerateAttribute(params Type[] attributes)
        {
            Attributes = attributes;
        }
    }
}
