// Copyright (c) DMO Consulting LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dmo.Threading
{
	public static class TaskExts
	{
		/// <summary>
		/// Throws a TimeoutException if the task fails to complete within the specified waiting period
		/// </summary>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeoutMlsec">Represents the number of milliseconds to wait.</param>
		/// <param name="cancelToken">A cancellation token to observe while waiting for the task to complete.</param>
		/// <returns>A Task that represents a proxy for the source task.</returns>
		public static Task TimeoutAfter(this Task task,int timeoutMlsec,CancellationToken cancelToken = default) =>	
			TimeoutAfterIntern(task,TimeSpan.FromMilliseconds(timeoutMlsec),cancelToken,null);

		/// <summary>
		/// Throws a TimeoutException if the task fails to complete within the specified waiting period
		/// </summary>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeout">Represents a TimeSpan for the task to complete.</param>
		/// <param name="cancelToken">A cancellation token to observe while waiting for the task to complete.</param>
		/// <returns>A Task that represents a proxy for the source task.</returns>
		public static Task TimeoutAfter(this Task task,TimeSpan timeout,CancellationToken cancelToken = default) =>		
			TimeoutAfterIntern(task,timeout,cancelToken,null);

		/// <summary>
		/// Schedules a cancel operation on the CancellationTokenSource if the task fails to complete within the specified waiting period
		/// </summary>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeoutMlsec">Represents the number of milliseconds to wait.</param>
		/// <param name="cancelTokenSrc">A CancellationTokenSource that will be in the canceled state if a timeout occurs/param>
		/// <returns>A Task that represents a proxy for the source task.</returns>
		public static Task CancelAfter(this Task task,int timeoutMlsec,CancellationTokenSource cancelTokenSrc = default) =>		
			TimeoutAfterIntern(task,TimeSpan.FromMilliseconds(timeoutMlsec),cancelTokenSrc.TokenOrDefault(),cancelTokenSrc);

		/// <summary>
		/// Schedules a cancel operation on the CancellationTokenSource if the task fails to complete within the specified waiting period
		/// </summary>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeout">Represents a TimeSpan for the task to complete.</param>
		/// <param name="cancelTokenSrc">A CancellationTokenSource that will be in the canceled state if a timeout occurs/param>
		/// <returns>A Task that represents a proxy for the source task.</returns>
		public static Task CancelAfter(this Task task,TimeSpan timeout,CancellationTokenSource cancelTokenSrc = default)
		{			
			return TimeoutAfterIntern(task,timeout,cancelTokenSrc.TokenOrDefault(),cancelTokenSrc);
		}

		private static CancellationToken TokenOrDefault(this CancellationTokenSource cts) => cts?.Token ?? default;

		private static async Task TimeoutAfterIntern(Task task,TimeSpan timeout,CancellationToken cancelToken,CancellationTokenSource cancelTokenSrc = default)
		{
			if ((task ?? throw new ArgumentNullException(nameof(task))).IsCompleted || timeout == Timeout.InfiniteTimeSpan)
			{
				await task.ConfigureAwait(false); //propagate exception/cancellation
				return;
			}

			if (timeout < Timeout.InfiniteTimeSpan)	throw new ArgumentOutOfRangeException("timeout");

			if (timeout == TimeSpan.Zero) throw new TimeoutException();

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelTokenSrc?.Token ?? default))
			{
				if (task == await Task.WhenAny(task,Task.Delay(timeout,cts.Token)).ConfigureAwait(false))
				{
					cts.Cancel(); //ensure that the Delay task is cleaned up
					await task.ConfigureAwait(false); //propagate exception/cancellation
					return;
				}
			}

			cancelTokenSrc?.Cancel();
			throw new TimeoutException();
		}
		
		/// <summary>
		/// Throws a TimeoutException if the task fails to complete within the specified waiting period
		/// </summary>
		/// <typeparam name="TRes">The type of the result produced by the source task.</typeparam>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeoutMlsec">Represents the number of milliseconds to wait.</param>
		/// <param name="cancelToken">A cancellation token to observe while waiting for the task to complete.</param>
		/// <returns>A Task(TRes) that represents a proxy for the source task.</returns>
		public static Task<TRes> TimeoutAfter<TRes>(this Task<TRes> task,int timeoutMlsec,CancellationToken cancelToken = default)
		{
			return TimeoutAfterIntern(task,TimeSpan.FromMilliseconds(timeoutMlsec),cancelToken,null);
		}

		/// <summary>
		/// Throws a TimeoutException if the task fails to complete within the specified waiting period
		/// </summary>
		/// <typeparam name="TRes">The type of the result produced by the source task.</typeparam>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeout">Represents a TimeSpan for the task to complete.</param>
		/// <param name="cancelToken">A cancellation token to observe while waiting for the task to complete.</param>
		/// <returns>A Task(TRes) that represents a proxy for the source task.</returns>
		public static Task<TRes> TimeoutAfter<TRes>(this Task<TRes> task,TimeSpan timeout,CancellationToken cancelToken = default)
		{
			return TimeoutAfterIntern(task,timeout,cancelToken,null);
		}

		/// <summary>
		/// Schedules a cancel operation on the CancellationTokenSource if the task fails to complete within the specified waiting period
		/// </summary>
		/// <typeparam name="TRes">The type of the result produced by the source task.</typeparam>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeoutMlsec">Represents the number of milliseconds to wait.</param>
		/// <param name="cancelTokenSrc">A CancellationTokenSource that will be in the canceled state if a timeout occurs/param>
		/// <returns>A Task(TRes) that represents a proxy for the source task.</returns>
		public static Task<TRes> CancelAfter<TRes>(this Task<TRes> task,int timeoutMlsec,CancellationTokenSource cancelTokenSrc = default) =>		
			TimeoutAfterIntern(task,TimeSpan.FromMilliseconds(timeoutMlsec),cancelTokenSrc.TokenOrDefault(),cancelTokenSrc);

		/// <summary>
		/// Schedules a cancel operation on the CancellationTokenSource if the task fails to complete within the specified waiting period
		/// </summary>
		/// <typeparam name="TRes">The type of the result produced by the source task.</typeparam>
		/// <param name="task">The source task to wait for.</param>
		/// <param name="timeout">Represents a TimeSpan for the task to complete.</param>
		/// <param name="cancelTokenSrc">A CancellationTokenSource that will be in the canceled state if a timeout occurs/param>
		/// <returns>A Task(TRes) that represents a proxy for the source task.</returns>
		public static Task<TRes> CancelAfter<TRes>(this Task<TRes> task,TimeSpan timeout,CancellationTokenSource cancelTokenSrc = default) =>		
			TimeoutAfterIntern(task,timeout,cancelTokenSrc.TokenOrDefault(),cancelTokenSrc);		

		private static async Task<TRes> TimeoutAfterIntern<TRes>(Task<TRes> task,TimeSpan timeout,CancellationToken cancelToken,CancellationTokenSource cancelTokenSrc)
		{
			try
			{
				if ((task ?? throw new ArgumentNullException(nameof(task))).IsCompleted || timeout == Timeout.InfiniteTimeSpan)
					return await task.ConfigureAwait(false); //propagate exception/cancellation							

				if (timeout < Timeout.InfiniteTimeSpan)
					throw new ArgumentOutOfRangeException("timeout");

				if (timeout == TimeSpan.Zero)
					throw new TimeoutException();

				using (var delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken))
				{
					if (task == await Task.WhenAny(task,Task.Delay(timeout,delayCts.Token)).ConfigureAwait(false))
					{
						delayCts.Cancel(); //ensure that the Delay task is cleaned up
						return await task.ConfigureAwait(false); //propagate exception/cancellation
					}
				}

				cancelTokenSrc?.Cancel();
			}
			finally
			{
				cancelTokenSrc?.Dispose();
			}

			throw new TimeoutException();
		}
	}
}
