using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XReadOnlyCollection<T> : XCollection<ReadOnlyCollection<T>, T>
	{
		internal XReadOnlyCollection() { }

		protected override void AddItem(ReadOnlyCollection<T> collection, T item) { }

		protected override void OnBuild(XType<ReadOnlyCollection<T>> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<ReadOnlyCollection<T>> objectBuilder)
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
				reader.Read<T>(subElement, x =>
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
					objectBuilder.Object = new ReadOnlyCollection<T>(new List<T>(items.Cast<T>()));
					return true;
				}
				return false;
			});
		}
	}
}
