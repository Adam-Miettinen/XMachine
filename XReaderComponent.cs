using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A component for the <see cref="XReader"/> class. An <see cref="XReaderComponent"/> extends the 
	/// functionality of XML-reading operations.
	/// </summary>
	public abstract class XReaderComponent : IXComponent
	{
		/// <summary>
		/// Get or set whether the component is enabled and should be invoked by its owner.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Called when the <see cref="XReader"/> has arrived at an <see cref="XElement"/> expecting an object of type
		/// <typeparamref name="T"/>, but before <see cref="XTypeComponent{T}"/>s have attempted deserialization.
		/// </summary>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance that exposes methods for reading XML and
		/// scheduling tasks using the active <see cref="XReader"/>.</param>
		/// <param name="xType">An <see cref="XType{T}"/> reflected from the <see cref="XDomain"/> to which this 
		/// <see cref="XReader"/> belongs.</param>
		/// <param name="element">The <see cref="XElement"/> to be read.</param>
		/// <param name="assign">A delegate to perform on the deserialized <typeparamref name="T"/>. If it returns <c>false</c>,
		/// it will be added to the task queue and repeatedly attempted until it returns <c>true</c>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// read.</param>
		/// <returns><c>True</c> if the <see cref="XReaderComponent"/> fully deserialized the object and all further
		/// processing of <paramref name="element"/> should stop.</returns>
		protected virtual bool OnRead<T>(IXReadOperation reader, XType<T> xType, XElement element, Func<object, bool> assign,
			XObjectArgs args) => false;

		/// <summary>
		/// Called when the <see cref="XReader"/> has arrived at an <see cref="XAttribute"/> expecting an object of type
		/// <typeparamref name="T"/>, but before <see cref="XTypeComponent{T}"/>s have attempted deserialization.
		/// </summary>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance that exposes methods for reading XML and
		/// scheduling tasks using the active <see cref="XReader"/>.</param>
		/// <param name="xType">An <see cref="XType{T}"/> reflected from the <see cref="XDomain"/> to which this 
		/// <see cref="XReader"/> belongs.</param>
		/// <param name="attribute">The <see cref="XAttribute"/> to be read.</param>
		/// <param name="assign">A delegate to perform on the deserialized <typeparamref name="T"/>. If it returns <c>false</c>,
		/// it will be added to the task queue and repeatedly attempted until it returns <c>true</c>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="attribute"/> should be
		/// read.</param>
		/// <returns><c>True</c> if the <see cref="XReaderComponent"/> fully deserialized the object and all further
		/// processing of <paramref name="attribute"/> should stop.</returns>
		protected virtual bool OnRead<T>(IXReadOperation reader, XType<T> xType, XAttribute attribute, Func<object, bool> assign,
			XObjectArgs args) => false;

		/// <summary>
		/// Called whenever an object is submitted to the <see cref="XReader"/>, which occurs either because the user
		/// has submitted a contextual object or because a deserialized object was successfully constructed.
		/// </summary>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance that exposes methods for reading XML and
		/// scheduling tasks using the active <see cref="XReader"/>.</param>
		/// <param name="obj">The object that was submitted.</param>
		protected virtual void OnSubmit(IXReadOperation reader, object obj) { }

		internal bool Read<T>(IXReadOperation reader, XType<T> xType, XElement element, Func<object, bool> assign, XObjectArgs args) =>
			OnRead(reader, xType, element, assign, args);

		internal bool Read<T>(IXReadOperation reader, XType<T> xType, XAttribute attribute, Func<object, bool> assign, XObjectArgs args) =>
			OnRead(reader, xType, attribute, assign, args);

		internal void Submit(IXReadOperation reader, object obj) => OnSubmit(reader, obj);
	}
}
