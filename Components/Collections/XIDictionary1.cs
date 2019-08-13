using System;
using System.Collections;

namespace XMachine.Components.Collections
{
	internal sealed class XIDictionary<T> : XCollection<T> where T : IDictionary
	{
		internal XIDictionary(XType<T> xType) : base(xType) { }

		protected override Type ItemType => typeof(DictionaryEntry);

		protected override void AddItem(T collection, int index, object item)
		{
			DictionaryEntry entry = (DictionaryEntry)item;

			if (collection.Contains(entry.Key))
			{
				collection[entry.Key] = entry.Value;
			}
			else
			{
				collection.Add(entry.Key, entry.Value);
			}
		}

		protected override IEnumerator EnumerateItems(T collection)
		{
			IDictionaryEnumerator enumerator = collection.GetEnumerator();

			while (enumerator.MoveNext())
			{
				yield return enumerator.Entry;
			}
		}
	}
}
