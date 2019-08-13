using System.Xml.Linq;

namespace XMachine.Components
{
	/// <summary>
	/// The <see cref="XBuilder{T}"/> class instructs an <see cref="XReader"/> or <see cref="XWriter"/> how to deserialize or
	/// serialize an object of type <typeparamref name="T"/>. The <see cref="OnBuild(XType{T}, IXReadOperation, XElement, ObjectBuilder{T}, XObjectArgs)"/>
	/// and <see cref="OnWrite(XType{T}, IXWriteOperation, T, XElement, XObjectArgs)"/> methods give you full access to the underlying
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
		/// <paramref name="element"/> where it was serialized.
		/// </summary>
		/// <param name="xType">The <see cref="XType{T}"/> instance from the current <see cref="XDomain"/>.</param>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance for deserializing XML and scheduling tasks.</param>
		/// <param name="element">The <see cref="XElement"/> to be read as an instance of <typeparamref name="T"/>.</param>
		/// <param name="objectBuilder">An <see cref="ObjectBuilder{T}"/> to contain the constructed <typeparamref name="T"/>.</param>
		/// <param name="args">Optional arguments with information about XML formatting.</param>
		protected abstract void OnBuild(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder, XObjectArgs args);

		/// <summary>
		/// Implement this method to write an object of type <typeparamref name="T"/> to the given <see cref="XElement"/>.
		/// </summary>
		/// <param name="xType">The <see cref="XType{T}"/> instance from the current <see cref="XDomain"/>.</param>
		/// <param name="writer">An <see cref="IXWriteOperation"/> instance for serializing XML.</param>
		/// <param name="obj">The <typeparamref name="T"/> to be serialized.</param>
		/// <param name="element">The <see cref="XElement"/> to which <paramref name="obj"/> is being written.</param>
		/// <param name="args">Optional arguments with information about XML formatting.</param>
		protected abstract bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element, XObjectArgs args);

		internal void Build(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder, XObjectArgs args) =>
			OnBuild(xType, reader, element, objectBuilder, args);

		internal bool Write(XType<T> xType, IXWriteOperation writer, T obj, XElement element, XObjectArgs args) =>
			OnWrite(xType, writer, obj, element, args);
	}
}
