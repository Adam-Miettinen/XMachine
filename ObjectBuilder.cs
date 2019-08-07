using System;

namespace XMachine
{
	/// <summary>
	/// Wraps a single object, either constructed or to be constructed in the future, and having type 
	/// <typeparamref name="T"/>. A delegate may be invoked when the object is constructed.
	/// </summary>
	public sealed class ObjectBuilder<T>
	{
		private Action<object> onConstructed;

		private T innerObject;

		internal ObjectBuilder(Action<object> onConstructed) =>
			this.onConstructed = onConstructed;

		/// <summary>
		/// Get or set the inner object wrapped by this <see cref="ObjectBuilder{T}"/>. This
		/// property may be set only once. Returns a default value if <see cref="IsConstructed"/>
		/// is <c>false</c>.
		/// </summary>
		public T Object
		{
			get => innerObject;
			set
			{
				if (IsConstructed)
				{
					throw new InvalidOperationException($"The {typeof(T).FullName} has already been constructed.");
				}

				innerObject = value;
				IsConstructed = true;

				if (onConstructed != null)
				{
					onConstructed(Object);
					onConstructed = null;
				}
			}
		}

		/// <summary>
		/// <c>true</c> if the <see cref="Object"/> has been constructed.
		/// </summary>
		public bool IsConstructed { get; private set; }
	}
}
