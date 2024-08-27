using AutoAopProxyGenerator;

namespace Blazor.Test.Client.Aop
{
    public class ExceptionAop : IAspectHandler
    {
        private readonly ILogger<ExceptionAop> logger;

        public ExceptionAop(ILogger<ExceptionAop> logger)
        {
            this.logger = logger;
        }
        public async Task Invoke(ProxyContext context, Func<Task> process)
        {
			try
			{
				await process();
			}
			catch (Exception ex)
			{
                logger.LogError("Exception {Message}", ex.Message);
			}
        }
    }
}
