using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	/// <summary>
	/// An abstract implementation of <see cref="XTypeComponent{TType}"/> that handles collection types.
	/// </summary>
	public abstract class XCollection<TCollection, TItem> : XCollection<TCollection> where TCollection : IEnumerable
	{
		/// <summary>
		/// Create a new instance of <see cref="XCollection{TCollection, TItem}"/>.
		/// </summary>
		protected XCollection() { }

		/// <summary>
		/// Implement this method to provide an "Add" method for the collection implementation.
		/// </summary>
		protected abstract void AddItem(TCollection collection, TItem item);

		/// <summary>
		/// Scans for elements representing collection items and reads them.
		/// </summary>
		protected override void OnBuild(XType<TCollection> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<TCollection> objectBuilder)
		{
			if (!element.HasElements)
			{
				return;
			}

			IEnumerable<XElement> itemElements = ItemsAsElements ? element.Elements() : element.Elements(ItemName);

			int i = 0, highest = 0, n = itemElements.Count();

			object[] items = Enumerable.Repeat(PlaceholderObject, n).ToArray();

			foreach (XElement subElement in itemElements)
			{
				int idx = i++;
				reader.Read<TItem>(subElement, x =>
				{
					items[idx] = x;
					return true;
				},
				ReaderHints.IgnoreElementName);
			}

			reader.AddTask(this, () =>
			{
				if (!objectBuilder.IsConstructed)
				{
					return false;
				}

				for (; highest < items.Length; highest++)
				{
					if (ReferenceEquals(items[highest], PlaceholderObject))
					{
						return false;
					}

					try
					{
						AddItem(objectBuilder.Object, (TItem)items[highest]);
					}
					catch (Exception e)
					{
						reader.ExceptionHandler(e);
					}
				}

				return true;
			});
		}

		/// <summary>
		/// Writes collection items as elements in the order provided by the collection's enumerator.
		/// </summary>
		protected override bool OnWrite(XType<TCollection> xType, IXWriteOperation writer, TCollection obj, XElement element)
		{
			IEnumerator enumerator = EnumerateItems(obj);

			if (ItemsAsElements)
			{
				while (enumerator.MoveNext())
				{
					element.Add(writer.WriteElement((TItem)enumerator.Current));
				}
			}
			else
			{
				while (enumerator.MoveNext())
				{
					element.Add(writer.WriteTo(new XElement(ItemName), (TItem)enumerator.Current));
				}
			}

			return false;
		}
	}
}
