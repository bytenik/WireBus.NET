using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WireBus
{
	static class TPLExtensions
	{
		public static void WaitOne(this Task task)
		{
			try
			{
				task.Wait();
			}
			catch(AggregateException ae)
			{
				ae = ae.Flatten();
				
				if(ae.InnerExceptions.Count == 1)
					throw ae.InnerException;

				throw;
			}
		}

		public static void WaitOne(this Task task, CancellationToken cancellationToken)
		{
			try
			{
				task.Wait(cancellationToken);
			}
			catch (AggregateException ae)
			{
				ae = ae.Flatten();

				if (ae.InnerExceptions.Count == 1)
					throw ae.InnerException;

				throw;
			}
		}

		public static bool WaitOne(this Task task, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			try
			{
				return task.Wait(millisecondsTimeout, cancellationToken);
			}
			catch (AggregateException ae)
			{
				ae = ae.Flatten();

				if (ae.InnerExceptions.Count == 1)
					throw ae.InnerException;

				throw;
			}
		}

		public static bool WaitOne(this Task task, int millisecondsTimeout)
		{
			try
			{
				return task.Wait(millisecondsTimeout);
			}
			catch (AggregateException ae)
			{
				ae = ae.Flatten();

				if (ae.InnerExceptions.Count == 1)
					throw ae.InnerException;

				throw;
			}
		}

		public static bool WaitOne(this Task task, TimeSpan timeout)
		{
			try
			{
				return task.Wait(timeout);
			}
			catch (AggregateException ae)
			{
				ae = ae.Flatten();

				if (ae.InnerExceptions.Count == 1)
					throw ae.InnerException;

				throw;
			}
		}

        public static Task IgnoreExceptions(this Task task)
        {
            task.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        public static Task<T> IgnoreExceptions<T>(this Task<T> task)
        {
            task.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        public static async Task TimeoutAsync(TimeSpan timeout)
        {
            await TaskEx.Delay(timeout);
            throw new TimeoutException();
        }

        /// <summary>
        /// A naive implementation of timeout and cancellation over an uncancellable <see cref="Task"/>.
        /// </summary>
        /// <typeparam name="T">The result type of the task</typeparam>
        /// <param name="task">the uncancellable task</param>
        /// <param name="timeout">timeout after which to give up</param>
        /// <param name="token">token to monitor for cancellation</param>
        /// <returns></returns>
        public static async Task<T> NaiveTimeoutAndCancellation<T>(this Task<T> task, TimeSpan timeout, CancellationToken token)
        {
            task.IgnoreExceptions();

            var timeoutTask = TimeoutAsync(timeout).IgnoreExceptions();
            var cancelTask = token.ToTask().IgnoreExceptions();
            
            var resultTask = await TaskEx.WhenAny(task, timeoutTask, cancelTask);
            if (resultTask == cancelTask)  // this should not happen -- cancelTask should cause the await to throw
                throw new OperationCanceledException();
            else if (resultTask == timeoutTask)
                throw new TimeoutException();
            else
                return task.Result;
        }

        /// <summary>
        /// A naive implementation of timeout and cancellation over an uncancellable <see cref="Task"/>.
        /// </summary>
        /// <param name="task">the uncancellable task</param>
        /// <param name="timeout">timeout after which to give up</param>
        /// <param name="token">token to monitor for cancellation</param>
        /// <returns></returns>
        public static async Task NaiveTimeoutAndCancellation<T>(this Task task, TimeSpan timeout, CancellationToken token)
        {
            task.IgnoreExceptions();

            var timeoutTask = TimeoutAsync(timeout).IgnoreExceptions();
            var cancelTask = token.ToTask().IgnoreExceptions();

            var resultTask = await TaskEx.WhenAny(task, timeoutTask, cancelTask);
            if (resultTask == cancelTask)  // this should not happen -- cancelTask should cause the await to throw
                throw new OperationCanceledException();
            else if (resultTask == timeoutTask)
                throw new TimeoutException();
        }

	    private static readonly TaskCompletionSource<object> NeverCompleteSource = new TaskCompletionSource<object>();
        public static Task NeverComplete { get { return NeverCompleteSource.Task; }}

        public static Task ToTask(this CancellationToken token)
        {
            var tcs = new TaskCompletionSource<object>();
            token.Register(tcs.SetCanceled);
            return tcs.Task;
        }
	}
}
