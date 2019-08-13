using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A component for the <see cref="XWriter"/> class. An <see cref="XWriterComponent"/> extends the 
	/// functionality of XML-writing operations.
	/// </summary>
	public abstract class XWriterComponent : IXComponent
	{
		/// <summary>
		/// Get or set whether the component is enabled and should be invoked by its owner.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Called when an <see cref="XElement"/> has been created with the intention of writing an <see cref="object"/>
		/// to it, but before any XML has been added by <see cref="XTypeComponent{T}"/>s.
		/// </summary>
		/// <param name="writer">An <see cref="IXWriteOperation"/> instance that exposes methods for writing XML
		/// using the active <see cref="XWriter"/>.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="element">The <see cref="XElement"/> to which <paramref name="obj"/> is being written. The
		/// element will already have been assigned the correct <see cref="XName"/>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>True if this <see cref="XWriterComponent"/> wrote the complete object to XML and all further
		/// processing of this element should end.</returns>
		protected virtual bool OnWrite<T>(IXWriteOperation writer, T obj, XElement element, XObjectArgs args) => false;

		/// <summary>
		/// Called when an <see cref="XAttribute"/> has been created with the intention of writing an <see cref="object"/>
		/// to it, but before any XML has been added by <see cref="XTypeComponent{T}"/>s.
		/// </summary>
		/// <param name="writer">An <see cref="IXWriteOperation"/> instance that exposes methods for writing XML
		/// using the active <see cref="XWriter"/>.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="attribute">The <see cref="XAttribute"/> to which <paramref name="obj"/> is being written. The
		/// attribute will already have been assigned the correct <see cref="XName"/>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>True if this <see cref="XWriterComponent"/> wrote the complete object to XML and all further
		/// processing of this element should end.</returns>
		protected virtual bool OnWrite<T>(IXWriteOperation writer, T obj, XAttribute attribute, XObjectArgs args) => false;

		/// <summary>
		/// Called whenever an object is submitted to the <see cref="XWriter"/>, which occurs either because the user
		/// has submitted a contextual object or because an object was successfully written to XML.
		/// </summary>
		/// <param name="writer">An <see cref="IXWriteOperation"/> instance that exposes methods for writing XML
		/// using the active <see cref="XWriter"/>.</param>
		/// <param name="obj">The object that was submitted.</param>
		protected virtual void OnSubmit(IXWriteOperation writer, object obj) { }

		internal bool Write<T>(IXWriteOperation writer, T obj, XElement element, XObjectArgs args) =>
			OnWrite(writer, obj, element, args);

		internal bool Write<T>(IXWriteOperation writer, T obj, XAttribute attribute, XObjectArgs args) =>
			OnWrite(writer, obj, attribute, args);

		internal void Submit(IXWriteOperation writer, object obj) => OnSubmit(writer, obj);
	}
}
