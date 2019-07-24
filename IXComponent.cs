namespace XMachine
{
	/// <summary>
	/// An <see cref="IXComponent"/> can be registered as a component with an <see cref="IXWithComponents{T}"/>.
	/// </summary>
	public interface IXComponent
	{
		/// <summary>
		/// Whether the component is enabled and should receive method calls.
		/// </summary>
		bool Enabled { get; set; }
	}
}
