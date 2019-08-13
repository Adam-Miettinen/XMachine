using System;
using System.Xml;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// <see cref="XNameAttribute"/> instructs <see cref="XMachine"/> that a type or property should be read and
	/// written to <see cref="XObject"/>s under an alternative <see cref="XName"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum |
		AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class XNameAttribute : Attribute
	{
		/// <summary>
		/// The text of the <see cref="XName"/> that will be used for this type or member.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Create a new <see cref="XNameAttribute"/> with the given <see cref="Name"/>. Throws an 
		/// exception if the provided name is invalid.
		/// </summary>
		/// <param name="name">The textual value of <see cref="Name"/>.</param>
		public XNameAttribute(string name)
		{
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(name = XmlConvert.EncodeName(name)))
			{
				throw new ArgumentException($"{name} is not a valid XName", nameof(name));
			}
			Name = name;
		}
	}
}
