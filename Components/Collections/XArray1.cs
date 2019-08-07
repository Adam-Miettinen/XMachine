using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XArray1<T> : XCollection<T[], T>
	{
		internal XArray1() { }

		protected override void AddItem(T[] collection, T item) { }

		protected override void OnBuild(XType<T[]> xType, IXReadOperation reader, XElement element, ObjectBuilder<T[]> objectBuilder)
		{
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

			IEnumerable<XElement> itemElements = ItemsAsElements ? element.Elements() : element.Elements(ItemName);

			objectBuilder.Object = lb == 0
				? new T[itemElements.Count()]
				: (T[])Array.CreateInstance(typeof(T), new int[1] { itemElements.Count() }, new int[1] { lb });

			// Read in items

			if (objectBuilder.Object.Length > 0)
			{
				foreach (XElement subElement in itemElements)
				{
					int idx = lb++;
					reader.Read<T>(subElement, x =>
					{
						objectBuilder.Object[idx] = x;
						return true;
					},
					ReaderHints.IgnoreElementName);
				}
			}
		}

		protected override bool OnWrite(XType<T[]> xType, IXWriteOperation writer, T[] obj, XElement element)
		{
			int lb = obj.GetLowerBound(0);

			if (lb != 0)
			{
				element.SetAttributeValue(XComponents.Component<XAutoCollections>().ArrayLowerBoundName, XmlTools.Write(lb));
			}

			IEnumerator enumerator = EnumerateItems(obj);

			while (enumerator.MoveNext())
			{
				element.Add(writer.WriteTo(new XElement(ItemName), (T)enumerator.Current));
			}

			return true;
		}

	}
}
