using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticQuery.Exporter.Lib.Extension
{
    public static class TaskExtensions
    {
        public class MaybeTimeout<T>
        {
            public MaybeTimeout(T result, bool timedOut)
            {
                Result = result;
                TimedOut = timedOut;
            }

            public T Result { get; }

            public bool TimedOut { get; }
        }

        public static async Task<MaybeTimeout<TResult>> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource();

            var delayTask = Task.Delay(timeout, timeoutCancellationTokenSource.Token);
            var completedTask = await Task.WhenAny(task, delayTask);
            if (completedTask == delayTask) 
                return new MaybeTimeout<TResult>(default, timedOut: true);

            timeoutCancellationTokenSource.Cancel();
            return new MaybeTimeout<TResult>(await task, timedOut: false);
        }
    }
}
