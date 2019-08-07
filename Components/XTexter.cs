using System;
using System.Xml.Linq;

namespace XMachine.Components
{
	/// <summary>
	/// <see cref="XTexter{T}"/> reads and writes an object of type <typeparamref name="T"/> to and from
	/// the string value of an XML attribute or element. This is ideal for very simple objects.
	/// </summary>
	public sealed class XTexter<T> : XTypeComponent<T>
	{
		private static readonly Func<T, string> toStringWriter = (x) => x.ToString();

		private Func<string, T> reader;
		private Func<T, string> writer;

		/// <summary>
		/// Create a new text reader/writer using the given delegates.
		/// </summary>
		public XTexter(Func<string, T> reader, Func<T, string> writer = null)
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
		public Func<T, string> Writer
		{
			get => writer ?? toStringWriter;
			set => writer = value;
		}

		/// <summary>
		/// Reads an object of type <typeparamref name="T"/> from the text of an element.
		/// </summary>
		protected override bool OnRead(XType<T> xType, IXReadOperation reader, XElement element, out T result)
		{
			result = Reader(XmlTools.GetElementText(element));
			return true;
		}

		/// <summary>
		/// Reads an object of type <typeparamref name="T"/> from the text of an attribute.
		/// </summary>
		protected override bool OnRead(XType<T> xType, IXReadOperation reader, XAttribute attribute, out T result)
		{
			result = Reader(attribute.Value);
			return true;
		}

		/// <summary>
		/// Write the object as a string into the given element.
		/// </summary>
		protected override bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XElement element)
		{
			element.Add(XmlTools.WriteText(Writer(obj)));
			return true;
		}

		/// <summary>
		/// Write the object as a string into the given attribute.
		/// </summary>
		protected override bool OnWrite(XType<T> xType, IXWriteOperation writer, T obj, XAttribute attribute)
		{
			attribute.Value = attribute.Value == null ? Writer(obj) : (attribute.Value + Writer(obj));
			return true;
		}
	}
}
