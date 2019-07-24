using System;

namespace XMachine
{
	/// <summary>
	/// You may tag any type, property, constructor or assembly with this attribute to instruct
	/// <see cref="XMachine"/> to ignore it. The tagged member (as well as its subclasses and nested types)
	/// will not be read from or written to XML and will not be searched for customization methods or fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Constructor |
		AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct |
		AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
	public sealed class XIgnoreAttribute : Attribute
	{
	}
}
