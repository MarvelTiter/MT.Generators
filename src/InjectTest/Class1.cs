using AutoWasmApiGenerator;

namespace InjectTest
{
    [AutoInjectGenerator.AutoInject]
    public class Class1
    {

    }

    public class Base
    {

    }

    [WebController]
    [ApiInvokerGenerate]
    public interface IB
    {
        void Hello();
    }

    [AutoInjectGenerator.AutoInject]
    public class Class2 : Base, IB
    {
        public void Hello()
        {
            throw new NotImplementedException();
        }
    }
}
