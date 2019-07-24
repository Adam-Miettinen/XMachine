using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Xml.Linq;

namespace XMachine.Namers
{
	/// <summary>
	/// An implementation of <see cref="XNamer"/> that caches all <see cref="XName"/>s for serializable types
	/// on initialization. This improves speed at the cost of some memory.
	/// </summary>
	public abstract class AbstractXNamer : XNamer
	{
		private static readonly IEnumerable<string> ignoredAssemblies = new string[]
		{
			"System.Configuration",
			"System.Drawing",
			"System.Runtime",
			"System.Reflection",
			"System.Threading",
			"System.Xaml",
			"System.Xml"
		};

		private static readonly Predicate<Assembly> ignoreAssembly = x =>
			x.IsXIgnored() || ignoredAssemblies.Any(y => x.GetName().Name.StartsWith(y));

		private static readonly IEnumerable<Type> priorityTypes = new Type[]
		{
			typeof(object),
			typeof(bool),
			typeof(char),typeof(string),
			typeof(byte), typeof(sbyte),
			typeof(decimal),
			typeof(float), typeof(double),
			typeof(int), typeof(uint),
			typeof(long), typeof(ulong),
			typeof(short), typeof(ushort),

			typeof(DateTime), typeof(Version), typeof(BigInteger),

			typeof(ICollection<>), typeof(Collection<>),
			typeof(IList<>), typeof(List<>), typeof(LinkedList<>), typeof(LinkedListNode<>),
			typeof(IDictionary<,>), typeof(Dictionary<,>), typeof(KeyValuePair<,>),
			typeof(ISet<>), typeof(HashSet<>)
		};

		private readonly IDictionary<Type, XName> namesByType = new Dictionary<Type, XName>();
		private readonly IDictionary<XName, Type> typesByName = new Dictionary<XName, Type>();

		private bool initialized;

		/// <summary>
		/// Create an uninitialized <see cref="AbstractXNamer"/>.
		/// </summary>
		protected AbstractXNamer()
		{
		}

		/// <summary>
		/// Get or set the <see cref="XName"/> associated with the given <see cref="Type"/>. Returns null if
		/// the <see cref="Type"/> is not eligible for a name.
		/// </summary>
		public override XName this[Type type]
		{
			get
			{
				if (type == null)
				{
					throw new ArgumentNullException(nameof(type));
				}
				if (type.IsXIgnored())
				{
					return null;
				}
				if (namesByType.TryGetValue(type, out XName xName))
				{
					return xName;
				}

				string name = GetName(type);
				if (name != null)
				{
					xName = name;
					Put(type, xName);
					return xName;
				}

				return null;
			}
			set
			{
				if (type == null)
				{
					throw new ArgumentNullException(nameof(type));
				}
				if (value == null)
				{
					throw new ArgumentNullException(nameof(value));
				}
				Put(type, value);
			}
		}

		/// <summary>
		/// Retrieve the <see cref="Type"/> matching a given <see cref="XName"/> from the cache.
		/// </summary>
		protected Type this[XName name] => typesByName.TryGetValue(name, out Type type) ? type : null;

		/// <summary>
		/// Attempts to locate the <see cref="Type"/> matching the given <see cref="XElement"/>. Checks a dictionary
		/// of already-named types, and if no matching entry is found, delegates the task to the implementing
		/// class's <see cref="ParseXName(XName)"/> method.
		/// </summary>
		protected override Type GetType(XElement element, Type expectedType)
		{
			if (!initialized)
			{
				initialized = true;
				Initialize();
			}

			if (typesByName.TryGetValue(element.Name, out Type type))
			{
				return type;
			}

			type = ParseXName(element.Name);

			if (type != null && !type.IsXIgnored())
			{
				Put(type, element.Name);
				return type;
			}

			return null;
		}

		/// <summary>
		/// Called when <see cref="XMachine"/> scans a new <see cref="Assembly"/>.
		/// </summary>
		protected override void OnAssemblyScan(Assembly assembly)
		{
			if (ignoreAssembly(assembly))
			{
				return;
			}

			IEnumerable<Type> toIndex = null;

			try
			{
				toIndex = assembly.ExportedTypes
					.Where(x => !x.IsXIgnored() &&
						!x.IsArray && !x.IsCOMObject && !x.IsImport &&
						(!x.IsGenericType || x.IsGenericTypeDefinition));
			}
			catch (Exception e)
			{
				ExceptionHandler(e);
				return;
			}

			foreach (Type type in toIndex)
			{
				_ = this[type];
			}
		}

		/// <summary>
		/// Implement this method to provide a string name for a <see cref="Type"/> object.
		/// </summary>
		protected abstract string GetName(Type type);

		/// <summary>
		/// Override this method to parse, and retrieve a <see cref="Type"/> object for, an <see cref="XName"/>.
		/// </summary>
		protected abstract Type ParseXName(XName xName);

		/// <summary>
		/// Override this method to resolve cases where the same <see cref="XName"/> is being assigned to more than one 
		/// <see cref="Type"/>. The first type parameter, <paramref name="type1"/>, is currently assigned the XName
		/// <paramref name="xName"/>. The second type parameter, <paramref name="type2"/>, is unassigned.
		/// </summary>
		/// <returns>The <see cref="Type"/> object that should be assigned the <see cref="XName"/>, or null if the
		/// assignment to that <see cref="XName"/> should be removed.</returns>
		protected virtual Type ResolveCollision(XName xName, Type type1, Type type2)
		{
			ExceptionHandler(new InvalidOperationException(
				$"Duplicate XName {xName} for Types {type1.FullName} and {type2.FullName}"));
			return type1;
		}

		private void Put(Type type, XName name)
		{
			if (typesByName.TryGetValue(name, out Type prevType))
			{
				type = ResolveCollision(name, prevType, type);
				_ = typesByName.Remove(name);
				_ = namesByType.Remove(prevType);

				if (type == null)
				{
					return;
				}
			}

			typesByName.Add(name, type);
			namesByType.Add(type, name);
		}

		private void Initialize()
		{
			foreach (Type type in priorityTypes)
			{
				_ = this[type];
			}

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				OnAssemblyScan(assembly);
			}
		}
	}
}
