using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// This critical utility resolves XML elements to object <see cref="Type"/>s and vice versa.
	/// </summary>
	public abstract class XNamer : IExceptionHandler
	{
		/// <summary>
		/// Get or set the <see cref="XName"/> associated with the given <see cref="Type"/>.
		/// </summary>
		public abstract XName this[Type type] { get; set; }

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public Action<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// Reset the <see cref="XNamer"/>, clearing out any internal mappings between <see cref="Type"/>s and XML.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// Attempt to resolve the given <see cref="XElement"/> to a <see cref="Type"/> of serialized object.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to resolve.</param>
		/// <param name="expectedType">A <see cref="Type"/> object that the serialized object either belongs to or is
		/// assignable to.</param>
		/// <returns>The runtime <see cref="Type"/> of the serialized object, or <c>null</c> if it could not be resolved.</returns>
		protected abstract Type GetType(XElement element, Type expectedType);

		/// <summary>
		/// Called when <see cref="XComponents"/> detects that a new assembly has been loaded, and the assembly is
		/// tagged with <see cref="XMachineAssemblyAttribute"/>. All public <see cref="Type"/> objects defined in
		/// the assembly will be passed to this method excluding arrays, constructed generics, COM objects, and 
		/// imported types.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> object to inspect.</param>
		protected abstract void OnInspectType(Type type);

		internal void InspectType(Type type) => OnInspectType(type);

		internal Type GetTypeInternal(XElement element, Type expectedType) => GetType(element, expectedType);
	}
}
