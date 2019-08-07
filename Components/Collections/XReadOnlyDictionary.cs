using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XReadOnlyDictionary<TKey, TValue> :
		XCollection<ReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
	{
		internal XReadOnlyDictionary() { }

		protected override void AddItem(ReadOnlyDictionary<TKey, TValue> collection, KeyValuePair<TKey, TValue> item) { }

		protected override void OnBuild(XType<ReadOnlyDictionary<TKey, TValue>> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<ReadOnlyDictionary<TKey, TValue>> objectBuilder)
		{
			if (!element.HasElements)
			{
				return;
			}

			IEnumerable<XElement> itemElements = ItemsAsElements ? element.Elements() : element.Elements(ItemName);

			if (!itemElements.Any())
			{
				return;
			}

			object[] items = Enumerable.Repeat(PlaceholderObject, itemElements.Count()).ToArray();

			int i = 0;

			foreach (XElement subElement in itemElements)
			{
				int idx = i++;
				reader.Read<KeyValuePair<TKey, TValue>>(subElement, x =>
				{
					items[idx] = x;
					return true;
				},
				ReaderHints.IgnoreElementName);
			}

			reader.AddTask(this, () =>
			{
				if (items.All(x => !ReferenceEquals(x, PlaceholderObject)))
				{
					IDictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>(items.Length);

					foreach (KeyValuePair<TKey, TValue> kv in items.Cast<KeyValuePair<TKey, TValue>>())
					{
						dict.Add(kv.Key, kv.Value);
					}

					objectBuilder.Object = new ReadOnlyDictionary<TKey, TValue>(dict);
					return true;
				}
				return false;
			});
		}
	}
}
