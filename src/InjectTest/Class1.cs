using AutoInjectGenerator;
using AutoWasmApiGenerator;

namespace InjectTest
{
    [AutoInject(Group = "SERVER")]
    public class Class1
    {

    }

    [AutoInject]
    public class Base
    {

    }

    [WebController]
    [ApiInvokerGenerate(typeof(AutoInjectAttribute))]
    [MT.Generators.Abstraction.AttachAttributeArgument(typeof(ApiInvokerGenerateAttribute), typeof(AutoInjectAttribute), "Group", "WASM")]
    public interface IB
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
