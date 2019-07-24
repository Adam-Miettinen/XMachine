using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XMachine.Components;
using XMachine.Components.Collections;
using XMachine.Components.Constructors;
using XMachine.Components.Identifiers;
using XMachine.Components.Properties;

namespace XMachine
{
	/// <summary>
	/// <see cref="XComponents"/> manages loaded <see cref="XMachineComponent"/>s and provides convenient extension methods to
	/// customize their behaviour.
	/// </summary>
	public static class XComponents
	{
		private class XMachineComponents : XWithComponents<XMachineComponent>
		{
			protected override void OnComponentsRegistered(IEnumerable<XMachineComponent> components)
			{
				base.OnComponentsRegistered(components);

				if (isInitializedStatic)
				{
					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						ScanAssembly(assembly, components.ToArray());
					}
				}
			}

			internal void CreateXType<T>(XType<T> xType) => ForEachComponent((comp) => comp.CreateXType(xType));

			internal void CreateXTypeLate<T>(XType<T> xType) => ForEachComponent((comp) => comp.CreateXTypeLate(xType));

			internal void CreateDomain(XDomain domain) => ForEachComponent((comp) => comp.CreateDomain(domain));

			internal void CreateReader(XReader reader) => ForEachComponent((comp) => comp.CreateReader(reader));

			internal void CreateWriter(XWriter writer) => ForEachComponent((comp) => comp.CreateWriter(writer));
		}

		internal static readonly Action<Exception> ThrowHandler = (e) => throw e;

		private static readonly ICollection<Exception> startupErrors;

		private static readonly IList<WeakReference<XDomain>> domains;

		private static readonly XMachineComponents componentManager;
		private static readonly object staticLocker = new object();
		private static bool isInitializedStatic = false;

		private static Action<Exception> exceptionHandler;

		static XComponents()
		{
			domains = new List<WeakReference<XDomain>>();

			componentManager = new XMachineComponents();

			startupErrors = new List<Exception>();
			ExceptionHandler = (e) => startupErrors.Add(e);
		}

		/// <summary>
		/// A collection of <see cref="Exception"/>s that were generated during <see cref="XDomain"/>'s initialization. These
		/// usually result from a failure to process a custom method tagged with one of the <see cref="XMachine"/>
		/// attributes.
		/// </summary>
		public static IEnumerable<Exception> StartupErrors => startupErrors;

