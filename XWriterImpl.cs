using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace XMachine
{
	internal sealed class XWriterImpl : XWriter, IXWriteOperation
	{
		private int writeTimeout;
		private Stopwatch stopwatch;

		internal XWriterImpl(XDomain domain) : base(domain) { }

		public override int WriteTimeout
		{
			get => writeTimeout;
			set
			{
				writeTimeout = value;
				if (writeTimeout > 0 && stopwatch == null)
				{
					stopwatch = new Stopwatch();
				}
				else if (writeTimeout <= 0 && stopwatch != null)
				{
					stopwatch.Reset();
					stopwatch = null;
				}
			}
		}

		/*
		 * API Methods (XWriter)
		 */

		public override XElement Write(object obj)
		{
			stopwatch?.Restart();
			XElement element = WriteElement(obj, obj == null ? typeof(object) : obj.GetType());
			stopwatch?.Reset();
			return element;
		}

		public override IEnumerable<XElement> WriteAll(IEnumerable objects)
		{
			if (objects == null)
			{
				throw new ArgumentNullException(nameof(objects));
			}

			stopwatch?.Restart();
			List<XElement> elements = new List<XElement>();

			foreach (object obj in objects)
			{
				elements.Add(WriteElement(obj, obj == null ? typeof(object) : obj.GetType()));
			}

			stopwatch?.Reset();
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
			if (objects == null)
			{
				throw new ArgumentNullException(nameof(objects));
			}

			foreach (object obj in objects)
			{
				Submit(obj);
			}
		}

		/*
		 * Backend methods (IXWriteOperation)
		 */

		public XElement WriteElement<T>(T obj, XObjectArgs args = null) => WriteElement(obj, typeof(T), args);

		public XElement WriteElement(object obj, XObjectArgs args = null) => WriteElement(obj, typeof(object), args);

		public XElement WriteElement(object obj, Type expectedType, XObjectArgs args = null)
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
				expected = Domain.ReflectFromType(expectedType);
				return expected == null ? null : new XElement(expected.XName);
			}

			// Reflect the object type

			if ((reflect = Domain.ReflectFromType(obj.GetType())) == null ||
				(expected = Domain.ReflectFromType(expectedType)) == null)
			{
				return null;
			}

			// Write

			XElement element = WriteToElement(new XElement(reflect.XName), obj, reflect, args);

			// Return the element (or for explicitly typed elements, the element wrapped)

			return reflect.Type == expectedType
				? element
				: new XElement(expected.XName, element);
		}

		public XElement WriteTo<T>(XElement element, T obj, XObjectArgs args = null) => WriteTo(element, obj, typeof(T), args);

		public XElement WriteTo(XElement element, object obj, XObjectArgs args = null) => WriteTo(element, obj, typeof(object), args);

		public XElement WriteTo(XElement element, object obj, Type expectedType, XObjectArgs args = null)
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

			// Reflect the type

			XTypeBox reflect = Domain.ReflectFromType(obj.GetType());
			if (reflect == null)
			{
				return element;
			}

			// Write contents

			if (reflect.Type == expectedType)
			{
				return WriteToElement(element, obj, reflect, args);
			}
			else
			{
				element.Add(WriteToElement(new XElement(reflect.XName), obj, reflect, args));
				return element;
			}
		}

		public XAttribute WriteAttribute(object obj, XName xName, XObjectArgs args = null)
		{
			CheckWatch();

			if (xName == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(xName)));
				return null;
			}

			// Write contents to a new attribute and return

			return WriteTo(new XAttribute(xName, string.Empty), obj, args);
		}

		public XAttribute WriteTo(XAttribute attribute, object obj, XObjectArgs args = null)
		{
			CheckWatch();

			if (attribute == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(attribute)));
				return attribute;
			}

			// If null, leave it blank

			if (obj == null)
			{
				attribute.Value = string.Empty;
				return attribute;
			}

			// Reflect the (runtime) type

			XTypeBox reflect = Domain.ReflectFromType(obj.GetType());
			if (reflect == null)
			{
				return attribute;
			}

			// Write with XWriterComponents

			if (ForEachComponent((comp) => reflect.OnComponentWrite(comp, this, obj, attribute, args)))
			{
				Submit(obj);
				return attribute;
			}

			// Write with XTypeComponents

			Submit(obj);
			_ = reflect.OnWrite(this, obj, attribute, args);

			return attribute;
		}

		private XElement WriteToElement(XElement element, object obj, XTypeBox reflect, XObjectArgs args = null)
		{
			CheckWatch();

			// Write with XWriterComponents
			
			if (ForEachComponent((comp) => reflect.OnComponentWrite(comp, this, obj, element, args)))
			{
				Submit(obj);
				return element;
			}

			// Write with XTypeComponents

			Submit(obj);
			_ = reflect.OnWrite(this, obj, element, args);

			return element;
		}

		private void CheckWatch()
		{
			if (WriteTimeout <= 0)
			{
				return;
			}
			if (stopwatch.ElapsedMilliseconds > WriteTimeout)
			{
				// Took too long, abort

				stopwatch.Reset();
				throw new TimeoutException(
					$"{nameof(XWriter)} was unable to complete its write operation within {WriteTimeout} milliseconds.");
			}
		}
	}
}
