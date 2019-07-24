using System.Collections.Generic;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XKeyValuePair<TKey, TValue> : XTypeComponent<KeyValuePair<TKey, TValue>>
	{
		internal XKeyValuePair() { }

		protected override void OnBuild(XType<KeyValuePair<TKey, TValue>> xType, IXReadOperation reader,
			XElement element, ObjectBuilder<KeyValuePair<TKey, TValue>> objectBuilder)
		{
			XName keyName = XComponents.Component<XAutoCollections>().KeyName,
				valueName = XComponents.Component<XAutoCollections>().ValueName;

			XElement keyElement = element.Element(keyName);
			if (keyElement == null)
			{
				return;
			}

			bool foundKey = false, foundValue = false;
			TKey key = default;
			TValue value = default;

			reader.Read<TKey>(keyElement, x =>
				{
					key = x;
					return foundKey = true;
				},
				ReaderHints.IgnoreElementName);

			XElement valueElement = element.Element(valueName);
			if (valueElement != null)
			{
				reader.Read<TValue>(valueElement, x =>
					{
						value = x;
						return foundValue = true;
					},
					ReaderHints.IgnoreElementName);
			}
			else
			{
				foundValue = true;
			}

			objectBuilder.AddTask(() =>
			{
				if (foundKey && foundValue)
				{
					objectBuilder.Object = new KeyValuePair<TKey, TValue>(key, value);
					return true;
				}
				return false;
			});
		}

		protected override bool OnWrite(XType<KeyValuePair<TKey, TValue>> xType, IXWriteOperation writer,
			KeyValuePair<TKey, TValue> obj, XElement element)
		{
			XName keyName = XComponents.Component<XAutoCollections>().KeyName,
				valueName = XComponents.Component<XAutoCollections>().ValueName;

			if (obj.Key != null)
			{
				element.Add(writer.WriteTo(new XElement(keyName), obj.Key));

				if (obj.Value != default)
				{
					element.Add(writer.WriteTo(new XElement(valueName), obj.Value));
				}
			}

			return true;
		}
	}
}
