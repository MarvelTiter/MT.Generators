using AutoAopProxyGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest
{
    //[AddAspectHandler(AspectType = typeof(LogAop))]
    public interface IHello
    {
        [IgnoreAspect]
        void Hello();
        void Hello(string message);
        Task HelloAsync();
        Task HelloAsync(string message);
        int Count();
        int Count(string message);
        Task<int> CountAsync();
        [AddAspectHandler(AspectType = typeof(ExceptionAop))]
        Task<int> CountAsync(string message);
        [IgnoreAspect]
        Task<bool> RunJobAsync<T>();
        [IgnoreAspect]
        Task RunJobAsync<T>(string message);

    }

    [GenAspectProxy]
    public class User : IHello
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
            return Task.FromResult(3);
        }

        public int Count()
        {
            return 2;
        }

        public Task<bool> RunJobAsync<T>()
        {
            throw new NotImplementedException();
        }

        public void Hello(string message)
        {
            throw new NotImplementedException();
        }

        public Task HelloAsync(string message)
        {
            throw new NotImplementedException();
        }

        public int Count(string message)
        {
            return message.Length;
        }

        public Task<int> CountAsync(string message)
        {
            return Task.FromResult(message.Length);
        }

        public Task RunJobAsync<T>(string message)
        {
            throw new NotImplementedException();
        }
    }

    //[GenAspectProxy]
    ////[AddAspectHandler(AspectType = typeof(LogAop))]
    //public class UserProxy : IHello
    //{
    //    private readonly User proxy;
    //    LogAop log;
    //    public UserProxy()
    //    {
    //        this.proxy = new User();
    //        this.log = new LogAop();
    //    }

    //    public void Hello()
    //    {
    //        Task Done(ProxyContext ctx)
    //        {
    //            proxy.Hello();
    //            ctx.Executed = true;
    //            return Task.CompletedTask;
    //        }
    //        var builder = AsyncPipelineBuilder<ProxyContext>.Create(Done);
    //        var job = builder.Build();
    //        var context = ContextHelper.GetOrCreate(typeof(IHello), typeof(User), "Hello", Type.EmptyTypes);

    //        job.Invoke(new ProxyContext()).GetAwaiter().GetResult();
    //    }

    //    public Task HelloAsync()
    //    {
    //        async Task done(ProxyContext ctx)
    //        {
    //            await proxy.HelloAsync();
    //            ctx.Executed = true;
    //        }
    //        var builder = AsyncPipelineBuilder<ProxyContext>.Create(done);
    //        var job = builder.Build();
    //        return job.Invoke(new ProxyContext());
    //    }

    //    public int Count()
    //    {
    //        int returnValue = default;
    //        Task done(ProxyContext ctx)
    //        {
    //            returnValue = proxy.Count();
    //            ctx.ReturnValue = returnValue;
    //            ctx.Executed = true;
    //            return global::System.Threading.Tasks.Task.CompletedTask;
    //        }
    //        var builder = AsyncPipelineBuilder<ProxyContext>.Create(done);
    //        builder.Use(log.Invoke);
    //        var job = builder.Build();
    //        job.Invoke(new ProxyContext()).GetAwaiter().GetResult();
    //        return returnValue;
    //    }

    //    public async Task<int> CountAsync()
    //    {
    //        int returnValue = default;
    //        async Task done(ProxyContext ctx)
    //        {
    //            returnValue = await proxy.CountAsync();
    //            ctx.ReturnValue = returnValue;
    //            ctx.Executed = true;
    //        }
    //        var builder = AsyncPipelineBuilder<ProxyContext>.Create(done);
    //        var job = builder.Build();
    //        await job.Invoke(new ProxyContext());
    //        return returnValue;
    //    }

    //    public async Task<bool> RunJobAsync<T>()
    //    {
    //        bool returnValue = default;
    //        async Task Done(ProxyContext ctx)
    //        {
    //            returnValue = await proxy.RunJobAsync<T>();
    //            ctx.ReturnValue = returnValue;
    //            ctx.Executed = true;
    //        }
    //        var builder = AsyncPipelineBuilder<ProxyContext>.Create(Done);
    //        var job = builder.Build();
    //        await job.Invoke(new ProxyContext());
    //        return returnValue;
    //    }
    //}
}

