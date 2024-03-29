﻿using System.Collections;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XDictionaryEntry : XBuilder<DictionaryEntry>
	{
		public XDictionaryEntry() { }

		protected override void OnBuild(XType<DictionaryEntry> xType, IXReadOperation reader,
			XElement element, ObjectBuilder<DictionaryEntry> objectBuilder, XObjectArgs args)
		{
			XName keyName = XComponents.Component<XAutoCollections>().KeyName,
				valueName = XComponents.Component<XAutoCollections>().ValueName;

			XElement keyElement = element.Element(keyName);
			if (keyElement == null)
			{
				return;
			}

			bool foundKey = false, foundValue = false;
			object key = null, value = null;

			reader.Read(keyElement, x =>
			{
				key = x;
				return foundKey = true;
			},
			XObjectArgs.DefaultIgnoreElementName);

			XElement valueElement = element.Element(valueName);
			if (valueElement != null)
			{
				reader.Read(valueElement, x =>
				{
					value = x;
					return foundValue = true;
				},
				XObjectArgs.DefaultIgnoreElementName);
			}
			else
			{
				foundValue = true;
			}

			reader.AddTask(this, () =>
			{
				if (foundKey && foundValue)
				{
					objectBuilder.Object = new DictionaryEntry(key, value);
					return true;
				}
				return false;
			});
		}

		protected override bool OnWrite(XType<DictionaryEntry> xType, IXWriteOperation writer,
			DictionaryEntry obj, XElement element, XObjectArgs args)
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
