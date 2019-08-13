using System;

namespace XMachine.Components.Rules
{
	/// <summary>
	/// The <see cref="XDomainRuleAttribute"/> may be applied to a public static method with
	/// a return type of void and a single parameter of type <see cref="XDomain"/>. That
	/// method will be invoked on every instance of <see cref="XDomain"/> created by
	/// <see cref="XMachine"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class XDomainRuleAttribute : Attribute
	{
	}
}
