using System;
using System.Reflection;
using XMachine.Components.Constructors;
using XMachine.Components.Properties;

namespace XMachine
{
	/// <summary>
	/// Assemblies that wish to use attributes to customize how <see cref="XMachine"/> reads and writes
	/// XML must tag themselves with this attribute. Simply add <c>[assembly: XMachine.XMachineAssembly]</c> 
	/// to any file in your project.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public sealed class XMachineAssemblyAttribute : Attribute
	{
		/// <summary>
		/// Get or set the recommended property access level for <see cref="Type"/>s defined in this
		/// <see cref="Assembly"/>.
		/// </summary>
		public PropertyAccess PropertyAccess { get; set; }

		/// <summary>
		/// Get or set the recommended constructor access level for <see cref="Type"/>s defined in this
		/// <see cref="Assembly"/>.
		/// </summary>
		public ConstructorAccess ConstructorAccess { get; set; }
	}
}
