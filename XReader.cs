using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// <see cref="XReader"/> provides the public API for XML reading operations.
	/// </summary>
	public abstract class XReader : XWithComponents<XReaderComponent>
	{
		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object.
		/// </summary>
		public abstract object Read(XElement element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		public abstract T Read<T>(XElement element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects.
		/// </summary>
		public abstract IEnumerable<object> ReadAll(IEnumerable<XElement> elements);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public abstract IEnumerable<T> ReadAll<T>(IEnumerable<XElement> elements);

		/// <summary>
		/// Submit a contextual object to the <see cref="XReader"/>. A contextual object will be 
		/// available to <see cref="XReaderComponent"/>s.
		/// </summary>
		public abstract void Submit(object obj);

		/// <summary>
		/// Submit contextual objects to the <see cref="XReader"/>. A contextual object will be 
		/// available to <see cref="XReaderComponent"/>s.
		/// </summary>
		public abstract void SubmitAll(IEnumerable objects);
	}
}
