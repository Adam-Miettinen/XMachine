using System;
using System.Xml.Linq;

namespace XMachine
{
	internal sealed class XTypeBox
	{
		internal static XTypeBox Box<T>(XType<T> xType) =>
			new XTypeBox(xType)
			{
				XName = xType.Name,
				Type = typeof(T),

				onReadElement = (reader, element, type) =>
					xType.Read(reader, element, type, out T result)
						? new Tuple<bool, object>(true, result)
						: new Tuple<bool, object>(false, null),

				onReadAttribute = (reader, attribute, type) =>
					xType.Read(reader, attribute, type, out T result)
						? new Tuple<bool, object>(true, result)
						: new Tuple<bool, object>(false, null),

				onBuildObject = (reader, element, objectBuilder) =>
					xType.Build(reader, element, ObjectBuilderBox.Unbox<T>(objectBuilder)),

				onWriteElement = (writer, obj, element) =>
					xType.Write(writer, (T)obj, element),

				onWriteAttribute = (writer, obj, attribute) =>
					xType.Write(writer, (T)obj, attribute)
			};

		internal static XType<T> Unbox<T>(XTypeBox xTypeBox) => xTypeBox?.xType as XType<T>;

		private readonly object xType;

		private Func<IXReadOperation, XElement, Type, Tuple<bool, object>> onReadElement;
		private Func<IXReadOperation, XAttribute, Type, Tuple<bool, object>> onReadAttribute;
		private Action<IXReadOperation, XElement, ObjectBuilderBox> onBuildObject;

		private Func<IXWriteOperation, object, XElement, bool> onWriteElement;
		private Func<IXWriteOperation, object, XAttribute, bool> onWriteAttribute;

		private XTypeBox(object xType) => this.xType = xType;

		internal XName XName { get; private set; }

		internal Type Type { get; private set; }

		internal bool OnRead(IXReadOperation reader, XElement element, Type expectedType, out object result)
		{
			Tuple<bool, object> x = onReadElement(reader, element, expectedType);
			result = x.Item2;
			return x.Item1;
		}

		internal bool OnRead(IXReadOperation reader, XAttribute attribute, Type expectedType, out object result)
		{
			Tuple<bool, object> x = onReadAttribute(reader, attribute, expectedType);
			result = x.Item2;
			return x.Item1;
		}

		internal void OnBuild(IXReadOperation reader, XElement element, ObjectBuilderBox objectBuilder) =>
			onBuildObject(reader, element, objectBuilder);

		internal bool OnWrite(IXWriteOperation writer, object obj, XElement element) =>
			onWriteElement(writer, obj, element);

		internal bool OnWrite(IXWriteOperation writer, object obj, XAttribute attribute) =>
			onWriteAttribute(writer, obj, attribute);
	}
}
