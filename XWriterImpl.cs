using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMachine
{
	internal sealed class XWriterImpl : XWriter, IXWriteOperation
	{
		private readonly XDomain domain;

		internal XWriterImpl(XDomain domain)
		{
			this.domain = domain;
			ExceptionHandler = domain.ExceptionHandler;
		}

		public override XElement Write(object obj) => WriteElement(obj, obj.GetType());

		public override IEnumerable<XElement> WriteAll(IEnumerable objects)
		{
			if (objects == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(objects)));
				return Enumerable.Empty<XElement>();
			}

			List<XElement> elements = new List<XElement>();

			foreach (object obj in objects)
			{
				elements.Add(Write(obj));
			}

			return elements;
		}

		public override void Submit(object obj)
		{
			if (obj != null)
			{
				ForEachComponent((comp) => comp.Submit(this, obj));
			}
		}

		public override void SubmitAll(IEnumerable objects)
		{
			if (objects != null)
			{
				foreach (object obj in objects)
				{
					Submit(obj);
				}
			}
		}

		public XElement WriteElement<T>(T obj) => WriteElement(obj, typeof(T));

		public XElement WriteElement(object obj) => WriteElement(obj, typeof(object));

		public XElement WriteElement(object obj, Type expectedType)
		{
			if (expectedType == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(expectedType)));
				return null;
			}

			XTypeBox reflect, expected;

			// If null, we're done

			if (obj == null)
			{
				expected = domain.ReflectFromType(expectedType);
				return expected == null ? null : new XElement(expected.XName);
			}

			// Reflect the object type

			if ((reflect = domain.ReflectFromType(obj.GetType())) == null ||
				(expected = domain.ReflectFromType(expectedType)) == null)
			{
				return null;
			}

			// Write

			XElement element = WriteToElement(new XElement(reflect.XName), obj, reflect);

			// Return the element (or for explicitly typed elements, the element wrapped)

			return reflect.Type == expectedType
				? element
				: new XElement(expected.XName, element);
		}

		public XElement WriteTo<T>(XElement element, T obj) => WriteTo(element, obj, typeof(T));

		public XElement WriteTo(XElement element, object obj) => WriteTo(element, obj, typeof(object));

		public XElement WriteTo(XElement element, object obj, Type expectedType)
		{
			if (element == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(element)));
				return element;
			}
			if (expectedType == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(expectedType)));
				return element;
			}
			if (obj == null)
			{
				return element;
			}

			// Reflect

			XTypeBox reflect = domain.ReflectFromType(obj.GetType());
			if (reflect == null)
			{
				return element;
			}

			if (reflect.Type == expectedType)
			{
				return WriteToElement(element, obj, reflect);
			}
			else
			{
				element.Add(WriteToElement(new XElement(reflect.XName), obj, reflect));
				return element;
			}
		}

		public XAttribute WriteAttribute(object obj, XName xName)
		{
			if (xName == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(xName)));
				return null;
			}

			return WriteTo(new XAttribute(xName, string.Empty), obj);
		}

		public XAttribute WriteTo(XAttribute attribute, object obj)
		{
			if (attribute == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(attribute)));
				return attribute;
			}

			if (obj == null)
			{
				attribute.Value = string.Empty;
				return attribute;
			}

			// Reflect the runtime type

			XTypeBox reflect = domain.ReflectFromType(obj.GetType());
			if (reflect == null)
			{
				return attribute;
			}

			// Start writing

			if (ForEachComponent((comp) => comp.Write(this, obj, attribute)))
			{
				Submit(obj);
				return attribute;
			}

			// Let XType extensions have a go

			Submit(obj);
			_ = reflect.OnWrite(this, obj, attribute);
			return attribute;
		}

		private XElement WriteToElement(XElement element, object obj, XTypeBox reflect)
		{
			if (ForEachComponent((comp) => comp.Write(this, obj, element)))
			{
				Submit(obj);
				return element;
			}

			Submit(obj);
			_ = reflect.OnWrite(this, obj, element);
			return element;
		}
	}
}
