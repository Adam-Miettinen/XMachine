using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A component that extends the serialization and deserialization functionality of objects belonging to the type
	/// <typeparamref name="T"/> when applied to the <see cref="XType{T}"/> within the active <see cref="XDomain"/>.
	/// </summary>
	public abstract class XTypeComponent<T> : IXComponent
	{
		/// <summary>
		/// The <see cref="XType{T}"/> object to which this <see cref="XTypeComponent{T}"/> belongs.
		/// </summary>
		protected readonly XType<T> XType;

		/// <summary>
		/// Create a new <see cref="XTypeComponent{T}"/> belonging to the given <see cref="XType{T}"/>.
		/// </summary>
		/// <param name="xType">The <see cref="XType{T}"/> object to which this <see cref="XTypeComponent{T}"/> belongs.</param>
		protected XTypeComponent(XType<T> xType) => XType = xType;

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Called when the <see cref="XType{T}"/> has been created, after <see cref="XMachineComponent.OnCreateXType{T}(XType{T})"/>
		/// but before <see cref="XMachineComponent.OnCreateXTypeLate{T}(XType{T})"/>.
		/// </summary>
		protected virtual void OnInitialized() { }

		/// <summary>
		/// Called when an <see cref="IXReadOperation"/> begins reading an <see cref="XElement"/> as an object of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance that exposes methods for reading XML and
		/// scheduling tasks using the active <see cref="XReader"/>.</param>
		/// <param name="element">The <see cref="XElement"/> to be read.</param>
		/// <param name="result">The deserialized object if this <see cref="XTypeComponent{T}"/> is able to produce it;
		/// a default value otherwise.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// read.</param>
		/// <returns><c>True</c> if this <see cref="XTypeComponent{T}"/> was able to deserialize the object, 
		/// <paramref name="result"/> was assigned, and all further processing of <paramref name="element"/> should cease.
		/// </returns>
		protected virtual bool OnRead(IXReadOperation reader, XElement element, out T result, XObjectArgs args)
		{
			result = default;
			return false;
		}

		/// <summary>
		/// Called when an <see cref="IXReadOperation"/> begins reading an <see cref="XAttribute"/> as an object of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance that exposes methods for reading XML and
		/// scheduling tasks using the active <see cref="XReader"/>.</param>
		/// <param name="attribute">The <see cref="XAttribute"/> to be read.</param>
		/// <param name="result">The deserialized object if this <see cref="XTypeComponent{T}"/> is able to produce it;
		/// a default value otherwise.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="attribute"/> should be
		/// read.</param>
		/// <returns><c>True</c> if this <see cref="XTypeComponent{T}"/> was able to deserialize the object, 
		/// <paramref name="result"/> was assigned, and all further processing of <paramref name="attribute"/> should cease.
		/// </returns>
		protected virtual bool OnRead(IXReadOperation reader, XAttribute attribute, out T result, XObjectArgs args)
		{
			result = default;
			return false;
		}

		/// <summary>
		/// Called after <see cref="OnRead(IXReadOperation, XElement, out T, XObjectArgs)"/> has been invoked on all
		/// <see cref="XTypeComponent{T}"/>s, reading has not been halted, and <see cref="XReader"/> has constructed 
		/// an <see cref="ObjectBuilder{T}"/> for deferred deserialization.
		/// </summary>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance that exposes methods for reading XML and
		/// scheduling tasks using the active <see cref="XReader"/>.</param>
		/// <param name="element">The <see cref="XElement"/> to be read.</param>
		/// <param name="objectBuilder">An instance of <see cref="ObjectBuilder{T}"/> containing, or eventually containing,
		/// the deserialized object.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// read.</param>
		protected virtual void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder, XObjectArgs args) { }

		/// <summary>
		/// Called when an <see cref="IXWriteOperation"/> begins writing an object of type <typeparamref name="T"/>
		/// as an <see cref="XElement"/>.
		/// </summary>
		/// <param name="writer">An <see cref="IXWriteOperation"/> instance that exposes methods for writing XML
		/// using the active <see cref="XWriter"/>.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="element">The <see cref="XElement"/> to which <paramref name="obj"/> is being written. The
		/// element will already have been assigned the correct <see cref="XName"/>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// written.</param>
		/// <returns><c>True</c> if this <see cref="XTypeComponent{T}"/> was able to serialize the object and all further 
		/// processing of <paramref name="element"/> should cease.</returns>
		protected virtual bool OnWrite(IXWriteOperation writer, T obj, XElement element, XObjectArgs args) => false;

		/// <summary>
		/// Called when an <see cref="IXWriteOperation"/> begins writing an object of type <typeparamref name="T"/>
		/// as an <see cref="XAttribute"/>.
		/// </summary>
		/// <param name="writer">An <see cref="IXWriteOperation"/> instance that exposes methods for writing XML
		/// using the active <see cref="XWriter"/>.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="attribute">The <see cref="XAttribute"/> to which <paramref name="obj"/> is being written. The
		/// attribute will already have been assigned the correct <see cref="XName"/>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="attribute"/> should be
		/// written.</param>
		/// <returns><c>True</c> if this <see cref="XTypeComponent{T}"/> was able to serialize the object and all further 
		/// processing of <paramref name="attribute"/> should cease.</returns>
		protected virtual bool OnWrite(IXWriteOperation writer, T obj, XAttribute attribute, XObjectArgs args = null) => false;

		internal void Initialize() => OnInitialized();

		internal bool Read(IXReadOperation reader, XElement element, out T result, XObjectArgs args) =>
			OnRead(reader, element, out result, args);

		internal bool Read(IXReadOperation reader, XAttribute attribute, out T result, XObjectArgs args) =>
			OnRead(reader, attribute, out result, args);

		internal void Build(IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder, XObjectArgs args) =>
			OnBuild(reader, element, objectBuilder, args);

		internal bool Write(IXWriteOperation writer, T obj, XElement element, XObjectArgs args) =>
			OnWrite(writer, obj, element, args);

		internal bool Write(IXWriteOperation writer, T obj, XAttribute attribute, XObjectArgs args) =>
			OnWrite(writer, obj, attribute, args);
	}
}
