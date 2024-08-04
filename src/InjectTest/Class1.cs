namespace InjectTest
{
    [AutoInjectGenerator.AutoInject]
    public class Class1
    {

    }

    public class Base
    {

    }

    public interface IB
    {

    }

    [AutoInjectGenerator.AutoInject]
    public class Class2 : Base, IB
    {

    }
}
