using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// Represents an individual read operation initialized by a call to one of the methods in
	/// <see cref="XReader"/>.
	/// </summary>
	public interface IXReadOperation : IExceptionHandler
	{
		/// <summary>
		/// Reads the given <see cref="XElement"/> as an object assignable to the type <typeparamref name="T"/>,
		/// performing the given delegate once the object is successfully constructed.
		/// </summary>
		void Read<T>(XElement element, Func<T, bool> assign, ReaderHints hint = ReaderHints.Default);

		/// <summary>
		/// Reads the given <see cref="XElement"/> as an object, performing the given delegate once the object 
		/// is successfully constructed.
		/// </summary>
		void Read(XElement element, Func<object, bool> assign, ReaderHints hint = ReaderHints.Default);

		/// <summary>
		/// Reads the given <see cref="XAttribute"/> as an object assignable to the type <typeparamref name="T"/>,
		/// performing the given delegate once the object is successfully constructed.
		/// </summary>
		void Read<T>(XAttribute attribute, Func<T, bool> assign, ReaderHints hint = ReaderHints.Default);

		/// <summary>
		/// Reads the given <see cref="XElement"/> as an object assignable to the type <paramref name="expectedType"/>,
		/// performing the given delegate once the object is successfully constructed. 
		/// </summary>
		void Read(XElement element, Type expectedType, Func<object, bool> assign, ReaderHints hint = ReaderHints.Default);

		/// <summary>
		/// Reads the given <see cref="XAttribute"/> as an object assignable to the type <paramref name="expectedType"/>,
		/// performing the given delegate once the object is successfully constructed. 
		/// </summary>
		void Read(XAttribute attribute, Type expectedType, Func<object, bool> assign, ReaderHints hint = ReaderHints.Default);
	}
}
