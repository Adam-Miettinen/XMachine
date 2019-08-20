using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using XMachine.Namers;

namespace XMachine
{
	/// <summary>
	/// The <see cref="XDomain"/> class hosts a set of <see cref="XType{TType}"/> objects and instructions on how to resolve
	/// them from XML. It can generate <see cref="XReader"/> and <see cref="XWriter"/> objects to perform serialization
	/// and deserialization of objects.
	/// </summary>
	public sealed class XDomain : IExceptionHandler
	{
		private static readonly MethodInfo makeXType =
			typeof(XDomain).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
				.FirstOrDefault(x => x.Name == nameof(MakeXType) && x.IsGenericMethodDefinition);

		private static readonly object staticLocker = new object();

		private static readonly IList<WeakReference<XDomain>> domains = new List<WeakReference<XDomain>>();
		private static XDomain global;

		/// <summary>
		/// A global instance of the <see cref="XDomain"/> class. The instance is not initialized until accessed for the first
		/// time. If set to null, it will be garbage collected (assuming no other references have been retained) and not 
		/// re-instantiated until accessed again.
		/// </summary>
		public static XDomain Global
		{
			get
			{
				if (global == null)
				{
					lock (staticLocker)
					{
						if (global == null)
						{
							global = new XDomain();
						}
					}
				}
				return global;
			}
			set
			{
				if (global != value)
				{
					lock (staticLocker)
					{
						if (global != value)
						{
							global = value;
						}
					}
				}
			}
		}

		/// <summary>
		/// Enumerate the <see cref="XDomain"/>s that exist in this <see cref="AppDomain"/>.
		/// </summary>
		public static IEnumerable<XDomain> Domains
		{
			get
			{
				lock (staticLocker)
				{
					for (int i = 0; i < domains.Count; i++)
					{
						if (domains[i].TryGetTarget(out XDomain domain))
						{
							yield return domain;
						}
						else
						{
							domains.RemoveAt(i--);
						}
					}
				}
			}
		}

		private readonly IDictionary<Type, XTypeBox> xTypes = new Dictionary<Type, XTypeBox>();
		private readonly object locker = new object();

		private Action<Exception> exceptionHandler;

		/// <summary>
		/// Create a new instance of <see cref="XDomain"/> using a new <see cref="DefaultXNamer"/>.
		/// </summary>
		public XDomain() : this(new DefaultXNamer()) { }

		/// <summary>
		/// Create a new <see cref="XDomain"/> instance with the given <see cref="XNamer"/>.
		/// </summary>
		public XDomain(XNamer namer)
		{
			Namer = namer ?? throw new ArgumentNullException($"{nameof(XDomain)} must be initialized with a non-null namer.");
			Namer.ExceptionHandler = ExceptionHandler;
			XComponents.OnXDomainCreated(this);
			domains.Add(new WeakReference<XDomain>(this));
		}

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? XmlTools.ThrowHandler;
			set => Namer.ExceptionHandler = exceptionHandler = value;
		}

		/// <summary>
		/// Get the <see cref="XNamer"/> in use.
		/// </summary>
		public XNamer Namer { get; }

		/// <summary>
		/// Get an <see cref="XType{T}"/> object that controls how the provided type <typeparamref name="T"/> 
		/// will be read from and written to XML.
		/// </summary>
		/// <returns>An instance of <see cref="XType{T}"/>.</returns>
		public XType<T> Reflect<T>() => XTypeBox.Unbox<T>(ReflectFromType(typeof(T)));

		/// <summary>
		/// Get a new <see cref="XReader"/> object that can be used to perform a read operation within this
		/// <see cref="XDomain"/>.
		/// </summary>
		/// <returns>An instance of <see cref="XReader"/>.</returns>
		public XReader GetReader()
		{
			XReader reader = new XReaderImpl(this);
			XComponents.OnReaderCreated(reader);
			return reader;
		}

		/// <summary>
		/// Get a new <see cref="XWriter"/> object that can be used to perform a write operation within this 
		/// <see cref="XDomain"/>.
		/// </summary>
		/// <returns>An instance of <see cref="XWriter"/>.</returns>
		public XWriter GetWriter()
		{
			XWriter writer = new XWriterImpl(this);
			XComponents.OnWriterCreated(writer);
			return writer;
		}

		/// <summary>
		/// Reset the <see cref="XDomain"/>, clearing it and its <see cref="Namer"/> of mappings between <see cref="Type"/>s,
		/// XML, and <see cref="XType{T}"/>s.
		/// </summary>
		public void Reset()
		{
			lock (locker)
			{
				xTypes.Clear();
			}
			Namer.Reset();
		}

		internal XTypeBox ReflectFromType(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			lock (locker)
			{
				// Check if XType created already, create one if not

				return xTypes.TryGetValue(type, out XTypeBox reflect)
					? reflect
					: MakeXType(type);
			}
		}

		internal XTypeBox ReflectElement(XElement element, Type declared, bool exceptionIfNotFound)
		{
			if (element == null)
			{
				throw new ArgumentNullException("Unexpected error: attempted to reflect a null XElement");
			}
			if (declared == null)
			{
				throw new ArgumentNullException($"Unexpected error: attempted to reflect XElement without a type context.");
			}

			Type reflect;

			lock (locker)
			{
				try
				{
					reflect = Namer.GetTypeInternal(element, declared);
				}
				catch (Exception e)
				{
					ExceptionHandler(new InvalidOperationException(
						$"Error while resolving element {element.Name} to type.", e));
					return null;
				}

				if (reflect == null)
				{
					if (exceptionIfNotFound)
					{
						ExceptionHandler(new InvalidOperationException(
							$"Could not resolve the element {element.Name} to a serializable Type."));
					}
					return null;
				}

				if (!declared.IsAssignableFrom(reflect))
				{
					if (exceptionIfNotFound)
					{
						ExceptionHandler(new InvalidOperationException(
							$"Element {element.Name} is Type {reflect.Name} and cannot be read as {declared.Name}"));
					}
					return null;
				}

				return xTypes.TryGetValue(reflect, out XTypeBox box) ? box : MakeXType(reflect);
			}
		}

		private XTypeBox MakeXType(Type type) =>
			(XTypeBox)makeXType.MakeGenericMethod(type).Invoke(this, null);

		private XTypeBox MakeXType<T>()
		{
			Type type = typeof(T);

			// Get a name

			XName name = Namer[type];
			if (name == null)
			{
				ExceptionHandler(new InvalidOperationException($"Namer failed to generate XName for {type.Name}."));
				return null;
			}

			// Instantiate a new XType

			XType<T> xType = new XType<T>(this, name);

			// Alert components

			XComponents.OnXTypeCreated(xType);
			xType.Initialize();
			XComponents.OnXTypeCreatedLate(xType);

			// Create an untyped "box" for the XType

			XTypeBox box = XTypeBox.Box(xType);
			xTypes.Add(type, box);

			return box;
		}
	}
}
