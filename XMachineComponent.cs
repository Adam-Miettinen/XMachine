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
		/// Create a new instance of <see cref="XMachineComponent"/>.
		/// </summary>
		protected XMachineComponent() { }

		/// <summary>
		/// Get or set whether the component is enabled and should be invoked by its owner.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s. By default, it will use
		/// <see cref="XComponents.ExceptionHandler"/>.
		/// </summary>
		public virtual Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? XComponents.ExceptionHandler;
			set => exceptionHandler = value;
		}

		/// <summary>
		/// Called when <see cref="XComponents"/> detects that a new assembly has been loaded, and the assembly is
		/// tagged with <see cref="XMachineAssemblyAttribute"/>. All public <see cref="Type"/> objects defined in
		/// the assembly will be passed to this method excluding arrays, constructed generics, COM objects, and 
		/// imported types.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> object to inspect.</param>
		protected virtual void OnInspectType(Type type) { }

		/// <summary>
		/// Called when a new instance of <see cref="XDomain"/> is created.
		/// </summary>
		/// <param name="domain">The new instance of <see cref="XDomain"/>.</param>
		protected virtual void OnCreateDomain(XDomain domain) { }

		/// <summary>
		/// Called when a new instance of <see cref="XType{TType}"/> is created.
		/// </summary>
		/// <param name="xType">The new instance of <see cref="XType{T}"/>.</param>
		protected virtual void OnCreateXType<T>(XType<T> xType) { }

		/// <summary>
		/// Called when a new instance of <see cref="XType{TType}"/> is created, after <see cref="OnCreateXType{T}(XType{T})"/>
		/// and after <see cref="XTypeComponent{T}.Initialize"/>.
		/// </summary>
		/// <param name="xType">The new instance of <see cref="XType{T}"/>.</param>
		protected virtual void OnCreateXTypeLate<T>(XType<T> xType) { }

		/// <summary>
		/// Called when a new <see cref="XReader"/> is created.
		/// </summary>
		/// <param name="reader">The new instance of <see cref="XReader"/>.</param>
		protected virtual void OnCreateReader(XReader reader) { }

		/// <summary>
		/// Called when a new <see cref="XWriter"/> is created.
		/// </summary>
		/// <param name="writer">The new instance of <see cref="XWriter"/>.</param>
		protected virtual void OnCreateWriter(XWriter writer) { }

		internal void InspectType(Type type) => OnInspectType(type);

		internal void CreateDomain(XDomain domain) => OnCreateDomain(domain);

		internal void CreateXType<T>(XType<T> xType) => OnCreateXType(xType);

		internal void CreateXTypeLate<T>(XType<T> xType) => OnCreateXTypeLate(xType);

		internal void CreateReader(XReader reader) => OnCreateReader(reader);

		internal void CreateWriter(XWriter writer) => OnCreateWriter(writer);
	}
}
