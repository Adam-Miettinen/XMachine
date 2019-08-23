using System;
using System.Xml.Linq;

namespace XMachine.Components
{
	internal sealed class XBuilderComponent<T> : XTypeComponent<T>
	{
		private XBuilder<T> builder;

		public XBuilderComponent(XType<T> xType, XBuilder<T> builder) : base(xType) =>
			Builder = builder;

		internal XBuilder<T> Builder
		{
			get => builder;
			set => builder = value ?? throw new ArgumentNullException(nameof(value));
		}

		protected override void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<T>
			objectBuilder, XObjectArgs args) =>
			Builder.Build(XType, reader, element, objectBuilder, args);

		protected override bool OnWrite(IXWriteOperation writer, T obj, XElement element, XObjectArgs args) =>
			Builder.Write(XType, writer, obj, element, args);
	}
}
