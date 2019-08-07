using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// <see cref="XWriter"/> provides the public API for simple XML writing operations.
	/// </summary>
	public abstract class XWriter : XWithComponents<XWriterComponent>
	{
		private int writeTimeout = 10000;

		/// <summary>
		/// Get or set the number of milliseconds that <see cref="XWriter"/> is allowed to execute its write operation before
		/// it cancels, throwing a <see cref="TimeoutException"/>. The default is 10 seconds; the minimum is 100 milliseconds.
		/// </summary>
		public int WriteTimeout
		{
			get => writeTimeout;
			set => writeTimeout = value >= 100 ? value :
				throw new ArgumentException("Minimum timeout is 100 milliseconds", nameof(value));
		}

		/// <summary>
		/// Submit a contextual object to the <see cref="XWriter"/>. A contextual object will be available to
		/// <see cref="XWriterComponent"/>s, but it will not itself be written to XML.
		/// </summary>
		public abstract void Submit(object obj);

		/// <summary>
		/// Submit contextual objects to the <see cref="XWriter"/>. A contextual object will be 
		/// available to <see cref="XWriterComponent"/>s.
		/// </summary>
		public abstract void SubmitAll(IEnumerable objects);

		/// <summary>
		/// Attempts to write the given object as an <see cref="XElement"/>.
		/// </summary>
		public abstract XElement Write(object obj);

		/// <summary>
		/// Attempts to write the given collection of objects as <see cref="XElement"/>s.
		/// </summary>
		public abstract IEnumerable<XElement> WriteAll(IEnumerable objects);
	}
}
