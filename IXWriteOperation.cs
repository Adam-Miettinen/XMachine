using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// Represents a serialization routine in which objects are converted to XML. Exposes backend methods used
	/// by <see cref="XWriterComponent"/>s and <see cref="XTypeComponent{T}"/>s during writing.
	/// </summary>
	public interface IXWriteOperation : IExceptionHandler
	{
		/// <summary>
		/// Write the given object as an <see cref="XElement"/>. The type parameter <typeparamref name="T"/> must
		/// be provided when deserializing.
		/// </summary>
		/// <param name="obj">The object to be written.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>The produced <see cref="XElement"/>, not yet added to any parent.</returns>
		XElement WriteElement<T>(T obj, XObjectArgs args = null);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/>.
		/// </summary>
		/// <param name="obj">The object to be written.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>The produced <see cref="XElement"/>, not yet added to any parent.</returns>
		XElement WriteElement(object obj, XObjectArgs args = null);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/>.
		/// </summary>
		/// <param name="obj">The object to be written.</param>
		/// <param name="expectedType">A <see cref="Type"/> object to which <paramref name="obj"/> is assignable
		/// and will be passed to <see cref="IXReadOperation.Read(XElement, Type, Func{object, bool}, XObjectArgs)"/>
		/// when this element is deserialized.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>The produced <see cref="XElement"/>, not yet added to any parent.</returns>
		XElement WriteElement(object obj, Type expectedType, XObjectArgs args = null);

		/// <summary>
		/// Write the given object to the contents of the given <see cref="XElement"/>. The type parameter
		/// <typeparamref name="T"/> must be provided when deserializing.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to write to.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>Returns the modified <paramref name="element"/>.</returns>
		XElement WriteTo<T>(XElement element, T obj, XObjectArgs args = null);

		/// <summary>
		/// Write the given object to the contents of the given <see cref="XElement"/>.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to write to.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>Returns the modified <paramref name="element"/>.</returns>
		XElement WriteTo(XElement element, object obj, XObjectArgs args = null);

		/// <summary>
		/// Write the given object to the contents of the given <see cref="XElement"/>.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to write to.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="expectedType">A <see cref="Type"/> object to which <paramref name="obj"/> is assignable
		/// and will be passed to <see cref="IXReadOperation.Read(XElement, Type, Func{object, bool}, XObjectArgs)"/>
		/// when this element is deserialized.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>Returns the modified <paramref name="element"/>.</returns>
		XElement WriteTo(XElement element, object obj, Type expectedType, XObjectArgs args = null);

		/// <summary>
		/// Write the given object as an <see cref="XAttribute"/>.
		/// </summary>
		/// <param name="obj">The object to be written.</param>
		/// <param name="xName">The <see cref="XName"/> to use for the new <see cref="XAttribute"/>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>The produced <see cref="XAttribute"/>, not yet added to an <see cref="XElement"/>.</returns>
		XAttribute WriteAttribute(object obj, XName xName, XObjectArgs args = null);

		/// <summary>
		/// Write the given object to the contents of the given <see cref="XAttribute"/>.
		/// </summary>
		/// <param name="attribute">The <see cref="XAttribute"/> to write to.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="obj"/> should be
		/// written.</param>
		/// <returns>Returns the modified <paramref name="attribute"/>.</returns>
		XAttribute WriteTo(XAttribute attribute, object obj, XObjectArgs args = null);
	}
}
