using System;
using System.Collections.Generic;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// A default implementation of <see cref="XIdentifier{TType, TId}"/> that assigns all objects of type 
	/// <typeparamref name="T"/> a probably-unique <see cref="Guid"/> that is generated from the object's 
	/// runtime type, its hashcode, the current time in ticks, and a random integer. A <see cref="Guid"/> 
	/// appears in XML as a 32-digit integer.<br />
	/// <see cref="XGuidIdentifier{T}"/> stores a mapping between objects and assigned IDs internally. You 
	/// must call the <see cref="Reset"/> method after each read/write operation if you plan to re-use 
	/// <see cref="XGuidIdentifier{T}"/> across multiple operations.
	/// </summary>
	public sealed class XGuidIdentifier<T> : XIdentifier<T, Guid> where T : class
	{
		private readonly IDictionary<T, Guid> assigned = new Dictionary<T, Guid>();

		private readonly Random randomizer = new Random();

		/// <summary>
		/// Check whether the <see cref="XGuidIdentifier{T}"/> can assign an ID to the given type.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to check.</param>
		/// <returns>True for by-reference types assignable to <typeparamref name="T"/>.</returns>
		public override bool CanId(Type type) => type == null
			? throw new ArgumentNullException(nameof(type))
			: typeof(T).IsAssignableFrom(type) && type.IsByRef;

		/// <summary>
		/// Get the <see cref="Guid"/> associated with the given object, generating a new <see cref="Guid"/>
		/// if necessary.
		/// </summary>
		/// <param name="obj">The object to be ID'd.</param>
		/// <returns>An existing or new <see cref="Guid"/>.</returns>
		public override Guid GetId(T obj)
		{
			if (obj == null)
			{
				return Guid.Empty;
			}
			if (assigned.TryGetValue(obj, out Guid id))
			{
				return id;
			}

			byte[] bytes = new byte[16];

			// Use all 4 bytes of the hashcode

			int hash = obj.GetHashCode();
			bytes[0] = (byte)hash;
			bytes[1] = (byte)(hash >> 8);
			bytes[2] = (byte)(hash >> 16);
			bytes[3] = (byte)(hash >> 24);

			// Use the smallest 4 bytes of the current ticks (8 bytes)

			long time = DateTime.Now.Ticks;
			bytes[4] = (byte)time;
			bytes[5] = (byte)(time >> 8);
			bytes[6] = (byte)(time >> 16);
			bytes[7] = (byte)(time >> 24);

			// Use all 4 bytes of a random integer (12 bytes)

			int rand = randomizer.Next();
			bytes[8] = (byte)rand;
			bytes[9] = (byte)(rand >> 8);
			bytes[10] = (byte)(rand >> 16);
			bytes[11] = (byte)(rand >> 24);

			// Use all 4 bytes of the runtime type's hash (16 bytes)

			int typeHash = obj.GetType().GetHashCode();
			bytes[0] = (byte)typeHash;
			bytes[1] = (byte)(typeHash >> 8);
			bytes[2] = (byte)(typeHash >> 16);
			bytes[3] = (byte)(typeHash >> 24);

			// Assign the Guid

			id = new Guid(bytes);
			assigned.Add(obj, id);
			return id;
		}

		/// <summary>
		/// Resets the <see cref="XGuidIdentifier{T}"/>, clearing its internal memory of objects that have been
		/// assigned IDs. Calling this method is important if you plan to re-use <see cref="XGuidIdentifier{T}"/>
		/// across more than one read or write operation.
		/// </summary>
		public void Reset() => assigned.Clear();
	}
}
