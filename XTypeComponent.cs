using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A component for <see cref="XType{T}"/> can enable custom reading and writing behaviour that applies
	/// only to objects of specific <see cref="Type"/>s.
	/// </summary>
	public abstract class XTypeComponent<T> : IXComponent
	{
		/// <summary>
		/// Whether the component is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Called after the <see cref="XType{T}"/> has been created and all <see cref="XMachineComponent"/>s
		/// have been informed.
		/// </summary>
		protected virtual void OnInitialized(XType<T> xType) { }

		/// <summary>
		/// Called when the <see cref="IXReadOperation"/> arrives at an <see cref="XElement"/> and begins reading. This
		/// method should return true only if the <see cref="XElement"/> was successfully deserialized and the read
		/// operation should end. Provide the result of the operation in the out parameter: the result can either be
		/// an <see cref="ObjectBuilder{T}"/> or an object of type <typeparamref name="T"/>.
		/// </summary>
		protected virtual bool OnRead(XType<T> xType, IXReadOperation reader, XElement element,
			Type expectedType, out T result)
		{
			result = default;
			return false;
		}

		/// <summary>
		/// Called when the <see cref="IXReadOperation"/> arrives at an <see cref="XAttribute"/> and begins reading. This
		/// method should return true only if the <see cref="XAttribute"/> was successfully deserialized and the read
		/// operation should end. Provide the result of the operation in the out parameter: the result can either be
		/// an <see cref="ObjectBuilder{T}"/> or an object of type <typeparamref name="T"/>.
		/// </summary>
		protected virtual bool OnRead(XType<T> xType, IXReadOperation reader, XAttribute attribute,
			Type expectedType, out T result)
		{
			result = default;
			return false;
		}

		/// <summary>
		/// Called after <see cref="OnRead(XType{T}, IXReadOperation, XElement, Type, out T)"/> returned false for
		/// all <see cref="XTypeComponent{T}"/>s on this <see cref="XType{T}"/>.
		/// </summary>
		protected virtual void OnBuild(XType<T> xType, IXReadOperation reader, XElement element,
			ObjectBuilder<T> objectBuilder)
		{ }

		/// <summary>
		/// Called by <see cref="IXWriteOperation"/> when it needs to write an object belonging to this <see cref="XType{T}"/>.
		/// This method should return true only if the <see cref="XElement"/> was successfully serialized and the write
		/// operation should end.
		/// </summary>
		protected virtual bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element) => false;

		/// <summary>
		/// Called by <see cref="IXWriteOperation"/> when it needs to write an object belonging to this <see cref="XType{T}"/>.
		/// This method should return true only if the <see cref="XAttribute"/> was successfully serialized and the write
		/// operation should end.
		/// </summary>
		protected virtual bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XAttribute attribute) => false;

		internal void Initialize(XType<T> xType) => OnInitialized(xType);

		internal bool Read(XType<T> xType, IXReadOperation reader, XElement element, Type expectedType, out T result) =>
			OnRead(xType, reader, element, expectedType, out result);

		internal bool Read(XType<T> xType, IXReadOperation reader, XAttribute attribute, Type expectedType, out T result) =>
			OnRead(xType, reader, attribute, expectedType, out result);

		internal void Build(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder) =>
			OnBuild(xType, reader, element, objectBuilder);

		internal bool Write(XType<T> xType, IXWriteOperation writer, T obj, XElement element) =>
			OnWrite(xType, writer, obj, element);

		internal bool Write(XType<T> xType, IXWriteOperation writer, T obj, XAttribute attribute) =>
			OnWrite(xType, writer, obj, attribute);
	}
}
