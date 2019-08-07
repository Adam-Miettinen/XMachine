using System;
using System.Collections.Generic;

namespace XMachine.Components.Collections
{
	internal sealed class XIDictionary<TDictionary, TKey, TValue> : XCollection<TDictionary, KeyValuePair<TKey, TValue>>
		where TDictionary : IDictionary<TKey, TValue>
	{
		internal XIDictionary() { }

		protected override void AddItem(TDictionary collection, KeyValuePair<TKey, TValue> item)
		{
			if (item.Key == null)
			{
				throw new ArgumentNullException(nameof(item.Key));
			}
			if (collection.ContainsKey(item.Key))
			{
				collection[item.Key] = item.Value;
			}
			else
			{
				collection.Add(item);
			}
		}
	}
}
