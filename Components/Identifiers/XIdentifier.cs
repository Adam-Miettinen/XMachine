using System;
using System.Collections.Generic;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// <see cref="XIdentifier{TType, TId}"/> enables in-XML references for objects of type <typeparamref name="TType"/>
	/// by assigning them unique identifiers of type <typeparamref name="TId"/>.
	/// </summary>
	public abstract class XIdentifier<TType, TId> : IEqualityComparer<TType>
		where TType : class where TId : class
	{
		/// <summary>
		/// Create a new <see cref="XIdentifier{TType, TId}"/> from the given delegate and optional 
		/// <see cref="IEqualityComparer{TId}"/>.
		/// </summary>
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
		/// Implement this method to assign a unique object of type <typeparamref name="TId"/> to an object of
		/// type <typeparamref name="TType"/>.
		/// </summary>
		public abstract TId GetId(TType obj);

		/// <summary>
		/// Uses <see cref="GetId(TType)"/> and <see cref="KeyComparer"/> to determine equality between two objects 
		/// of type <typeparamref name="TType"/>.
		/// </summary>
		public bool Equals(TType x, TType y) => KeyComparer.Equals(GetId(x), GetId(y));

		/// <summary>
		/// use <see cref="KeyComparer"/> to generate a hashcode from the ID of <paramref name="obj"/>.
		/// </summary>
		public int GetHashCode(TType obj) => KeyComparer.GetHashCode(GetId(obj));
	}
}
