using System;
using System.Collections.Generic;
using System.Text;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// A default implementation of <see cref="XIdentifier{TType, TId}"/> that assigns all objects of type 
	/// <typeparamref name="T"/> a unique (probably) <see cref="Guid"/> that is generated from the object's 
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
		/// Check whether the <see cref="XGuidIdentifier{T}"/> can assign an ID to the given type. Returns true
		/// for all <see cref="Type"/>s assignable to <typeparamref name="T"/> that are passed by reference.
		/// </summary>
		public override bool CanId(Type type) => type == null
			? throw new ArgumentNullException(nameof(type))
			: typeof(T).IsAssignableFrom(type) && type.IsByRef;

		/// <summary>
		/// Get the <see cref="Guid"/> associated with the given object, generating a new <see cref="Guid"/>
		/// if necessary.
		/// </summary>
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

			int hash = obj.GetHashCode();
			bytes[0] = (byte)hash;
			bytes[1] = (byte)(hash >> 8);
			bytes[2] = (byte)(hash >> 16);
			bytes[3] = (byte)(hash >> 24);

			long time = DateTime.Now.Ticks;
			bytes[4] = (byte)time;
			bytes[5] = (byte)(time >> 8);

			int rand = randomizer.Next();
			bytes[6] = (byte)rand;
			bytes[7] = (byte)(rand >> 8);
			bytes[8] = (byte)(rand >> 16);
			bytes[9] = (byte)(rand >> 24);

			string name = obj.GetType().Name;
			_ = Encoding.UTF8.GetBytes(name, 0, name.Length < 6 ? name.Length : 6, bytes, 10);

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
