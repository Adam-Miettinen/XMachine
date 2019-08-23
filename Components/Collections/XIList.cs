using System;
using System.Collections;

namespace XMachine.Components.Collections
{
	internal sealed class XIList<T> : XCollection<T> where T : IList
	{
		public XIList(XType<T> xType) : base(xType) { }

		protected override Type ItemType => typeof(object);

		protected override void AddItem(T collection, int index, object item) => collection.Add(item);
	}
}
