using System;
using System.Xml.Linq;

namespace XMachine.Components
{
	internal sealed class XDelegateBuilder<T> : XBuilder<T>
	{
		private readonly Action<XType<T>, IXReadOperation, XElement, ObjectBuilder<T>, XObjectArgs> readMethod;
		private readonly Func<XType<T>, IXWriteOperation, T, XElement, XObjectArgs, bool> writeMethod;

		internal XDelegateBuilder(
			Action<XType<T>, IXReadOperation, XElement, ObjectBuilder<T>, XObjectArgs> reader,
			Func<XType<T>, IXWriteOperation, T, XElement, XObjectArgs, bool> writer)
		{
			readMethod = reader;
			writeMethod = writer;
		}

		protected override void OnBuild(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder,
			XObjectArgs args) =>
			readMethod(xType, reader, element, objectBuilder, args);

		protected override bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element, XObjectArgs args) =>
			writeMethod(xType, writer, obj, element, args);
	}
}
