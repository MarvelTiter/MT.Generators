using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AutoAopProxyGenerator
{
    public class ProxyContext
    {
        public ProxyContext(IServiceProvider services)
        {
            Services = services;
        }
        public ProxyContext()
        {

        }
        public bool Executed { get; set; }
        public bool HasReturnValue { get; set; }
        public object? ReturnValue { get; set; }
        public object?[] Parameters { get; set; } = [];
        public IServiceProvider? Services { get; }
        public Type? ServiceType { get; set; }
        public Type? ImplementType { get; set; }
        public MethodInfo? ServiceMethod { get; set; }
        public MethodInfo? ImplementMethod { get; set; }
    }
}
