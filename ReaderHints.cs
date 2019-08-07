using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A bitflag containing hints that can be passed to <see cref="IXReadOperation"/> to increase its performance.
	/// </summary>
	[Flags]
	public enum ReaderHints
	{
		/// <summary>
		/// Default read behaviour, no hints.
		/// </summary>
		Default = 0b0000_0000_0000_0000,

		/// <summary>
		/// The <see cref="XName"/> of the <see cref="XElement"/> to be read has been overriden and should not be used 
		/// to resolve the <see cref="Type"/> of the serialized object.
		/// </summary>
		IgnoreElementName = 0b0000_0000_0000_0001,
	}
}
