using System;
using System.Xml.Linq;

namespace XMachine
{
	/// <summary>
	/// <see cref="XType{T}"/> is a wrapper around a <see cref="Type"/> that stores information on how objects
	/// having that (exact) type should be read from and written to XML. Each <see cref="XType{T}"/> affects a 
	/// single <see cref="Type"/> and affects read/write operations within a single <see cref="XDomain"/>.
	/// Its functionality can be extended with <see cref="XTypeComponent{T}"/>s.
	/// </summary>
	public sealed class XType<T> : XWithComponents<XTypeComponent<T>>, IEquatable<XType<T>>
	{
		private static readonly string toString = string.Concat("XType<", typeof(T).FullName, ">");

		internal XType(XDomain domain, XName name)
		{
			Domain = domain ?? throw new ArgumentNullException("Domain cannot be null");

			Name = name;
			if (name == null)
			{
				throw new InvalidOperationException($"Null XName assigned to {this}.");
			}
		}

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public override Action<Exception> ExceptionHandler =>
			Domain.ExceptionHandler;

		/// <summary>
		/// Get the <see cref="XDomain"/> instance to which this <see cref="XType{TType}"/> belongs.
		/// </summary>
		public XDomain Domain { get; }

		/// <summary>
		/// Get the <see cref="XName"/> applied to <see cref="XElement"/>s that store a serialized object of type
		/// <typeparamref name="T"/>. To customize the mapping between types and names, see <see cref="XNamer"/>.
		/// </summary>
		public XName Name { get; }

		/// <summary>
		/// <c>true</c> if and only if these <see cref="XType{TType}"/>s belong to the same <see cref="XDomain"/>.
		/// </summary>
		/// <param name="other">An <see cref="XType{T}"/> for comparison.</param>
		public bool Equals(XType<T> other) => other?.Domain == Domain;

		/// <summary>
		/// <c>true</c> if and only if these <see cref="XType{TType}"/>s have the same type parameter 
		/// <typeparamref name="T"/> and belong to the same <see cref="XDomain"/>.
		/// </summary>
		/// <param name="obj">An <see cref="object"/> for comparison.</param>
		public override bool Equals(object obj) => obj is XType<T> other && other.Domain == Domain;

		/// <summary>
		/// Get a hashcode implemented through <typeparamref name="T"/>.
		/// </summary>
		public override int GetHashCode() => typeof(T).GetHashCode();

		/// <summary>
		/// Get a string representation: 'XType&lt;TType&gt;', where TType is typeof(<typeparamref name="T"/>).FullName.
		/// </summary>
		public override string ToString() => toString;

		internal void Initialize() => ForEachComponent((comp) => comp.Initialize());

		internal bool Read(IXReadOperation reader, XElement element, out T result, XObjectArgs args)
		{
			T compResult = default;
			if (ForEachComponent(x => x.Read(reader, element, out compResult, args)))
			{
				result = compResult;
				return true;
			}
			result = compResult;
			return false;
		}

		internal bool Read(IXReadOperation reader, XAttribute attribute, out T result, XObjectArgs args)
		{
			T compResult = default;
			if (ForEachComponent(x => x.Read(reader, attribute, out compResult, args)))
			{
				result = compResult;
				return true;
			}
			result = compResult;
			return false;
		}

		internal void Build(IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder, XObjectArgs args) =>
			ForEachComponent(x => x.Build(reader, element, objectBuilder, args));

		internal bool Write(IXWriteOperation writer, T obj, XElement element, XObjectArgs args) =>
			ForEachComponent(x => x.Write(writer, obj, element, args));

		internal bool Write(IXWriteOperation writer, T obj, XAttribute attribute, XObjectArgs args) =>
			ForEachComponent(x => x.Write(writer, obj, attribute, args));
	}
}