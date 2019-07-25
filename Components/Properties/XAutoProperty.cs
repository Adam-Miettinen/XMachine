using System.Reflection;
using System.Xml.Serialization;
using XMachine.Reflection;

namespace XMachine.Components.Properties
{
	internal sealed class XAutoProperty<TType, TProperty> : XProperty<TType, TProperty>
	{
		private readonly PropertyInfo propertyInfo;

		internal XAutoProperty(PropertyInfo propertyInfo) :
			base(propertyInfo.GetXmlNameFromAttributes() ?? propertyInfo.Name)
		{
			this.propertyInfo = propertyInfo;

			if (propertyInfo.HasCustomAttribute<XmlAttributeAttribute>())
			{
				WriteAs = PropertyWriteMode.Attribute;
			}
			else if (propertyInfo.HasCustomAttribute<XmlTextAttribute>())
			{
				WriteAs = PropertyWriteMode.Text;
			}
		}

		public override TProperty Get(TType obj) => (TProperty)propertyInfo.GetValue(obj);

		public override void Set(TType obj, TProperty value) => propertyInfo.SetValue(obj, value);
	}
}
