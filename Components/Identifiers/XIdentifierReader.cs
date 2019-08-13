using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XMachine.Reflection;

namespace XMachine.Components.Identifiers
{
	/// <summary>
	/// The <see cref="XIdentifierReader"/> component uses a set of <see cref="XIdentifier{TType, TId}"/> objects to
	/// allow an <see cref="XReader"/> to deserialize objects that were serialized by reference.
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
		/// The <see cref="XCompositeIdentifier"/> object used to store the <see cref="XIdentifier{TType, TId}"/> 
		/// objects affecting this read operation.
		/// </summary>
		public XCompositeIdentifier Identifier { get; }

		protected override bool OnRead<T>(IXReadOperation reader, XType<T> xType, XElement element, Func<object, bool> assign,
			XObjectArgs args)
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
				XObjectArgs.DefaultIgnoreElementName);

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

		protected override bool OnRead<T>(IXReadOperation reader, XType<T> xType, XAttribute attribute, Func<object, bool> assign,
			XObjectArgs args)
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
