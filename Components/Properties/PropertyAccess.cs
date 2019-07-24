using System;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// Bitflags that determine what access levels of properties will be read from and written to XML.
	/// Readonly properties are not supported by default.
	/// </summary>
	[Flags]
	public enum PropertyAccess
	{
		/// <summary>
		/// Must have public get and set methods
		/// </summary>
		PublicOnly = 0b0000_0000_0000_0000,

		/// <summary>
		/// May have public or protected internal get and set methods
		/// </summary>
		ProtectedInternal = ProtectedInternalGet | ProtectedInternalSet,

		/// <summary>
		/// May have public, protected internal, or internal get and set methods
		/// </summary>
		Internal = InternalGet | InternalSet,

		/// <summary>
		/// May have public, protected internal, or protected get and set methods
		/// </summary>
		Protected = ProtectedGet | ProtectedSet,

		/// <summary>
		/// May have public, protected internal, protected, internal, or private protected get and set methods
		/// </summary>
		PrivateProtected = PrivateProtectedGet | PrivateProtectedSet,

		/// <summary>
		/// May have any access level of get and set methods
		/// </summary>
		Private = PrivateGet | PrivateSet,

		/// <summary>
		/// May have a public or protected internal get method
		/// </summary>
		ProtectedInternalGet = 0b0000_0000_0000_0001,

		/// <summary>
		/// May have a public or protected internal set method
		/// </summary>
		ProtectedInternalSet = 0b0000_0001_0000_0000,

		/// <summary>
		/// May have a public, protected internal or internal get method
		/// </summary>
		InternalGet = ProtectedInternalGet | 0b0000_0000_0000_0010,

		/// <summary>
		/// May have a public, protected internal or internal set method
		/// </summary>
		InternalSet = ProtectedInternalSet | 0b0000_0010_0000_0000,

		/// <summary>
		/// May have a public, protected internal or protected get method
		/// </summary>
		ProtectedGet = ProtectedInternalGet | 0b0000_0000_0000_0100,

		/// <summary>
		/// May have a public, protected internal or protected set method
		/// </summary>
		ProtectedSet = ProtectedInternalSet | 0b0000_0100_0000_0000,

		/// <summary>
		/// May have a public, protected internal, protected, internal, or private protected get method
		/// </summary>
		PrivateProtectedGet = InternalGet | ProtectedGet | 0b0000_0000_0000_1000,

		/// <summary>
		/// May have a public, protected internal, protected, internal, or private protected set method
		/// </summary>
		PrivateProtectedSet = InternalSet | ProtectedSet | 0b0000_1000_0000_0000,

		/// <summary>
		/// May have any access level get method
		/// </summary>
		PrivateGet = PrivateProtectedGet | 0b0000_0000_0001_0000,

		/// <summary>
		/// May have any access level set method
		/// </summary>
		PrivateSet = PrivateProtectedSet | 0b0001_0000_0000_0000
	}
}
