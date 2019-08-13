using System;
using System.Xml.Linq;

namespace XMachine.Components
{
	/// <summary>
	/// <see cref="XTexter{T}"/> reads and writes an object of type <typeparamref name="T"/> to and from
	/// the text content of an XML attribute or element. This is ideal for simple objects: primitive
	/// types are automatically read and written as text.
	/// </summary>
	public sealed class XTexter<T> : XTypeComponent<T>
	{
		private Func<string, T> reader;

		/// <summary>
		/// Create a new <see cref="XTexter{T}"/> using the given delegates.
		/// </summary>
		/// <param name="xType">The <see cref="XType{T}"/> object to which this <see cref="XTypeComponent{T}"/> belongs.</param>
		/// <param name="reader">A delegate that creates a <typeparamref name="T"/> from a <see cref="string"/>.</param>
		/// <param name="writer">A delegate that creates a <see cref="string"/> from a <typeparamref name="T"/>.</param>
		public XTexter(XType<T> xType, Func<string, T> reader, Func<T, string> writer = null) : base(xType)
		{
			Reader = reader;
			Writer = writer;
		}

		/// <summary>
		/// Get or set the reader delegate.
		/// </summary>
		public Func<string, T> Reader
		{
			get => reader;
			set => reader = value ?? throw new ArgumentNullException("Cannot use null delegate");
		}

		/// <summary>
		/// Get or set the writer delegate. If no value or a null value is set, <typeparamref name="T"/>'s
		/// ToString() method will be used.
		/// </summary>
		public Func<T, string> Writer { get; set; }

		protected override bool OnRead(IXReadOperation reader, XElement element, out T result, XObjectArgs args)
		{
			result = Reader(XmlTools.GetElementText(element));
			return true;
		}

		protected override bool OnRead(IXReadOperation reader, XAttribute attribute, out T result, XObjectArgs args)
		{
			result = Reader(attribute.Value);
			return true;
		}

		protected override bool OnWrite(IXWriteOperation writer, T obj, XElement element, XObjectArgs args)
		{
			element.Add(XmlTools.WriteText(Writer == null ? obj.ToString() : Writer(obj)));
			return true;
		}

		protected override bool OnWrite(IXWriteOperation writer, T obj, XAttribute attribute, XObjectArgs args)
		{
			string value = Writer == null ? obj.ToString() : Writer(obj);
			attribute.Value = attribute.Value == null
				? value
				: (attribute.Value + value);
			return true;
		}
	}
}
