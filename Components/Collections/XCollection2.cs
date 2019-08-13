using System;

namespace XMachine.Components.Collections
{
	/// <summary>
	/// A version of <see cref="XCollection{T}"/> that provides strong typing of collection items.
	/// </summary>
	public abstract class XCollection<TCollection, TItem> : XCollection<TCollection>
	{
		/// <summary>
		/// Create a new instance of <see cref="XCollection{TCollection, TItem}"/>.
		/// </summary>
		/// <param name="xType">The <see cref="XType{T}"/> object to which this <see cref="XTypeComponent{T}"/> belongs.</param>
		protected XCollection(XType<TCollection> xType) : base(xType) { }

		/// <summary>
		/// Get the <see cref="Type"/> object for <typeparamref name="TItem"/>.
		/// </summary>
		protected override Type ItemType => typeof(TItem);

		/// <summary>
		/// Performs a cast and adds the item to the collection.
		/// </summary>
		/// <param name="collection">The instance to add items to.</param>
		/// <param name="index">The position at which to add the item. This method is guaranteed to be called in ascending
		/// index order from zero.</param>
		/// <param name="item">The item to add.</param>
		protected override void AddItem(TCollection collection, int index, object item) =>
			AddItem(collection, index, (TItem)item);

		/// <summary>
		/// Implementers must override this method to add deserialized items to the collection.
		/// </summary>
		/// <param name="collection">The instance to add items to.</param>
		/// <param name="index">The position at which to add the item. This method is guaranteed to be called in ascending
		/// index order from zero.</param>
		/// <param name="item">The item to add.</param>
		protected abstract void AddItem(TCollection collection, int index, TItem item);
	}
}
