using System;
using System.Collections.Generic;

namespace XMachine.Components.Collections
{
	internal sealed class XIDictionary<TDictionary, TKey, TValue> : XCollection<TDictionary, KeyValuePair<TKey, TValue>>
		where TDictionary : IDictionary<TKey, TValue>
	{
		public XIDictionary(XType<TDictionary> xType) : base(xType) { }

		protected override void AddItem(TDictionary collection, int index, KeyValuePair<TKey, TValue> item)
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
