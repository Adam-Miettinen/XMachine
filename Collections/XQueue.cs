using System.Collections.Generic;

namespace XMachine.Components.Collections
{
	internal sealed class XQueue<TQueue, TItem> : XCollection<TQueue, TItem> where TQueue : Queue<TItem>
	{
		public XQueue(XType<TQueue> xType) : base(xType) { }

		protected override void AddItem(TQueue collection, int index, TItem item) => collection.Enqueue(item);
	}
}
