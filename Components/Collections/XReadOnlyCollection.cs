using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using XMachine.Components.Constructors;

namespace XMachine.Components.Collections
{
	internal sealed class XReadOnlyCollection<T> : XCollection<ReadOnlyCollection<T>, T>
	{
		internal XReadOnlyCollection(XType<ReadOnlyCollection<T>> xType) : base(xType) { }

		protected override void OnInitialized()
		{
			base.OnInitialized();
			XConstructor<ReadOnlyCollection<T>> ctor = XType.Component<XConstructor<ReadOnlyCollection<T>>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		protected override void AddItem(ReadOnlyCollection<T> collection, int index, T item) { }

		protected override void OnBuild(IXReadOperation reader, XElement element,
			ObjectBuilder<ReadOnlyCollection<T>> objectBuilder, XObjectArgs args) =>
			reader.Read<List<T>>(element, x =>
				{
					objectBuilder.Object = new ReadOnlyCollection<T>(x);
					return true;
				},
				args ?? XObjectArgs.DefaultIgnoreElementName);
	}
}
