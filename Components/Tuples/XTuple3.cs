using System;
using System.Xml.Linq;

namespace XMachine.Components.Tuples
{
	internal sealed class XTuple<T1, T2, T3> : XTypeComponent<Tuple<T1, T2, T3>>
	{
		internal XTuple() { }

		protected override void OnBuild(XType<Tuple<T1, T2, T3>> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<Tuple<T1, T2, T3>> objectBuilder)
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

			// T2

			bool found3 = false;
			T3 item3 = default;

			XElement element3 = element.Element(XTuples.Item3);
			if (element3 != null)
			{
				reader.Read<T3>(element1, x =>
					{
						item3 = x;
						return found3 = true;
					},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found3 = true;
			}

			reader.AddTask(this, () =>
			{
				if (found1 && found2 && found3)
				{
					objectBuilder.Object = new Tuple<T1, T2, T3>(item1, item2, item3);
					return true;
				}
				return false;
			});
		}

		protected override bool OnWrite(XType<Tuple<T1, T2, T3>> xType, IXWriteOperation writer, Tuple<T1, T2, T3> obj, XElement element)
		{
			element.Add(writer.WriteTo(new XElement(XTuples.Item1), obj.Item1));
			element.Add(writer.WriteTo(new XElement(XTuples.Item2), obj.Item2));
			element.Add(writer.WriteTo(new XElement(XTuples.Item3), obj.Item3));
			return true;
		}
	}
}
