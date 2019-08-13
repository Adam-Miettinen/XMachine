using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using XMachine.Components.Properties;
using XMachine.Reflection;

namespace XMachine.Components.Collections
{
	/// <summary>
	/// An <see cref="XMachineComponent"/> that adds <see cref="XTypeComponent{T}"/>s to control the reading and
	/// writing of collection items from collection-like objects that have known behaviour for enumerating
	/// and adding items.
	/// </summary>
	public sealed class XAutoCollections : XMachineComponent
	{
		private static readonly MethodInfo dictionaryConstructor = typeof(XAutoCollections)
			.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
			.FirstOrDefault(x => x.Name == nameof(DictionaryConstructor) && x.IsGenericMethodDefinition);

		private XName itemName, keyName, valueName, arrayLowerBoundName;

		internal XAutoCollections() { }

		/// <summary>
		/// Get or set the default <see cref="XName"/> used for collection elements.
		/// </summary>
		public XName ItemName
		{
			get => itemName ?? (itemName = "li");
			set => itemName = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Get or set the <see cref="XName"/> used for the key of a dictionary entry (<see cref="KeyValuePair{TKey, TValue}"/>
		/// or <see cref="DictionaryEntry"/>).
		/// </summary>
		public XName KeyName
		{
			get => keyName ?? (keyName = "Key");
			set => keyName = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Get or set the <see cref="XName"/> used for the value of a dictionary entry (<see cref="KeyValuePair{TKey, TValue}"/>
		/// or <see cref="DictionaryEntry"/>).
		/// </summary>
		public XName ValueName
		{
			get => valueName ?? (valueName = "Value");
			set => valueName = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Get or set the <see cref="XName"/> used for an attribute denoting an array's non-zero lower bound.
		/// </summary>
		public XName ArrayLowerBoundName
		{
			get => arrayLowerBoundName ?? (arrayLowerBoundName = "lb");
			set => arrayLowerBoundName = value ?? throw new ArgumentNullException(nameof(value));
		}

		protected override void OnCreateXType<T>(XType<T> xType)
		{
			Type type = typeof(T);

			Type typeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			Type[] typeArgs = type.IsGenericType ? type.GenericTypeArguments : null;

			// KeyValuePair & DictionaryEntry

			if (typeDefinition == typeof(KeyValuePair<,>))
			{
				xType.Register((XTypeComponent<T>)typeof(XBuilderComponent<>)
					.MakeGenericType(type)
					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault()
					.Invoke(new object[]
					{
						xType,
						typeof(XKeyValuePair<,>)
							.MakeGenericType(typeArgs)
							.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
							.FirstOrDefault()
							.Invoke(null)
					}));
				return;
			}
			if (xType is XType<DictionaryEntry> xTypeDictEntry)
			{
				xTypeDictEntry.Register(new XBuilderComponent<DictionaryEntry>(xTypeDictEntry, new XDictionaryEntry()));
				return;
			}

			// ReadOnlyDictionary<,>

			if (typeDefinition == typeof(ReadOnlyDictionary<,>))
			{
				xType.Register((XTypeComponent<T>)typeof(XReadOnlyDictionary<,>)
					.MakeGenericType(typeArgs)
					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault()
					.Invoke(new object[]
					{
						xType
					}));
				return;
			}

			// ReadOnlyCollection<>

			if (typeDefinition == typeof(ReadOnlyCollection<>))
			{
				xType.Register((XTypeComponent<T>)typeof(XReadOnlyCollection<>)
					.MakeGenericType(typeArgs)
					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault()
					.Invoke(new object[]
					{
						xType
					}));
				return;
			}

			// LinkedListNode

			if (typeDefinition == typeof(LinkedListNode<>))
			{
				xType.Register((XTypeComponent<T>)typeof(XLinkedListNode<>)
					.MakeGenericType(typeArgs)
					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault()
					.Invoke(new object[]
					{
						xType
					}));
				return;
			}

			// Array

			if (type.IsArray)
			{
				Type arrayCompType = null;

				switch (type.GetArrayRank())
				{
					case 1:
						arrayCompType = typeof(XArray1<>);
						break;
					case 2:
						arrayCompType = typeof(XArray2<>);
						break;
				}

				if (arrayCompType != null)
				{
					xType.Register((XTypeComponent<T>)arrayCompType
						.MakeGenericType(type.GetElementType())
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(new object[]
						{
							xType
						}));
				}

				return;
			}

			// IDictionary<,>

			{
				Type dictType = type.GetInterfaces().FirstOrDefault(x =>
					x.IsGenericType &&
					x.GetGenericTypeDefinition() == typeof(IDictionary<,>));

				if (dictType != null)
				{
					Type[] dictArgs = dictType.GenericTypeArguments;
					xType.Register((XTypeComponent<T>)typeof(XIDictionary<,,>)
						.MakeGenericType(type, dictArgs[0], dictArgs[1])
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(new object[]
						{
							xType
						}));
					return;
				}
			}

			// IDictionary

			if (typeof(IDictionary).IsAssignableFrom(type))
			{
				xType.Register((XTypeComponent<T>)typeof(XIDictionary<>)
					.MakeGenericType(type)
					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault()
					.Invoke(new object[]
					{
						xType
					}));
				return;
			}

			// ICollection<>

			{
				Type itemType = type.GetInterfaces().FirstOrDefault(x =>
					x.IsGenericType &&
					x.GetGenericTypeDefinition() == typeof(ICollection<>))?.GenericTypeArguments[0];

				if (itemType != null)
				{
					xType.Register((XTypeComponent<T>)typeof(XICollection<,>)
						.MakeGenericType(type, itemType)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(new object[]
						{
							xType
						}));
					return;
				}
			}

			// IList

			if (typeof(IList).IsAssignableFrom(type))
			{
				xType.Register((XTypeComponent<T>)typeof(XIList<>)
					.MakeGenericType(type)
					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault()
					.Invoke(new object[]
					{
						xType
					}));
				return;
			}

			if (type.IsClass)
			{
				// Queue<T>

				Type queueType = type.GetSelfAndBaseTypes()
					.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Queue<>));

				if (queueType != null)
				{
					xType.Register((XTypeComponent<T>)typeof(XQueue<,>)
						.MakeGenericType(type, queueType.GenericTypeArguments[0])
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(new object[]
						{
							xType
						}));
					return;
				}

				// Stack<T>

				{
					Type stackType = type.GetSelfAndBaseTypes()
						.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Stack<>));

					if (stackType != null)
					{
						xType.Register((XTypeComponent<T>)typeof(XStack<,>)
							.MakeGenericType(type, stackType.GenericTypeArguments[0])
							.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
							.FirstOrDefault()
							.Invoke(new object[]
							{
							xType
							}));
						return;
					}
				}
			}
		}

		protected override void OnCreateXTypeLate<T>(XType<T> xType)
		{
			if (xType.Components<XTexter<T>>().Any(x => x.Enabled) ||
				xType.Components<XBuilderComponent<T>>().Any(x => x.Enabled))
			{
				XCollection<T> collection = xType.Component<XCollection<T>>();
				if (collection != null)
				{
					collection.Enabled = false;
				}
			}

			Type type = typeof(T);
			Type typeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			Type[] typeArgs = type.IsGenericType ? type.GenericTypeArguments : null;

			// Dictionary<,> constructor

			if (typeDefinition == typeof(Dictionary<,>))
			{
				_ = dictionaryConstructor.MakeGenericMethod(typeArgs).Invoke(this, new object[] { xType });
			}
		}

		private void DictionaryConstructor<TKey, TValue>(XType<Dictionary<TKey, TValue>> xType)
		{
			XProperty<Dictionary<TKey, TValue>, IEqualityComparer<TKey>> property =
				new XDelegateProperty<Dictionary<TKey, TValue>, IEqualityComparer<TKey>>(
					name: nameof(Dictionary<TKey, TValue>.Comparer),
					get: x => x.Comparer)
				{
					WriteIf = x => !Equals(x.Comparer, EqualityComparer<TKey>.Default)
				};

			xType.Component<XProperties<Dictionary<TKey, TValue>>>().Add(property);

			xType.Component<XProperties<Dictionary<TKey, TValue>>>().ConstructWith(
				expression1: x => x.Comparer,
				constructor: (arg1) => new Dictionary<TKey, TValue>(arg1));
		}
	}
}
