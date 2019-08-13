using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// A set of bitflags used by <see cref="XObjectArgs"/> that can adjust or optimize serialization and deserialization.
	/// </summary>
	[Flags]
	public enum ObjectHints
	{
		/// <summary>
		/// No hints, the default.
		/// </summary>
		None = 0b0000_0000_0000_0000,

		/// <summary>
		/// The <see cref="XName"/> of this <see cref="XElement"/> or <see cref="XAttribute"/> has been overriden 
		/// and does not contain useful information about the <see cref="Type"/> of the serialized object. Passing
		/// the correct value for this argument is optional but can modestly increase performance.
		/// </summary>
		IgnoreElementName = 0b0000_0000_0000_0001,

		/// <summary>
		/// Instructs native <see cref="XMachine"/> components not to construct this object. The deserializer will 
		/// assume that the object's construction is handled by other components.
		/// </summary>
		DontConstruct = 0b0000_0000_0000_0010,

		/// <summary>
		/// Instructs components that this object should not be constructed and, instead, its constructed value should
		/// be retrieved from the object that declares it. Use this hint for properties that are constructed in their
		/// containing objects' constructors.
		/// </summary>
		ConstructedByOwner = 0b0000_0000_0100 | DontConstruct,
	}
}
