using System;
using System.Xml.Linq;

namespace XMachine.Components.Tuples
{
	internal sealed class XTuple<T1, T2> : XTypeComponent<Tuple<T1, T2>>
	{
		internal XTuple() { }
		protected override void OnBuild(XType<Tuple<T1, T2>> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<Tuple<T1, T2>> objectBuilder)
		{
			// T1

			bool found1 = false;
			T1 item1 = default;

			XElement element1 = element.Element(XTuples.Item1);
			if (element1 != null)
			{
				reader.Read<T1>(element1, x =>
					{
						item1 = x;
						return found1 = true;
					},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found1 = true;
			}

			// T2

			bool found2 = false;
			T2 item2 = default;

			XElement element2 = element.Element(XTuples.Item2);
			if (element2 != null)
			{
				reader.Read<T2>(element1, x =>
					{
						item2 = x;
						return found2 = true;
					},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found2 = true;
			}

			reader.AddTask(this, () =>
			{
				if (found1 && found2)
				{
					objectBuilder.Object = new Tuple<T1, T2>(item1, item2);
					return true;
				}
				return false;
			});
		}

		protected override bool OnWrite(XType<Tuple<T1, T2>> xType, IXWriteOperation writer, Tuple<T1, T2> obj, XElement element)
		{
			element.Add(writer.WriteTo(new XElement(XTuples.Item1), obj.Item1));
			element.Add(writer.WriteTo(new XElement(XTuples.Item2), obj.Item2));
			return true;
		}
	}
}
