using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using XMachine.Reflection;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// Enables the reading and writing of object properties by <see cref="XType{TType}"/>s.
	/// </summary>
	public sealed class XProperties<TType> : XTypeComponent<TType>
	{
		private static readonly object placeHolder = new object();

		/// <summary>
		/// An optional predicate affecting all properties.
		/// </summary>
		public Predicate<TType> WriteIf { get; set; }

		internal IDictionary<XName, XPropertyBox<TType>> Properties { get; } =
			new Dictionary<XName, XPropertyBox<TType>>();

		internal XName[] ConstructWithNames { get; set; }

		internal Func<IDictionary<XName, object>, TType> ConstructorMethod { get; set; }

		/// <summary>
		/// Add a new property of the given type, represented by an <see cref="XProperty{TType, TProperty}"/>
		/// object.
		/// </summary>
		public void Add<TProperty>(XProperty<TType, TProperty> property)
		{
			if (property == null)
			{
				throw new ArgumentNullException("Cannot add null property");
			}
			if (property.Name == null)
			{
				throw new ArgumentException("Property must have a valid XName");
			}
			if (Properties.ContainsKey(property.Name))
			{
				throw new ArgumentException($"Component already has a property with XName {property.Name}.");
			}
			Properties.Add(property.Name, XPropertyBox<TType>.Box(property));
		}

		/// <summary>
		/// Removes the given property.
		/// </summary>
		public void Remove<TProperty>(XProperty<TType, TProperty> property)
		{
			if (property != null &&
				property.Name != null &&
				Properties.TryGetValue(property.Name, out XPropertyBox<TType> existing) &&
				Equals(property, XPropertyBox<TType>.Unbox<TProperty>(existing)))
			{
				_ = Properties.Remove(property.Name);
			}
		}

		/// <summary>
		/// Removes the property with the given <see cref="XName"/>.
		/// </summary>
		public void Remove(XName propertyName)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException("XName cannot be null");
			}
			_ = Properties.Remove(propertyName);
		}

		/// <summary>
		/// Retrieves the property of the given type and with the given <see cref="XName"/>.
		/// </summary>
		public XProperty<TType, TProperty> Get<TProperty>(XName name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("XName cannot be null");
			}
			return Properties.TryGetValue(name, out XPropertyBox<TType> value)
				? XPropertyBox<TType>.Unbox<TProperty>(value)
				: null;
		}

		/// <summary>
		/// Retrieve the property identified by the given LINQ expression if it exists on this object and has
		/// the default <see cref="XName"/>.
		/// </summary>
		public XProperty<TType, TProperty> Get<TProperty>(Expression<Func<TType, TProperty>> expression)
		{
			MemberInfo mi = ReflectionTools.ParseFieldOrPropertyExpression(expression);

			if (mi != null)
			{
				if (Properties.TryGetValue(mi.GetXmlNameFromAttributes() ?? mi.Name, out XPropertyBox<TType> value))
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
		/// Removes any parametered constructor from the component.
		/// </summary>
		public void ConstructWith()
		{
			ConstructWithNames = null;
			ConstructorMethod = null;
		}

		/// <summary>
		/// Instructs the component to deserialize the given property before construction, and to use either the given
		/// constructor or a constructor that takes a single parameter of type <typeparamref name="TArg1"/> with a 
		/// name equal to the given property's (ignoring case).
		/// </summary>
		public void ConstructWith<TArg1>(Expression<Func<TType, TArg1>> expression1,
			Func<TArg1, TType> constructor = null)
		{
			XName arg1 = Get(expression1)?.Name;
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
		public void ConstructWith<TArg1>(XProperty<TType, TArg1> property1,
			Func<TArg1, TType> constructor = null)
		{
			XName arg1 = property1?.Name;
			if (arg1 == null ||
				!Properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
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

			ConstructWithNames = new XName[] { property1 };
			ConstructorMethod = (args) => constructor((TArg1)args[property1]);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes two parameters that match the order, types and names (case insensitive) 
		/// of the two properties given.
		/// </summary>
		public void ConstructWith<TArg1, TArg2>(Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			XName arg1 = Get(expression1)?.Name, arg2 = Get(expression2)?.Name;
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
		public void ConstructWith<TArg1, TArg2>(XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			XName arg1 = property1?.Name, arg2 = property2?.Name;
			if (arg1 == null ||
				!Properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
				property1 != XPropertyBox<TType>.Unbox<TArg1>(arg1Existing) ||
				arg2 == null ||
				!Properties.TryGetValue(arg2, out XPropertyBox<TType> arg2Existing) ||
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

			ConstructWithNames = new XName[] { property1, property2 };
			ConstructorMethod = (args) => constructor((TArg1)args[property1], (TArg2)args[property2]);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes three parameters that match the order, types and names (case insensitive) 
		/// of the three properties given.
		/// </summary>
		public void ConstructWith<TArg1, TArg2, TArg3>(Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			XName arg1 = Get(expression1)?.Name,
				arg2 = Get(expression2)?.Name,
				arg3 = Get(expression3)?.Name;

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
		public void ConstructWith<TArg1, TArg2, TArg3>(XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			XName arg1 = property1?.Name, arg2 = property2?.Name, arg3 = property3?.Name;
			if (arg1 == null ||
				!Properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
				property1 != XPropertyBox<TType>.Unbox<TArg1>(arg1Existing) ||
				arg2 == null ||
				!Properties.TryGetValue(arg2, out XPropertyBox<TType> arg2Existing) ||
				property2 != XPropertyBox<TType>.Unbox<TArg2>(arg2Existing) ||
				arg3 == null ||
				!Properties.TryGetValue(arg3, out XPropertyBox<TType> arg3Existing) ||
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

			ConstructWithNames = new XName[] { property1, property2, property3 };
			ConstructorMethod = (args) => constructor((TArg1)args[property1], (TArg2)args[property2], (TArg3)args[property3]);
		}

		/// <summary>
		/// Instructs the component to deserialize the given properties before construction, and to use either the given
		/// constructor or a constructor that takes four parameters that match the order, types and names (case insensitive) 
		/// of the four properties given.
		/// </summary>
		public void ConstructWith<TArg1, TArg2, TArg3, TArg4>(Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Expression<Func<TType, TArg4>> expression4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			XName property1 = Get(expression1)?.Name,
				property2 = Get(expression2)?.Name,
				property3 = Get(expression3)?.Name,
				property4 = Get(expression4)?.Name;

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
		public void ConstructWith<TArg1, TArg2, TArg3, TArg4>(XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			XProperty<TType, TArg4> property4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			XName arg1 = property1?.Name, arg2 = property2?.Name, arg3 = property3?.Name, arg4 = property4?.Name;
			if (arg1 == null ||
				!Properties.TryGetValue(arg1, out XPropertyBox<TType> arg1Existing) ||
				property1 != XPropertyBox<TType>.Unbox<TArg1>(arg1Existing) ||
				arg2 == null ||
				!Properties.TryGetValue(arg2, out XPropertyBox<TType> arg2Existing) ||
				property2 != XPropertyBox<TType>.Unbox<TArg2>(arg2Existing) ||
				arg3 == null ||
				!Properties.TryGetValue(arg3, out XPropertyBox<TType> arg3Existing) ||
				property3 != XPropertyBox<TType>.Unbox<TArg3>(arg3Existing) ||
				arg4 == null ||
				!Properties.TryGetValue(arg4, out XPropertyBox<TType> arg4Existing) ||
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

			ConstructWithNames = new XName[] { property1, property2, property3, property4 };
			ConstructorMethod = (args) => constructor((TArg1)args[property1], (TArg2)args[property2],
				(TArg3)args[property3], (TArg4)args[property4]);
		}

		/// <summary>
		/// Reads XML elements and attributes as properties and assign thems to the constructed object.
		/// </summary>
		protected override void OnBuild(XType<TType> xType, IXReadOperation reader, XElement element, ObjectBuilder<TType> objectBuilder)
		{
			IDictionary<XName, object> propertyValues = new Dictionary<XName, object>();

			foreach (XPropertyBox<TType> textProperty in Properties.Values
				.Where(x => x.WriteAs == PropertyWriteMode.Text))
			{
				propertyValues.Add(textProperty.Name, placeHolder);
				reader.Read(element, textProperty.PropertyType, x =>
				{
					propertyValues[textProperty.Name] = x;
					return true;
				},
				ReaderHints.IgnoreElementName);
			}

			foreach (XAttribute attribute in element.Attributes())
			{
				if (Properties.TryGetValue(attribute.Name, out XPropertyBox<TType> property) &&
					!propertyValues.ContainsKey(property.Name))
				{
					propertyValues.Add(property.Name, placeHolder);

					reader.Read(attribute, property.PropertyType, x =>
					{
						propertyValues[property.Name] = x;
						return true;
					});
				}
			}

			foreach (XElement subElement in element.Elements())
			{
				if (Properties.TryGetValue(subElement.Name, out XPropertyBox<TType> property) &&
					!propertyValues.ContainsKey(property.Name))
				{
					propertyValues.Add(property.Name, placeHolder);

					reader.Read(subElement, property.PropertyType, x =>
					{
						propertyValues[property.Name] = x;
						return true;
					},
					ReaderHints.IgnoreElementName);
				}
			}

			if (propertyValues.Count == 0)
			{
				return;
			}

			// With parameterized constructor

			if (ConstructWithNames != null)
			{
				// Use default values if constructor parameters weren't found in XML

				foreach (XName cwName in ConstructWithNames)
				{
					if (!propertyValues.ContainsKey(cwName))
					{
						propertyValues.Add(cwName, null);
					}
				}

				// Schedule construction when ready

				reader.AddTask(this, () =>
				{
					if (ConstructWithNames.All(x => !ReferenceEquals(propertyValues[x], placeHolder)))
					{
						try
						{
							objectBuilder.Object = ConstructorMethod(propertyValues);
						}
						finally
						{
							foreach (XName cw in ConstructWithNames)
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
						XName[] keysToSet = new XName[propertyValues.Count];
						propertyValues.Keys.CopyTo(keysToSet, 0);

						foreach (XName ppty in keysToSet)
						{
							object value = propertyValues[ppty];
							if (!ReferenceEquals(value, placeHolder))
							{
								try
								{
									Properties[ppty].Set(objectBuilder.Object, value);
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

		/// <summary>
		/// Retrieves properties from the object and writes them as XML.
		/// </summary>
		protected override bool OnWrite(XType<TType> xType, IXWriteOperation writer, TType obj, XElement element)
		{
			if (WriteIf?.Invoke(obj) == false)
			{
				return false;
			}

			foreach (XPropertyBox<TType> property in Properties.Values)
			{
				if (property.WriteIf?.Invoke(obj) == false)
				{
					continue;
				}

				object value = property.Get(obj);

				if (property.WriteAs == PropertyWriteMode.Attribute)
				{
					XAttribute propertyAttribute = writer.WriteAttribute(value, property.Name);
					if (propertyAttribute != null)
					{
						element.Add(propertyAttribute);
					}
				}
				else if (property.WriteAs == PropertyWriteMode.Text)
				{
					_ = writer.WriteTo(element, value, property.PropertyType);
				}
				else
				{
					element.Add(writer.WriteTo(new XElement(property.Name), value, property.PropertyType));
				}
			}

			return false;
		}
	}
}
