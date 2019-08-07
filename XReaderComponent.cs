using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A component to <see cref="IXReadOperation"/> has several entry points where it can execute behaviour during a
	/// read operation.
	/// </summary>
	public abstract class XReaderComponent : IXComponent
	{
		/// <summary>
		/// Whether the component is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Called at the start of any read operation on an <see cref="XElement"/>. Return true if all further processing
		/// of this <see cref="XElement"/> should stop.
		/// </summary>
		protected virtual bool OnRead<T>(IXReadOperation reader, XType<T> xType, XElement element,
			Func<object, bool> assign) => false;

		/// <summary>
		/// Called at the start of any read operation on an <see cref="XAttribute"/>. Return true if all further processing
		/// of this <see cref="XAttribute"/> should stop.
		/// </summary>
		protected virtual bool OnRead<T>(IXReadOperation reader, XType<T> xType, XAttribute attribute,
			Func<object, bool> assign) => false;

		/// <summary>
		/// Called whenever an object is submitted to <see cref="IXReadOperation"/>, which occurs either because the user
		/// has submitted a contextual object or because an object was successfully read from XML.
		/// </summary>
		protected virtual void OnSubmit(IXReadOperation reader, object obj) { }

		internal bool Read<T>(IXReadOperation reader, XType<T> xType, XElement element, Func<object, bool> assign) =>
			OnRead(reader, xType, element, assign);

		internal bool Read<T>(IXReadOperation reader, XType<T> xType, XAttribute attribute, Func<object, bool> assign) =>
			OnRead(reader, xType, attribute, assign);

		internal void Submit(IXReadOperation reader, object obj) =>
			OnSubmit(reader, obj);
	}
}
