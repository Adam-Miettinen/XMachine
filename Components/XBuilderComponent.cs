using System.Xml.Linq;

namespace XMachine.Components
{
	internal sealed class XBuilderComponent<T> : XTypeComponent<T>
	{
		internal XBuilderComponent(XBuilder<T> builder) => Builder = builder;

		internal XBuilder<T> Builder { get; set; }

		protected override void OnBuild(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder) =>
			Builder.Build(xType, reader, element, objectBuilder);

		protected override bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element) =>
			Builder.Write(xType, writer, obj, element);
	}
}
