using System.Xml.Linq;

namespace XMachine.Components
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class XBuilder<T>
	{
		/// <summary>
		/// Create a new <see cref="XBuilder{T}"/>.
		/// </summary>
		public XBuilder() { }

		/// <summary>
		/// 
		/// </summary>
		protected abstract void OnBuild(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder);

		/// <summary>
		/// 
		/// </summary>
		protected abstract bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element);

		internal void Build(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder) =>
			OnBuild(xType, reader, element, objectBuilder);

		internal bool Write(XType<T> xType, IXWriteOperation writer, T obj, XElement element) =>
			OnWrite(xType, writer, obj, element);
	}
}
