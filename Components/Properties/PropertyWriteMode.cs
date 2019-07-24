namespace XMachine.Components.Properties
{
	/// <summary>
	/// Ways in which a property may be written to an XML element.
	/// </summary>
	public enum PropertyWriteMode
	{
		/// <summary>
		/// Write the property as a child element (necessary for complex objects)
		/// </summary>
		Element = 0,

		/// <summary>
		/// Write the property as an attribute
		/// </summary>
		Attribute = 1,

		/// <summary>
		/// Write the property as inner text (maximum 1 such property per object)
		/// </summary>
		Text = 2
	}
}
