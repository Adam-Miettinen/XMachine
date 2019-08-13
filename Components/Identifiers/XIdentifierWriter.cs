using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// An <see cref="XWriterComponent"/> that uses a set of <see cref="XIdentifier{TType, TId}"/> objects
	/// to allow objects to be serialized by reference.
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
		/// The <see cref="XCompositeIdentifier"/> object used to store the <see cref="XIdentifier{TType, TId}"/> 
		/// objects affecting this write operation.
		/// </summary>
		public XCompositeIdentifier Identifier { get; }

		protected override bool OnWrite<T>(IXWriteOperation writer, T obj, XElement element, XObjectArgs args)
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

		protected override bool OnWrite<T>(IXWriteOperation writer, T obj, XAttribute attribute, XObjectArgs args)
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
