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
		protected virtual bool OnRead(IXReadOperation reader, XElement element, Type expectedType, Func<object, bool> assign,
			out Func<bool> task)
		{
			task = null;
			return false;
		}

		/// <summary>
		/// Called at the start of any read operation on an <see cref="XAttribute"/>. Return true if all further processing
		/// of this <see cref="XAttribute"/> should stop.
		/// </summary>
		protected virtual bool OnRead(IXReadOperation reader, XAttribute attribute, Type expectedType, Func<object, bool> assign,
			out Func<bool> task)
		{
			task = null;
			return false;
		}

		/// <summary>
		/// Called whenever an object is submitted to <see cref="IXReadOperation"/>, which occurs either because the user
		/// has submitted a contextual object or because an object was successfully read from XML.
		/// </summary>
		protected virtual void OnSubmit(IXReadOperation reader, object obj, out Func<bool> task) => task = null;

		internal bool Read(IXReadOperation reader, XElement element, Type expectedType, Func<object, bool> assign,
			out Func<bool> task) =>
			OnRead(reader, element, expectedType, assign, out task);

		internal bool Read(IXReadOperation reader, XAttribute attribute, Type expectedType, Func<object, bool> assign,
			out Func<bool> task) =>
			OnRead(reader, attribute, expectedType, assign, out task);

		internal void Submit(IXReadOperation reader, object obj, out Func<bool> task) =>
			OnSubmit(reader, obj, out task);
	}
}
