using AutoInjectGenerator;

namespace Blazor.Test
{
    [AutoInjectContext]
    public static partial class AutoInjectContext
    {
        [AutoInjectConfigurationAttribute(Include = "SERVER")]
        public static partial void Inject(this IServiceCollection services);
    }

    [AutoInjectContext]
    public static partial class AutoInjectContextHybrid
    {
        [AutoInjectConfigurationAttribute(Include = "HYBRID")]
        public static partial void InjectHybrid(this IServiceCollection services);
    }
}

//namespace Blazor.Test
//{
//    [global::System.CodeDom.Compiler.GeneratedCode("AutoInjectGenerator.AutoInjectContextGenerator", "0.1.1.0")]
//    /// <inheritdoc/>
//    static partial class AutoInjectContext
//    {
//        [global::System.CodeDom.Compiler.GeneratedCode("AutoInjectGenerator.AutoInjectContextGenerator", "0.1.1.0")]
//        public static partial void Inject(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
//        {
//            services.Add(new ServiceDescriptor(typeof(InjectTest.Class2), typeof(InjectTest.Class2), ServiceLifetime.Scoped));

//            services.Add(new ServiceDescriptor(typeof(InjectTest.Class2), "Test", typeof(InjectTest.Class2), ServiceLifetime.Scoped));

//            services.Add(new ServiceDescriptor(typeof(InjectTest.IB), "Test", (p, k) => p.GetRequiredKeyedService<InjectTest.Class2>(k), ServiceLifetime.Scoped));

//            services.Add(new ServiceDescriptor(typeof(InjectTest.IEmpty), p => p.GetRequiredService<InjectTest.Class2>(), ServiceLifetime.Scoped));
//        }
//    }
//}