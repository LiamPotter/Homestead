#if !UNITY_WEBGL

using System;
using System.Collections.Generic;
using System.Threading;

#if NetFX_CORE
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace BeardedManStudios.Threading
{
	public static class ThreadManagement
	{
		public static int MainThreadId { get; private set; }

		public static int GetCurrentThreadId()
		{
#if NetFX_CORE
			return System.Threading.Tasks.Task.CurrentId.GetValueOrDefault();
#else
			return Thread.CurrentThread.ManagedThreadId;
#endif
		}

		public static void Initialize() { MainThreadId = GetCurrentThreadId(); }

		public static bool IsMainThread
		{
			get { return GetCurrentThreadId() == MainThreadId; }
		}
	}

	public class Task
	{
		private static List<Task> tasks = new List<Task>();
		private static object taskMutex = new Object();

#if !NetFX_CORE
		public Thread TrackedThread { get; private set; }
#endif

		private Task() { }

		private void SetExpression(Action expression)
		{
#if !NetFX_CORE
			TrackedThread = new Thread(new ThreadStart(expression));
			TrackedThread.IsBackground = true;
#endif
		}

		public void Kill()
		{
#if !NetFX_CORE
			TrackedThread.Abort();
#endif

			lock (taskMutex)
			{
				tasks.Remove(this);
			}
		}

		public void Wait()
		{
#if !NetFX_CORE
			while (TrackedThread.IsAlive) { }
#endif
		}

		public static void KillAll()
		{
			for (int i = tasks.Count - 1; i >= 0; --i)
				tasks[i].Kill();
		}
		
#if NetFX_CORE
		public static System.Threading.Tasks.Task Run(Action expression, int delayOrSleep = 0)
#else
		public static Task Run(Action expression, int delayOrSleep = 0)
#endif
		{
			Task task = new Task();

			Action inline = () =>
			{
#if !NetFX_CORE
				Thread.Sleep(delayOrSleep);
#endif

				expression();

				lock (taskMutex)
				{
					tasks.Remove(task);
				}
			};

			task.SetExpression(inline);

#if NetFX_CORE
			return System.Threading.Tasks.Task.Run(async () =>
			{
				await System.Threading.Tasks.Task.Delay(delayOrSleep);

				inline();
			});
#else

			task.TrackedThread.Start();

			lock (taskMutex)
			{
				tasks.Add(task);
			}

			return task;
#endif
		}
	}
}
#endif