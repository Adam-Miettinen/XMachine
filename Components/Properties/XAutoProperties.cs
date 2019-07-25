using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using XMachine.Components.Constructors;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// This extension automatically scans <see cref="Type"/>s for properties and adds 
	/// <see cref="XProperties{TType}"/>s to new <see cref="XType{TType}"/>s that allow
	/// them to read and write those properties.
	/// </summary>
	public sealed class XAutoProperties : XMachineComponent
	{
		private static readonly MethodInfo addPropertyMethod = typeof(XAutoProperties)
			.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
			.FirstOrDefault(x => x.IsGenericMethodDefinition && x.Name == nameof(AddAutoProperty));

		private static readonly IDictionary<Type, IEnumerable<string>> ignoredProperties =
			new Dictionary<Type, IEnumerable<string>>
			{
				{ typeof(List<>), new string[] { nameof(List<object>.Capacity) } }
			};

		internal XAutoProperties() { }

		/// <summary>
		/// Get or set a <see cref="PropertyAccess"/> bitflag that determines the access levels that object
		/// properties must have for them to be read from or written to XML. By default, properties must
		/// have public Get and Set methods. Altering this value will not change the treatment of primitive
		/// types, types defined in an assembly tagged with <see cref="XMachineAssemblyAttribute"/>, or types 
		/// that have already been reflected.
		/// </summary>
		public PropertyAccess AccessIncluded { get; set; }

		/// <summary>
		/// A predicate that will be applied to the <see cref="Type"/> represented by an <see cref="XType{TType}"/>
		/// before an <see cref="XProperties{TType}"/> is added.
		/// </summary>
		public Predicate<Type> TypesIncluded { get; set; }

		/// <summary>
		/// A predicate that will be applied to individual <see cref="PropertyInfo"/> objects before they are added
		/// to a new <see cref="XProperties{TType}"/>.
		/// </summary>
		public Predicate<PropertyInfo> PropertiesIncluded { get; set; }

		/// <summary>
		/// Scans the <see cref="Type"/> for properties and adds an <see cref="XProperties{TType}"/>.
		/// </summary>
		protected override void OnCreateXType<T>(XType<T> xType)
		{
			Type type = typeof(T);
			if (type.IsArray || XDefaultTypes.IsDefaultType(type) || TypesIncluded?.Invoke(type) == false)
			{
				return;
			}

			XProperties<T> extension = new XProperties<T>();

			// Determine correct access level

			PropertyAccess pptyAccess;

			if (type.Assembly == typeof(object).Assembly)
			{
				pptyAccess = PropertyAccess.PublicOnly;
			}
			else
			{
				XMachineAssemblyAttribute xma = type.Assembly.GetCustomAttribute<XMachineAssemblyAttribute>();
				pptyAccess = xma == null ? AccessIncluded : xma.PropertyAccess;
			}

			// Scan for properties

			foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x =>
					PropertiesIncluded?.Invoke(x) != false &&
					!x.IsXIgnored() &&
					x.GetIndexParameters().Length == 0 &&
					x.SetMethod != null &&
					(x.SetMethod.IsPublic ||
					(x.SetMethod.IsFamilyOrAssembly && pptyAccess.HasFlag(PropertyAccess.ProtectedInternalSet)) ||
					(x.SetMethod.IsAssembly && pptyAccess.HasFlag(PropertyAccess.InternalSet)) ||
					(x.SetMethod.IsFamily && pptyAccess.HasFlag(PropertyAccess.ProtectedSet)) ||
					(x.SetMethod.IsFamilyAndAssembly && pptyAccess.HasFlag(PropertyAccess.PrivateProtectedSet)) ||
					(x.SetMethod.IsPrivate && pptyAccess.HasFlag(PropertyAccess.PrivateSet))) &&
					(x.GetMethod.IsPublic ||
					(x.GetMethod.IsFamilyOrAssembly && pptyAccess.HasFlag(PropertyAccess.ProtectedInternalGet)) ||
					(x.GetMethod.IsAssembly && pptyAccess.HasFlag(PropertyAccess.InternalGet)) ||
					(x.GetMethod.IsFamily && pptyAccess.HasFlag(PropertyAccess.ProtectedGet)) ||
					(x.GetMethod.IsFamilyAndAssembly && pptyAccess.HasFlag(PropertyAccess.PrivateProtectedGet)) ||
					(x.GetMethod.IsPrivate && pptyAccess.HasFlag(PropertyAccess.PrivateGet)))))
			{
				Type declaring = pi.DeclaringType;

				if ((declaring.IsGenericType &&
					ignoredProperties.TryGetValue(declaring.GetGenericTypeDefinition(), out IEnumerable<string> ignored) &&
					ignored.Contains(pi.Name)) ||
					(!declaring.IsGenericType &&
					ignoredProperties.TryGetValue(declaring, out ignored) &&
					ignored.Contains(pi.Name)))
				{
					continue;
				}

				_ = addPropertyMethod.MakeGenericMethod(type, pi.PropertyType).Invoke(this, new object[] { extension, pi });
			}

			// Register

			xType.Register(extension);
		}

		/// <summary>
		/// Disable <see cref="XProperties{TType}"/> components for types that have an <see cref="XTexter{T}"/> or
		/// <see cref="XBuilderComponent{T}"/>. Also adds support for certain readonly properties.
		/// </summary>
		protected override void OnCreateXTypeLate<T>(XType<T> xType)
		{
			XProperties<T> properties = xType.Component<XProperties<T>>();
			if (properties == null)
			{
				return;
			}

			if (xType.Components<XTexter<T>>().Any(x => x.Enabled) ||
				xType.Components<XBuilderComponent<T>>().Any(x => x.Enabled))
			{
				properties.Enabled = false;
			}

			// If no parameterless constructor registered, scan for parameterized constructors

			XAutoConstructors autoConstructors = XComponents.Component<XAutoConstructors>();

			if (autoConstructors != null && xType.Component<XConstructor<T>>() == null)
			{
				Type type = typeof(T);

				if (!autoConstructors.ConstructorEligible(xType))
				{
					return;
				}
				ConstructorAccess ctorAccess = autoConstructors.GetAccessLevel<T>();

				ICollection<XPropertyBox<T>> boxedProperties = properties.Properties.Values;

				foreach (ConstructorInfo ci in type
					.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(x => !x.IsXIgnored() &&
						(x.IsPublic ||
						(x.IsFamilyOrAssembly && ctorAccess.HasFlag(ConstructorAccess.ProtectedInternal)) ||
						(x.IsAssembly && ctorAccess.HasFlag(ConstructorAccess.Internal)) ||
						(x.IsFamily && ctorAccess.HasFlag(ConstructorAccess.Protected)) ||
						(x.IsFamilyAndAssembly && ctorAccess.HasFlag(ConstructorAccess.PrivateProtected)) ||
						(x.IsPrivate && ctorAccess.HasFlag(ConstructorAccess.Private)))))
				{
					ParameterInfo[] parameters = ci.GetParameters();
					XPropertyBox<T>[] matches = new XPropertyBox<T>[parameters.Length];
					bool success = true;

					// See if we can match all parameters by name and type

					for (int i = 0; i < parameters.Length; i++)
					{
						XPropertyBox<T> match = boxedProperties.FirstOrDefault(x =>
							string.Compare(parameters[i].Name, x.Name.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0 &&
							parameters[i].ParameterType.IsAssignableFrom(x.PropertyType));

						if (match != null)
						{
							matches[i] = match;
						}
						else
						{
							success = false;
							break;
						}
					}

					// Create the constructor delegate if we matched everything

					if (success)
					{
						properties.ConstructWithNames = matches.Select(x => x.Name).ToArray();

						properties.ConstructorMethod = (props) => 
							(T)ci.Invoke(properties.ConstructWithNames.Select(x => props[x]).ToArray());
					}
				}
			}
		}

		private void AddAutoProperty<TType, TProperty>(XProperties<TType> properties, PropertyInfo pi) =>
			properties.Add(new XAutoProperty<TType, TProperty>(pi));
	}
}
