using System.Collections.Generic;

namespace XMachine.Components.Collections
{
	internal sealed class XICollection<TCollection, TItem> : XCollection<TCollection, TItem>
		where TCollection : ICollection<TItem>
	{
		public XICollection(XType<TCollection> xType) : base(xType) { }

		protected override void AddItem(TCollection collection, int index, TItem item) =>
			collection.Add(item);
	}
}
