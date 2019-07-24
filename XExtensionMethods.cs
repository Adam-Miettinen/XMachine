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
	/// Extension methods for various classes in <see cref="XMachine"/>, gathered here to keep class files clean.
	/// </summary>
	public static class XExtensionMethods
	{
		private static readonly XName defaultRoot = "xml";

		#region IXComponent

		/// <summary>
		/// Enable a component.
		/// </summary>
		public static void Enable(this IXComponent comp) => comp.Enabled = true;

		/// <summary>
		/// Disable all components.
		/// </summary>
		public static void Disable(this IXComponent comp) => comp.Enabled = false;

		/// <summary>
		/// Enable all components.
		/// </summary>
		public static void Enable(this IEnumerable<IXComponent> comps)
		{
			foreach (IXComponent comp in comps)
			{
				comp.Enabled = true;
			}
		}

		/// <summary>
		/// Disable all components.
		/// </summary>
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
		/// Retrieve the delegate used to construct objects of this <see cref="XType{TType}"/>, if it exists.
		/// </summary>
		public static Func<T> Constructor<T>(this XType<T> xType) => xType.Component<XConstructor<T>>()?.Constructor;

		/// <summary>
		/// Set a delegate that will be used to construct objects of this <see cref="XType{TType}"/>.
		/// </summary>
		public static void Constructor<T>(this XType<T> xType, Func<T> constructor)
		{
			XConstructor<T> component = xType.Component<XConstructor<T>>();
			if (component != null)
			{
				component.Constructor = constructor;
			}
			else
			{
				xType.Register(new XConstructor<T>(constructor));
			}
		}

		/// <summary>
		/// Retrieve the <see cref="XProperties{TType}"/> component from the given <see cref="XType{TType}"/>.
		/// </summary>
		public static XProperties<T> Properties<T>(this XType<T> xType) => xType.Component<XProperties<T>>();

		/// <summary>
		/// Retrieve the <see cref="XProperty{TType, TProperty}"/> with the given <see cref="Type"/> and <see cref="XName"/>.
		/// </summary>
		public static XProperty<TType, TProperty> Property<TType, TProperty>(this XType<TType> xType, XName propertyName) =>
			xType.Component<XProperties<TType>>()?.Get<TProperty>(propertyName);

		/// <summary>
		/// Retrieve the <see cref="XProperty{TType, TProperty}"/> identified by the given expression.
		/// </summary>
		public static XProperty<TType, TProperty> Property<TType, TProperty>(this XType<TType> xType,
			Expression<Func<TType, TProperty>> propertyExpression) =>
			xType.Component<XProperties<TType>>()?.Get(propertyExpression);

		/// <summary>
		/// Creates a property with the given name, get accessor and set accessor and adds it to this <see cref="XType{TType}"/>.
		/// </summary>
		public static XProperty<TType, TProperty> AddProperty<TType, TProperty>(this XType<TType> xType, XName name,
			Func<TType, TProperty> get, Action<TType, TProperty> set = null)
		{
			XProperty<TType, TProperty> property = new XDelegateProperty<TType, TProperty>(
				name: name ?? throw new ArgumentNullException(nameof(name)),
				get: get ?? throw new ArgumentNullException(nameof(get)),
				set: set);
			xType.Component<XProperties<TType>>().Add(property);
			return property;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1>(this XType<TType> xType, Expression<Func<TType, TArg1>> expression1,
			Func<TArg1, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1>(this XType<TType> xType, XProperty<TType, TArg1> property1,
			Func<TArg1, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1, TArg2>(this XType<TType> xType,
			Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, expression2, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1, TArg2>(this XType<TType> xType,
			XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			Func<TArg1, TArg2, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, property2, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3>(this XType<TType> xType,
			Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, expression2, expression3, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3>(this XType<TType> xType,
			XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			Func<TArg1, TArg2, TArg3, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, property2, property3, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3, TArg4>(this XType<TType> xType,
			Expression<Func<TType, TArg1>> expression1,
			Expression<Func<TType, TArg2>> expression2,
			Expression<Func<TType, TArg3>> expression3,
			Expression<Func<TType, TArg4>> expression4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(expression1, expression2, expression3, expression4, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Instruct the <see cref="XProperties{TType}"/> component to deserialize the given property before construction,
		/// then pass it as a parameter to the constructor. Disables the default constructor.
		/// </summary>
		public static void ConstructWith<TType, TArg1, TArg2, TArg3, TArg4>(this XType<TType> xType,
			XProperty<TType, TArg1> property1,
			XProperty<TType, TArg2> property2,
			XProperty<TType, TArg3> property3,
			XProperty<TType, TArg4> property4,
			Func<TArg1, TArg2, TArg3, TArg4, TType> constructor = null)
		{
			xType.Component<XProperties<TType>>().ConstructWith(property1, property2, property3, property4, constructor);
			xType.Component<XConstructor<TType>>().Enabled = false;
		}

		/// <summary>
		/// Retrieve the <see cref="XTexter{T}"/> component on this <see cref="XType{TType}"/>, if it exists.
		/// </summary>
		public static XTexter<T> Texter<T>(this XType<T> xType) => xType.Component<XTexter<T>>();

		/// <summary>
		/// Set delegates to read and write this <see cref="XType{TType}"/> from text. This method automatically disables all 
		/// other <see cref="XTypeComponent{TType}"/> objects on this <see cref="XType{T}"/>.
		/// </summary>
		public static void Texter<T>(this XType<T> xType, Func<string, T> reader, Func<T, string> writer = null,
			bool leaveCompsEnabled = false)
		{
			XTexter<T> texter = xType.Component<XTexter<T>>();
			if (texter != null)
			{
				texter.Reader = reader;
				texter.Writer = writer;
			}
			else
			{
				if (!leaveCompsEnabled)
				{
					foreach (XTypeComponent<T> comp in xType.Components())
					{
						comp.Enabled = false;
					}
				}

				xType.Register(new XTexter<T>(reader, writer));
			}
		}

		/// <summary>
		/// Retrieve the <see cref="XCollection{T}"/> on this <see cref="XType{T}"/>, if it exists.
		/// </summary>
		public static XCollection<T> Collection<T>(this XType<T> xType) => xType.Component<XCollection<T>>();

		/// <summary>
		/// Retrieve the <see cref="XCollection{TCollection, TItem}"/> on this <see cref="XType{TType}"/>, if it exists.
		/// </summary>
		public static XCollection<TCollection, TItem> Collection<TCollection, TItem>(this XType<TCollection> xType)
			where TCollection : IEnumerable =>
			xType.Component<XCollection<TCollection, TItem>>();

		/// <summary>
		/// Retrieve the <see cref="XBuilder{T}"/> object being used to serialize and deserialize objects of this
		/// <see cref="XType{TType}"/>.
		/// </summary>
		public static XBuilder<T> Builder<T>(this XType<T> xType) => xType.Component<XBuilderComponent<T>>()?.Builder;

		/// <summary>
		/// Set the <see cref="XBuilder{T}"/> object used to serialize and deserialize objects of this <see cref="XType{TType}"/>.
		/// This method automatically disables all other <see cref="XTypeComponent{TType}"/> objects on this <see cref="XType{T}"/>.
		/// </summary>
		public static void Builder<T>(this XType<T> xType, XBuilder<T> builder, bool leaveCompsEnabled = false)
		{
			XBuilderComponent<T> buildComp = xType.Component<XBuilderComponent<T>>();
			if (buildComp != null)
			{
				buildComp.Builder = builder;
			}
			else
			{
				if (!leaveCompsEnabled)
				{
					foreach (XTypeComponent<T> comp in xType.Components())
					{
						comp.Enabled = false;
					}
				}

				xType.Register(new XBuilderComponent<T>(builder));
			}
		}

		/// <summary>
		/// Set the <see cref="XBuilder{T}"/> object used to serialize and deserialize objects of this <see cref="XType{TType}"/>
		/// using delegates for the read and write methods. This method automatically disables all other <see cref="XTypeComponent{TType}"/> 
		/// objects on this <see cref="XType{T}"/>.
		/// </summary>
		public static void Builder<T>(this XType<T> xType,
			Action<XType<T>, IXReadOperation, XElement, ObjectBuilder<T>> reader,
			Func<XType<T>, IXWriteOperation, T, XElement, bool> writer,
			bool leaveCompsEnabled = false)
		{
			XBuilderComponent<T> buildComp = xType.Component<XBuilderComponent<T>>();
			if (buildComp != null)
			{
				buildComp.Builder = new XDelegateBuilder<T>(reader, writer);
			}
			else
			{
				if (!leaveCompsEnabled)
				{
					foreach (XTypeComponent<T> comp in xType.Components())
					{
						comp.Enabled = false;
					}
				}

				xType.Register(new XBuilderComponent<T>(new XDelegateBuilder<T>(reader, writer)));
			}
		}

		#endregion

		#region XReader/XWriter

		/// <summary>
		/// Retrieve the <see cref="XCompositeIdentifier"/> being used by the <see cref="XIdentifierReader"/> component
		/// on this <see cref="XReader"/>.
		/// </summary>
		public static XCompositeIdentifier Identifier(this XReader reader) =>
			reader.Component<XIdentifierReader>()?.Identifier;

		/// <summary>
		/// Add an <see cref="XIdentifier{TType, TId}"/> characterized by the given delegate to the <see cref="XIdentifierReader"/>
		/// component on this <see cref="XReader"/>.
		/// </summary>
		public static void Identify<TType, TId>(this XReader reader, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null) where TType : class where TId : class =>
			reader.Component<XIdentifierReader>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));

		/// <summary>
		/// Retrieve the <see cref="XCompositeIdentifier"/> being used by the <see cref="XIdentifierWriter"/> component
		/// on this <see cref="XWriter"/>.
		/// </summary>
		public static XCompositeIdentifier Identifier(this XWriter writer) =>
			writer.Component<XIdentifierWriter>()?.Identifier;

		/// <summary>
		/// Add an <see cref="XIdentifier{TType, TId}"/> characterized by the given delegate to the <see cref="XIdentifierWriter"/>
		/// component on this <see cref="XWriter"/>.
		/// </summary>
		public static void Identify<TType, TId>(this XWriter reader, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null) where TType : class where TId : class =>
			reader.Component<XIdentifierWriter>()?.Identifier.Identify(new XDelegateIdentifier<TType, TId>(identifier, keyComparer));

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		public static object ReadFrom(this XReader reader, string file) =>
			reader.Read<object>(XmlTools.ReadFile(file));

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		public static object ReadFrom(this XReader reader, Stream stream) =>
			reader.Read<object>(XmlTools.ReadFile(stream));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(this XReader reader, params string[] files) =>
			reader.ReadAll(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll(XmlTools.ReadFiles(streams));

		/// <summary>
		/// Attempts to read the root element of the given file as an object as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		public static T ReadFrom<T>(this XReader reader, string file) =>
			reader.Read<T>(XmlTools.ReadFile(file));

		/// <summary>
		/// Attempts to read the root element of the given file as an object as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		public static T ReadFrom<T>(this XReader reader, Stream stream) =>
			reader.Read<T>(XmlTools.ReadFile(stream));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(this XReader reader, params string[] files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files));

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(streams));

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, string file) =>
			reader.ReadAll(XmlTools.ReadFile(file).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, Stream stream) =>
			reader.ReadAll(XmlTools.ReadFile(stream).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, params string[] files) =>
			reader.ReadAll(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll(XmlTools.ReadFiles(streams).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects assignable to
		/// <typeparamref name="T"/>, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, string file) =>
			reader.ReadAll<T>(XmlTools.ReadFile(file).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects assignable to
		/// <typeparamref name="T"/>, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, Stream stream) =>
			reader.ReadAll<T>(XmlTools.ReadFile(stream).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, params string[] files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, IEnumerable<string> files) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(files).Elements());

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XReader reader, IEnumerable<Stream> streams) =>
			reader.ReadAll<T>(XmlTools.ReadFiles(streams).Elements());

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader With<TType, TId>(this XReader reader, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class where TId : class
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
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader With<TType, TId>(this XReader reader, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class where TId : class
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
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader With<TType, TId>(this XReader reader, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class where TId : class
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
		/// Add the given <see cref="IEnumerable"/> of contextual objects to the <see cref="XReader"/>, then return it.
		/// </summary>
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
		/// Attempts to write the given object as the root element of the given file, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static void WriteTo(this XWriter writer, object obj, string file) =>
			XmlTools.WriteFile(file, writer.Write(obj));

		/// <summary>
		/// Attempts to write the given object as the root element of the given file, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static void WriteTo(this XWriter writer, object obj, Stream stream) =>
			XmlTools.WriteFile(stream, writer.Write(obj));

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of a root element
		/// with the given name (default is 'XML').
		/// </summary>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, string file, XName rootElement = null)
		{
			XElement root = new XElement(rootElement ?? "XML");
			foreach (XElement element in writer.WriteAll(objects))
			{
				root.Add(element);
			}
			XmlTools.WriteFile(file, root);
		}

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of a root element
		/// with the given name (default is 'XML').
		/// </summary>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, Stream stream, XName rootElement = null)
		{
			XElement root = new XElement(rootElement ?? "XML");
			foreach (XElement element in writer.WriteAll(objects))
			{
				root.Add(element);
			}
			XmlTools.WriteFile(stream, root);
		}

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of the given root element.
		/// </summary>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, string file, XElement rootElement = null)
		{
			if (rootElement == null)
			{
				rootElement = new XElement(defaultRoot);
			}
			foreach (XElement element in writer.WriteAll(objects))
			{
				rootElement.Add(element);
			}
			XmlTools.WriteFile(file, rootElement);
		}

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of the given root element.
		/// </summary>
		public static void WriteToElements(this XWriter writer, IEnumerable objects, Stream stream, XElement rootElement = null)
		{
			if (rootElement == null)
			{
				rootElement = new XElement(defaultRoot);
			}
			foreach (XElement element in writer.WriteAll(objects))
			{
				rootElement.Add(element);
			}
			XmlTools.WriteFile(stream, rootElement);
		}

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to the 
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter With<TType, TId>(this XWriter writer, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class where TId : class
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
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter With<TType, TId>(this XWriter writer, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class where TId : class
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
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter With<TType, TId>(this XWriter writer, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class where TId : class
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
		/// Add the given <see cref="IEnumerable"/> of contextual objects to the <see cref="XWriter"/>, then return it.
		/// </summary>
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
		/// Attempts to read the given <see cref="XElement"/> as an object, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static object Read(this XDomain domain, XElement element) => domain.GetReader().Read(element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object assignable to <typeparamref name="T"/>, using a 
		/// new instance of <see cref="XReader"/>.
		/// </summary>
		public static T Read<T>(this XDomain domain, XElement element) => domain.GetReader().Read<T>(element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<object> ReadAll(this XDomain domain, IEnumerable<XElement> elements) =>
			domain.GetReader().ReadAll(elements);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects assignable to <typeparamref name="T"/>, using a 
		/// new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadAll<T>(this XDomain domain, IEnumerable<XElement> elements) =>
			domain.GetReader().ReadAll<T>(elements);

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		public static object ReadFrom(this XDomain domain, string file) => ReadFrom(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		public static object ReadFrom(this XDomain domain, Stream stream) => ReadFrom(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(this XDomain domain, params string[] files) =>
			ReadFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(this XDomain domain, IEnumerable<string> files) =>
			ReadFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadFrom(domain.GetReader(), streams);

		/// <summary>
		/// Attempts to read the root element of the given file as an object as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		public static T ReadFrom<T>(this XDomain domain, string file) =>
			ReadFrom<T>(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the root element of the given file as an object as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		public static T ReadFrom<T>(this XDomain domain, Stream stream) =>
			ReadFrom<T>(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(this XDomain domain, params string[] files) =>
			ReadFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(this XDomain domain, IEnumerable<string> files) =>
			ReadFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadFrom<T>(domain.GetReader(), streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, string file) =>
			ReadElementsFrom(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, Stream stream) =>
			ReadElementsFrom(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, params string[] files) =>
			ReadElementsFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, IEnumerable<string> files) =>
			ReadElementsFrom(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadElementsFrom(domain.GetReader(), streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects assignable to
		/// <typeparamref name="T"/>, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, string file) =>
			ReadElementsFrom<T>(domain.GetReader(), file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects assignable to
		/// <typeparamref name="T"/>, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, Stream stream) =>
			ReadElementsFrom<T>(domain.GetReader(), stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, params string[] files) =>
			ReadElementsFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, IEnumerable<string> files) =>
			ReadElementsFrom<T>(domain.GetReader(), files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(this XDomain domain, IEnumerable<Stream> streams) =>
			ReadElementsFrom<T>(domain.GetReader(), streams);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith<TType, TId>(this XDomain domain, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class where TId : class =>
			With(domain.GetReader(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			With(domain.GetReader(), identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			With(domain.GetReader(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith(this XDomain domain, IEnumerable contextObjects) =>
			With(domain.GetReader(), contextObjects);

		/// <summary>
		/// Attempts to write the given object as an <see cref="XElement"/>, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static XElement Write(this XDomain domain, object obj) => domain.GetWriter().Write(obj);

		/// <summary>
		/// Attempts to write the given collection of objects as <see cref="XElement"/>s, using a new instance of 
		/// <see cref="XWriter"/>.
		/// </summary>
		public static IEnumerable<XElement> WriteAll(this XDomain domain, IEnumerable objects) =>
			domain.GetWriter().WriteAll(objects);

		/// <summary>
		/// Attempts to write the given object as the root element of the given file, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static void WriteTo(this XDomain domain, object obj, string file) =>
			WriteTo(domain.GetWriter(), obj, file);

		/// <summary>
		/// Attempts to write the given object as the root element of the given file, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static void WriteTo(this XDomain domain, object obj, Stream stream) =>
			WriteTo(domain.GetWriter(), obj, stream);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of a root element
		/// with the given name (default is 'XML').
		/// </summary>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, string file, XName rootElement = null) =>
			WriteToElements(domain.GetWriter(), objects, file, rootElement);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of a root element
		/// with the given name (default is 'XML').
		/// </summary>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, Stream stream, XName rootElement = null) =>
			WriteToElements(domain.GetWriter(), objects, stream, rootElement);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of the given root element.
		/// </summary>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, string file, XElement rootElement = null) =>
			WriteToElements(domain.GetWriter(), objects, file, rootElement);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of the given root element.
		/// </summary>
		public static void WriteToElements(this XDomain domain, IEnumerable objects, Stream stream, XElement rootElement = null) =>
			WriteToElements(domain.GetWriter(), objects, stream, rootElement);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith<TType, TId>(this XDomain domain, XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class where TId : class =>
			With(domain.GetWriter(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new  
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			With(domain.GetWriter(), identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new  
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith<TType, TId>(this XDomain domain, Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			With(domain.GetWriter(), identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith(this XDomain domain, IEnumerable contextObjects) =>
			With(domain.GetWriter(), contextObjects);

		#endregion
	}
}
