using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XLinkedListNode<T> : XTypeComponent<LinkedListNode<T>>
	{
		internal XLinkedListNode() { }

		protected override void OnBuild(XType<LinkedListNode<T>> xType, IXReadOperation reader,
			XElement element, ObjectBuilder<LinkedListNode<T>> objectBuilder) =>
			reader.Read<T>(element, x =>
			{
				objectBuilder.Object = new LinkedListNode<T>(x);
				return true;
			},
			ReaderHints.IgnoreElementName);

		protected override bool OnWrite(XType<LinkedListNode<T>> xType, IXWriteOperation writer,
			LinkedListNode<T> obj, XElement element)
		{
			_ = writer.WriteTo(element, obj.Value);
			return true;
		}
	}
}
