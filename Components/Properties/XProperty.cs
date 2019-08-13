using System;
using System.Xml.Linq;

namespace XMachine.Components.Properties
{
	/// <summary>
	/// Represents a property of type <typeparamref name="TProperty"/> on an object of type <typeparamref name="TType"/>.
	/// </summary>
	public abstract class XProperty<TType, TProperty>
	{
		/// <summary>
		/// Create an <see cref="XProperty{TType, TProperty}"/>.
		/// </summary>
		/// <param name="name">The <see cref="XName"/> to assign to <see cref="Name"/>.</param>
		protected XProperty(XName name) => Name = name;

		/// <summary>
		/// Get the <see cref="XName"/> of this property.
		/// </summary>
		public XName Name { get; }

		/// <summary>
		/// Get or set a predicate that decides whether to write this property to XML.
		/// </summary>
		public virtual Predicate<TType> WriteIf { get; set; }

		/// <summary>
		/// A <see cref="PropertyWriteMode"/> member defining the property's XML format.
		/// </summary>
		public virtual PropertyWriteMode WriteAs { get; set; }

		/// <summary>
		/// A <see cref="XObjectArgs"/> instance to be passed when the value of the property is read or written. It
		/// is recommended that you always set the <see cref="ObjectHints.IgnoreElementName"/> flag on
		/// <see cref="XObjectArgs.Hints"/>.
		/// </summary>
		public virtual XObjectArgs WithArgs { get; set; }

		/// <summary>
		/// Get the <see cref="Type"/> object corresponding to <typeparamref name="TProperty"/>.
		/// </summary>
		public virtual Type PropertyType => typeof(TProperty);

		/// <summary>
		/// Write this property as an attribute by setting <see cref="WriteAs"/> to <see cref="PropertyWriteMode.Attribute"/>.
		/// </summary>
		public void WriteAsAttribute() => WriteAs = PropertyWriteMode.Attribute;

		/// <summary>
		/// Write this property as an element by setting <see cref="WriteAs"/> to <see cref="PropertyWriteMode.Element"/>.
		/// </summary>
		public void WriteAsElement() => WriteAs = PropertyWriteMode.Element;

		/// <summary>
		/// Write this property as inner text by setting <see cref="WriteAs"/> to <see cref="PropertyWriteMode.Text"/>.
		/// </summary>
		public void WriteAsText() => WriteAs = PropertyWriteMode.Text;

		/// <summary>
		/// Override this method to provide a get accessor.
		/// </summary>
		/// <param name="obj">The instance of <typeparamref name="TType"/> from which the property value should be got.</param>
		/// <returns>The value of the property on <paramref name="obj"/>.</returns>
		public abstract TProperty Get(TType obj);

		/// <summary>
		/// Override this method to provide a set accessor.
		/// </summary>
		/// <param name="obj">The instance of <typeparamref name="TType"/> on which the property value should be set.</param>
		/// <param name="value">The <typeparamref name="TProperty"/> value to which the property should be set.</param>
		public abstract void Set(TType obj, TProperty value);
	}
}
