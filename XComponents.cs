﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XMachine.Components;
using XMachine.Components.Collections;
using XMachine.Components.Constructors;
using XMachine.Components.Identifiers;
using XMachine.Components.Properties;
using XMachine.Components.Rules;

namespace XMachine
{
	/// <summary>
	/// The static <see cref="XComponents"/> class manages loaded <see cref="XMachineComponent"/>s.
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
					foreach (Assembly assembly in loadedAssemblies)
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

		private static readonly Queue<Exception> startupErrors;

		private static readonly XMachineComponents componentManager;
		private static readonly object staticLocker = new object();
		private static bool isInitializedStatic = false;

		private static readonly HashSet<Assembly> loadedAssemblies;

		private static Action<Exception> exceptionHandler;

		static XComponents()
		{
			loadedAssemblies = new HashSet<Assembly>();

			componentManager = new XMachineComponents();

			startupErrors = new Queue<Exception>();
			ExceptionHandler = (e) => startupErrors.Enqueue(e);
		}

		/// <summary>
		/// A collection of <see cref="Exception"/>s that were generated during <see cref="XDomain"/>'s initialization. These
		/// usually result from a failure to process a custom method tagged with one of the <see cref="XMachine"/>
		/// attributes.
		/// </summary>
		public static IEnumerable<Exception> StartupErrors
		{
			get
			{
				while (startupErrors.Count > 0)
				{
					yield return startupErrors.Dequeue();
				}
			}
		}

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public static Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? XmlTools.ThrowHandler;
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
		/// Retrieve a component of type <typeparamref name="V"/>.
		/// </summary>
		public static V Component<V>() where V : XMachineComponent
		{
			lock (ComponentManager)
			{
				return ComponentManager.Component<V>();
			}
		}

		/// <summary>
		/// Retrieve all components of type <typeparamref name="V"/>.
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
		/// <param name="component">The component to register.</param>
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
		/// <param name="components">The components to register.</param>
		public static void Register(params XMachineComponent[] components)
		{
			lock (ComponentManager)
			{
				ComponentManager.Register(components);
			}
		}

		/// <summary>
		/// Deregister a component.
		/// </summary>
		/// <param name="component">The component to deregister.</param>
		public static void Deregister(XMachineComponent component)
		{
			lock (ComponentManager)
			{
				ComponentManager.Deregister(component);
			}
		}

		/// <summary>
		/// Deregister components.
		/// </summary>
		/// <param name="components">The components to deregister.</param>
		public static void Deregister(params XMachineComponent[] components)
		{
			lock (ComponentManager)
			{
				ComponentManager.Deregister(components);
			}
		}

		internal static void OnXDomainCreated(XDomain domain)
		{
			InitializeStatic();

			foreach (Assembly assembly in loadedAssemblies)
			{
				if (IsAssemblyNamerEligible(assembly))
				{
					foreach (Type type in GetAssemblyTypes(assembly))
					{
						domain.Namer.InspectType(type);
					}
				}
			}

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

		internal static void OnReaderCreated(XReader reader)
		{
			lock (ComponentManager)
			{
				ComponentManager.CreateReader(reader);
			}
		}

		internal static void OnWriterCreated(XWriter writer)
		{
			lock (ComponentManager)
			{
				ComponentManager.CreateWriter(writer);
			}
		}

		internal static void ResetAllXDomains()
		{
			foreach (XDomain domain in XDomain.Domains)
			{
				domain.Reset();

				foreach (Assembly assembly in loadedAssemblies)
				{
					if (IsAssemblyNamerEligible(assembly))
					{
						foreach (Type type in GetAssemblyTypes(assembly))
						{
							domain.Namer.InspectType(type);
						}
					}
				}
			}
		}

		private static void InitializeStatic()
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
				new XRules()
			);

			AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
			{
				if (args.LoadedAssembly != null)
				{
					ScanAssembly(args.LoadedAssembly, Components());
				}
			};

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				ScanAssembly(assembly, Components());
			}
		}

		private static void ScanAssembly(Assembly assembly, IEnumerable<XMachineComponent> components)
		{
			if (assembly.IsDynamic)
			{
				return;
			}

			_ = loadedAssemblies.Add(assembly);

			XMachineAssemblyAttribute attr = assembly.GetCustomAttribute<XMachineAssemblyAttribute>();
			if (attr != null)
			{
				XmlTools.SetXMachineAssemblyAttribute(assembly, attr);
			}

			IEnumerable<Type> assemblyTypes = null;

			if (IsAssemblyNamerEligible(assembly))
			{
				assemblyTypes = GetAssemblyTypes(assembly);

				// Alert namers

				foreach (XDomain domain in XDomain.Domains)
				{
					foreach (Type assemblyType in assemblyTypes)
					{
						domain.Namer.InspectType(assemblyType);
					}
				}
			}

			if (components?.Any() == true && IsAssemblyComponentEligible(assembly))
			{
				if (assemblyTypes == null)
				{
					assemblyTypes = GetAssemblyTypes(assembly);
				}

				// Alert components

				foreach (XMachineComponent comp in components)
				{
					foreach (Type assemblyType in assemblyTypes)
					{
						try
						{
							comp.InspectType(assemblyType);
						}
						catch (Exception e)
						{
							ExceptionHandler(new ComponentException(comp, e));
						}
					}
				}
			}
		}

		private static bool IsAssemblyNamerEligible(Assembly assembly) =>
			XmlTools.ScanUnknownAssemblies ||
				XmlTools.GetXMachineAssemblyAttribute(assembly) != null ||
				assembly == typeof(object).Assembly;

		private static bool IsAssemblyComponentEligible(Assembly assembly) =>
			XmlTools.GetXMachineAssemblyAttribute(assembly) != null;

		private static IEnumerable<Type> GetAssemblyTypes(Assembly assembly)
		{
			IEnumerable<Type> exportedTypes;

			try
			{
				exportedTypes = assembly.ExportedTypes;
			}
			catch
			{
				return Enumerable.Empty<Type>();
			}

			List<Type> types = new List<Type>(256);

			foreach (Type type in exportedTypes)
			{
				AddTypesRecursive(type);
			}

			void AddTypesRecursive(Type type)
			{
				if (!type.IsArray && !type.IsCOMObject && !type.IsImport && (!type.IsGenericType || type.IsGenericTypeDefinition))
				{
					types.Add(type);

					Type[] nestedTypes = type.GetNestedTypes();
					for (int i = 0; i < nestedTypes.Length; i++)
					{
						AddTypesRecursive(nestedTypes[i]);
					}
				}
			}

			return types;
		}
	}
}
