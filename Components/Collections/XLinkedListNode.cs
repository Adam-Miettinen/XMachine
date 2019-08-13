using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XLinkedListNode<T> : XTypeComponent<LinkedListNode<T>>
	{
		internal XLinkedListNode(XType<LinkedListNode<T>> xType) : base(xType) { }

		protected override void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<LinkedListNode<T>> objectBuilder,
			XObjectArgs args) =>
			reader.Read<T>(element, x =>
			{
				objectBuilder.Object = new LinkedListNode<T>(x);
				return true;
			},
			args ?? XObjectArgs.DefaultIgnoreElementName);

		protected override bool OnWrite(IXWriteOperation writer, LinkedListNode<T> obj, XElement element, XObjectArgs args)
		{
			_ = writer.WriteTo(element, obj.Value);
			return true;
		}
	}
}
