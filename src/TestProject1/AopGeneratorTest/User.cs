using AutoAopProxyGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest
{
    [AddAspectHandler(AspectType = typeof(LogAop))]
    public interface IHello
    {
        void Hello();
        Task HelloAsync();
        int Count();
        Task<int> CountAsync();
    }

    internal class User : IHello
    {
        public void Hello()
        {
            Console.WriteLine("Hello world");
        }

        public Task HelloAsync()
        {
            throw new NotImplementedException();
        }
        public Task<int> CountAsync()
        {
            return Task.FromResult(0);
        }

        public int Count()
        {
            throw new NotImplementedException();
        }
    }

    [GenAspectProxy]
    //[AddAspectHandler(AspectType = typeof(LogAop))]
    internal class UserProxy : IHello
    {
        private readonly User proxy;
        public UserProxy(User proxy)
        {
            this.proxy = proxy;
        }

        public void Hello()
        {
            Func<ProxyContext, Task> done = ctx =>
            {
                proxy.Hello();
                ctx.Executed = true;
                return Task.CompletedTask;
            };
            var builder = AsyncPipelineBuilder<ProxyContext>.Create(done);
            var job = builder.Build();
            job.Invoke(new ProxyContext()).GetAwaiter().GetResult();
        }

        public Task HelloAsync()
        {
            Func<ProxyContext, Task> done = async ctx =>
            {
                await proxy.HelloAsync();
                ctx.Executed = true;
            };
            var builder = AsyncPipelineBuilder<ProxyContext>.Create(done);
            var job = builder.Build();
            return job.Invoke(new ProxyContext());
        }

        public int Count()
        {
            int returnValue = default;
            Func<ProxyContext, Task> done = ctx =>
            {
                returnValue = proxy.Count();
                ctx.ReturnValue = returnValue;
                ctx.Executed = true;
                return Task.CompletedTask;
            };
            var builder = AsyncPipelineBuilder<ProxyContext>.Create(done);
            var job = builder.Build();
            job.Invoke(new ProxyContext()).GetAwaiter().GetResult();
            return returnValue;
        }

        public async Task<int> CountAsync()
        {
            int returnValue = default;
            async Task done(ProxyContext ctx)
            {
                returnValue = await proxy.CountAsync();
                ctx.ReturnValue = returnValue;
                ctx.Executed = true;
            }
            var builder = AsyncPipelineBuilder<ProxyContext>.Create(done);
            var job = builder.Build();
            await job.Invoke(new ProxyContext());
            return returnValue;
        }

    }
}
