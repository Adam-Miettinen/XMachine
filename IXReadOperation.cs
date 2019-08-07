using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// Represents an individual read operation, which corresponds to a single method call by <see cref="XReader"/>.
	/// </summary>
	public interface IXReadOperation : IExceptionHandler
	{
		/// <summary>
		/// Add a task that will be attempted repeatedly in a loop with all other scheduled tasks registered with the
		/// <see cref="IXReadOperation"/>. The task should return true when it has been successfully completed.
		/// </summary>
		void AddTask(object source, Func<bool> task);

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
