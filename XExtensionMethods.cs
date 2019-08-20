using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Linq;
using XMachine.Components;
using XMachine.Components.Collections;
using XMachine.Components.Constructors;
using XMachine.Components.Identifiers;
using XMachine.Components.Properties;

namespace XMachine
{
	/// <summary>
	/// Extension methods for various classes in <see cref="XMachine"/>.
	/// </summary>
	public static class XExtensionMethods
	{
		private static readonly XName defaultRoot = "xml";

		/// <summary>
		/// Get an <see cref="XObjectArgs"/> instance.
		/// </summary>
		/// <param name="hints">The current <see cref="ObjectHints"/>.</param>
		/// <returns>An instance of <see cref="XObjectArgs"/>.</returns>
		public static XObjectArgs ToArgs(this ObjectHints hints) =>
			hints == ObjectHints.None ? XObjectArgs.Default
				: (hints == ObjectHints.IgnoreElementName ? XObjectArgs.DefaultIgnoreElementName
					: new XObjectArgs(hints));

		#region IXComponent

		/// <summary>
		/// Enable the given <see cref="IXComponent"/>, instructing its owner to invoke its methods.
		/// </summary>
		/// <param name="comp">The <see cref="IXComponent"/> to enable.</param>
		public static void Enable(this IXComponent comp) => comp.Enabled = true;

		/// <summary>
		/// Disable the given <see cref="IXComponent"/>, instructing its owner not to invoke its methods.
		/// </summary>
		/// <param name="comp">The <see cref="IXComponent"/> to disable.</param>
		public static void Disable(this IXComponent comp) => comp.Enabled = false;

		/// <summary>
		/// Enable the given <see cref="IXComponent"/>s, instructing their owners to invoke their methods.
		/// </summary>
		/// <param name="comps">The <see cref="IXComponent"/>s to enable.</param>
		public static void Enable(this IEnumerable<IXComponent> comps)
		{
			foreach (IXComponent comp in comps)
			{
				comp.Enabled = true;
			}
		}

		/// <summary>
		/// Disable the given <see cref="IXComponent"/>, instructing their owners not to invoke their methods.
		/// </summary>
		/// <param name="comps">The <see cref="IXComponent"/>s to disable.</param>
		public static void Disable(this IEnumerable<IXComponent> comps)
		{
			foreach (IXComponent comp in comps)
			{
				comp.Enabled = false;
			}
		}

		#endregion

		#region XType

		/// <summary>
		/// Get the <see cref="XConstructor{T}"/> component.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <returns>An <see cref="XConstructor{T}"/>, or null.</returns>
		public static XConstructor<T> Constructor<T>(this XType<T> xType) => xType.Component<XConstructor<T>>();

		/// <summary>
		/// Set a delegate that will be used to construct objects of this <see cref="XType{TType}"/>.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="constructor">The delegate to use as a constructor.</param>
		public static void Constructor<T>(this XType<T> xType, Func<T> constructor)
		{
			XConstructor<T> component = xType.Component<XConstructor<T>>();
			if (component != null)
			{
				component.Constructor = constructor;
			}
			else
			{
				xType.Register(new XConstructor<T>(xType, constructor));
			}
		}

		/// <summary>
		/// Get the <see cref="XProperties{TType}"/> component.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <returns>An <see cref="XProperties{TType}"/>, or null.</returns>
		public static XProperties<T> Properties<T>(this XType<T> xType) => xType.Component<XProperties<T>>();

		/// <summary>
		/// Get an <see cref="XProperty{TType, TProperty}"/> by name.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="propertyName">The <see cref="XName"/> alias of the desired property.</param>
		/// <returns>An <see cref="XProperty{TType, TProperty}"/>.</returns>
		public static XProperty<TType, TProperty> Property<TType, TProperty>(this XType<TType> xType, XName propertyName) =>
			xType.Component<XProperties<TType>>()?.Get<TProperty>(propertyName);

		/// <summary>
		/// Get an <see cref="XProperty{TType, TProperty}"/> by expression.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="propertyExpression">A <see cref="MemberExpression"/> selecting the member of <typeparamref name="TType"/> 
		/// from which the desired <see cref="XProperty{TType, TProperty}"/> was created.</param>
		/// <returns>An <see cref="XProperty{TType, TProperty}"/>.</returns>
		public static XProperty<TType, TProperty> Property<TType, TProperty>(this XType<TType> xType,
			Expression<Func<TType, TProperty>> propertyExpression) =>
			xType.Component<XProperties<TType>>()?.Get(propertyExpression);

