using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XMachine.Reflection;

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
		protected override bool OnRead<T>(IXReadOperation reader, XType<T> xType, XElement element, Func<object, bool> assign)
		{
			Type type = typeof(T);

			// A serialized reference has no attributes, no elements, some text, is of a type that can be ID'd,
			// and not of a type that has a registered XTexter

			string value = element.Value;

			if (!element.HasAttributes &&
				!element.HasElements &&
				!string.IsNullOrEmpty(value) &&
				xType.Component<XTexter<T>>() == null &&
				Identifier.CanId(type, out Type idType))
			{
				bool idFound = false;
				object id = null;

				reader.Read(element, idType, x =>
				{
					idFound = true;
					if (!Identifier.KeyComparer.Equals(x, ReflectionTools.GetDefaultValue(idType)))
					{
						id = x;
					}
					return true;
				},
				ReaderHints.IgnoreElementName);

				// Schedule a task to assign the object if it shows up in the dictionary

				reader.AddTask(this, () =>
				{
					if (!idFound)
					{
						return false;
					}
					if (id == null)
					{
						return true;
					}
					if (referenceObjects.TryGetValue(id, out object refObject))
					{
						if (refObject == null || type == refObject.GetType())
						{
							return assign(refObject);
						}
						else
						{
							throw new InvalidOperationException(
								$"Possible collision: the reference object with ID {id} was of expected type {type.Name}, " +
								$"but that ID resolved to an object of type {refObject.GetType().Name}.");
						}
					}

					return false;
				});

				return true;
			}

			return false;
		}

		/// <summary>
		/// Resolves the attribute as a reference if possible
		/// </summary>
		protected override bool OnRead<T>(IXReadOperation reader, XType<T> xType, XAttribute attribute, Func<object, bool> assign)
		{
			Type type = typeof(T);

			if (Identifier.CanId(type))
			{
				string text = attribute.Value;
				if (!string.IsNullOrWhiteSpace(text) && text.Length > 0)
				{
					reader.AddTask(this, () =>
					{
						if (referenceObjects.TryGetValue(text, out object refObject))
						{
							if (refObject == null || refObject.GetType() == type)
							{
								return assign(refObject);
							}
							else
							{
								throw new InvalidOperationException(
									$"Possible collision: the reference object with ID {text} was of expected type {type.Name}, " +
									$"but that ID resolved to an object of type {refObject.GetType().Name}.");
							}
						}
						return false;
					});
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Register a constructed object as a reference
		/// </summary>
		protected override void OnSubmit(IXReadOperation reader, object obj)
		{
			if (obj != null && Identifier.CanId(obj.GetType()))
			{
				object id = Identifier.GetId(obj);

				if (id != null)
				{
					Submit(id, obj);
				}
				else
				{
					reader.AddTask(this, () =>
					{
						object id2 = Identifier.GetId(obj);
						if (id2 != null)
						{
							Submit(id2, obj);
							return true;
						}
						return false;
					});
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
