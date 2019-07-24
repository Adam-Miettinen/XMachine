using System;

namespace XMachine.Components.Constructors
{
	/// <summary>
	/// Bit flags that determine, by default, what access levels of parameterless constructors
	/// will be used to construct objects read from XML.
	/// </summary>
	[Flags]
	public enum ConstructorAccess
	{
		/// <summary>
		/// Only public constructors are allowed
		/// </summary>
		Public = 0b0000_0000_0000_0000,

		/// <summary>
		/// Public and protected internal constructors are allowed
		/// </summary>
		ProtectedInternal = 0b0000_0000_0000_0001,

		/// <summary>
		/// Public, protected internal and internal constructors are allowed
		/// </summary>
		Internal = ProtectedInternal | 0b0000_0000_0000_0010,

		/// <summary>
		/// Public, protected internal and protected constructors are allowed
		/// </summary>
		Protected = ProtectedInternal | 0b0000_0000_0000_0100,

		/// <summary>
		/// Public, protected internal, internal, protected and private protected constructors are allowed
		/// </summary>
		PrivateProtected = Internal | Protected | 0b0000_0000_0000_1000,

		/// <summary>
		/// All constructors are allowed
		/// </summary>
		Private = PrivateProtected | 0b0000_0000_0001_0000,
	}
}
