using AutoAopProxyGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest;

public interface IUser1
{
    string Hello1(string message);
}

public interface IUser2
{
    string Hello2(string message);
}

public interface IUser : IUser1, IUser2
{
}

public interface IUser3
{
    [AddAspectHandler(AspectType = typeof(MethodTestAop1))]
    string Hello3(string message);

}

public class UserBase : IUser
{
    public string Hello1(string message)
    {
        System.Diagnostics.Debug.WriteLine($"Hello {message} From IUser1");
        return $"Hello {message} From IUser1";
    }

    public string Hello2(string message)
    {
        System.Diagnostics.Debug.WriteLine($"Hello {message} From IUser2");
        return $"Hello {message} From IUser2";
    }

    public virtual string TestOverride()
    {
        return "Base";
    }

    public virtual int TestOverrideInt => 1;


}

[AutoAopProxyGenerator.GenAspectProxy]
public class User2 : UserBase, IUser3
{

    public string Hello3(string message)
    {
        System.Diagnostics.Debug.WriteLine($"Hello {message} From IUser");
        return $"Hello {message} From IUser";
    }

    public override string TestOverride()
    {
        return "User2";
    }

    public override int TestOverrideInt => 2;
}
