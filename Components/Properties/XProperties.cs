﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// Enables the reading and writing of object properties by <see cref="XType{TType}"/>s.
	/// </summary>
	public sealed class XProperties<TType> : XTypeComponent<TType>
	{
		private static readonly object placeHolder = new object();

		private readonly IDictionary<XName, XPropertyBox<TType>> properties =
			new Dictionary<XName, XPropertyBox<TType>>();

		private XName[] constructWith;

		private Func<IDictionary<XName, object>, TType> constructor;

		/// <summary>
		/// An optional predicate affecting all properties.
		/// </summary>
		public Predicate<TType> WriteIf { get; set; }

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
			if (properties.ContainsKey(property.Name))
			{
				throw new ArgumentException($"Component already has a property with XName {property.Name}.");
			}
			properties.Add(property.Name, XPropertyBox<TType>.Box(property));
		}

		/// <summary>
		/// Removes the given property.
		/// </summary>
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
		/// Removes the property with the given <see cref="XName"/>.
		/// </summary>
		public void Remove(XName propertyName)
		{
			if (propertyName == null)
			{
				throw new ArgumentNullException("XName cannot be null");
			}
			_ = properties.Remove(propertyName);
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
			return properties.TryGetValue(name, out XPropertyBox<TType> value)
				? XPropertyBox<TType>.Unbox<TProperty>(value)
				: null;
		}

		/// <summary>
		/// Retrieve the property identified by the given LINQ expression if it exists on this object and has
		/// the default <see cref="XName"/>.
		/// </summary>
		public XProperty<TType, TProperty> Get<TProperty>(Expression<Func<TType, TProperty>> expression)
		{
			MemberInfo mi = ReflectionTools.ParseMemberExpression(expression);
			
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
				$"Expression {expression} does not define an {nameof(XProperty<TType,TProperty>)} on {nameof(XProperties<TType>)}",
				nameof(expression));
		}

		/// <summary>
		/// Removes any parametered constructor from the component.
		/// </summary>
		public void ConstructWith()
		{
			constructWith = null;
			constructor = null;
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

			constructWith = new XName[] { property1 };
			this.constructor = (args) => constructor((TArg1)args[property1]);
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

			constructWith = new XName[] { property1, property2 };
			this.constructor = (args) => constructor((TArg1)args[property1], (TArg2)args[property2]);
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

			constructWith = new XName[] { property1, property2, property3 };
			this.constructor = (args) => constructor((TArg1)args[property1], (TArg2)args[property2], (TArg3)args[property3]);
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

			constructWith = new XName[] { property1, property2, property3, property4 };
			this.constructor = (args) => constructor((TArg1)args[property1], (TArg2)args[property2],
				(TArg3)args[property3], (TArg4)args[property4]);
		}

		/// <summary>
		/// Reads XML elements and attributes as properties and assign thems to the constructed object.
		/// </summary>
		protected override void OnBuild(XType<TType> xType, IXReadOperation reader, XElement element, ObjectBuilder<TType> objectBuilder)
		{
			IDictionary<XName, object> propertyValues = new Dictionary<XName, object>();

			int toSet = 0;

			foreach (XPropertyBox<TType> textProperty in properties.Values
				.Where(x => x.WriteAs == PropertyWriteMode.Text))
			{
				propertyValues.Add(textProperty.Name, placeHolder);
				toSet++;
				reader.Read(element, textProperty.PropertyType, x =>
				{
					propertyValues[textProperty.Name] = x;
					return true;
				},
				ReaderHints.IgnoreElementName);
			}

			foreach (XAttribute attribute in element.Attributes())
			{
				if (properties.TryGetValue(attribute.Name, out XPropertyBox<TType> property))
				{
					propertyValues.Add(property.Name, placeHolder);
					toSet++;

					reader.Read(attribute, property.PropertyType, x =>
					{
						propertyValues[property.Name] = x;
						return true;
					});
				}
			}

			foreach (XElement subElement in element.Elements())
			{
				if (properties.TryGetValue(subElement.Name, out XPropertyBox<TType> property))
				{
					propertyValues.Add(property.Name, placeHolder);
					toSet++;
						
					reader.Read(subElement, property.PropertyType, x =>
					{
						propertyValues[property.Name] = x;
						return true;
					},
					ReaderHints.IgnoreElementName);
				}
			}

			// With constructor

			if (constructWith != null)
			{
				// Use default values if constructor parameters weren't found in XML

				foreach (XName cwName in constructWith)
				{
					if (!propertyValues.ContainsKey(cwName))
					{
						propertyValues.Add(cwName, null);
					}
				}

				// Schedule construction when ready

				objectBuilder.AddTask(() =>
				{
					if (!constructWith.Any(x => propertyValues[x] == placeHolder))
					{
						try
						{
							objectBuilder.Object = constructor(propertyValues);
						}
						finally
						{
							foreach (XName cw in constructWith)
							{
								_ = propertyValues.Remove(cw);
								toSet--;
							}
						}

						return true;
					}
					return false;
				});
			}

			// Task to set properties

			objectBuilder.AddTask(() =>
			{
				if (objectBuilder.IsConstructed)
				{
					XName[] keysToSet = propertyValues.Keys.ToArray();

					foreach (XName ppty in keysToSet)
					{
						try
						{
							properties[ppty].Set(objectBuilder.Object, propertyValues[ppty]);
						}
						finally
						{
							_ = propertyValues.Remove(ppty);
							toSet--;
						}
					}
					return toSet == 0;
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

			foreach (XPropertyBox<TType> property in properties.Values)
			{
				if (property.WriteIf?.Invoke(obj) == false)
				{
					continue;
				}

				if (property.WriteAs == PropertyWriteMode.Attribute)
				{
					XAttribute propertyAttribute = writer.WriteAttribute(property.Get(obj), property.Name);
					if (propertyAttribute != null)
					{
						element.Add(propertyAttribute);
					}
				}
				else if (property.WriteAs == PropertyWriteMode.Text)
				{
					_ = writer.WriteTo(element, property.Get(obj), property.PropertyType);
				}
				else
				{
					element.Add(writer.WriteTo(new XElement(property.Name), property.Get(obj), property.PropertyType));
				}
			}

			return false;
		}
	}
}
