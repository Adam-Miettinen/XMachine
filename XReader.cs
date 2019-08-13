using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// <see cref="XWriter"/> presents an API for XML-reading operations, either one object at a time via
	/// <see cref="Read(XElement)"/> or in collections via <see cref="ReadAll(IEnumerable{XElement})"/>.
	/// </summary>
	public abstract class XReader : XWithComponents<XReaderComponent>
	{
		/// <summary>
		/// The <see cref="XDomain"/> to which this <see cref="XReader"/> belongs.
		/// </summary>
		protected readonly XDomain Domain;

		/// <summary>
		/// Create a new <see cref="XReader"/> belonging to the given <see cref="XDomain"/>.
		/// </summary>
		/// <param name="domain">An <see cref="XDomain"/> object that will provide the <see cref="XReader"/> with
		/// <see cref="XType{T}"/>s.</param>
		protected XReader(XDomain domain) =>
			Domain = domain ?? throw new ArgumentNullException(nameof(domain));

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public override Action<Exception> ExceptionHandler => Domain.ExceptionHandler;

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object.
		/// </summary>
		/// <param name="element">An <see cref="XElement"/> containing a serialized object.</param>
		/// <returns>The deserialized object.</returns>
		public abstract object Read(XElement element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		/// <param name="element">An <see cref="XElement"/> containing a serialized object.</param>
		/// <returns>The deserialized object.</returns>
		public abstract T Read<T>(XElement element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects.
		/// </summary>
		/// <param name="elements">An <see cref="IEnumerable{XElement}"/> that contain serialized objects.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> containing the deserialized objects.</returns>
		public abstract IEnumerable<object> ReadAll(IEnumerable<XElement> elements);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		/// <param name="elements">An <see cref="IEnumerable{XElement}"/> that contain serialized objects.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> containing the deserialized objects.</returns>
		public abstract IEnumerable<T> ReadAll<T>(IEnumerable<XElement> elements);

		/// <summary>
		/// Submit a contextual object to the <see cref="XReader"/>. A contextual object will be available to
		/// <see cref="XReaderComponent"/>s.
		/// </summary>
		/// <param name="obj">The object to submit; <c>null</c> values are ignored.</param>
		public abstract void Submit(object obj);

		/// <summary>
		/// Submit contextual objects to the <see cref="XReader"/>. A contextual object will be available to 
		/// <see cref="XReaderComponent"/>s.
		/// </summary>
		/// <param name="objects">An <see cref="IEnumerable"/> of objects to be submitted.</param>
		public abstract void SubmitAll(IEnumerable objects);
	}
}
