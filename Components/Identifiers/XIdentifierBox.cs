using System;

namespace XMachine.Components.Identifiers
{
	internal sealed class XIdentifierBox
	{
		internal static XIdentifierBox Box<TType, TId>(XIdentifier<TType, TId> identifier)
			where TType : class where TId : class =>
			new XIdentifierBox(identifier)
			{
				Type = typeof(TType),
				IdType = typeof(TId),
				getId = (obj) => identifier.GetId((TType)obj),
				equals = (x, y) => identifier.Equals((TType)x, (TType)y),
				hashCode = (obj) => identifier.GetHashCode((TType)obj),
				keyEquals = (x, y) => identifier.KeyComparer.Equals((TId)x, (TId)y),
				keyHash = (obj) => identifier.KeyComparer.GetHashCode((TId)obj)
			};

		internal static XIdentifier<TType, TId> Unbox<TType, TId>(XIdentifierBox box)
			where TType : class where TId : class =>
			box.identifier as XIdentifier<TType, TId>;

		private readonly object identifier;

		private Func<object, object> getId;
		private Func<object, object, bool> equals;
		private Func<object, int> hashCode;
		private Func<object, object, bool> keyEquals;
		private Func<object, int> keyHash;

		private XIdentifierBox(object identifier) => this.identifier = identifier;

		internal Type Type { get; private set; }

		internal Type IdType { get; private set; }

		public object GetId(object obj) => getId(obj);

		public new bool Equals(object x, object y) => equals(x, y);

		public int GetHashCode(object obj) => hashCode(obj);

		public override bool Equals(object obj) => obj is XIdentifierBox box && box.identifier == identifier;

		public override int GetHashCode() => identifier.GetHashCode();

		public bool KeyEquals(object x, object y) => keyEquals(x, y);

		public int KeyHash(object obj) => keyHash(obj);
	}
}
