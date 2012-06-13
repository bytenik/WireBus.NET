using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WireBus
{
	static class MyTaskEx
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
	}
}