		/// <summary>
		/// Create a property with the given name and accessors and add it to the <see cref="XProperties{TType}"/>
		/// component.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="name">An <see cref="XName"/>, unique on the <see cref="XProperties{TType}"/> component, that will
		/// identify the new <see cref="XProperty{TType, TProperty}"/>.</param>
		/// <param name="get">A delegate to act as a get accessor.</param>
		/// <param name="set">An optional delegate to act as a set accessor.</param>
		/// <returns>The new <see cref="XProperty{TType, TProperty}"/>.</returns>
		public static XProperty<TType, TProperty> AddProperty<TType, TProperty>(this XType<TType> xType, XName name,
			Func<TType, TProperty> get, Action<TType, TProperty> set = null)
		{
			XProperty<TType, TProperty> property = new DelegatedXProperty<TType, TProperty>(
				name: name ?? throw new ArgumentNullException(nameof(name)),
				get: get ?? throw new ArgumentNullException(nameof(get)),
				set: set);
			xType.Component<XProperties<TType>>().Add(property);
			return property;
		}

		/// <summary>
		/// Create a property representing a member of <typeparamref name="TType"/> and add it to the 
		/// <see cref="XProperties{TType}"/> component.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="memberExpression">A <see cref="MemberExpression"/> selecting the member of <typeparamref name="TType"/> 
		/// from which the new <see cref="XProperty{TType, TProperty}"/> should be created.</param>
		/// <param name="set">An optional delegate to override the default set accessor for the member.</param>
		/// <returns>The new <see cref="XProperty{TType, TProperty}"/>.</returns>
		public static XProperty<TType, TProperty> AddProperty<TType, TProperty>(this XType<TType> xType,
			Expression<Func<TType, TProperty>> memberExpression, Action<TType, TProperty> set = null) =>
			xType.Component<XProperties<TType>>().Add(memberExpression, set);

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as an argument to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="expression1">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as a constructor argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1>(this XType<TType> xType, Expression<Func<TType, TArg1>> expression1,
			Func<TArg1, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as an argument to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="property1">The <see cref="XProperty{TType,TProperty}"/> to pass as an argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1>(this XType<TType> xType, XProperty<TType, TArg1> property1,
			Func<TArg1, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given properties before construction,
		/// then pass them as arguments to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="expression1">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the first constructor argument.</param>
		/// <param name="expression2">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the second constructor argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1, TArg2>(this XType<TType> xType,
			Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, expression2, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given properties before construction,
		/// then pass them as arguments to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="property1">The <see cref="XProperty{TType,TProperty}"/> to pass as the first argument.</param>
		/// <param name="property2">The <see cref="XProperty{TType,TProperty}"/> to pass as the second argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1, TArg2>(this XType<TType> xType,
			XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, property2, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given properties before construction,
		/// then pass them as arguments to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="expression1">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the first constructor argument.</param>
		/// <param name="expression2">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the second constructor argument.</param>
		/// <param name="expression3">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the third constructor argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3>(this XType<TType> xType,
			Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, expression2, expression3, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given properties before construction,
		/// then pass them as arguments to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="property1">The <see cref="XProperty{TType,TProperty}"/> to pass as the first argument.</param>
		/// <param name="property2">The <see cref="XProperty{TType,TProperty}"/> to pass as the second argument.</param>
		/// <param name="property3">The <see cref="XProperty{TType,TProperty}"/> to pass as the third argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3>(this XType<TType> xType,
			XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, property2, property3, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given properties before construction,
		/// then pass them as arguments to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="expression1">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the first constructor argument.</param>
		/// <param name="expression2">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the second constructor argument.</param>
		/// <param name="expression3">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the third constructor argument.</param>
		/// <param name="expression4">A <see cref="MemberExpression"/> identifying the member of <typeparamref name="TType"/>
		/// that should be passed as the fourth constructor argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3, TArg4>(this XType<TType> xType,
			Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Expression<Func<TType, TArg4>> expression4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, expression2, expression3, expression4, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given properties before construction,
		/// then pass them as arguments to the constructor. Disables the default constructor.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="property1">The <see cref="XProperty{TType,TProperty}"/> to pass as the first argument.</param>
		/// <param name="property2">The <see cref="XProperty{TType,TProperty}"/> to pass as the second argument.</param>
		/// <param name="property3">The <see cref="XProperty{TType,TProperty}"/> to pass as the third argument.</param>
		/// <param name="property4">The <see cref="XProperty{TType,TProperty}"/> to pass as the fourth argument.</param>
		/// <param name="constructor">An optional delegate to use as a constructor. If null, <typeparamref name="TType"/>
		/// will be scanned for constructors that match the given properties' order, types and names.</param>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3, TArg4>(this XType<TType> xType,
			XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			XProperty<TType, TArg4> property4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, property2, property3, property4, constructor);
			XConstructor<TType> ctor = xType.Component<XConstructor<TType>>();
			if (ctor != null)
			{
				ctor.Enabled = false;
			}
		}

