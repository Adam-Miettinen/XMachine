using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XArray1<T> : XCollection<T[], T>
	{
		internal XArray1(XType<T[]> xType) : base(xType) { }

		protected override void AddItem(T[] collection, int index, T item) =>
			collection.SetValue(item, collection.GetLowerBound(0) + index);

		protected override void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<T[]> objectBuilder,
			XObjectArgs args)
		{
			XCollectionArgs collectionArgs = args as XCollectionArgs ?? new XCollectionArgs(default, ItemsAsElements, ItemName);

			// Read in items

			IEnumerable<XElement> itemElements = GetItemElements(element, collectionArgs);

			object[] itemsRead = Enumerable.Repeat(XmlTools.PlaceholderObject, itemElements.Count()).ToArray();

			int i = 0;

			foreach (XElement item in itemElements)
			{
				int idx = i++;
				reader.Read(item, ItemType, x =>
					{
						itemsRead[idx] = x;
						return true;
					},
					collectionArgs.ItemArgs);
			}

			// Determine array lower bound

			int lb = 0;

			if (element.HasAttributes)
			{
				XAttribute lbAttribute = element.Attribute(XComponents.Component<XAutoCollections>().ArrayLowerBoundName);
				if (lbAttribute != null)
				{
					lb = XmlTools.ReadInt(lbAttribute.Value);
				}
			}

			// Instantiate the object

			reader.AddTask(this, () =>
			{
				if (!itemsRead.Any(x => ReferenceEquals(x, XmlTools.PlaceholderObject)))
				{
					T[] array = lb == 0
						? new T[itemElements.Count()]
						: (T[])Array.CreateInstance(typeof(T), new int[1] { itemElements.Count() }, new int[1] { lb });

					for (int j = 0; j < itemsRead.Length; j++)
					{
						array[lb + j] = (T)itemsRead[j];
					}

					objectBuilder.Object = array;

					return true;
				}
				return false;
			});
		}

		protected override bool OnWrite(IXWriteOperation writer, T[] obj, XElement element, XObjectArgs args)
		{
			_ = base.OnWrite(writer, obj, element, args);

			// Record lower bound if non-zero

			int lb = obj.GetLowerBound(0);

			if (lb != 0)
			{
				element.SetAttributeValue(XComponents.Component<XAutoCollections>().ArrayLowerBoundName, XmlTools.Write(lb));
			}

			return true;
		}

	}
}
