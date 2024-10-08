using AutoAopProxyGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest
{
    [TestClass]
    public class PipeLineTest
    {
        [TestMethod]
        public void Test()
        {
            var builder = new PipelineBuilder<string>(s => { Console.WriteLine("job done"); });
            builder.Use(next =>
            {
                return s =>
                {
                    Console.WriteLine("job 1 before");
                    next(s);
                    Console.WriteLine("job 1 after");
                };
            }).Use((context, next) =>
            {
                next();
            });

            var job = builder.Build();
            job.Invoke("hello");
        }

        public Task Done(ProxyContext ctx)
        {
            Console.WriteLine($"Invoke Actual Method Result: {ctx.Status}");
            return Task.CompletedTask;
        }

        public class LogAop
        {
            public Task Invoke(ProxyContext ctx, Func<Task> next)
            {
                Console.WriteLine("LogAop Invoke");
                return next();
            }
        }

        [TestMethod]
        public async Task AsyncTest()
        {
            var log = new LogAop();
            var builder = AsyncPipelineBuilder<ProxyContext>.Create(Done);
            builder.Use(log.Invoke);
            var job = builder.Build();
            ProxyContext context = new ProxyContext();
            await job.Invoke(context);
        }
    }
}
