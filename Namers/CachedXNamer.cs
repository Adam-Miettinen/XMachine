using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine.Namers
{
	/// <summary>
	/// An implementation of <see cref="XNamer"/> that caches all <see cref="XName"/>s for serializable types
	/// at initialization. This improves speed at the cost of memory.
	/// </summary>
	public abstract class CachedXNamer : XNamer
	{
		private readonly IDictionary<Type, XName> namesByType = new Dictionary<Type, XName>();
		private readonly IDictionary<XName, Type> typesByName = new Dictionary<XName, Type>();

		/// <summary>
		/// Create a new instance of <see cref="CachedXNamer"/>.
		/// </summary>
		protected CachedXNamer() { }

		/// <summary>
		/// Get or set the <see cref="XName"/> associated with the given <see cref="Type"/>. Gets null if
		/// the <see cref="Type"/> is not eligible for a name.
		/// </summary>
		/// <param name="type">A <see cref="Type"/> to reference.</param>
		public override XName this[Type type]
		{
			get
			{
				if (type == null)
				{
					throw new ArgumentNullException(nameof(type));
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
		/// Clear the internal caches of associations between <see cref="Type"/>s and <see cref="XName"/>s.
		/// </summary>
		public override void Reset()
		{
			typesByName.Clear();
			namesByType.Clear();
		}

		/// <summary>
		/// Get the <see cref="Type"/> matching a given <see cref="XName"/> from the cache.
		/// </summary>
		/// <param name="name">An <see cref="XName"/> to look up.</param>
		protected Type this[XName name] => typesByName.TryGetValue(name, out Type type) ? type : null;

		/// <summary>
		/// Attempts to locate the <see cref="Type"/> matching the given <see cref="XElement"/>. Checks a dictionary
		/// of already-named types, and if no matching entry is found, delegates the task to the implementing
		/// class's <see cref="ParseXName(XName)"/> method.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> being checked.</param>
		/// <param name="expectedType">The <see cref="Type"/> to which the return value of this method must be
		/// assigned.</param>
		/// <returns>A <see cref="Type"/> object to which <paramref name="element"/> resolves, or <c>null</c>.</returns>
		protected override Type GetType(XElement element, Type expectedType)
		{
			if (typesByName.TryGetValue(element.Name, out Type type))
			{
				return type;
			}

			type = ParseXName(element.Name);

			if (type == null || !expectedType.IsAssignableFrom(type))
			{
				return null;
			}
			else
			{
				Put(type, element.Name);
				return type;
			}
		}

		/// <summary>
		/// Called when <see cref="XComponents"/> detects that a new assembly has been loaded, and the assembly is
		/// tagged with <see cref="XMachineAssemblyAttribute"/>. All public <see cref="Type"/> objects defined in
		/// the assembly will be passed to this method excluding arrays, constructed generics, COM objects, and 
		/// imported types.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> object to inspect.</param>
		protected override void OnInspectType(Type type) => _ = this[type];

		/// <summary>
		/// Implement this method to provide a name for a <see cref="Type"/> object.
		/// </summary>
		protected abstract string GetName(Type type);

		/// <summary>
		/// Override this method to parse, and retrieve a <see cref="Type"/> for, an <see cref="XName"/>.
		/// </summary>
		/// <param name="xName">The <see cref="XName"/> of the <see cref="XElement"/> being resolved.</param>
		/// <returns>A <see cref="Type"/> to which <paramref name="xName"/> resolves.</returns>
		protected abstract Type ParseXName(XName xName);

		/// <summary>
		/// Override this method to resolve cases where the same <see cref="XName"/> is being assigned to more than one 
		/// <see cref="Type"/>.
		/// </summary>
		/// <param name="xName">The <see cref="XName"/> being assigned.</param>
		/// <param name="type1">The <see cref="Type"/> that is currently assigned <paramref name="xName"/>.</param>
		/// <param name="type2">The <see cref="Type"/> that has just been resolved to <see cref="xName"/>.</param>
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
	}
}
