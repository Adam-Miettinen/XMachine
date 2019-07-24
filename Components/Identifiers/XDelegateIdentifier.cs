using System;
using System.Collections.Generic;

namespace XMachine.Components.Identifiers
{
	internal sealed class XDelegateIdentifier<TType, TId> : XIdentifier<TType, TId>
		where TType : class where TId : class
	{
		private readonly Func<TType, TId> getId;

		internal XDelegateIdentifier(Func<TType, TId> getId, IEqualityComparer<TId> keyComparer = null)
			: base(keyComparer) =>
			this.getId = getId ?? throw new ArgumentNullException(nameof(getId));

		public override TId GetId(TType obj) => getId(obj);
	}
}
