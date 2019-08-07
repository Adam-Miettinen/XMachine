using System;

namespace XMachine
{
	/// <summary>
	/// Implement this class to extend the functionality of <see cref="XMachine"/>. Register your component
	/// with <see cref="XComponents.Register(XMachineComponent[])"/>. An <see cref="XMachineComponent"/> will 
	/// be fed <see cref="Type"/> objects scanned from loaded assemblies that have the <see cref="XMachineAssemblyAttribute"/>, 
	/// then given the opportunity to modify <see cref="XDomain"/>, <see cref="XReader"/>, 
	/// <see cref="XWriter"/> and <see cref="XType{TType}"/> objects as they are instantiated.
	/// </summary>
	public abstract class XMachineComponent : IXComponent, IExceptionHandler
	{
		private Action<Exception> exceptionHandler;

		/// <summary>
		/// Whether the component is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// A delegate that handles exceptions thrown by this <see cref="XMachineComponent"/>. By default, it will use
		/// <see cref="XComponents.ExceptionHandler"/>.
		/// </summary>
		public Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? XComponents.ExceptionHandler;
			set => exceptionHandler = value;
		}

		/// <summary>
		/// Called when <see cref="XMachine"/> is initialized and scans <see cref="Type"/> objects from assemblies
		/// tagged with <see cref="XMachineAssemblyAttribute"/>.
		/// </summary>
		protected virtual void OnInspectType(Type type)
		{

		}

		/// <summary>
		/// Called when a new instance of <see cref="XDomain"/> is created.
		/// </summary>
		protected virtual void OnCreateDomain(XDomain domain)
		{

		}

		/// <summary>
		/// Called when a new instance of <see cref="XType{TType}"/> is created.
		/// </summary>
		protected virtual void OnCreateXType<T>(XType<T> xType)
		{

		}

		/// <summary>
		/// Called when a new instance of <see cref="XType{TType}"/> is created, after <see cref="OnCreateXType{T}(XType{T})"/>.
		/// </summary>
		protected virtual void OnCreateXTypeLate<T>(XType<T> xType)
		{

		}

		/// <summary>
		/// Called when a new <see cref="XReader"/> is created.
		/// </summary>
		protected virtual void OnCreateReader(XReader reader)
		{

		}

		/// <summary>
		/// Called when a new <see cref="XWriter"/> is created.
		/// </summary>
		protected virtual void OnCreateWriter(XWriter writer)
		{

		}

		internal void InspectType(Type type) => OnInspectType(type);

		internal void CreateDomain(XDomain domain) => OnCreateDomain(domain);

		internal void CreateXType<T>(XType<T> xType) => OnCreateXType(xType);

		internal void CreateXTypeLate<T>(XType<T> xType) => OnCreateXTypeLate(xType);

		internal void CreateReader(XReader reader) => OnCreateReader(reader);

		internal void CreateWriter(XWriter writer) => OnCreateWriter(writer);
	}
}
