using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XStack<TStack, TItem> : XCollection<TStack, TItem> where TStack : Stack<TItem>
	{
		public XStack(XType<TStack> xType) : base(xType) { }

		protected override void AddItem(TStack collection, int index, TItem item) => collection.Push(item);

		protected override IEnumerator EnumerateItems(TStack collection) => collection.Reverse().GetEnumerator();
	}
}
