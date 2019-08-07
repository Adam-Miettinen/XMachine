using System;
using System.Xml.Linq;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// A set of bitflags describing how an <see cref="XProperty{TType, TProperty}"/> is read from
	/// and written to XML.
	/// </summary>
	[Flags]
	public enum PropertyWriteMode
	{
		/// <summary>
		/// Write the property as a plain <see cref="XElement"/> (the default).
		/// </summary>
		Element = 0,

		/// <summary>
		/// Write the property as an <see cref="XAttribute"/>. Such properties must have a runtime type
		/// equal to their declared type, and their declared type must have an <see cref="XTexter{T}"/>.
		/// </summary>
		Attribute = 1,

		/// <summary>
		/// Write the property as inner text. Such properties must have a runtime type equal to their 
		/// declared type, and their declared type must have an <see cref="XTexter{T}"/>.
		/// </summary>
		Text = 2
	}
}
