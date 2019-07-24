using System;
using System.Xml.Linq;

namespace XMachine.Components.Tuples
{
	internal sealed class XTuple<T1> : XTypeComponent<Tuple<T1>>
	{
		internal XTuple() { }

		protected override void OnBuild(XType<Tuple<T1>> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<Tuple<T1>> objectBuilder)
		{
			// T1

			XElement element1 = element.Element(XTuples.Item1);
			if (element1 != null)
			{
				reader.Read<T1>(element1, x =>
					{
						objectBuilder.Object = new Tuple<T1>(x);
						return true;
					},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				objectBuilder.Object = new Tuple<T1>(default);
			}
		}

		protected override bool OnWrite(XType<Tuple<T1>> xType, IXWriteOperation writer, Tuple<T1> obj, XElement element)
		{
			element.Add(writer.WriteTo(new XElement(XTuples.Item1), obj.Item1));
			return true;
		}
	}
}
