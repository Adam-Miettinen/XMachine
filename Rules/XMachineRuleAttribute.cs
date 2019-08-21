using System;

namespace XMachine.Components.Rules
{
	/// <summary>
	/// The <see cref="XMachineRuleAttribute"/> may be applied to a public static method with
	/// a return type of void and zero parameters. That method will be invoked by <see cref="XMachine"/>
	/// as soon as the assembly containing it is loaded.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class XMachineRuleAttribute : Attribute
	{
	}
}
