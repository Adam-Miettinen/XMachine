using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// The <see cref="XIdentifierWriter"/> represents a set of <see cref="XIdentifier{TType, TId}"/> objects active 
	/// on an <see cref="XWriter"/>.
	/// </summary>
	public sealed class XIdentifierWriter : XWriterComponent
	{
		private readonly IDictionary<object, object> referenceObjects;

		internal XIdentifierWriter()
		{
			Identifier = new XCompositeIdentifier();
			referenceObjects = new Dictionary<object, object>(Identifier.KeyComparer);
		}

		internal XIdentifierWriter(XCompositeIdentifier identifiers)
		{
			Identifier = new XCompositeIdentifier(identifiers);
			referenceObjects = new Dictionary<object, object>(Identifier.KeyComparer);
		}

		/// <summary>
		/// The <see cref="XCompositeIdentifier"/> object used by <see cref="XIdentifiers"/> to store 
		/// <see cref="XIdentifier{TType, TId}"/> objects affecting this write operation.
		/// </summary>
		public XCompositeIdentifier Identifier { get; }

		/// <summary>
		/// Record the written object as a reference
		/// </summary>
		protected override bool OnWrite(IXWriteOperation writer, object obj, XElement element)
		{
			if (obj != null && Identifier.CanId(obj.GetType(), out Type idType))
			{
				object id = Identifier.GetId(obj);

				if (id != null && referenceObjects.ContainsKey(id))
				{
					_ = writer.WriteTo(element, id, idType);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Record the written object as a reference
		/// </summary>
		protected override bool OnWrite(IXWriteOperation writer, object obj, XAttribute attribute)
		{
			if (obj != null && Identifier.CanId(obj.GetType()))
			{
				object id = Identifier.GetId(obj);

				if (id != null && referenceObjects.ContainsKey(id))
				{
					_ = writer.WriteTo(attribute, id);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Record a contextual object as a reference
		/// </summary>
		protected override void OnSubmit(IXWriteOperation writer, object obj)
		{
			if (obj != null && Identifier.CanId(obj.GetType()))
			{
				object id = Identifier.GetId(obj);

				if (id != null)
				{
					if (referenceObjects.TryGetValue(id, out object existing))
					{
						if (!Equals(existing, obj))
						{
							throw new InvalidOperationException(
								$"Reference collision on ID '{id}' between objects of type " +
								$"{existing.GetType().Name} and {obj.GetType().Name}.");
						}
					}
					else
					{
						referenceObjects.Add(id, obj);
					}
				}
			}
		}
	}
}
