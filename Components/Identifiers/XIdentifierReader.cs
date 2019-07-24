using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// The <see cref="XIdentifierReader"/> represents a set of <see cref="XIdentifier{TType, TId}"/> objects active 
	/// on an <see cref="XReader"/>.
	/// </summary>
	public sealed class XIdentifierReader : XReaderComponent
	{
		private readonly IDictionary<object, object> referenceObjects;

		internal XIdentifierReader()
		{
			Identifier = new XCompositeIdentifier();
			referenceObjects = new Dictionary<object, object>(Identifier.KeyComparer);
		}

		internal XIdentifierReader(XCompositeIdentifier identifiers)
		{
			Identifier = new XCompositeIdentifier(identifiers);
			referenceObjects = new Dictionary<object, object>(Identifier.KeyComparer);
		}

		/// <summary>
		/// The <see cref="XCompositeIdentifier"/> object used by <see cref="XIdentifiers"/> to store 
		/// <see cref="XIdentifier{TType, TId}"/> objects affecting this read operation.
		/// </summary>
		public XCompositeIdentifier Identifier { get; }

		/// <summary>
		/// Resolves the element as a reference if possible
		/// </summary>
		protected override bool OnRead(IXReadOperation reader, XElement element, Type expectedType, Func<object, bool> assign,
			out Func<bool> task)
		{
			if (!element.HasAttributes &&
				!element.HasElements &&
				Identifier.CanId(expectedType, out Type idType))
			{
				// Read the element as an ID

				bool idFound = false;
				object id = null;

				reader.Read(element, idType, x =>
				{
					id = x;
					return true;
				},
				ReaderHints.IgnoreElementName);

				// Schedule a task to assign the object if it shows up in the dictionary

				bool refFound = false;
				object referenceObject = null;

				task = () =>
				{
					if (refFound)
					{
						return assign(referenceObject);
					}

					if (idFound && referenceObjects.TryGetValue(id, out referenceObject))
					{
						refFound = true;
						if (!expectedType.IsAssignableFrom(referenceObject.GetType()) ||
							assign(referenceObject))
						{
							return true;
						}
					}

					return false;
				};

				return true;
			}

			task = null;
			return false;
		}

		/// <summary>
		/// Resolves the attribute as a reference if possible
		/// </summary>
		protected override bool OnRead(IXReadOperation reader, XAttribute attribute, Type expectedType, Func<object, bool> assign,
			out Func<bool> task)
		{
			if (Identifier.CanId(expectedType))
			{
				string text = attribute.Value;
				if (!string.IsNullOrWhiteSpace(text) && text.Length > 0)
				{
					task = () =>
					{
						if (referenceObjects.TryGetValue(text, out object referenceObject))
						{
							if (expectedType.IsAssignableFrom(referenceObject.GetType()))
							{
								_ = assign(referenceObject);
							}
							return true;
						}
						return false;
					};
					return true;
				}
			}

			task = null;
			return false;
		}

		/// <summary>
		/// Register a constructed object as a reference
		/// </summary>
		protected override void OnSubmit(IXReadOperation reader, object obj, out Func<bool> task)
		{
			task = null;

			if (obj != null && Identifier.CanId(obj.GetType()))
			{
				object id = Identifier.GetId(obj);

				if (id != null)
				{
					Submit(id, obj);
				}
				else
				{
					task = () =>
					{
						object id2 = Identifier.GetId(obj);
						if (id2 != null)
						{
							Submit(id2, obj);
							return true;
						}
						return false;
					};
				}
			}
		}

		private void Submit(object id, object obj)
		{
			if (!referenceObjects.TryGetValue(id, out object existing))
			{
				referenceObjects.Add(id, obj);
			}
			else if (Equals(obj, existing))
			{
				throw new InvalidOperationException($"Reference-type object read more than once on ID '{id}'.");
			}
			else
			{
				throw new InvalidOperationException($"Reference collision on ID '{id}'.");
			}
		}
	}
}
