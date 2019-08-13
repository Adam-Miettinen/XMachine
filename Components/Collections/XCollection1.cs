using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XMachine.Components.Collections
{
	/// <summary>
	/// A subclassable <see cref="XTypeComponent{T}"/> for collection types that defines default methods to
	/// enumerate, serialize and deserialize the items of an object of type <typeparamref name="T"/>.
	/// </summary>
	public abstract class XCollection<T> : XTypeComponent<T>
	{
		private XName itemName;

		/// <summary>
		/// Create a new instance of <see cref="XCollection{T}"/>.
		/// </summary>
		/// <param name="xType">The <see cref="XType{T}"/> object to which this <see cref="XTypeComponent{T}"/> belongs.</param>
		protected XCollection(XType<T> xType) : base(xType) { }

		/// <summary>
		/// Get or set the <see cref="XName"/> that will identify collection items.
		/// </summary>
		public XName ItemName
		{
			get => itemName ?? XComponents.Component<XAutoCollections>().ItemName;
			set => itemName = value;
		}

		/// <summary>
		/// Get or set whether this collection should behave as if tagged with <see cref="XmlElementAttribute"/>.
		/// Items will not be wrapped in an element.
		/// </summary>
		public bool ItemsAsElements { get; set; }

		/// <summary>
		/// Implementers that do not override the <see cref="OnBuild(IXReadOperation, XElement, ObjectBuilder{T}, XObjectArgs)"/> and
		/// <see cref="OnWrite(IXWriteOperation, T, XElement, XObjectArgs)"/> methods must provide the <see cref="Type"/>
		/// of items in this collection.
		/// </summary>
		protected abstract Type ItemType { get; }

		/// <summary>
		/// Enumerate the items of the collection by casting <paramref name="collection"/> to <see cref="IEnumerable"/>
		/// and getting an <see cref="IEnumerator"/>. Implementers can override this method to provide alternative 
		/// <see cref="IEnumerator"/>s.
		/// </summary>
		/// <param name="collection">The instance to get an <see cref="IEnumerator"/> for.</param>
		/// <returns>An instance of <see cref="IEnumerator"/> over the items in <paramref name="collection"/>.</returns>
		protected virtual IEnumerator EnumerateItems(T collection) => ((IEnumerable)collection).GetEnumerator();

		/// <summary>
		/// Implementers must override this method to add deserialized items to the collection.
		/// </summary>
		/// <param name="collection">The instance to add items to.</param>
		/// <param name="index">The position at which to add the item. This method is guaranteed to be called in ascending
		/// index order from zero.</param>
		/// <param name="item">The item to add.</param>
		protected abstract void AddItem(T collection, int index, object item);

		/// <summary>
		/// Fetches the subelements of the given <see cref="XElement"/> that appear to contain serialized collection items.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to search.</param>
		/// <param name="args">Optional <see cref="XCollectionArgs"/> defining how collection items were serialized.</param>
		protected IEnumerable<XElement> GetItemElements(XElement element, XCollectionArgs args)
		{
			if (args == null)
			{
				args = new XCollectionArgs(default, ItemsAsElements, ItemName);
			}
			return args.ItemsAsElements && args.ItemName == null
				? element.Elements()
				: element.Elements(args.ItemName);
		}

		/// <summary>
		/// Called after <see cref="XTypeComponent{T}.OnRead(IXReadOperation, XElement, out T, XObjectArgs)"/> has been invoked on all
		/// <see cref="XTypeComponent{T}"/>s, reading has not been halted, and <see cref="XReader"/> has constructed 
		/// an <see cref="ObjectBuilder{T}"/> for deferred deserialization.
		/// </summary>
		/// <param name="reader">An <see cref="IXReadOperation"/> instance that exposes methods for reading XML and
		/// scheduling tasks using the active <see cref="XReader"/>.</param>
		/// <param name="element">The <see cref="XElement"/> to be read.</param>
		/// <param name="objectBuilder">An instance of <see cref="ObjectBuilder{T}"/> containing, or eventually containing,
		/// the deserialized object.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// read.</param>
		protected override void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder, XObjectArgs args)
		{
			if (!element.HasElements)
			{
				return;
			}

			XCollectionArgs collectionArgs = args as XCollectionArgs ?? new XCollectionArgs(default, ItemsAsElements, ItemName);

			IEnumerable<XElement> itemElements = GetItemElements(element, collectionArgs);

			object[] itemsRead = Enumerable.Repeat(XmlTools.PlaceholderObject, itemElements.Count()).ToArray();
			int i = 0;

			foreach (XElement item in itemElements)
			{
				int idx = i++;
				reader.Read(item, ItemType, x =>
					{
						itemsRead[idx] = x;
						return true;
					},
					collectionArgs.ItemArgs);
			}

			reader.AddTask(this, () =>
			{
				if (objectBuilder.IsConstructed && !itemsRead.Any(x => ReferenceEquals(x, XmlTools.PlaceholderObject)))
				{
					for (int j = 0; j < itemsRead.Length; j++)
					{
						try
						{
							AddItem(objectBuilder.Object, j, itemsRead[j]);
						}
						catch (Exception e)
						{
							reader.ExceptionHandler(e);
						}
					}

					return true;
				}
				return false;
			});
		}

		/// <summary>
		/// Called when an <see cref="IXWriteOperation"/> begins writing an object of type <typeparamref name="T"/>
		/// as an <see cref="XElement"/>.
		/// </summary>
		/// <param name="writer">An <see cref="IXWriteOperation"/> instance that exposes methods for writing XML
		/// using the active <see cref="XWriter"/>.</param>
		/// <param name="obj">The object to be written.</param>
		/// <param name="element">The <see cref="XElement"/> to which <paramref name="obj"/> is being written. The
		/// element will already have been assigned the correct <see cref="XName"/>.</param>
		/// <param name="args">Optional arguments to communicate to components how <paramref name="element"/> should be
		/// written.</param>
		/// <returns><c>True</c> if this <see cref="XTypeComponent{T}"/> was able to serialize the object and all further 
		/// processing of <paramref name="element"/> should cease.</returns>
		protected override bool OnWrite(IXWriteOperation writer, T obj, XElement element, XObjectArgs args)
		{
			XCollectionArgs collectionArgs = args as XCollectionArgs ?? new XCollectionArgs(default, ItemsAsElements, ItemName);

			IEnumerator enumerator = EnumerateItems(obj);

			if (collectionArgs.ItemsAsElements && collectionArgs.ItemName == null)
			{
				while (enumerator.MoveNext())
				{
					element.Add(writer.WriteElement(enumerator.Current, ItemType, collectionArgs.ItemArgs));
				}
			}
			else
			{
				while (enumerator.MoveNext())
				{
					element.Add(writer.WriteTo(new XElement(collectionArgs.ItemName), enumerator.Current, ItemType, collectionArgs.ItemArgs));
				}
			}

			return false;
		}
	}
}