		/// <summary>
		/// Get the <see cref="XTexter{T}"/> component on this <see cref="XType{TType}"/>, if it exists.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <returns>An <see cref="XTexter{T}"/> instance, or null.</returns>
		public static XTexter<T> Texter<T>(this XType<T> xType) => xType.Component<XTexter<T>>();

		/// <summary>
		/// Set delegates to read and write this <see cref="XType{TType}"/> from text. This method automatically disables all 
		/// other <see cref="XTypeComponent{TType}"/> objects on this <see cref="XType{T}"/>.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="reader">The delegate used to read a <typeparamref name="T"/> from text.</param>
		/// <param name="writer">The delegate used to write a <typeparamref name="T"/> from text. If null, the 
		/// <see cref="object.ToString"/> method will be used.</param>
		/// <param name="leaveCompsEnabled">If true, other <see cref="XTypeComponent{T}"/>s will be left enabled.</param>
		public static void Texter<T>(this XType<T> xType, Func<string, T> reader, Func<T, string> writer = null,
			bool leaveCompsEnabled = false)
		{
			if (reader == null)
			{
				throw new ArgumentNullException(nameof(reader));
			}

			if (!leaveCompsEnabled)
			{
				foreach (XTypeComponent<T> comp in xType.Components())
				{
					comp.Enabled = false;
				}
			}

			XTexter<T> texter = xType.Component<XTexter<T>>();
			if (texter == null)
			{
				xType.Register(new XTexter<T>(xType, reader, writer));
			}
			else
			{
				texter.Enabled = true;
				texter.Reader = reader;
				texter.Writer = writer;
			}
		}

		/// <summary>
		/// Get the <see cref="XCollection{T}"/> component, if it exists.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <returns>An <see cref="XCollection{T}"/>, or null.</returns>
		public static XCollection<T> Collection<T>(this XType<T> xType) => xType.Component<XCollection<T>>();

		/// <summary>
		/// Get the <see cref="XCollection{TCollection, TItem}"/> component, if it exists.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <returns>An <see cref="XCollection{TCollection, TItem}"/>, or null.</returns>
		public static XCollection<TCollection, TItem> Collection<TCollection, TItem>(this XType<TCollection> xType) =>
			xType.Component<XCollection<TCollection, TItem>>();

		/// <summary>
		/// Get the active <see cref="XBuilder{T}"/>, if it exists.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <returns>An <see cref="XBuilder{T}"/>, or null.</returns>
		public static XBuilder<T> Builder<T>(this XType<T> xType) => xType.Component<XBuilderComponent<T>>()?.Builder;

