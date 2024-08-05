using System;

namespace AutoWasmApiGenerator
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class WebControllerAttribute : Attribute
    {
        public string? Route { get; set; }
        public Type[] Attributes { get; set; } = [];

        public WebControllerAttribute(params Type[] attributes)
        {
            Attributes = attributes;
        }
    }
}
