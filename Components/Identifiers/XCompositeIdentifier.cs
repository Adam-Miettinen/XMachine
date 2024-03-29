﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// Combines multiple <see cref="XIdentifier{TType, TId}"/> objects into a single object that can
	/// assign IDs and perform equality comparisons.
	/// </summary>
	public sealed class XCompositeIdentifier : XIdentifier<object, object>
	{
		private class CompositeEqualityComparer : IEqualityComparer<object>
		{
			private readonly ICollection<XIdentifierBox> identifiers;

			internal CompositeEqualityComparer(ICollection<XIdentifierBox> identifiers) =>
				this.identifiers = identifiers;

			public new bool Equals(object x, object y)
			{
				if (x == null)
				{
					return y == null;
				}
				if (y != null)
				{
					Type xt = x.GetType(), yt = y.GetType();

					foreach (XIdentifierBox box in identifiers)
					{
						if (box.IdType.IsAssignableFrom(xt) && box.IdType.IsAssignableFrom(yt))
						{
							return box.KeyEquals(x, y);
						}
					}
				}

				return x == y;
			}

			public int GetHashCode(object obj)
			{
				if (obj != null)
				{
					Type type = obj.GetType();

					foreach (XIdentifierBox box in identifiers)
					{
						if (box.IdType.IsAssignableFrom(type))
						{
							return box.KeyHash(obj);
						}
					}
				}

				return 0;
			}
		}

		private readonly ICollection<XIdentifierBox> identifiers = new HashSet<XIdentifierBox>();

		/// <summary>
		/// Create a new, empty <see cref="XCompositeIdentifier"/>.
		/// </summary>
		public XCompositeIdentifier() => KeyComparer = new CompositeEqualityComparer(identifiers);

		/// <summary>
		/// Create a new <see cref="XCompositeIdentifier"/> that copies the contents of <paramref name="other"/>.
		/// </summary>
		/// <param name="other">Another <see cref="XCompositeIdentifier"/> whose identifiers will be copied
		/// into the new instance.</param>
		public XCompositeIdentifier(XCompositeIdentifier other) : this()
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other));
			}
			foreach (XIdentifierBox box in other.identifiers)
			{
				identifiers.Add(box);
			}
		}

		/// <summary>
		/// Get the number of <see cref="XIdentifier{TType, TId}"/> objects contained.
		/// </summary>
		public int Count => identifiers.Count;

		/// <summary>
		/// Get the ID produced by the <see cref="XIdentifier{TType, TId}"/> that has the closest-matching
		/// object type.
		/// </summary>
		/// <param name="obj">The <see cref="object"/> to create an ID for.</param>
		/// <returns>An <see cref="object"/> that acts as ID, or <c>null</c>.</returns>
		public override object GetId(object obj)
		{
			if (obj == null)
			{
				return null;
			}
			Type type = obj.GetType();

			if (XDefaultTypes.IsDefaultType(type))
			{
				return null;
			}

			XIdentifierBox bestBox = null;

			foreach (XIdentifierBox box in identifiers)
			{
				if (box.Type.IsAssignableFrom(type) &&
					(bestBox == null || bestBox.Type.IsAssignableFrom(box.Type)))
				{
					bestBox = box;
				}
			}

			return bestBox?.GetId(obj);
		}

		/// <summary>
		/// Get whether any <see cref="XIdentifier{TType, TId}"/> in this <see cref="XCompositeIdentifier"/> is
		/// able to assign an ID to objects of the given <see cref="Type"/>.
		/// </summary>
		/// <param name="type">A <see cref="Type"/> to be checked as ID-able.</param>
		/// <returns>True if IDs can be generated for objects of the given type.</returns>
		public override bool CanId(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			foreach (XIdentifierBox box in identifiers)
			{
				if (box.CanId(type))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Get whether any <see cref="XIdentifier{TType, TId}"/> in this <see cref="XCompositeIdentifier"/> is
		/// able to assign an ID to objects of the given <see cref="Type"/>.
		/// </summary>
		/// <param name="type">A <see cref="Type"/> to be checked as ID-able.</param>
		/// <param name="idType">The <see cref="Type"/> of the ID that will be generated.</param>
		/// <returns>True if IDs can be generated for objects of the given type.</returns>
		public bool CanId(Type type, out Type idType)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			XIdentifierBox bestBox = null;

			foreach (XIdentifierBox box in identifiers)
			{
				if (box.CanId(type) &&
					(bestBox == null || bestBox.Type.IsAssignableFrom(box.Type)))
				{
					bestBox = box;
				}
			}

			if (bestBox != null)
			{
				idType = bestBox.IdType;
				return true;
			}

			idType = null;
			return false;
		}

		/// <summary>
		/// Add a new <see cref="XIdentifier{TType, TId}"/> to this <see cref="XCompositeIdentifier"/>.
		/// </summary>
		/// <param name="identifier">The <see cref="XIdentifier{TType, TId}"/> to add.</param>
		public void Identify<TType, TId>(XIdentifier<TType, TId> identifier) where TType : class =>
			identifiers.Add(XIdentifierBox.Box(identifier ?? throw new ArgumentNullException(nameof(identifier))));

		/// <summary>
		/// Add a new <see cref="XIdentifier{TType, TId}"/> to this <see cref="XCompositeIdentifier"/>.
		/// </summary>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		public void Identify<TType, TId>(Func<TType, TId> identifier) where TType : class =>
			identifiers.Add(XIdentifierBox.Box(XIdentifier<TType, TId>.Create(identifier ??
				throw new ArgumentNullException(nameof(identifier)))));

		/// <summary>
		/// Remove any <see cref="XIdentifier{TType, TId}"/> from this <see cref="XCompositeIdentifier"/> if its
		/// reference type is assignable to <typeparamref name="TType"/>.
		/// </summary>
		public void Clear<TType>()
		{
			Type type = typeof(TType);

			foreach (XIdentifierBox box in identifiers.ToArray())
			{
				if (box.CanId(type))
				{
					_ = identifiers.Remove(box);
				}
			}
		}

		/// <summary>
		/// Remove all identifiers.
		/// </summary>
		public void Clear() => identifiers.Clear();
	}
}
