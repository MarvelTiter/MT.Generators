using AutoAopProxyGenerator;
using AutoInjectGenerator;
using AutoWasmApiGenerator;
using Microsoft.Extensions.DependencyInjection;

namespace InjectTest
{
    [AutoInject(Group = "SERVER", LifeTime = ServiceLifetime.Singleton)]
    public class InjectTestClass
    {

    }
    public class Base2 { }
    public class Base1 : Base2
    {

    }

    //[AutoInject]
    //[AutoInject(ServiceType = typeof(Base))]
    [AutoInjectSelf]
    public class Base : IDisposable
    {
        public void Dispose()
        {

        }
    }

    public interface IEmpty
    {
        Task Hello();
    }

    [WebController]
    [AddAspectHandler(AspectType = typeof(InjectTestAspectHandler))]
    public interface IB : IEmpty
    {
    }

    [AutoInject(ServiceKey = "Test")]
    [AutoInject]
    [AutoInject(ServiceType = typeof(IEmpty))]
    [GenAspectProxy]
    public class Class2 : Base, IB
    {
        public Task Hello()
        {
            Console.WriteLine("Hello World");
            return Task.CompletedTask;
        }
    }
}
