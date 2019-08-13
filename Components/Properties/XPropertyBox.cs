using System;
using System.Xml.Linq;

namespace XMachine.Components.Properties
{
	internal sealed class XPropertyBox<TType> : XProperty<TType, object>
	{
		internal static XPropertyBox<TType> Box<TProperty>(XProperty<TType, TProperty> property) =>
			new XPropertyBox<TType>(property, property.Name)
			{
				writeIf = () => property.WriteIf,
				writeAs = () => property.WriteAs,
				withArgs = () => property.WithArgs,
				propertyType = property.PropertyType,
				getter = (obj) => property.Get(obj),
				setter = (obj, value) => property.Set(obj, (TProperty)value)
			};

		internal static XProperty<TType, TProperty> Unbox<TProperty>(XPropertyBox<TType> boxed) =>
			boxed.innerProperty as XProperty<TType, TProperty>;

		private readonly object innerProperty;

		private Type propertyType;
		private Func<TType, object> getter;
		private Action<TType, object> setter;

		private Func<Predicate<TType>> writeIf;
		private Func<PropertyWriteMode> writeAs;
		private Func<XObjectArgs> withArgs;

		private XPropertyBox(object property, XName name) : base(name) => innerProperty = property;

		public override Type PropertyType => propertyType;

		public override PropertyWriteMode WriteAs => writeAs();

		public override Predicate<TType> WriteIf => writeIf();

		public override XObjectArgs WithArgs => withArgs();

		public override object Get(TType obj) => getter(obj);

		public override void Set(TType obj, object value) => setter(obj, value);
	}
}
