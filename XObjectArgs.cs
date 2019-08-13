using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// Arguments passed to <see cref="IXWriteOperation"/> and <see cref="IXReadOperation"/> methods that
	/// affect component behaviour toward a single <see cref="object"/> or <see cref="XObject"/>.
	/// </summary>
	public class XObjectArgs
	{
		/// <summary>
		/// An instance of <see cref="XObjectArgs"/> containing the default settings.
		/// </summary>
		public static readonly XObjectArgs Default = new XObjectArgs();

		/// <summary>
		/// An instance of <see cref="XObjectArgs"/> with the <see cref="ObjectHints.IgnoreElementName"/> flag on.
		/// </summary>
		public static readonly XObjectArgs DefaultIgnoreElementName = new XObjectArgs(ObjectHints.IgnoreElementName);

		/// <summary>
		/// A set of bitflags that can adjust or optimize serialization and deserialization.
		/// </summary>
		public readonly ObjectHints Hints;

		/// <summary>
		/// Create a new instance of <see cref="XObjectArgs"/> with the given field settings.
		/// </summary>
		/// <param name="hints">The setting for <see cref="Hints"/>.</param>
		public XObjectArgs(ObjectHints hints = ObjectHints.None) => Hints = hints;
	}
}
