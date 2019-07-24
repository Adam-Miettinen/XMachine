using System;
using System.Collections.Generic;
namespace XMachine
{
	/// <summary>
	/// The <see cref="ObjectBuilder{T}"/> class facilitates the construction of complex objects. Steps in the
	/// construction and modification of the object are divided into tasks that may complete in any order.
	/// </summary>
	public sealed class ObjectBuilder<T>
	{
		private static readonly bool isByRef = typeof(T).IsByRef;

		private readonly LinkedList<Func<bool>> tasks = new LinkedList<Func<bool>>();

		private T innerObject;

		internal ObjectBuilder() { }

		/// <summary>
		/// Get or set the inner object being constructed by this <see cref="ObjectBuilder{T}"/>. This
		/// property may only be set once.
		/// </summary>
		public T Object
		{
			get => innerObject;
			set
			{
				if (IsConstructed && isByRef)
				{
					throw new InvalidOperationException($"The {typeof(T).FullName} has already been constructed.");
				}

				innerObject = value;
				IsConstructed = true;

				if (OnConstructed != null)
				{
					OnConstructed();
					OnConstructed = null;
				}
			}
		}

		/// <summary>
		/// <c>true</c> if the inner object has been constructed, i.e. <see cref="Object"/> != null;
		/// </summary>
		public bool IsConstructed { get; private set; }

		internal bool IsFinished => tasks.Count == 0;

		internal Action OnConstructed { get; set; }

		/// <summary>
		/// Add a task to be performed by this <see cref="ObjectBuilder{T}"/>. 
		/// </summary>
		public void AddTask(Func<bool> task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("Can't execute null task");
			}
			tasks.AddLast(task);
		}

		internal bool TryFinish()
		{
			if (IsFinished)
			{
				return false;
			}

			bool progress = false;
			LinkedListNode<Func<bool>> node = tasks.First;

			while (node != null)
			{
				try
				{
					if (node.Value.Invoke())
					{
						node.List.Remove(node);
						progress = true;
					}
				}
				catch (Exception e)
				{
					node.List.Remove(node);
					throw e;
				}

				node = node.Next;
			}

			return progress;
		}
	}
}
