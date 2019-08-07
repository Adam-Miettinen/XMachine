using System.Collections.Generic;

namespace XMachine
{
	/// <summary>
	/// Represents an object within <see cref="XMachine"/> whose functionality is extensible by <see cref="IXComponent"/>
	/// objects of type <typeparamref name="T"/>.
	/// </summary>
	public interface IXWithComponents<T> where T : IXComponent
	{
		/// <summary>
		/// Retrieve a component of type <typeparamref name="V"/>.
		/// </summary>
		V Component<V>() where V : T;

		/// <summary>
		/// Retrieve all components of type <typeparamref name="V"/>.
		/// </summary>
		IEnumerable<V> Components<V>() where V : T;

		/// <summary>
		/// Retrieve all components.
		/// </summary>
		IEnumerable<T> Components();

		/// <summary>
		/// Register a component.
		/// </summary>
		void Register(T component);

		/// <summary>
		/// Register components.
		/// </summary>
		void Register(params T[] components);

		/// <summary>
		/// Deregister a component.
		/// </summary>
		void Deregister(T component);

		/// <summary>
		/// Deregister components.
		/// </summary>
		void Deregister(params T[] components);
	}
}
