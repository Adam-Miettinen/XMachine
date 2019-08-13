using System;
using System.Collections.Generic;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// <see cref="XIdentifier{TType, TId}"/> enables in-XML references for objects of type <typeparamref name="TType"/>
	/// by assigning them unique identifiers of type <typeparamref name="TId"/>.
	/// </summary>
	/// <typeparam name="TType">A by-reference type that may be serialized as a reference.</typeparam>
	/// <typeparam name="TId">An object type that will serve as the unique ID for <typeparamref name="TType"/>s. For
	/// deserialization to work correctly, <typeparamref name="TId"/> must have a registered <see cref="XTexter{T}"/>
	/// in the active <see cref="XDomain"/>.</typeparam>
	public abstract class XIdentifier<TType, TId> : IEqualityComparer<TType>
		where TType : class
	{
		/// <summary>
		/// Create a new <see cref="XIdentifier{TType, TId}"/> from the given delegate and optional 
		/// <see cref="IEqualityComparer{TId}"/>.
		/// </summary>
		/// <param name="getId">A delegate that generates IDs for objects.</param>
		/// <param name="keyComparer">An optional <see cref="IEqualityComparer{T}"/> that determines equality between
		/// IDs.</param>
		/// <returns>A new instance of <see cref="XIdentifier{TType, TId}"/>.</returns>
		public static XIdentifier<TType, TId> Create(Func<TType, TId> getId, IEqualityComparer<TId> keyComparer = null) =>
			new XDelegateIdentifier<TType, TId>(getId, keyComparer);

		/// <summary>
		/// Create a new <see cref="XIdentifier{TType, TId}"/> using the default equality comparer on IDs.
		/// </summary>
		protected XIdentifier() : this(EqualityComparer<TId>.Default) { }

		/// <summary>
		/// Create a new <see cref="XIdentifier{TType, TId}"/> using the given equality comparer for IDs.
		/// </summary>
		protected XIdentifier(IEqualityComparer<TId> keyEquality) =>
			KeyComparer = keyEquality ?? EqualityComparer<TId>.Default;

		/// <summary>
		/// The <see cref="IEqualityComparer{TId}"/> used to compare IDs for equality.
		/// </summary>
		public virtual IEqualityComparer<TId> KeyComparer { get; protected set; }

		/// <summary>
		/// Implement this method to specify whether the <see cref="XIdentifier{TType, TId}"/> can provide an
		/// ID for an object of the given <see cref="Type"/>.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to be checked.</param>
		/// <returns>True if this <see cref="XIdentifier{TType, TId}"/> can generate IDs for <paramref name="type"/>.</returns>
		public abstract bool CanId(Type type);

		/// <summary>
		/// Implement this method to assign a unique object of type <typeparamref name="TId"/> to an object of
		/// type <typeparamref name="TType"/>.
		/// </summary>
		/// <param name="obj">The object to be assigned an ID.</param>
		/// <returns>A unique <typeparamref name="TId"/>, or a default value.</returns>
		public abstract TId GetId(TType obj);

		/// <summary>
		/// Determine equality between two objects of type <typeparamref name="TType"/> using a comparison of keys.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>True if <paramref name="x"/> and <paramref name="y"/> are assigned equal IDs.</returns>
		public bool Equals(TType x, TType y) => KeyComparer.Equals(GetId(x), GetId(y));

		/// <summary>
		/// Generate a hashcode from the ID of <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">The object to be hashed.</param>
		/// <returns>An <see cref="int"/> hashcode for <paramref name="obj"/> based on its ID.</returns>
		public int GetHashCode(TType obj) => KeyComparer.GetHashCode(GetId(obj));
	}
}
