using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace AutoAopProxyGenerator
{
    public record ProxyContext
    {
        public bool Executed { get; set; }
        public bool HasReturnValue { get; set; }
        public object? ReturnValue { get; private set; }
        public void SetReturnValue(object value) => ReturnValue = value;
        public object?[] Parameters { get; set; } = [];
        [NotNull] public Type? ServiceType { get; set; }
        [NotNull] public Type? ImplementType { get; set; }
        [NotNull] public MethodInfo? ServiceMethod { get; set; }
        [NotNull] public MethodInfo? ImplementMethod { get; set; }
    }
}
