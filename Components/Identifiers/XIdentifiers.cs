namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// The <see cref="XIdentifiers"/> component controls the reading and writing of object references in XML.
	/// </summary>
	public sealed class XIdentifiers : XMachineComponent
	{
		internal XIdentifiers() { }

		/// <summary>
		/// The <see cref="XCompositeIdentifier"/> object used by <see cref="XIdentifiers"/> to store global
		/// <see cref="XIdentifier{TType, TId}"/> objects.
		/// </summary>
		public XCompositeIdentifier Identifier { get; } = new XCompositeIdentifier();

		/// <summary>
		/// Registers an <see cref="XReaderComponent"/> that enables reading with references.
		/// </summary>
		protected override void OnCreateReader(XReader reader) =>
			reader.Register(new XIdentifierReader(Identifier));

		/// <summary>
		/// Registers an <see cref="XWriterComponent"/> that enables writing with references.
		/// </summary>
		protected override void OnCreateWriter(XWriter writer) =>
			writer.Register(new XIdentifierWriter(Identifier));
	}
}
