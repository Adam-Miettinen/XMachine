using System;
using System.Xml.Linq;

namespace XMachine.Components.Tuples
{
	internal sealed class XTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : XTypeComponent<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
	{
		internal XTuple() { }

		protected override void OnBuild(XType<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>> objectBuilder)
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

			// T3

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

			// T4

			bool found4 = false;
			T4 item4 = default;

			XElement element4 = element.Element(XTuples.Item4);
			if (element4 != null)
			{
				reader.Read<T4>(element1, x =>
					{
						item4 = x;
						return found4 = true;
					},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found4 = true;
			}

			// T5

			bool found5 = false;
			T5 item5 = default;

			XElement element5 = element.Element(XTuples.Item5);
			if (element5 != null)
			{
				reader.Read<T5>(element1, x =>
					{
						item5 = x;
						return found5 = true;
					},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found5 = true;
			}

			// T6

			bool found6 = false;
			T6 item6 = default;

			XElement element6 = element.Element(XTuples.Item6);
			if (element6 != null)
			{
				reader.Read<T6>(element1, x =>
				{
					item6 = x;
					return found6 = true;
				},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found6 = true;
			}

			// T7

			bool found7 = false;
			T7 item7 = default;

			XElement element7 = element.Element(XTuples.Item7);
			if (element7 != null)
			{
				reader.Read<T7>(element1, x =>
				{
					item7 = x;
					return found7 = true;
				},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found7 = true;
			}

			// T8

			bool found8 = false;
			TRest item8 = default;

			XElement element8 = element.Element(XTuples.Rest);
			if (element8 != null)
			{
				reader.Read<TRest>(element1, x =>
				{
					item8 = x;
					return found8 = true;
				},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				found8 = true;
			}

			objectBuilder.AddTask(() =>
			{
				if (found1 && found2 && found3 && found4 && found5 && found6 && found7 && found8)
				{
					objectBuilder.Object = new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, item8);
					return true;
				}
				return false;
			});
		}

		protected override bool OnWrite(XType<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>> xType, IXWriteOperation writer,
			Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> obj, XElement element)
		{
			element.Add(writer.WriteTo(new XElement(XTuples.Item1), obj.Item1));
			element.Add(writer.WriteTo(new XElement(XTuples.Item2), obj.Item2));
			element.Add(writer.WriteTo(new XElement(XTuples.Item3), obj.Item3));
			element.Add(writer.WriteTo(new XElement(XTuples.Item4), obj.Item4));
			element.Add(writer.WriteTo(new XElement(XTuples.Item5), obj.Item5));
			element.Add(writer.WriteTo(new XElement(XTuples.Item6), obj.Item6));
			element.Add(writer.WriteTo(new XElement(XTuples.Item7), obj.Item7));
			element.Add(writer.WriteTo(new XElement(XTuples.Rest), obj.Rest));
			return true;
		}
	}
}
