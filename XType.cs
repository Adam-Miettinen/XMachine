using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// <see cref="XType{T}"/> is a wrapper around a <see cref="Type"/> that stores information on how its 
	/// instances should be read and written to XML. Its functionality can be extended through 
	/// <see cref="XTypeComponent{T}"/>.
	/// </summary>
	public sealed class XType<T> : XWithComponents<XTypeComponent<T>>, IEquatable<XType<T>>
	{
		private static readonly string toString = string.Concat("XType<", typeof(T).FullName, ">");

		internal XType(XDomain domain, XName name)
		{
			Domain = domain ?? throw new ArgumentNullException("Domain cannot be null");
			ExceptionHandler = domain.ExceptionHandler;

			// Have to throw these exceptions: don't want invalid XType instances floating around

			if (typeof(T).IsXIgnored(true))
			{
				throw new InvalidOperationException($"Cannot reflect ineligible type {typeof(T).FullName}.");
			}

			Name = name;
			if (name == null)
			{
				throw new InvalidOperationException($"Null XName assigned to {this}.");
			}
		}

		/// <summary>
		/// The <see cref="XDomain"/> instance to which this <see cref="XType{TType}"/> belongs.
		/// </summary>
		public XDomain Domain { get; }

		/// <summary>
		/// The <see cref="Name"/> that will represent this <see cref="Type"/> in XML. To customize this, modify
		/// the <see cref="XNamer"/> at <see cref="XDomain.Namer"/>.
		/// </summary>
		public XName Name { get; }

		/// <summary>
		/// <c>true</c> if and only if these <see cref="XType{TType}"/>s belong to the same <see cref="XDomain"/>.
		/// </summary>
		public bool Equals(XType<T> other) => other.Domain == Domain;

		/// <summary>
		/// <c>true</c> if and only if these <see cref="XType{TType}"/>s are the same <typeparamref name="T"/>
		/// and belong to the same <see cref="XDomain"/>.
		/// </summary>
		public override bool Equals(object obj) => obj is XType<T> other && other.Domain == Domain;

		/// <summary>
		/// A hashcode implemented through <typeparamref name="T"/>.
		/// </summary>
		public override int GetHashCode() => typeof(T).GetHashCode();

		/// <summary>
		/// Returns 'XType&lt;TType&gt;', where TType is typeof(<typeparamref name="T"/>).FullName.
		/// </summary>
		public override string ToString() => toString;

		internal void Initialize() => ForEachComponent((comp) => comp.Initialize(this));

		internal bool Read(IXReadOperation reader, XElement element, Type expectedType, out T result)
		{
			T compResult = default;
			if (ForEachComponent((comp) => comp.Read(this, reader, element, expectedType, out compResult)))
			{
				result = compResult;
				return true;
			}
			result = default;
			return false;
		}

		internal bool Read(IXReadOperation reader, XAttribute attribute, Type expectedType, out T result)
		{
			T compResult = default;
			if (ForEachComponent((comp) => comp.Read(this, reader, attribute, expectedType, out compResult)))
			{
				result = compResult;
				return true;
			}
			result = default;
			return false;
		}

		internal void Build(IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder) =>
			ForEachComponent((comp) => comp.Build(this, reader, element, objectBuilder));

		internal bool Write(IXWriteOperation writer, T obj, XElement element) =>
			ForEachComponent((comp) => comp.Write(this, writer, obj, element));

		internal bool Write(IXWriteOperation writer, T obj, XAttribute attribute) =>
			ForEachComponent((comp) => comp.Write(this, writer, obj, attribute));
	}
}