		/// <summary>
		/// Set an <see cref="XBuilder{T}"/> to read and write this <see cref="XType{TType}"/> from XML. This method 
		/// automatically disables all other <see cref="XTypeComponent{TType}"/> objects on the <see cref="XType{T}"/>.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="builder">The <see cref="XBuilder{T}"/> to use.</param>
		/// <param name="leaveCompsEnabled">If true, other <see cref="XTypeComponent{T}"/>s will be left enabled.</param>
		public static void Builder<T>(this XType<T> xType, XBuilder<T> builder, bool leaveCompsEnabled = false)
		{
			if (builder == null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			if (!leaveCompsEnabled)
			{
				foreach (XTypeComponent<T> comp in xType.Components())
				{
					comp.Enabled = false;
				}
			}

			XBuilderComponent<T> buildComp = xType.Component<XBuilderComponent<T>>();
			if (buildComp == null)
			{
				xType.Register(new XBuilderComponent<T>(xType, builder));
			}
			else
			{
				buildComp.Enabled = true;
				buildComp.Builder = builder;
			}
		}

		/// <summary>
		/// Set a pair of <see cref="XBuilder{T}"/> delegates to read and write this <see cref="XType{TType}"/> from XML. This method 
		/// automatically disables all other <see cref="XTypeComponent{TType}"/> objects on the <see cref="XType{T}"/>.
		/// </summary>
		/// <param name="xType">The current <see cref="XType{T}"/>.</param>
		/// <param name="reader">The delegate to use for the <see cref="XBuilder{T}.OnBuild(XType{T}, IXReadOperation, XElement, ObjectBuilder{T}, XObjectArgs)"/> 
		/// method.</param>
		/// <param name="writer">The delegate to use for the <see cref="XBuilder{T}.OnWrite(XType{T}, IXWriteOperation, T, XElement, XObjectArgs)"/>
		/// method.</param>
		/// <param name="leaveCompsEnabled">If true, other <see cref="XTypeComponent{T}"/>s will be left enabled.</param>
		public static void Builder<T>(this XType<T> xType,
			Action<XType<T>, IXReadOperation, XElement, ObjectBuilder<T>, XObjectArgs> reader,
			Func<XType<T>, IXWriteOperation, T, XElement, XObjectArgs, bool> writer,
			bool leaveCompsEnabled = false)
		{
			XBuilderComponent<T> buildComp = xType.Component<XBuilderComponent<T>>();
			if (buildComp == null)
			{
				buildComp = new XBuilderComponent<T>(xType, new XDelegateBuilder<T>(reader, writer));
				xType.Register(buildComp);
			}
			else
			{
				buildComp.Builder = new XDelegateBuilder<T>(reader, writer);
			}

			if (!leaveCompsEnabled)
			{
				foreach (XTypeComponent<T> comp in xType.Components())
				{
					comp.Enabled = false;
				}
			}
		}

		#endregion

		#region XReader/XWriter

		/// <summary>
		/// Get the <see cref="XCompositeIdentifier"/> used by the <see cref="XIdentifierReader"/> component.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <returns>An <see cref="XCompositeIdentifier"/>, or null.</returns>
		public static XCompositeIdentifier Identifier(this XReader reader) =>
			reader.Component<XIdentifierReader>()?.Identifier;

		/// <summary>
		/// Add an <see cref="XIdentifier{TType, TId}"/> delegate to the <see cref="XIdentifierReader"/> component.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="identifier">An identifier delegate.</param>
		/// <param name="keyComparer">An optional <see cref="IEqualityComparer{T}"/> used to compare IDs generated by
		/// <paramref name="identifier"/>.</param>
		public static void Identify<TType, TId>(this XReader reader, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null) where TType : class =>
			reader.Component<XIdentifierReader>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));

		/// <summary>
		/// Get the <see cref="XCompositeIdentifier"/> used by the <see cref="XIdentifierWriter"/> component.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <returns>An <see cref="XCompositeIdentifier"/>, or null.</returns>
		public static XCompositeIdentifier Identifier(this XWriter writer) =>
			writer.Component<XIdentifierWriter>()?.Identifier;

		/// <summary>
		/// Add an <see cref="XIdentifier{TType, TId}"/> delegate to the <see cref="XIdentifierWriter"/> component.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="identifier">An identifier delegate.</param>
		/// <param name="keyComparer">An optional <see cref="IEqualityComparer{T}"/> used to compare IDs generated by
		/// <paramref name="identifier"/>.</param>
		public static void Identify<TType, TId>(this XWriter writer, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null) where TType : class =>
			writer.Component<XIdentifierWriter>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>A deserialized <see cref="object"/>.</returns>
		public static object ReadFrom(this XReader reader, string file) =>
			reader.Read<object>(XmlTools.ReadFile(file));

