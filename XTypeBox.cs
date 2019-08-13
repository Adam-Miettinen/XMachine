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

				onComponentReadElement = (comp, reader, element, assign, args) =>
					comp.Read(reader, xType, element, assign, args),

				onComponentReadAttribute = (comp, reader, attribute, assign, args) =>
					comp.Read(reader, xType, attribute, assign, args),

				onComponentWriteElement = (comp, writer, obj, element, args) =>
					comp.Write(writer, (T)obj, element, args),

				onComponentWriteAttribute = (comp, writer, obj, attribute, args) =>
					comp.Write(writer, (T)obj, attribute, args),

				onReadElement = (reader, element, args) =>
					xType.Read(reader, element, out T result, args)
						? new Tuple<bool, object>(true, result)
						: new Tuple<bool, object>(false, null),

				onReadAttribute = (reader, attribute, args) =>
					xType.Read(reader, attribute, out T result, args)
						? new Tuple<bool, object>(true, result)
						: new Tuple<bool, object>(false, null),

				onBuildObject = (reader, element, objectBuilder, args) =>
					xType.Build(reader, element, objectBuilder as ObjectBuilder<T>, args),

				onWriteElement = (writer, obj, element, args) =>
					xType.Write(writer, (T)obj, element, args),

				onWriteAttribute = (writer, obj, attribute, args) =>
					xType.Write(writer, (T)obj, attribute, args)
			};

		internal static XType<T> Unbox<T>(XTypeBox xTypeBox) => xTypeBox?.xType as XType<T>;

		private readonly object xType;

		private Func<XReaderComponent, IXReadOperation, XElement,
			Func<object, bool>, XObjectArgs, bool> onComponentReadElement;
		private Func<XReaderComponent, IXReadOperation, XAttribute,
			Func<object, bool>, XObjectArgs, bool> onComponentReadAttribute;

		private Func<XWriterComponent, IXWriteOperation, object, XElement,
			XObjectArgs, bool> onComponentWriteElement;
		private Func<XWriterComponent, IXWriteOperation, object, XAttribute,
			XObjectArgs, bool> onComponentWriteAttribute;

		private Func<IXReadOperation, XElement, XObjectArgs, Tuple<bool, object>> onReadElement;
		private Func<IXReadOperation, XAttribute, XObjectArgs, Tuple<bool, object>> onReadAttribute;
		private Action<IXReadOperation, XElement, object, XObjectArgs> onBuildObject;

		private Func<IXWriteOperation, object, XElement, XObjectArgs, bool> onWriteElement;
		private Func<IXWriteOperation, object, XAttribute, XObjectArgs, bool> onWriteAttribute;

		private XTypeBox(object xType) => this.xType = xType;

		internal XName XName { get; private set; }

		internal Type Type { get; private set; }

		internal bool OnComponentRead(XReaderComponent comp, IXReadOperation reader, XElement element,
			Func<object, bool> assign, XObjectArgs args) =>
			onComponentReadElement(comp, reader, element, assign, args);

		internal bool OnComponentRead(XReaderComponent comp, IXReadOperation reader, XAttribute attribute,
			Func<object, bool> assign, XObjectArgs args) =>
			onComponentReadAttribute(comp, reader, attribute, assign, args);

		internal bool OnComponentWrite(XWriterComponent comp, IXWriteOperation writer, object obj, XElement element,
			XObjectArgs args) =>
			onComponentWriteElement(comp, writer, obj, element, args);

		internal bool OnComponentWrite(XWriterComponent comp, IXWriteOperation writer, object obj, XAttribute attribute,
			XObjectArgs args) =>
			onComponentWriteAttribute(comp, writer, obj, attribute, args);

		internal bool OnRead(IXReadOperation reader, XElement element, out object result, XObjectArgs args)
		{
			Tuple<bool, object> x = onReadElement(reader, element, args);
			result = x.Item2;
			return x.Item1;
		}


		internal bool OnRead(IXReadOperation reader, XAttribute attribute, out object result, XObjectArgs args)
		{
			Tuple<bool, object> x = onReadAttribute(reader, attribute, args);
			result = x.Item2;
			return x.Item1;
		}

		internal void OnBuild(IXReadOperation reader, XElement element, object objectBuilder, XObjectArgs args) =>
			onBuildObject(reader, element, objectBuilder, args);

		internal bool OnWrite(IXWriteOperation writer, object obj, XElement element, XObjectArgs args) =>
			onWriteElement(writer, obj, element, args);

		internal bool OnWrite(IXWriteOperation writer, object obj, XAttribute attribute, XObjectArgs args) =>
			onWriteAttribute(writer, obj, attribute, args);
	}
}
