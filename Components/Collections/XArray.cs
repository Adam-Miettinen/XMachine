using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XArray<T> : XCollection<T[], T>
	{
		internal XArray() { }

		protected override void AddItem(T[] collection, T item) { }

		protected override void OnBuild(XType<T[]> xType, IXReadOperation reader, XElement element, ObjectBuilder<T[]> objectBuilder)
		{
			if (!element.HasElements)
			{
				return;
			}

			IEnumerable<XElement> itemElements = ItemsAsElements ? element.Elements() : element.Elements(ItemName);

			T[] array = new T[itemElements.Count()];

			if (array.Length > 0)
			{
				int i = 0;
				foreach (XElement subElement in itemElements)
				{
					int idx = i++;
					reader.Read<T>(subElement, x =>
						{
							array[i] = x;
							return true;
						},
						ReaderHints.IgnoreElementName);
				}
			}
		}
	}
}
