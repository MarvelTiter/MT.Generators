using AutoAopProxyGenerator;
using AutoInjectGenerator;

namespace InjectTest;

[AutoInjectSelf]
public class InjectTestAspectHandler : IAspectHandler
{
    public async Task Invoke(ProxyContext context, Func<Task> process)
    {
        Console.WriteLine($"调用{context.ServiceMethod.Name}前");
        await process();
        Console.WriteLine($"调用{context.ServiceMethod.Name}后");
    }
}