namespace XMachine
{
	/// <summary>
	/// An <see cref="IXComponent"/> can be registered as a component with an <see cref="IXWithComponents{T}"/>.
	/// </summary>
	public interface IXComponent
	{
		/// <summary>
		/// Get or set whether the component is enabled and should be invoked by its owner.
		/// </summary>
		bool Enabled { get; set; }
	}
}
