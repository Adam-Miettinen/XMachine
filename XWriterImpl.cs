using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace XMachine
{
	internal sealed class XWriterImpl : XWriter, IXWriteOperation
	{
		private readonly XDomain domain;

		private readonly Stopwatch stopwatch = new Stopwatch();

		internal XWriterImpl(XDomain domain)
		{
			this.domain = domain;
			ExceptionHandler = domain.ExceptionHandler;
		}

		public override XElement Write(object obj)
		{
			stopwatch.Restart();
			XElement element = WriteElement(obj, obj.GetType());
			stopwatch.Reset();
			return element;
		}

		public override IEnumerable<XElement> WriteAll(IEnumerable objects)
		{
			if (objects == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(objects)));
				return Enumerable.Empty<XElement>();
			}

			stopwatch.Restart();
			List<XElement> elements = new List<XElement>();

			foreach (object obj in objects)
			{
				elements.Add(WriteElement(obj, obj.GetType()));
			}

			stopwatch.Reset();
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
			CheckWatch();

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
			CheckWatch();

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
			CheckWatch();

			if (xName == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(xName)));
				return null;
			}

			return WriteTo(new XAttribute(xName, string.Empty), obj);
		}

		public XAttribute WriteTo(XAttribute attribute, object obj)
		{
			CheckWatch();

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
			CheckWatch();

			if (ForEachComponent((comp) => comp.Write(this, obj, element)))
			{
				Submit(obj);
				return element;
			}

			Submit(obj);
			_ = reflect.OnWrite(this, obj, element);
			return element;
		}

		private void CheckWatch()
		{
			if (stopwatch.ElapsedMilliseconds > WriteTimeout)
			{
				stopwatch.Reset();
				throw new TimeoutException(
					$"{nameof(XWriter)} was unable to complete its write operation within {WriteTimeout} milliseconds.");
			}
		}
	}
}
