using System;
using System.Xml.Linq;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// Represents a property of type <typeparamref name="TProperty"/> on an object of type <typeparamref name="TType"/>
	/// and contains instructions on how it should be read from and written to XML.
	/// </summary>
	public abstract class XProperty<TType, TProperty>
	{
		/// <summary>
		/// Create an <see cref="XProperty{TType, TProperty}"/> with the given <see cref="XName"/>,
		/// which will be assigned to the readonly property <see cref="Name"/>.
		/// </summary>
		protected XProperty(XName name) => Name = name;

		/// <summary>
		/// The readonly <see cref="XName"/> of this property.
		/// </summary>
		public XName Name { get; }

		/// <summary>
		/// A predicate that tests whether to write this property to XML.
		/// </summary>
		public virtual Predicate<TType> WriteIf { get; set; }

		/// <summary>
		/// How to write the property.
		/// </summary>
		public virtual PropertyWriteMode WriteAs { get; set; }

		/// <summary>
		/// Write this property as an attribute.
		/// </summary>
		public void WriteAsAttribute() => WriteAs = PropertyWriteMode.Attribute;

		/// <summary>
		/// Write this property as an element.
		/// </summary>
		public void WriteAsElement() => WriteAs = PropertyWriteMode.Element;

		/// <summary>
		/// Write this property as inner element text.
		/// </summary>
		public void WriteAsText() => WriteAs = PropertyWriteMode.Text;

		/// <summary>
		/// This method must return a <see cref="Type"/> object corresponding to <typeparamref name="TProperty"/>.
		/// </summary>
		public virtual Type PropertyType => typeof(TProperty);

		/// <summary>
		/// Implementers must provide a get accessor for the property.
		/// </summary>
		public abstract TProperty Get(TType obj);

		/// <summary>
		/// Implementers must provide a set accessor for the property.
		/// </summary>
		public abstract void Set(TType obj, TProperty value);
	}
}
