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

    [AutoInject]
    [AutoInject(ServiceType = typeof(Base))]
    public class Base : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public interface IEmpty { }

    [WebController]
    [ApiInvokerGenerate(typeof(AutoInjectAttribute))]
    [MT.Generators.Abstraction.AttachAttributeArgument(typeof(ApiInvokerGenerateAttribute), typeof(AutoInjectAttribute), "Group", "WASM")]
    public interface IB : IEmpty
    {
        void Hello();
    }

    [AutoInject(Group = "SERVER", ServiceKey = "Test", IsTry = true)]
    public class Class2 : Base, IB
    {
        public void Hello()
        {
            throw new NotImplementedException();
        }
    }
}