		/// <summary>
		/// Attempts to read the root element of the given stream as an object.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>A deserialized <see cref="object"/>.</returns>
		public static object ReadFrom(this XReader reader, Stream stream) =>
			reader.Read<object>(XmlTools.ReadFile(stream));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(this XReader reader, params string[] files) =>
			reader.ReadAll(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given streams as objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll(XmlTools.ReadFiles(streams));

		/// <summary>
		/// Attempts to read the root element of the given file as an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>A deserialized <typeparamref name="T"/>.</returns>
		public static T ReadFrom<T>(this XReader reader, string file) =>
			reader.Read<T>(XmlTools.ReadFile(file));

		/// <summary>
		/// Attempts to read the root element of the given stream as an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>A deserialized <typeparamref name="T"/>.</returns>
		public static T ReadFrom<T>(this XReader reader, Stream stream) =>
			reader.Read<T>(XmlTools.ReadFile(stream));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(this XReader reader, params string[] files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given streams as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(streams));

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, string file) =>
			reader.ReadAll(XmlTools.ReadFile(file).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root element of the given stream as a collection of objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, Stream stream) =>
			reader.ReadAll(XmlTools.ReadFile(stream).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, params string[] files) =>
			reader.ReadAll(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given streams as a collection of objects.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll(XmlTools.ReadFiles(streams).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, string file) =>
			reader.ReadAll<T>(XmlTools.ReadFile(file).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, Stream stream) =>
			reader.ReadAll<T>(XmlTools.ReadFile(stream).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, params string[] files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given streams as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(streams).Elements());

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="identifier">The <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="reader"/></returns>
		public static XReader With<TType, TId>(this XReader reader, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class
		{
			if (identifier == null)
			{
				throw new ArgumentNullException(nameof(identifier));
			}
			reader.Component<XIdentifierReader>()?.Identifier.Identify(identifier);
			if (contextObjects != null)
			{
				reader.SubmitAll(contextObjects);
			}
			return reader;
		}

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> defining equality between <typeparamref name="TId"/>s.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="reader"/></returns>
		public static XReader With<TType, TId>(this XReader reader, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class
		{
			if (identifier == null)
			{
				throw new ArgumentNullException(nameof(identifier));
			}
			reader.Component<XIdentifierReader>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));
			if (contextObjects != null)
			{
				reader.SubmitAll(contextObjects);
			}
			return reader;
		}

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="reader"/></returns>
		public static XReader With<TType, TId>(this XReader reader, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class
		{
			if (identifier == null)
			{
				throw new ArgumentNullException(nameof(identifier));
			}
			reader.Component<XIdentifierReader>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier));
			if (contextObjects != null)
			{
				reader.SubmitAll(contextObjects);
			}
			return reader;
		}

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to the <see cref="XReader"/>.
		/// </summary>
		/// <param name="reader">The current <see cref="XReader"/>.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="reader"/></returns>
		public static XReader With(this XReader reader, IEnumerable contextObjects)
		{
			if (contextObjects == null)
			{
				throw new ArgumentNullException(nameof(contextObjects));
			}
			reader.SubmitAll(contextObjects);
			return reader;
		}

		/// <summary>
		/// Attempts to write the given object as the root element of the given file.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		public static void WriteTo(this XWriter writer, object obj, string file) =>
			XmlTools.WriteFile(file, writer.Write(obj));