		/// <summary>
		/// Get or set a delegate that will handle exceptions.
		/// </summary>
		public static Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? ThrowHandler;
			set
			{
				exceptionHandler = value;
				ComponentManager.ExceptionHandler = ExceptionHandler;
			}
		}

		private static XMachineComponents ComponentManager
		{
			get
			{
				InitializeStatic();
				return componentManager;
			}
		}

		/// <summary>
		/// Retrieve a component of the given type.
		/// </summary>
		public static V Component<V>() where V : XMachineComponent
		{
			lock (ComponentManager)
			{
				return ComponentManager.Component<V>();
			}
		}

		/// <summary>
		/// Retrieve all components of the given type.
		/// </summary>
		public static IEnumerable<V> Components<V>() where V : XMachineComponent
		{
			lock (ComponentManager)
			{
				return ComponentManager.Components<V>().ToArray();
			}
		}

		/// <summary>
		/// Retrieve all components.
		/// </summary>
		public static IEnumerable<XMachineComponent> Components()
		{
			lock (ComponentManager)
			{
				return ComponentManager.Components();
			}
		}

		/// <summary>
		/// Register a component.
		/// </summary>
		public static void Register(XMachineComponent component)
		{
			lock (ComponentManager)
			{
				ComponentManager.Register(component);
			}
		}

		/// <summary>
		/// Register components.
		/// </summary>
		public static void Register(params XMachineComponent[] components)
		{
			lock (ComponentManager)
			{
				ComponentManager.Register(components);
			}
		}

		/// <summary>
		/// Deegister a component.
		/// </summary>
		public static void Deregister(XMachineComponent component)
		{
			lock (ComponentManager)
			{
				ComponentManager.Deregister(component);
			}
		}

		/// <summary>
		/// Deegister components.
		/// </summary>
		public static void Deegister(params XMachineComponent[] components)
		{
			lock (ComponentManager)
			{
				ComponentManager.Deregister(components);
			}
		}

		/// <summary>
		/// Retrieve the collection of global identifiers in the <see cref="XIdentifiers"/> component, if it exists.
		/// </summary>
		public static XCompositeIdentifier Identifier() => Component<XIdentifiers>()?.Identifier;

		/// <summary>
		/// Add an <see cref="XIdentifier{TType, TId}"/> characterized by the given delegate to the global identifiers 
		/// used by the <see cref="XIdentifiers"/> component, if it exists.
		/// </summary>
		public static void Identify<TType, TId>(Func<TType, TId> identifier, IEqualityComparer<TId> keyComparer = null)
			where TType : class where TId : class =>
			Component<XIdentifiers>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));

		internal static void OnXDomainCreated(XDomain domain)
		{
			for (int i = 0; i < domains.Count; i++)
			{
				if (!domains[i].TryGetTarget(out XDomain d))
				{
					domains.RemoveAt(i--);
				}
			}
			domains.Add(new WeakReference<XDomain>(domain));

			lock (ComponentManager)
			{
				ComponentManager.CreateDomain(domain);
			}
		}

		internal static void OnXTypeCreated<T>(XType<T> xType)
		{
			lock (ComponentManager)
			{
				ComponentManager.CreateXType(xType);
			}
		}

		internal static void OnXTypeCreatedLate<T>(XType<T> xType)
		{
			lock (ComponentManager)
			{
				ComponentManager.CreateXTypeLate(xType);
			}
		}

		internal static XReader GetReader(XDomain domain)
		{
			XReader reader = new XReaderImpl(domain);

			lock (ComponentManager)
			{
				ComponentManager.CreateReader(reader);
			}

			return reader;
		}

		internal static XWriter GetWriter(XDomain domain)
		{
			XWriter writer = new XWriterImpl(domain);

			lock (ComponentManager)
			{
				ComponentManager.CreateWriter(writer);
			}

			return writer;
		}

		internal static void InitializeStatic()
		{
			if (isInitializedStatic)
			{
				return;
			}
			lock (staticLocker)
			{
				if (isInitializedStatic)
				{
					return;
				}
				isInitializedStatic = true;
			}

			Register(
				new XAutoConstructors(),
				new XAutoProperties(),
				new XAutoCollections(),
				new XDefaultTypes(),
				new XIdentifiers(),
				new XTypeRules()
			);

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			AppDomain.CurrentDomain.AssemblyLoad += (sender, args) => ScanAssembly(args.LoadedAssembly, Components());

			ExceptionHandler = ThrowHandler;
		}

		private static void ScanAssembly(Assembly assembly, IEnumerable<XMachineComponent> components)
		{
			if (assembly == null)
			{
				return;
			}

			// Alert XNamers

			for (int i = 0; i < domains.Count; i++)
			{
				if (domains[i].TryGetTarget(out XDomain domain))
				{
					domain.Namer.Scan(assembly);
				}
				else
				{
					domains.RemoveAt(i--);
				}
			}

			// Alert XMachineComponents

			if (assembly.IsXIgnored() ||
				assembly.GetCustomAttribute<XMachineAssemblyAttribute>() == null ||
				components == null ||
				!components.Any())
			{
				return;
			}

			List<Type> types = assembly.ExportedTypes.Where(
				x => !x.IsXIgnored() &&
				!x.IsArray && !x.IsCOMObject && !x.IsImport &&
				(!x.IsGenericType || x.IsGenericTypeDefinition)).ToList();

			IEnumerator<Type> enumerator = types.GetEnumerator();

			foreach (XMachineComponent comp in components)
			{
				while (enumerator.MoveNext())
				{
					comp.InspectType(enumerator.Current);
				}
				enumerator.Reset();
			}
		}
	}
}
