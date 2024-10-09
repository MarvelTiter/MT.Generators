using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AutoAopProxyGenerator;

/// <summary></summary>
public class PipelineBuilder<TContext>
{
    private readonly Action<TContext> completeAction;
    private readonly IList<Func<Action<TContext>, Action<TContext>>> pipelines = [];
    /// <summary></summary>
    public static PipelineBuilder<TContext> Create(Action<TContext> completeAction) => new(completeAction);
    /// <summary></summary>
    public PipelineBuilder(Action<TContext> completeAction)
    {
        this.completeAction = completeAction;
    }
    /// <summary></summary>
    public PipelineBuilder<TContext> Use(Func<Action<TContext>, Action<TContext>> middleware)
    {
        pipelines.Add(middleware);
        return this;
    }
    /// <summary></summary>
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
/// <summary></summary>
public class AsyncPipelineBuilder<TContext>
{
    private readonly Func<TContext, Task> completeFunc;
    private readonly IList<Func<Func<TContext, Task>, Func<TContext, Task>>> pipelines = [];
    /// <summary></summary>
    public static AsyncPipelineBuilder<TContext> Create(Func<TContext, Task> completeFunc) => new(completeFunc);
    /// <summary></summary>
    public AsyncPipelineBuilder(Func<TContext, Task> completeFunc)
    {
        this.completeFunc = completeFunc;
    }

    /// <summary></summary>
    public AsyncPipelineBuilder<TContext> Use(Func<Func<TContext, Task>, Func<TContext, Task>> middleware)
    {
        pipelines.Add(middleware);
        return this;
    }


    /// <summary></summary>
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

/// <summary></summary>
public static class PipeLineBuilderExtensions
{
    /// <summary></summary>
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
    /// <summary></summary>
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
