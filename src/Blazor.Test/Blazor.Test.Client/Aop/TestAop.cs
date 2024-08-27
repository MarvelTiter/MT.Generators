using AutoAopProxyGenerator;

namespace Blazor.Test.Client.Aop
{
    public class TestAop : IAspectHandler
    {
        private readonly ILogger<TestAop> logger;

        public TestAop(ILogger<TestAop> logger)
        {
            this.logger = logger;
        }
        public async Task Invoke(ProxyContext context, Func<Task> process)
        {
            logger.LogInformation("执行前");
            await process();
            logger.LogInformation("执行后");
        }
    }
}
