using System;
using System.Reflection;

namespace XMachine
{
	/// <summary>
	/// Developers can add <c>[assembly: XMachine.XMachineAssembly]</c> to any file in their project to tag
	/// their assembly with <see cref="XMachineAssemblyAttribute"/>. The <see cref="Type"/>s exported from
	/// a tagged assembly will be scanned by <see cref="XNamer"/> and <see cref="XMachineComponent"/>s. This
	/// attribute's properties can be used to customize default access levels for constructors and properties
	/// defined in the tagged assembly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public sealed class XMachineAssemblyAttribute : Attribute
	{
		/// <summary>
		/// Get or set the recommended <see cref="XMachine.MemberAccess"/> level for <see cref="Type"/>s 
		/// defined in this <see cref="Assembly"/>.
		/// </summary>
		public MemberAccess PropertyAccess { get; set; }

		/// <summary>
		/// Get or set the recommended <see cref="XMachine.MethodAccess"/> level for <see cref="Type"/>s 
		/// defined in this <see cref="Assembly"/>.
		/// </summary>
		public MethodAccess ConstructorAccess { get; set; }
	}
}
