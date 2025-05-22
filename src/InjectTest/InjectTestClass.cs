using AutoInjectGenerator;
using AutoWasmApiGenerator;

namespace InjectTest
{
    [AutoInject(Group = "SERVER")]
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
            throw new NotImplementedException();
        }
    }

    public interface IEmpty { }

    [WebController]
    public interface IB : IEmpty
    {
        Task Hello();
    }

    [AutoInject(Group = "SERVER", ServiceKey = "Test", IsTry = true)]
    public class Class2 : Base, IB
    {
        public Task Hello()
        {
            throw new NotImplementedException();
        }
    }
}
