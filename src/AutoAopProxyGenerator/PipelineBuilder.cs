using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AutoAopProxyGenerator;

public class PipelineBuilder<TContext>
{
    private readonly Action<TContext> completeAction;
    private readonly IList<Func<Action<TContext>, Action<TContext>>> pipelines = [];
    public static PipelineBuilder<TContext> Create(Action<TContext> completeAction) => new(completeAction);
    public PipelineBuilder(Action<TContext> completeAction)
    {
        this.completeAction = completeAction;
    }
    public PipelineBuilder<TContext> Use(Func<Action<TContext>, Action<TContext>> middleware)
    {
        pipelines.Add(middleware);
        return this;
    }
    public Action<TContext> Build()
    {
        var request = completeAction;
        foreach (var pipeline in pipelines.Reverse())
        {
            request = pipeline(request);
        }
        return request;
    }
}

public class AsyncPipelineBuilder<TContext>
{
    private readonly Func<TContext, Task> completeFunc;
    private readonly IList<Func<Func<TContext, Task>, Func<TContext, Task>>> pipelines = [];
    public static AsyncPipelineBuilder<TContext> Create(Func<TContext, Task> completeFunc) => new(completeFunc);
    public AsyncPipelineBuilder(Func<TContext, Task> completeFunc)
    {
        this.completeFunc = completeFunc;
    }

    public AsyncPipelineBuilder<TContext> Use(Func<Func<TContext, Task>, Func<TContext, Task>> middleware)
    {
        pipelines.Add(middleware);
        return this;
    }

   
    public Func<TContext, Task> Build()
    {
        var request = completeFunc;
        foreach (var pipeline in pipelines.Reverse())
        {
            request = pipeline(request);
        }
        return request;
    }
}

public static class PipeLineBuilderExtensions
{
    public static PipelineBuilder<TContext> Use<TContext>(this PipelineBuilder<TContext> builder, Action<TContext, Action> action)
    {
        builder.Use(next =>
        {
            return context =>
            {
                action(context, () => next(context));
            };
        });
        return builder;
    }

    public static AsyncPipelineBuilder<TContext> Use<TContext>(this AsyncPipelineBuilder<TContext> builder, Func<TContext, Func<Task>, Task> asyncaction)
    {
        builder.Use(next =>
        {
            return context =>
            {
                return asyncaction(context, () => next(context));
            };
        });
        return builder;
    }
}
