using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// <see cref="XWriter"/> presents an API for XML-writing operations, either one object at a time via
	/// <see cref="Write(object)"/> or in collections via <see cref="WriteAll(IEnumerable)"/>.
	/// </summary>
	public abstract class XWriter : XWithComponents<XWriterComponent>
	{
		/// <summary>
		/// The <see cref="XDomain"/> to which this <see cref="XWriter"/> belongs.
		/// </summary>
		protected readonly XDomain Domain;

		/// <summary>
		/// Create a new <see cref="XWriter"/> belonging to the given <see cref="XDomain"/>.
		/// </summary>
		/// <param name="domain">An <see cref="XDomain"/> object that will provide the <see cref="XWriter"/> with
		/// <see cref="XType{T}"/>s.</param>
		protected XWriter(XDomain domain) =>
			Domain = domain ?? throw new ArgumentNullException(nameof(domain));

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public override Action<Exception> ExceptionHandler => Domain.ExceptionHandler;

		/// <summary>
		/// Get or set the number of milliseconds that <see cref="XWriter"/> is given to finish a call to <see cref="Write"/> 
		/// or <see cref="WriteAll(IEnumerable)"/> before it aborts and throws a <see cref="TimeoutException"/>. The default 
		/// is 10 seconds; assign zero or a negative value to disable timeouts.
		/// </summary>
		public virtual int WriteTimeout { get; set; } = 10000;

		/// <summary>
		/// Submit a contextual object to the <see cref="XWriter"/>. A contextual object will be available to
		/// <see cref="XWriterComponent"/>s, but it will not itself be written to XML.
		/// </summary>
		/// <param name="obj">The object to submit; <c>null</c> values are ignored.</param>
		public abstract void Submit(object obj);

		/// <summary>
		/// Submit contextual objects to the <see cref="XWriter"/>. A contextual object will be available to 
		/// <see cref="XWriterComponent"/>s, but it will not itself be written to XML.
		/// </summary>
		/// <param name="objects">An <see cref="IEnumerable"/> of objects to be submitted.</param>
		public abstract void SubmitAll(IEnumerable objects);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/>.
		/// </summary>
		/// <param name="obj">The object to be written.</param>
		/// <returns>An <see cref="XElement"/> containing the serialized <paramref name="obj"/>.</returns>
		public abstract XElement Write(object obj);

		/// <summary>
		/// Write the given collection of objects as a collection of <see cref="XElement"/>s.
		/// </summary>
		/// <param name="objects">The objects to be written.</param>
		/// <returns>An <see cref="IEnumerable{XElement}"/> containing the serialized <paramref name="objects"/>.</returns>
		public abstract IEnumerable<XElement> WriteAll(IEnumerable objects);
	}
}
