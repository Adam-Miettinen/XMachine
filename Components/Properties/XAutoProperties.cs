using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// An <see cref="XMachineComponent"/> that scans the <see cref="Type"/>s of newly-created <see cref="XType{T}"/>s
	/// and adds <see cref="XTypeComponent{T}"/>s that enable those properties to be read from and written to XML.
	/// </summary>
	public sealed class XAutoProperties : XMachineComponent
	{
		private static readonly IDictionary<Type, IEnumerable<string>> ignoredProperties =
			new Dictionary<Type, IEnumerable<string>>
			{
				{ typeof(List<>), new string[] { nameof(List<object>.Capacity) } }
			};

		internal XAutoProperties() { }

		/// <summary>
		/// Get or set a <see cref="MemberAccess"/> bitflag that determines the access levels that properties 
		/// must have for them to be read from or written to XML.
		/// </summary>
		public MemberAccess AccessIncluded { get; set; }

		/// <summary>
		/// Get the appropriate <see cref="MemberAccess"/> level for the given type <typeparamref name="T"/>.
		/// </summary>
		/// <returns>An instance of <see cref="MemberAccess"/>.</returns>
		public MemberAccess GetPropertyAccess<T>()
		{
			Type type = typeof(T);

			if (type.Assembly == typeof(object).Assembly)
			{
				return MemberAccess.PublicOnly;
			}
			else
			{
				XMachineAssemblyAttribute xma = type.Assembly.GetCustomAttribute<XMachineAssemblyAttribute>();
				return xma == null ? AccessIncluded : xma.PropertyAccess;
			}
		}

		protected override void OnCreateXType<T>(XType<T> xType)
		{
			Type type = typeof(T);
			if (type.IsArray || XDefaultTypes.IsDefaultType(type))
			{
				return;
			}

			// Determine correct access level

			MemberAccess pptyAccess = GetPropertyAccess<T>();

			// Scan for properties that meet the get-accessor requirements

			List<PropertyInfo> candidateProperties = new List<PropertyInfo>();

			foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x =>
					x.GetIndexParameters().Length == 0 &&
					(x.GetMethod.IsPublic ||
					(x.GetMethod.IsFamilyOrAssembly && pptyAccess.HasFlag(MemberAccess.ProtectedInternalGet)) ||
					(x.GetMethod.IsAssembly && pptyAccess.HasFlag(MemberAccess.InternalGet)) ||
					(x.GetMethod.IsFamily && pptyAccess.HasFlag(MemberAccess.ProtectedGet)) ||
					(x.GetMethod.IsFamilyAndAssembly && pptyAccess.HasFlag(MemberAccess.PrivateProtectedGet)) ||
					(x.GetMethod.IsPrivate && pptyAccess.HasFlag(MemberAccess.PrivateGet)))))
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

				candidateProperties.Add(pi);
			}

			// Register

			xType.Register(new XProperties<T>(xType, candidateProperties));
		}
	}
}
