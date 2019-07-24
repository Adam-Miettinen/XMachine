using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A component for <see cref="IXWriteOperation"/> class, which performs a single XML writing operation.
	/// </summary>
	public abstract class XWriterComponent : IXComponent
	{
		/// <summary>
		/// Whether the component is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Called when an <see cref="XElement"/> has been created in the context of an <see cref="object"/>, but
		/// before any XML has been added.
		/// </summary>
		protected virtual bool OnWrite(IXWriteOperation writer, object obj, XElement element) => false;

		/// <summary>
		/// Called when an <see cref="XAttribute"/> has been created in the context of an <see cref="object"/>, but
		/// before any XML has been added.
		/// </summary>
		protected virtual bool OnWrite(IXWriteOperation writer, object obj, XAttribute attribute) => false;

		/// <summary>
		/// Called whenever an object is submitted to <see cref="IXWriteOperation"/>, which occurs either because the user
		/// has submitted a contextual object or because an object was successfully written to XML.
		/// </summary>
		protected virtual void OnSubmit(IXWriteOperation writer, object obj) { }

		internal bool Write(IXWriteOperation writer, object obj, XElement element) =>
			OnWrite(writer, obj, element);

		internal bool Write(IXWriteOperation writer, object obj, XAttribute attribute) =>
			OnWrite(writer, obj, attribute);

		internal void Submit(IXWriteOperation writer, object obj) =>
			OnSubmit(writer, obj);
	}
}
