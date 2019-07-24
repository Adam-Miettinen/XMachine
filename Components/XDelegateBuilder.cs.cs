using System;
using System.Xml.Linq;

namespace XMachine.Components
{
	internal sealed class XDelegateBuilder<T> : XBuilder<T>
	{
		private readonly Action<XType<T>, IXReadOperation, XElement, ObjectBuilder<T>> readMethod;
		private readonly Func<XType<T>, IXWriteOperation, T, XElement, bool> writeMethod;

		internal XDelegateBuilder(
			Action<XType<T>, IXReadOperation, XElement, ObjectBuilder<T>> reader,
			Func<XType<T>, IXWriteOperation, T, XElement, bool> writer)
		{
			readMethod = reader;
			writeMethod = writer;
		}

		protected override void OnBuild(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder) =>
			readMethod(xType, reader, element, objectBuilder);

		protected override bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element) =>
			writeMethod(xType, writer, obj, element);
	}
}
