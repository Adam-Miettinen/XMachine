using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// Represents a single write operation of serializing objects to XML, corresponding to a single call 
	/// to one of the methods in <see cref="XWriter"/>.
	/// </summary>
	public interface IXWriteOperation : IExceptionHandler
	{
		/// <summary>
		/// Write the given object as an <see cref="XElement"/>. When read, the <see cref="IXReadOperation"/>
		/// will expect an object of type <typeparamref name="T"/>.
		/// </summary>
		XElement WriteElement<T>(T obj);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/>.
		/// </summary>
		XElement WriteElement(object obj);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/>. When read, the <see cref="IXReadOperation"/>
		/// will expect an object of type <paramref name="expectedType"/>.
		/// </summary>
		XElement WriteElement(object obj, Type expectedType);

		/// <summary>
		/// Write the given object to the given <see cref="XElement"/>. When read, the <see cref="IXReadOperation"/>
		/// will expect an object of type <typeparamref name="T"/>.
		/// </summary>
		XElement WriteTo<T>(XElement element, T obj);

		/// <summary>
		/// Write the given object to the given <see cref="XElement"/>. 
		/// </summary>
		XElement WriteTo(XElement element, object obj);

		/// <summary>
		/// Write the given object to the given <see cref="XElement"/>. When read, the <see cref="IXReadOperation"/>
		/// will expect an object of type <paramref name="expectedType"/>.
		/// </summary>
		XElement WriteTo(XElement element, object obj, Type expectedType);

		/// <summary>
		/// Write the given object as an <see cref="XAttribute"/>. When read, the <see cref="IXReadOperation"/> must
		/// know the object's runtime type.
		/// </summary>
		XAttribute WriteAttribute(object obj, XName xName);

		/// <summary>
		/// Write the given object to the given <see cref="XAttribute"/>. When read, the <see cref="IXReadOperation"/>
		/// must know the object's runtime type.
		/// </summary>
		XAttribute WriteTo(XAttribute attribute, object obj);
	}
}
