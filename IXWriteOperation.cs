using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// Represents an operation of serializing objects to XML triggered by a call to one of the methods in
	/// <see cref="XWriter"/>.
	/// </summary>
	public interface IXWriteOperation : IExceptionHandler
	{
		/// <summary>
		/// Write the given object as an <see cref="XElement"/> with a <see cref="Type"/> context of 
		/// <typeparamref name="T"/>.
		/// </summary>
		XElement WriteElement<T>(T obj);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/> with a <see cref="Type"/> context of 
		/// <see cref="object"/>.
		/// </summary>
		XElement WriteElement(object obj);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/> with a <see cref="Type"/> context of 
		/// <paramref name="expectedType"/>.
		/// </summary>
		XElement WriteElement(object obj, Type expectedType);

		/// <summary>
		/// Write the given object to the given <see cref="XElement"/> with a <see cref="Type"/> context of 
		/// <typeparamref name="T"/>.
		/// </summary>
		XElement WriteTo<T>(XElement element, T obj);

		/// <summary>
		/// Write the given object to the given <see cref="XElement"/> with a <see cref="Type"/> context of 
		/// <see cref="object"/>.
		/// </summary>
		XElement WriteTo(XElement element, object obj);

		/// <summary>
		/// Write the given object to the given <see cref="XElement"/> with a <see cref="Type"/> context of 
		/// <paramref name="expectedType"/>.
		/// </summary>
		XElement WriteTo(XElement element, object obj, Type expectedType);

		/// <summary>
		/// Write the given object as an <see cref="XAttribute"/> using the given <see cref="XName"/>.
		/// </summary>
		XAttribute WriteAttribute(object obj, XName xName);

		/// <summary>
		/// Write the given object to the given <see cref="XAttribute"/>.
		/// </summary>
		XAttribute WriteTo(XAttribute attribute, object obj);
	}
}
