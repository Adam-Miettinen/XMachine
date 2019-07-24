using System;
using System.Xml;

namespace XMachine
{
	/// <summary>
	/// <see cref="XNameAttribute"/> instructs <see cref="XMachine"/> that a <see cref="Type"/> or property
	/// should be serialized under a different <see cref="Name"/> in XML.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
		AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class XNameAttribute : Attribute
	{
		/// <summary>
		/// The text of the <see cref="Name"/> that will be used for this type or member.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Create a new <see cref="XNameAttribute"/> with the given <see cref="Name"/>. Throws an 
		/// exception if the provided name is invalid.
		/// </summary>
		public XNameAttribute(string name)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(name = XmlConvert.EncodeName(name)))
			{
				throw new ArgumentException($"{name} is not a valid XName", nameof(name));
			}
			Name = name;
		}
	}
}
