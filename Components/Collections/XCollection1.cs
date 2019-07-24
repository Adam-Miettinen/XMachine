using System.Collections;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XMachine.Components.Collections
{
	/// <summary>
	/// A single-parameter generic that serves as an abstract base for <see cref="XCollection{TCollection, TItem}"/>.
	/// </summary>
	public abstract class XCollection<T> : XTypeComponent<T>
	{
		/// <summary>
		/// An object used to denote an unassigned value in a collection.
		/// </summary>
		protected static readonly object PlaceholderObject = new object();

		private XName itemName;

		/// <summary>
		/// Create a new instance of <see cref="XCollection{T}"/>.
		/// </summary>
		protected XCollection()
		{
		}

		/// <summary>
		/// Get or set the <see cref="XName"/> that will identify collection items.
		/// </summary>
		public XName ItemName
		{
			get => itemName ?? XComponents.Component<XAutoCollections>().ItemName;
			set => itemName = value;
		}

		/// <summary>
		/// If <see cref="ItemsAsElements"/> is true, this collection will behave as if tagged with <see cref="XmlElementAttribute"/>.
		/// Items will not be wrapped in an element.
		/// </summary>
		public bool ItemsAsElements { get; set; }

		/// <summary>
		/// Override this method to alter how items are enumerated for writing.
		/// </summary>
		protected virtual IEnumerator EnumerateItems(T collection) => ((IEnumerable)collection).GetEnumerator();
	}
}
