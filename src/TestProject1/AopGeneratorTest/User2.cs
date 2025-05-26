using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest;

public interface IUser1
{
    string Hello(string message);
}

public interface IUser2
{
    string Hello(string message);
}

public interface IUser : IUser1, IUser2
{

}

public class UserBase : IUser
{
    string IUser1.Hello(string message)
    {
        System.Diagnostics.Debug.WriteLine($"Hello {message} From IUser1");
        return $"Hello {message} From IUser1";
    }

    string IUser2.Hello(string message)
    {
        System.Diagnostics.Debug.WriteLine($"Hello {message} From IUser2");
        return $"Hello {message} From IUser2";
    }
}

[AutoAopProxyGenerator.GenAspectProxy]
public class User2 : UserBase
{

}
