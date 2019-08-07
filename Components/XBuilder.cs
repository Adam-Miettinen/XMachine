using System.Xml.Linq;

namespace XMachine.Components
{
	/// <summary>
	/// The <see cref="XBuilder{T}"/> class instructs an <see cref="XReader"/> or <see cref="XWriter"/> how to deserialize or
	/// serialize an object of type <typeparamref name="T"/>. The <see cref="OnBuild(XType{T}, IXReadOperation, XElement, ObjectBuilder{T})"/>
	/// and <see cref="OnWrite(XType{T}, IXWriteOperation, T, XElement)"/> methods give you full access to the underlying
	/// serializer methods.
	/// </summary>
	public abstract class XBuilder<T>
	{
		/// <summary>
		/// Create a new <see cref="XBuilder{T}"/> for a type <typeparamref name="T"/>.
		/// </summary>
		public XBuilder() { }

		/// <summary>
		/// Implement this method to build (read) an object of type <typeparamref name="T"/> from the <see cref="XElement"/>
		/// <paramref name="element"/> where it was serialized. The <see cref="IXReadOperation"/> and <see cref="ObjectBuilder{T}"/>
		/// arguments let you schedule tasks to construct the object or modify its state.
		/// </summary>
		protected abstract void OnBuild(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder);

		/// <summary>
		/// Implement this method to write an object of type <typeparamref name="T"/> to the given <see cref="XElement"/>. The 
		/// <see cref="IXWriteOperation"/> argument contains several methods that let you generate <see cref="XAttribute"/>s
		/// and <see cref="XElement"/>s to represent the object's state.
		/// </summary>
		protected abstract bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element);

		internal void Build(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder) =>
			OnBuild(xType, reader, element, objectBuilder);

		internal bool Write(XType<T> xType, IXWriteOperation writer, T obj, XElement element) =>
			OnWrite(xType, writer, obj, element);
	}
}
