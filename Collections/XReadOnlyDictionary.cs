using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using XMachine.Components.Constructors;

namespace XMachine.Components.Collections
{
	internal sealed class XReadOnlyDictionary<TKey, TValue> :
		XCollection<ReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
	{
		public XReadOnlyDictionary(XType<ReadOnlyDictionary<TKey, TValue>> xType) : base(xType) { }

		protected override void OnInitialized()
		{
			base.OnInitialized();
			XConstructor<ReadOnlyDictionary<TKey, TValue>> ctor = XType.Component<XConstructor<ReadOnlyDictionary<TKey, TValue>>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		protected override void AddItem(ReadOnlyDictionary<TKey, TValue> collection, int index, KeyValuePair<TKey, TValue> item) { }

		protected override void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<ReadOnlyDictionary<TKey, TValue>> objectBuilder,
			XObjectArgs args) =>
			reader.Read<Dictionary<TKey, TValue>>(element, x =>
				{
					objectBuilder.Object = new ReadOnlyDictionary<TKey, TValue>(x);
					return true;
				},
				args ?? XObjectArgs.DefaultIgnoreElementName);
	}
}
