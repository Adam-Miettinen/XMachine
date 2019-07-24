using System.Collections;

namespace XMachine.Components.Collections
{
	internal sealed class XIDictionary<T> : XCollection<T, DictionaryEntry> where T : IDictionary
	{
		internal XIDictionary() { }

		protected override void AddItem(T collection, DictionaryEntry item)
		{
			if (collection.Contains(item.Key))
			{
				collection[item.Key] = item.Value;
			}
			else
			{
				collection.Add(item.Key, item.Value);
			}
		}
	}
}
