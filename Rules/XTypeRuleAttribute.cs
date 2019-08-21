using System;

namespace XMachine.Components.Rules
{
	/// <summary>
	/// Apply this attribute to a static method in a public class to create global rules for <see cref="XType{T}"/>s.
	/// The method must have a return type of <see cref="void"/> and a single parameter that is an <see cref="XType{T}"/>.
	/// For some <see cref="Type"/> T. Whenever an instance of <see cref="XDomain"/> generates an <see cref="XType{TType}"/>
	/// that is assignable to your method's parameter, the method will be invoked.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class XTypeRuleAttribute : Attribute
	{
	}
}
