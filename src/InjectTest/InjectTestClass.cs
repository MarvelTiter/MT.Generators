using AutoAopProxyGenerator;
using AutoInjectGenerator;
using AutoWasmApiGenerator;
using Microsoft.Extensions.DependencyInjection;

namespace InjectTest
{
    [AutoInject(Group = "SERVER", LifeTime = InjectLifeTime.Singleton)]
    public class InjectTestClass
    {

    }
    public class Base2 { }
    public class Base1 : Base2
    {

    }

    //[AutoInject]
    //[AutoInject(ServiceType = typeof(Base))]
    //[AutoInject(Factory = nameof(BaseInstanceFactory))]
    public class Base : IDisposable
    {
        public static Base IB => new Base();
        public static Base BaseInstanceFactory(IServiceProvider serviceProvider)
        {
            return new Base();
        }

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
    [AutoInject(ServiceType = typeof(IEmpty), ServiceKey = "G1")]
    [AutoInject(ServiceType = typeof(IEmpty), ServiceKey = "G2")]
    [GenAspectProxy]
    public class Class2 : Base, IB
    {
        public Task Hello()
        {
            Console.WriteLine("Hello World");
            return Task.CompletedTask;
        }
    }

    [AutoInject(Group = "SERVER", LifeTime = InjectLifeTime.Singleton)]
    [AutoInject(Group = "HYBRID", LifeTime = InjectLifeTime.Singleton)]
    public class FileService
    {

    }

    public interface IA;

    public class ParentType : IA
    {

    }

    [AutoInject]
    public class ChildType : ParentType
    {

    }

    public class InjectConfig
    {

    }
}

