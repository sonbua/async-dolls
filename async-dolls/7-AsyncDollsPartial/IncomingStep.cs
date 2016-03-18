using System;
using System.Threading.Tasks;

namespace AsyncDolls.AsyncDollsPartial
{
    public abstract class SurroundStep<TContext> : IIncomingStep<TContext, TContext> 
        where TContext : Context
    {
        public Task Invoke(TContext context, Func<TContext, Task> next)
        {
            return Invoke(context, () => next(context));
        }

        public abstract Task Invoke(TContext context, Func<Task> next);
    }

    public abstract class BeforeStep<TContext> : IIncomingStep<TContext, TContext>
        where TContext : Context
    {
        public async Task Invoke(TContext context, Func<TContext, Task> next)
        {
            await Invoke(context).ConfigureAwait(false);
            // await next(context).ConfigureAwait(false);
        }

        public abstract Task Invoke(TContext context);
    }

    public abstract class AfterStep<TContext> : IIncomingStep<TContext, TContext>
    where TContext : Context
    {
        public async Task Invoke(TContext context, Func<TContext, Task> next)
        {
            // await next(context).ConfigureAwait(false);
            await Invoke(context).ConfigureAwait(false);
        }

        public abstract Task Invoke(TContext context);
    }
}