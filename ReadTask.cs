using System;

namespace XMachine
{
	internal readonly struct ReadTask
	{
		private readonly Func<bool> task;

		internal ReadTask(object source, Func<bool> task)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			this.task = task ?? throw new ArgumentNullException(nameof(task));
		}

		internal object Source { get; }

		internal bool Invoke() => task();
	}
}
