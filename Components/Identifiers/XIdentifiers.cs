namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// An <see cref="XMachineComponent"/> that registers <see cref="XReaderComponent"/>s and
	/// <see cref="XWriterComponent"/>s to allow objects to be deserialized and serialized by
	/// reference.
	/// </summary>
	public sealed class XIdentifiers : XMachineComponent
	{
		internal XIdentifiers() { }

		/// <summary>
		/// The <see cref="XCompositeIdentifier"/> object used to store global <see cref="XIdentifier{TType, TId}"/> 
		/// objects that affect all read/write operations.
		/// </summary>
		public XCompositeIdentifier Identifier { get; } = new XCompositeIdentifier();

		protected override void OnCreateReader(XReader reader) =>
			reader.Register(new XIdentifierReader(Identifier));

		protected override void OnCreateWriter(XWriter writer) =>
			writer.Register(new XIdentifierWriter(Identifier));
	}
}
