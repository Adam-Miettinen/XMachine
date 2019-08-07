using System;

namespace XMachine.Components.Rules
{
	/// <summary>
	/// Apply this attribute to a static method in a public class to create global rules for <see cref="XType{TType}"/>s.
	/// The method must have a signature of:<br />
	/// <c>static void AnyMethodName(XType&lt;T&gt; xType)</c><br />
	/// For some <see cref="Type"/> T. Whenever an instance of <see cref="XDomain"/> generates an <see cref="XType{TType}"/>
	/// of that type, your method will be invoked.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class XTypeRuleAttribute : Attribute
	{
	}
}
