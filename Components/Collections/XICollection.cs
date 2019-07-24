using System.Collections.Generic;

namespace XMachine.Components.Collections
{
	internal sealed class XICollection<TCollection, TItem> : XCollection<TCollection, TItem>
		where TCollection : ICollection<TItem>
	{
		internal XICollection() { }

		protected override void AddItem(TCollection collection, TItem item) => collection.Add(item);
	}
}
