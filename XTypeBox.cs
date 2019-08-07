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

				onComponentReadElement = (comp, reader, element, assign) =>
					comp.Read(reader, xType, element, assign),

				onComponentReadAttribute = (comp, reader, attribute, assign) =>
					comp.Read(reader, xType, attribute, assign),

				onReadElement = (reader, element) =>
					xType.Read(reader, element, out T result)
						? new Tuple<bool, object>(true, result)
						: new Tuple<bool, object>(false, null),

				onReadAttribute = (reader, attribute) =>
					xType.Read(reader, attribute, out T result)
						? new Tuple<bool, object>(true, result)
						: new Tuple<bool, object>(false, null),

				onBuildObject = (reader, element, objectBuilder) =>
					xType.Build(reader, element, objectBuilder as ObjectBuilder<T>),

				onWriteElement = (writer, obj, element) =>
					xType.Write(writer, (T)obj, element),

				onWriteAttribute = (writer, obj, attribute) =>
					xType.Write(writer, (T)obj, attribute)
			};

		internal static XType<T> Unbox<T>(XTypeBox xTypeBox) => xTypeBox?.xType as XType<T>;

		private readonly object xType;

		private Func<XReaderComponent, IXReadOperation, XElement,
			Func<object, bool>, bool> onComponentReadElement;
		private Func<XReaderComponent, IXReadOperation, XAttribute,
			Func<object, bool>, bool> onComponentReadAttribute;

		private Func<IXReadOperation, XElement, Tuple<bool, object>> onReadElement;
		private Func<IXReadOperation, XAttribute, Tuple<bool, object>> onReadAttribute;
		private Action<IXReadOperation, XElement, object> onBuildObject;

		private Func<IXWriteOperation, object, XElement, bool> onWriteElement;
		private Func<IXWriteOperation, object, XAttribute, bool> onWriteAttribute;

		private XTypeBox(object xType) => this.xType = xType;

		internal XName XName { get; private set; }

		internal Type Type { get; private set; }

		internal bool OnComponentRead(XReaderComponent comp, IXReadOperation reader, XElement element,
			Func<object, bool> assign) =>
			onComponentReadElement(comp, reader, element, assign);

		internal bool OnComponentRead(XReaderComponent comp, IXReadOperation reader, XAttribute attribute,
			Func<object, bool> assign) =>
			onComponentReadAttribute(comp, reader, attribute, assign);

		internal bool OnRead(IXReadOperation reader, XElement element, out object result)
		{
			Tuple<bool, object> x = onReadElement(reader, element);
			result = x.Item2;
			return x.Item1;
		}


		internal bool OnRead(IXReadOperation reader, XAttribute attribute, out object result)
		{
			Tuple<bool, object> x = onReadAttribute(reader, attribute);
			result = x.Item2;
			return x.Item1;
		}

		internal void OnBuild(IXReadOperation reader, XElement element, object objectBuilder) =>
			onBuildObject(reader, element, objectBuilder);

		internal bool OnWrite(IXWriteOperation writer, object obj, XElement element) =>
			onWriteElement(writer, obj, element);

		internal bool OnWrite(IXWriteOperation writer, object obj, XAttribute attribute) =>
			onWriteAttribute(writer, obj, attribute);
	}
}
