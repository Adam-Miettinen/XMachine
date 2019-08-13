using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	/// <summary>
	/// Arguments passed to <see cref="IXWriteOperation"/> and <see cref="IXReadOperation"/> methods that
	/// affect how components from the <see cref="Collections"/> namespace will format XML.
	/// </summary>
	public class XCollectionArgs : XObjectArgs
	{
		/// <summary>
		/// An empty set of <see cref="XCollectionArgs"/>.
		/// </summary>
		public static readonly new XCollectionArgs Default = new XCollectionArgs();

		/// <summary>
		/// If true, collection items will not be wrapped as elements.
		/// </summary>
		public readonly bool ItemsAsElements;

		/// <summary>
		/// If non-null, the <see cref="XName"/> of item elements will be set to this value.
		/// </summary>
		public readonly XName ItemName;

		/// <summary>
		/// Arguments to be passed to the <see cref="IXReadOperation"/> or <see cref="IXWriteOperation"/> when
		/// items in this collection are read/written.
		/// </summary>
		public readonly XObjectArgs ItemArgs;

		/// <summary>
		/// Create a new instance of <see cref="XCollectionArgs"/> with the given field settings.
		/// </summary>
		/// <param name="hints">The setting for <see cref="XObjectArgs.Hints"/>.</param>
		/// <param name="itemsAsElements">The setting for <see cref="ItemsAsElements"/>.</param>
		/// <param name="itemName">The setting for <see cref="ItemName"/>.</param>
		/// <param name="itemArgs">The setting for <see cref="ItemArgs"/>.</param>
		public XCollectionArgs(ObjectHints hints = ObjectHints.None, bool itemsAsElements = false, XName itemName = null, 
			XObjectArgs itemArgs = null) : base(hints)
		{
			ItemsAsElements = itemsAsElements;
			ItemName = itemName;
			ItemArgs = itemArgs;
		}
	}
}
