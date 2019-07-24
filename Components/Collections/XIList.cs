using System.Collections;

namespace XMachine.Components.Collections
{
	internal sealed class XIList<T> : XCollection<T, object> where T : IList
	{
		internal XIList() { }

		protected override void AddItem(T collection, object item) => collection.Add(item);
	}
}
