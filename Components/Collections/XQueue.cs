using System.Collections.Generic;

namespace XMachine.Components.Collections
{
	internal sealed class XQueue<TQueue, TItem> : XCollection<TQueue, TItem> where TQueue : Queue<TItem>
	{
		internal XQueue() { }

		protected override void AddItem(TQueue collection, TItem item) => collection.Enqueue(item);
	}
}