		/// <summary>
		/// Attempts to write the given object as the root element of the given file.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		public static void WriteTo(this XWriter writer, object obj, Stream stream) =>
			XmlTools.WriteFile(stream, writer.Write(obj));

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="rootElement">The <see cref="XName"/> of the file's root element. If null, 'xml' is used.</param>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, string file, XName rootElement = null)
		{
			XElement root = new XElement(rootElement ?? defaultRoot);
			foreach (XElement element in writer.WriteAll(objects))
			{
				root.Add(element);
			}
			XmlTools.WriteFile(file, root);
		}

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="rootElement">The <see cref="XName"/> of the file's root element. If null, 'xml' is used.</param>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, Stream stream, XName rootElement = null)
		{
			XElement root = new XElement(rootElement ?? defaultRoot);
			foreach (XElement element in writer.WriteAll(objects))
			{
				root.Add(element);
			}
			XmlTools.WriteFile(stream, root);
		}

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="rootElement">The root <see cref="XElement"/> of the file.</param>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, string file, XElement rootElement)
		{
			if (rootElement == null)
			{
				throw new ArgumentNullException(nameof(rootElement));
			}
			foreach (XElement element in writer.WriteAll(objects))
			{
				rootElement.Add(element);
			}
			XmlTools.WriteFile(file, rootElement);
		}

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="rootElement">The root <see cref="XElement"/> of the file.</param>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, Stream stream, XElement rootElement)
		{
			if (rootElement == null)
			{
				throw new ArgumentNullException(nameof(rootElement));
			}
			foreach (XElement element in writer.WriteAll(objects))
			{
				rootElement.Add(element);
			}
			XmlTools.WriteFile(stream, rootElement);
		}

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XWriter"/>.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="identifier">The <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="writer"/></returns>
		public static XWriter With<TType, TId>(this XWriter writer, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class
		{
			if (identifier == null)
			{
				throw new ArgumentNullException(nameof(identifier));
			}
			writer.Component<XIdentifierWriter>()?.Identifier.Identify(identifier);
			if (contextObjects != null)
			{
				writer.SubmitAll(contextObjects);
			}
			return writer;
		}

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XWriter"/>.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> defining equality between <typeparamref name="TId"/>s.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="writer"/></returns>
		public static XWriter With<TType, TId>(this XWriter writer, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class
		{
			if (identifier == null)
			{
				throw new ArgumentNullException(nameof(identifier));
			}
			writer.Component<XIdentifierWriter>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));
			if (contextObjects != null)
			{
				writer.SubmitAll(contextObjects);
			}
			return writer;
		}

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XWriter"/>.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="writer"/></returns>
		public static XWriter With<TType, TId>(this XWriter writer, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class
		{
			if (identifier == null)
			{
				throw new ArgumentNullException(nameof(identifier));
			}
			writer.Component<XIdentifierWriter>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier));
			if (contextObjects != null)
			{
				writer.SubmitAll(contextObjects);
			}
			return writer;
		}

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to the <see cref="XWriter"/>.
		/// </summary>
		/// <param name="writer">The current <see cref="XWriter"/>.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns><paramref name="writer"/></returns>
		public static XWriter With(this XWriter writer, IEnumerable contextObjects)
		{
			if (contextObjects == null)
			{
				throw new ArgumentNullException(nameof(contextObjects));
			}
			writer.SubmitAll(contextObjects);
			return writer;
		}

		#endregion

		#region XDomain

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="element">An <see cref="XElement"/> containing a serialized object.</param>
		/// <returns>The deserialized object.</returns>
		public static object Read(this XDomain domain, XElement element) => domain.GetReader().Read(element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="element">An <see cref="XElement"/> containing a serialized object.</param>
		/// <returns>The deserialized object.</returns>
		public static T Read<T>(this XDomain domain, XElement element) => domain.GetReader().Read<T>(element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="elements">An <see cref="IEnumerable{XElement}"/> that contain serialized objects.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> containing the deserialized objects.</returns>
		public static IEnumerable<object> ReadAll(this XDomain domain, IEnumerable<XElement> elements) =>
			domain.GetReader().ReadAll(elements);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="elements">An <see cref="IEnumerable{XElement}"/> that contain serialized objects.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> containing the deserialized objects.</returns>
		public static IEnumerable<T> ReadAll<T>(this XDomain domain, IEnumerable<XElement> elements) =>
			domain.GetReader().ReadAll<T>(elements);

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>A deserialized <see cref="object"/>.</returns>
		public static object ReadFrom(this XDomain domain, string file) => ReadFrom(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the root element of the given stream as an object.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>A deserialized <see cref="object"/>.</returns>
		public static object ReadFrom(this XDomain domain, Stream stream) => ReadFrom(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(this XDomain domain, params string[] files) =>
			ReadFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(this XDomain domain, IEnumerable<string> files) =>
			ReadFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given streams as objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadFrom(domain.GetReader(), streams);

		/// <summary>
		/// Attempts to read the root element of the given file as an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>A deserialized <typeparamref name="T"/>.</returns>
		public static T ReadFrom<T>(this XDomain domain, string file) =>
			ReadFrom<T>(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the root element of the given stream as an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>A deserialized <typeparamref name="T"/>.</returns>
		public static T ReadFrom<T>(this XDomain domain, Stream stream) =>
			ReadFrom<T>(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(this XDomain domain, params string[] files) =>
			ReadFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(this XDomain domain, IEnumerable<string> files) =>
			ReadFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given streams as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadFrom<T>(domain.GetReader(), streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, string file) =>
			ReadElementsFrom(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given stream as a collection of objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, Stream stream) =>
			ReadElementsFrom(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, params string[] files) =>
			ReadElementsFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, IEnumerable<string> files) =>
			ReadElementsFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given streams as a collection of objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadElementsFrom(domain.GetReader(), streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, string file) =>
			ReadElementsFrom<T>(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, Stream stream) =>
			ReadElementsFrom<T>(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, params string[] files) =>
			ReadElementsFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, IEnumerable<string> files) =>
			ReadElementsFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given streams as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadElementsFrom<T>(domain.GetReader(), streams);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="identifier">The <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith<TType, TId>(this XDomain domain, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class =>
			With(domain.GetReader(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> defining equality between <typeparamref name="TId"/>s.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class =>
			With(domain.GetReader(), identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class =>
			With(domain.GetReader(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new <see cref="XReader"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith(this XDomain domain, IEnumerable contextObjects) =>
			With(domain.GetReader(), contextObjects);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="obj">The object to be written.</param>
		/// <returns>An <see cref="XElement"/> containing the serialized <paramref name="obj"/>.</returns>
		public static XElement Write(this XDomain domain, object obj) => domain.GetWriter().Write(obj);

		/// <summary>
		/// Write the given collection of objects as a collection of <see cref="XElement"/>s.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="objects">The objects to be written.</param>
		/// <returns>An <see cref="IEnumerable{XElement}"/> containing the serialized <paramref name="objects"/>.</returns>
		public static IEnumerable<XElement> WriteAll(this XDomain domain, IEnumerable objects) =>
			domain.GetWriter().WriteAll(objects);

		/// <summary>
		/// Attempts to write the given object as the root element of the given file.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		public static void WriteTo(this XDomain domain, object obj, string file) =>
			XmlTools.WriteFile(file, domain.GetWriter().Write(obj));

		/// <summary>
		/// Attempts to write the given object as the root element of the given file.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		public static void WriteTo(this XDomain domain, object obj, Stream stream) =>
			XmlTools.WriteFile(stream, domain.GetWriter().Write(obj));

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="rootElement">The <see cref="XName"/> of the file's root element. If null, 'xml' is used.</param>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, string file, XName rootElement = null)
		{
			XElement root = new XElement(rootElement ?? defaultRoot);
			foreach (XElement element in domain.GetWriter().WriteAll(objects))
			{
				root.Add(element);
			}
			XmlTools.WriteFile(file, root);
		}

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="rootElement">The <see cref="XName"/> of the file's root element. If null, 'xml' is used.</param>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, Stream stream, XName rootElement = null)
		{
			XElement root = new XElement(rootElement ?? defaultRoot);
			foreach (XElement element in domain.GetWriter().WriteAll(objects))
			{
				root.Add(element);
			}
			XmlTools.WriteFile(stream, root);
		}

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="rootElement">The root <see cref="XElement"/> of the file.</param>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, string file, XElement rootElement)
		{
			if (rootElement == null)
			{
				throw new ArgumentNullException(nameof(rootElement));
			}
			foreach (XElement element in domain.GetWriter().WriteAll(objects))
			{
				rootElement.Add(element);
			}
			XmlTools.WriteFile(file, rootElement);
		}

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="rootElement">The root <see cref="XElement"/> of the file.</param>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, Stream stream, XElement rootElement)
		{
			if (rootElement == null)
			{
				throw new ArgumentNullException(nameof(rootElement));
			}
			foreach (XElement element in domain.GetWriter().WriteAll(objects))
			{
				rootElement.Add(element);
			}
			XmlTools.WriteFile(stream, rootElement);
		}

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new
		/// instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="identifier">The <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith<TType, TId>(this XDomain domain, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class =>
			With(domain.GetWriter(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new
		/// instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> defining equality between <typeparamref name="TId"/>s.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class =>
			With(domain.GetWriter(), identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class =>
			With(domain.GetWriter(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith(this XDomain domain, IEnumerable contextObjects) =>
			With(domain.GetWriter(), contextObjects);

		#endregion
	}
}
