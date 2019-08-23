using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using XMachine.Components.Constructors;
using XMachine.Reflection;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// An <see cref="XTypeComponent{T}"/> that conducts the reading and writing of properties defined
	/// on an object of type <typeparamref name="TType"/>, as well as the construction of those
	/// objects using parameterized constructors.
	/// </summary>
	public sealed class XProperties<TType> : XTypeComponent<TType>
	{
		private static readonly MethodInfo addPropertyMethod = typeof(XProperties<TType>)
			.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
			.FirstOrDefault(x => x.IsGenericMethodDefinition && x.Name == nameof(AddAutoProperty));

		private readonly IDictionary<XName, XPropertyBox<TType>> properties =
			new Dictionary<XName, XPropertyBox<TType>>();

		private XName[] constructWithNames;

		private Func<IDictionary<XName, object>, TType> constructorMethod;

		private List<PropertyInfo> candidateProperties;

		internal XProperties(XType<TType> xType, List<PropertyInfo> properties) : base(xType) =>
			candidateProperties = properties;

		/// <summary>
		/// Add a new property of the given type, represented by an <see cref="XProperty{TType, TProperty}"/>.
		/// </summary>
		/// <param name="property">The <see cref="XProperty{TType, TProperty}"/> to add.</param>
		public void Add<TProperty>(XProperty<TType, TProperty> property)
		{
			if (property == null)
			{
				throw new ArgumentNullException(nameof(property));
			}
			if (property.Name == null)
			{
				throw new ArgumentException("Must have a valid XName", nameof(property));
			}
			if (properties.ContainsKey(property.Name))
			{
				throw new ArgumentException($"A property with XName {property.Name} already exists.", nameof(property));
			}
			properties.Add(property.Name, XPropertyBox<TType>.Box(property));
		}

		/// <summary>
		/// Add a new <see cref="XProperty{TType, TProperty}"/> representing the member identified by the
		/// given expression.
		/// </summary>
		/// <param name="expression">A <see cref="MemberExpression"/> identifying a field or property.</param>
		/// <param name="set">An optional delegate to override the default set accessor for the member.</param>
		/// <returns>The newly-created <see cref="XProperty{TType, TProperty}"/>.</returns>
		public XProperty<TType, TProperty> Add<TProperty>(Expression<Func<TType, TProperty>> expression, 
			Action<TType, TProperty> set = null)
		{
			MemberInfo mi = ReflectionTools.ParseFieldOrPropertyExpression(expression);

			if (mi == null)
			{
				throw new ArgumentException(
					$"Expression {expression} does not define a member of {typeof(TType).Name}",
					nameof(expression));
			}

			XName name = mi.GetXmlNameFromAttributes() ?? mi.Name;

			if (properties.ContainsKey(name))
			{
				throw new ArgumentException($"A property with XName {name} already exists.", nameof(expression));
			}

			if (set == null)
			{
				if (mi is FieldInfo fi)
				{
					set = (obj, value) => fi.SetValue(obj, value);
				}
				else
				{
					PropertyInfo pi = (PropertyInfo)mi;
					set = (obj, value) => pi.SetValue(obj, value);
				}
			}

			XProperty<TType, TProperty> property = new DelegatedXProperty<TType, TProperty>(
				name: name,
				get: expression.Compile(),
				set: set);
			properties.Add(property.Name, XPropertyBox<TType>.Box(property));

			return property;
		}

		/// <summary>
		/// Removes the given property.
		/// </summary>
		/// <param name="property">The <see cref="XProperty{TType, TProperty}"/> to remove.</param>
		public void Remove<TProperty>(XProperty<TType, TProperty> property)
		{
			if (property != null &&
				property.Name != null &&
				properties.TryGetValue(property.Name, out XPropertyBox<TType> existing) &&
				Equals(property, XPropertyBox<TType>.Unbox<TProperty>(existing)))
			{
				_ = properties.Remove(property.Name);
			}
		}

		/// <summary>
		/// Removes the given property.
		/// </summary>
		/// <param name="propertyName">The <see cref="XName"/> of the property to remove.</param>
		public void Remove(XName propertyName)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException("XName cannot be null");
			}
			_ = properties.Remove(propertyName);
		}

		/// <summary>
		/// Get the property with the type <typeparamref name="TProperty"/> and with the given <see cref="XName"/>.
		/// </summary>
		/// <param name="name">The <see cref="XName"/> of the <see cref="XProperty{TType, TProperty}"/> to get.</param>
		/// <returns>An <see cref="XProperty{TType, TProperty}"/> instance, or null.</returns>
		public XProperty<TType, TProperty> Get<TProperty>(XName name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("XName cannot be null");
			}
			return properties.TryGetValue(name, out XPropertyBox<TType> value)
				? XPropertyBox<TType>.Unbox<TProperty>(value)
				: null;
		}

		/// <summary>
		/// Get the property identified by the given LINQ expression if it exists on this object and has
		/// the default <see cref="XName"/>.
		/// </summary>
		/// <param name="expression">A LINQ expression showing the access of the requested property by an object
		/// of type <typeparamref name="TType"/>.</param>
		/// <returns>An <see cref="XProperty{TType, TProperty}"/> instance, or null.</returns>
		public XProperty<TType, TProperty> Get<TProperty>(Expression<Func<TType, TProperty>> expression)
		{
			MemberInfo mi = ReflectionTools.ParseFieldOrPropertyExpression(expression);

			if (mi != null)
			{
				if (properties.TryGetValue(mi.GetXmlNameFromAttributes() ?? mi.Name, out XPropertyBox<TType> value))
				{
					XProperty<TType, TProperty> property = XPropertyBox<TType>.Unbox<TProperty>(value);
					if (property != null)
					{
						return property;
					}
				}
			}

			throw new ArgumentException(
				$"Expression {expression} does not define an {nameof(XProperty<TType, TProperty>)} on {nameof(XProperties<TType>)}",
				nameof(expression));
		}

		/// <summary>
		/// Clear the parameterized constructor.
		/// </summary>
		public void ConstructWith()
		{
			constructWithNames = null;
			constructorMethod = null;
		}

		/// <summary>
		/// Instructs the component to deserialize the given property before construction, and to use either the given
		/// constructor or a constructor that takes a single parameter of type <typeparamref name="TArg1"/> with a 
		/// name equal to the given property's (ignoring case).
		/// </summary>
		/// <param name="expression1">A LINQ expression identifying the first parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1>(Expression<Func<TType, TArg1>> expression1,
			Func<TArg1, TType> constructor = null)
		{
			XName arg1 = GetOrAdd(expression1)?.Name;
			if (arg1 == null)
			{
				throw new ArgumentException($"The given expression does not identify a property on the {nameof(XType<TType>)}.");
			}

			ConstructWith(arg1, constructor);
		}

		/// <summary>
		/// Instructs the component to deserialize the given property before construction, and to use either the given
		/// constructor or a constructor that takes a single parameter of type <typeparamref name="TArg1"/> with a 
		/// name equal to the given property's (ignoring case).
		/// </summary>
		/// <param name="property1">An <see cref="XProperty{TType, TProperty}"/> to use as the first parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1>(XProperty<TType, TArg1> property1,
			Func<TArg1, TType> constructor = null)
		{
			XName arg1 = property1?.Name;
			if (arg1 == null ||
				!properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
				property1 != XPropertyBox<TType>.Unbox<TArg1>(arg1Existing))
			{
				throw new ArgumentException($"The given property is not defined on {nameof(XType<TType>)}.");
			}

			ConstructWith(arg1, constructor);
		}

		private void ConstructWith<TArg1>(XName property1, Func<TArg1, TType> constructor)
		{
			if (constructor == null)
			{
				foreach (ConstructorInfo ci in typeof(TType)
					.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					ParameterInfo[] parameters = ci.GetParameters();
					if (parameters.Length == 1 &&
						parameters[0].ParameterType.IsAssignableFrom(typeof(TArg1)) &&
						string.Compare(parameters[0].Name, property1.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0)
					{
						constructor = (arg1) => (TType)ci.Invoke(new object[] { arg1 });
					}
				}

				if (constructor == null)
				{
					throw new InvalidOperationException(
						$"Unable to find a constructor on {nameof(XType<TType>)} with matching parameters");
				}
			}

			constructWithNames = new XName[] { property1 };
			constructorMethod = (args) => constructor((TArg1)args[property1]);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes two parameters that match the order, types and names (case insensitive) 
		/// of the two properties given.
		/// </summary>
		/// <param name="expression1">A LINQ expression identifying the first parameter.</param>
		/// <param name="expression2">A LINQ expression identifying the second parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1, TArg2>(Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			XName arg1 = GetOrAdd(expression1)?.Name, 
				arg2 = GetOrAdd(expression2)?.Name;
			if (arg1 == null || arg2 == null)
			{
				throw new ArgumentException($"The given expression does not identify a property on the {nameof(XType<TType>)}.");
			}

			ConstructWith(arg1, arg2, constructor);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes two parameters that match the order, types and names (case insensitive) 
		/// of the two properties given.
		/// </summary>
		/// <param name="property1">An <see cref="XProperty{TType, TProperty}"/> to use as the first parameter.</param>
		/// <param name="property2">An <see cref="XProperty{TType, TProperty}"/> to use as the second parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1, TArg2>(XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			XName arg1 = property1?.Name, arg2 = property2?.Name;
			if (arg1 == null ||
				!properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
				property1 != XPropertyBox<TType>.Unbox<TArg1>(arg1Existing) ||
				arg2 == null ||
				!properties.TryGetValue(arg2, out XPropertyBox<TType> arg2Existing) ||
				property2 != XPropertyBox<TType>.Unbox<TArg2>(arg2Existing))
			{
				throw new ArgumentException($"The given property is not defined on {nameof(XType<TType>)}.");
			}

			ConstructWith(arg1, arg2, constructor);
		}

		private void ConstructWith<TArg1, TArg2>(XName property1, XName property2, Func<TArg1, TArg2, TType> constructor)
		{
			if (constructor == null)
			{
				foreach (ConstructorInfo ci in typeof(TType)
					.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					ParameterInfo[] parameters = ci.GetParameters();
					if (parameters.Length == 2 &&
						parameters[0].ParameterType.IsAssignableFrom(typeof(TArg1)) &&
						parameters[1].ParameterType.IsAssignableFrom(typeof(TArg2)) &&
						string.Compare(parameters[0].Name, property1.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0 &&
						string.Compare(parameters[1].Name, property2.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0)
					{
						constructor = (arg1, arg2) => (TType)ci.Invoke(new object[] { arg1, arg2 });
					}
				}

				if (constructor == null)
				{
					throw new InvalidOperationException(
						$"Unable to find a constructor on {nameof(XType<TType>)} with matching parameters");
				}
			}

			constructWithNames = new XName[] { property1, property2 };
			constructorMethod = (args) => constructor((TArg1)args[property1], (TArg2)args[property2]);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes three parameters that match the order, types and names (case insensitive) 
		/// of the three properties given.
		/// </summary>
		/// <param name="expression1">A LINQ expression identifying the first parameter.</param>
		/// <param name="expression2">A LINQ expression identifying the second parameter.</param>
		/// <param name="expression3">A LINQ expression identifying the third parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1, TArg2, TArg3>(Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			XName arg1 = GetOrAdd(expression1)?.Name,
				arg2 = GetOrAdd(expression2)?.Name,
				arg3 = GetOrAdd(expression3)?.Name;

			if (arg1 == null || arg2 == null || arg3 == null)
			{
				throw new ArgumentException($"The given expression does not identify a property on the {nameof(XType<TType>)}.");
			}

			ConstructWith(arg1, arg2, arg3, constructor);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes three parameters that match the order, types and names (case insensitive) 
		/// of the three properties given.
		/// </summary>
		/// <param name="property1">An <see cref="XProperty{TType, TProperty}"/> to use as the first parameter.</param>
		/// <param name="property2">An <see cref="XProperty{TType, TProperty}"/> to use as the second parameter.</param>
		/// <param name="property3">An <see cref="XProperty{TType, TProperty}"/> to use as the third parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1, TArg2, TArg3>(XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			XName arg1 = property1?.Name, arg2 = property2?.Name, arg3 = property3?.Name;
			if (arg1 == null ||
				!properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
				property1 != XPropertyBox<TType>.Unbox<TArg1>(arg1Existing) ||
				arg2 == null ||
				!properties.TryGetValue(arg2, out XPropertyBox<TType> arg2Existing) ||
				property2 != XPropertyBox<TType>.Unbox<TArg2>(arg2Existing) ||
				arg3 == null ||
				!properties.TryGetValue(arg3, out XPropertyBox<TType> arg3Existing) ||
				property3 != XPropertyBox<TType>.Unbox<TArg3>(arg3Existing))
			{
				throw new ArgumentException($"The given property is not defined on {nameof(XType<TType>)}.");
			}

			ConstructWith(arg1, arg2, arg3, constructor);
		}

		private void ConstructWith<TArg1, TArg2, TArg3>(XName property1, XName property2, XName property3,
			Func<TArg1, TArg2, TArg3, TType> constructor)
		{
			if (constructor == null)
			{
				foreach (ConstructorInfo ci in typeof(TType)
					.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					ParameterInfo[] parameters = ci.GetParameters();
					if (parameters.Length == 3 &&
						parameters[0].ParameterType.IsAssignableFrom(typeof(TArg1)) &&
						parameters[1].ParameterType.IsAssignableFrom(typeof(TArg2)) &&
						parameters[2].ParameterType.IsAssignableFrom(typeof(TArg3)) &&
						string.Compare(parameters[0].Name, property1.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0 &&
						string.Compare(parameters[1].Name, property2.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0 &&
						string.Compare(parameters[2].Name, property3.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0)
					{
						constructor = (arg1, arg2, arg3) => (TType)ci.Invoke(new object[] { arg1, arg2, arg3 });
					}
				}

				if (constructor == null)
				{
					throw new InvalidOperationException(
						$"Unable to find a constructor on {nameof(XType<TType>)} with matching parameters");
				}
			}

			constructWithNames = new XName[] { property1, property2, property3 };
			constructorMethod = (args) => constructor((TArg1)args[property1], (TArg2)args[property2], (TArg3)args[property3]);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes four parameters that match the order, types and names (case insensitive) 
		/// of the four properties given.
		/// </summary>
		/// <param name="expression1">A LINQ expression identifying the first parameter.</param>
		/// <param name="expression2">A LINQ expression identifying the second parameter.</param>
		/// <param name="expression3">A LINQ expression identifying the third parameter.</param>
		/// <param name="expression4">A LINQ expression identifying the fourth parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1, TArg2, TArg3, TArg4>(Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Expression<Func<TType, TArg4>> expression4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			XName property1 = GetOrAdd(expression1)?.Name,
				property2 = GetOrAdd(expression2)?.Name,
				property3 = GetOrAdd(expression3)?.Name,
				property4 = GetOrAdd(expression4)?.Name;

			if (property1 == null || property2 == null || property3 == null || property4 == null)
			{
				throw new ArgumentException($"The given expression does not identify a property on the {nameof(XType<TType>)}.");
			}

			ConstructWith(property1, property2, property3, property4, constructor);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes four parameters that match the order, types and names (case insensitive) 
		/// of the four properties given.
		/// </summary>
		/// <param name="property1">An <see cref="XProperty{TType, TProperty}"/> to use as the first parameter.</param>
		/// <param name="property2">An <see cref="XProperty{TType, TProperty}"/> to use as the second parameter.</param>
		/// <param name="property3">An <see cref="XProperty{TType, TProperty}"/> to use as the third parameter.</param>
		/// <param name="property4">An <see cref="XProperty{TType, TProperty}"/> to use as the fourth parameter.</param>
		/// <param name="constructor">An optional delegate to use as a constructor.</param>
		public void ConstructWith<TArg1, TArg2, TArg3, TArg4>(XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			XProperty<TType, TArg4> property4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			XName arg1 = property1?.Name, arg2 = property2?.Name, arg3 = property3?.Name, arg4 = property4?.Name;
			if (arg1 == null ||
				!properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
				property1 != XPropertyBox<TType>.Unbox<TArg1>(arg1Existing) ||
				arg2 == null ||
				!properties.TryGetValue(arg2, out XPropertyBox<TType> arg2Existing) ||
				property2 != XPropertyBox<TType>.Unbox<TArg2>(arg2Existing) ||
				arg3 == null ||
				!properties.TryGetValue(arg3, out XPropertyBox<TType> arg3Existing) ||
				property3 != XPropertyBox<TType>.Unbox<TArg3>(arg3Existing) ||
				arg4 == null ||
				!properties.TryGetValue(arg4, out XPropertyBox<TType> arg4Existing) ||
				property4 != XPropertyBox<TType>.Unbox<TArg4>(arg4Existing))
			{
				throw new ArgumentException($"The given property is not defined on {nameof(XType<TType>)}.");
			}

			ConstructWith(arg1, arg2, arg3, arg4, constructor);
		}

		private void ConstructWith<TArg1, TArg2, TArg3, TArg4>(XName property1, XName property2, XName property3, XName property4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			if (constructor == null)
			{
				foreach (ConstructorInfo ci in typeof(TType)
					.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					ParameterInfo[] parameters = ci.GetParameters();
					if (parameters.Length == 4 &&
						parameters[0].ParameterType.IsAssignableFrom(typeof(TArg1)) &&
						parameters[1].ParameterType.IsAssignableFrom(typeof(TArg2)) &&
						parameters[2].ParameterType.IsAssignableFrom(typeof(TArg3)) &&
						parameters[2].ParameterType.IsAssignableFrom(typeof(TArg4)) &&
						string.Compare(parameters[0].Name, property1.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0 &&
						string.Compare(parameters[1].Name, property2.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0 &&
						string.Compare(parameters[2].Name, property3.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0 &&
						string.Compare(parameters[3].Name, property4.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0)
					{
						constructor = (arg1, arg2, arg3, arg4) => (TType)ci.Invoke(new object[] { arg1, arg2, arg3, arg4 });
					}
				}

				if (constructor == null)
				{
					throw new InvalidOperationException(
						$"Unable to find a constructor on {nameof(XType<TType>)} with matching parameters");
				}
			}

			constructWithNames = new XName[] { property1, property2, property3, property4 };
			constructorMethod = (args) => constructor((TArg1)args[property1], (TArg2)args[property2],
				(TArg3)args[property3], (TArg4)args[property4]);
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();

			if (XType.Components<XTexter<TType>>().Any(x => x.Enabled) ||
				XType.Components<XBuilderComponent<TType>>().Any(x => x.Enabled))
			{
				Enabled = false;
			}

			if (candidateProperties?.Any() != true)
			{
				candidateProperties = null;
				return;
			}

			// If no parameterless constructor registered, scan for parameterized constructors

			if (XType.Component<XConstructor<TType>>() == null)
			{
				Type type = typeof(TType);

				XAutoConstructors autoConstructors = XComponents.Component<XAutoConstructors>();
				MethodAccess ctorAccess = autoConstructors.GetAccessLevel<TType>();

				if (autoConstructors.ConstructorEligible(XType))
				{
					foreach (ConstructorInfo ci in type
						.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						.Where(x => x.IsPublic ||
							(x.IsFamilyOrAssembly && ctorAccess.HasFlag(MethodAccess.ProtectedInternal)) ||
							(x.IsAssembly && ctorAccess.HasFlag(MethodAccess.Internal)) ||
							(x.IsFamily && ctorAccess.HasFlag(MethodAccess.Protected)) ||
							(x.IsFamilyAndAssembly && ctorAccess.HasFlag(MethodAccess.PrivateProtected)) ||
							(x.IsPrivate && ctorAccess.HasFlag(MethodAccess.Private))))
					{
						ParameterInfo[] parameters = ci.GetParameters();

						PropertyInfo[] matches = new PropertyInfo[parameters.Length];
						bool success = true;

						// See if we can match all parameters by name and type

						for (int i = 0; i < parameters.Length; i++)
						{
							PropertyInfo match = candidateProperties.FirstOrDefault(x =>
								string.Compare(
									parameters[i].Name,
									x.GetXmlNameFromAttributes() ?? x.Name,
									StringComparison.InvariantCultureIgnoreCase) == 0 &&
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
							constructWithNames = new XName[matches.Length];

							for (int i = 0; i < matches.Length; i++)
							{
								XPropertyBox<TType> box = AddAutoProperty(matches[i]);
								constructWithNames[i] = box.Name;
								properties.Add(box.Name, box);
								_ = candidateProperties.Remove(matches[i]);
							}

							constructorMethod = (props) =>
								(TType)ci.Invoke(constructWithNames.Select(x => props[x]).ToArray());
						}
					}
				}
			}

			// Test remaining properties for set accessors, add them if possible

			MemberAccess propertyAccess = XComponents.Component<XAutoProperties>().GetPropertyAccess<TType>();

			foreach (PropertyInfo pi in candidateProperties)
			{
				if (pi.SetMethod != null &&
					(pi.SetMethod.IsPublic ||
					(pi.SetMethod.IsFamilyOrAssembly && propertyAccess.HasFlag(MemberAccess.ProtectedInternalSet)) ||
					(pi.SetMethod.IsAssembly && propertyAccess.HasFlag(MemberAccess.InternalSet)) ||
					(pi.SetMethod.IsFamily && propertyAccess.HasFlag(MemberAccess.ProtectedSet)) ||
					(pi.SetMethod.IsFamilyAndAssembly && propertyAccess.HasFlag(MemberAccess.PrivateProtectedSet)) ||
					(pi.SetMethod.IsPrivate && propertyAccess.HasFlag(MemberAccess.PrivateSet))))
				{
					XPropertyBox<TType> box = AddAutoProperty(pi);
					properties.Add(box.Name, box);
				}
			}

			candidateProperties = null;
		}

		protected override void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<TType> objectBuilder, 
			XObjectArgs args)
		{
			// Check if we should construct from our owner

			if (!objectBuilder.IsConstructed && 
				args is XPropertyArgs xpArgs && 
				xpArgs.Hints.HasFlag(ObjectHints.ConstructedByOwner))
			{
				reader.AddTask(this, () =>
				{
					if (xpArgs.GetFromOwner(out object obj))
					{
						objectBuilder.Object = (TType)obj;
						return true;
					}
					return false;
				});
			}

			// Start reading properties

			IDictionary<XName, object> propertyValues = new Dictionary<XName, object>();

			XObjectArgs preparePropertyForReading(XPropertyBox<TType> prop)
			{
				propertyValues.Add(prop.Name, XmlTools.PlaceholderObject);

				if (prop.WithArgs != null)
				{
					if (prop.WithArgs.Hints.HasFlag(ObjectHints.ConstructedByOwner))
					{
						return new XPropertyArgs(prop.WithArgs, () =>
							objectBuilder.IsConstructed
								? new Tuple<object, bool>(prop.Get(objectBuilder.Object), true)
								: new Tuple<object, bool>(null, false));
					}

					return prop.WithArgs;
				}

				return XObjectArgs.DefaultIgnoreElementName;
			}

			// Read properties from inner text

			foreach (XPropertyBox<TType> textProperty in properties.Values
				.Where(x => x.WriteAs == PropertyWriteMode.Text))
			{
				reader.Read(element, textProperty.PropertyType, x =>
					{
						propertyValues[textProperty.Name] = x;
						return true;
					},
					preparePropertyForReading(textProperty));
			}

			// Read properties from attributes

			foreach (XAttribute attribute in element.Attributes())
			{
				if (properties.TryGetValue(attribute.Name, out XPropertyBox<TType> property) &&
					!propertyValues.ContainsKey(property.Name))
				{
					reader.Read(attribute, property.PropertyType, x =>
						{
							propertyValues[property.Name] = x;
							return true;
						},
						preparePropertyForReading(property));
				}
			}

			// Read properties from elements

			foreach (XElement subElement in element.Elements())
			{
				if (properties.TryGetValue(subElement.Name, out XPropertyBox<TType> property) &&
					!propertyValues.ContainsKey(property.Name))
				{
					reader.Read(subElement, property.PropertyType, x =>
						{
							propertyValues[property.Name] = x;
							return true;
						},
						preparePropertyForReading(property));
				}
			}

			if (propertyValues.Count == 0)
			{
				return;
			}

			// Use a parameterized constructor

			if (!objectBuilder.IsConstructed && 
				(args == null || !args.Hints.HasFlag(ObjectHints.DontConstruct)) && 
				constructWithNames != null)
			{
				// Use default values if constructor parameters weren't found in XML

				foreach (XName cwName in constructWithNames)
				{
					if (!propertyValues.ContainsKey(cwName))
					{
						propertyValues.Add(cwName, null);
					}
				}

				// Schedule construction when ready

				reader.AddTask(this, () =>
				{
					if (constructWithNames.All(x => !ReferenceEquals(propertyValues[x], XmlTools.PlaceholderObject)))
					{
						try
						{
							objectBuilder.Object = constructorMethod(propertyValues);
						}
						finally
						{
							foreach (XName cw in constructWithNames)
							{
								_ = propertyValues.Remove(cw);
							}
						}

						return true;
					}
					return false;
				});
			}

			// Add a task to set property values once constructed

			reader.AddTask(this, () =>
			{
				if (objectBuilder.IsConstructed)
				{
					if (propertyValues.Count > 0)
					{
						XName[] keysToSet = propertyValues.Keys.ToArray();

						foreach (XName ppty in keysToSet)
						{
							XPropertyBox<TType> box = properties[ppty];
							object value = propertyValues[ppty];

							if (!ReferenceEquals(value, XmlTools.PlaceholderObject))
							{
								try
								{
									if (box.WithArgs?.Hints.HasFlag(ObjectHints.ConstructedByOwner) != true)
									{
										properties[ppty].Set(objectBuilder.Object, value);
									}
								}
								finally
								{
									_ = propertyValues.Remove(ppty);
								}
							}
						}

						return propertyValues.Count == 0;
					}
					else
					{
						return true;
					}
				}
				return false;
			});
		}

		protected override bool OnWrite(IXWriteOperation writer, TType obj, XElement element, XObjectArgs args)
		{
			foreach (XPropertyBox<TType> property in properties.Values)
			{
				if (property.WriteIf?.Invoke(obj) == false)
				{
					continue;
				}

				object value = property.Get(obj);

				if (property.WriteAs == PropertyWriteMode.Attribute)
				{
					XAttribute propertyAttribute = writer.WriteAttribute(value, property.Name, property.WithArgs);
					if (propertyAttribute != null)
					{
						element.Add(propertyAttribute);
					}
				}
				else if (property.WriteAs == PropertyWriteMode.Text)
				{
					_ = writer.WriteTo(element, value, property.PropertyType, property.WithArgs);
				}
				else
				{
					element.Add(writer.WriteTo(new XElement(property.Name), value, property.PropertyType, property.WithArgs));
				}
			}

			return false;
		}

		private XProperty<TType, TProperty> GetOrAdd<TProperty>(Expression<Func<TType, TProperty>> expression)
		{
			MemberInfo mi = ReflectionTools.ParseFieldOrPropertyExpression(expression);

			if (mi != null)
			{
				XName name = mi.GetXmlNameFromAttributes() ?? mi.Name;

				// Try to get

				if (properties.TryGetValue(name, out XPropertyBox<TType> existing))
				{
					XProperty<TType, TProperty> unboxed = XPropertyBox<TType>.Unbox<TProperty>(existing);
					if (unboxed != null)
					{
						return unboxed;
					}
				}

				// Try to add

				Action<TType, TProperty> set;
				if (mi is FieldInfo fi)
				{
					set = (obj, value) => fi.SetValue(obj, value);
				}
				else
				{
					PropertyInfo pi = (PropertyInfo)mi;
					set = (obj, value) => pi.SetValue(obj, value);
				}

				XProperty<TType, TProperty> property = new DelegatedXProperty<TType, TProperty>(
					name: name,
					get: expression.Compile(),
					set: set);
				properties.Add(property.Name, XPropertyBox<TType>.Box(property));

				return property;
			}

			throw new ArgumentException(
				$"Expression {expression} does not define a member of {typeof(TType).Name}",
				nameof(expression));
		}

		private XPropertyBox<TType> AddAutoProperty(PropertyInfo pi) =>
			(XPropertyBox<TType>)addPropertyMethod.MakeGenericMethod(pi.PropertyType).Invoke(this, new object[] { pi });

		private XPropertyBox<TType> AddAutoProperty<TProperty>(PropertyInfo pi) =>
			XPropertyBox<TType>.Box(new XAutoProperty<TType, TProperty>(pi));
	}
}
