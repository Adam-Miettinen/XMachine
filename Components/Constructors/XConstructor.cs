using System;
using System.Xml.Linq;

namespace XMachine.Components.Constructors
{
	/// <summary>
	/// An <see cref="XTypeComponent{T}"/> representing a parameterless constructor
	/// </summary>
	public sealed class XConstructor<T> : XTypeComponent<T>
	{
		private Func<T> constructor;

		/// <summary>
		/// Create a new <see cref="XConstructor{T}"/> using the given delegate.
		/// </summary>
		public XConstructor(Func<T> constructor) => Constructor = constructor;

		/// <summary>
		/// Get or set the constructor delegate
		/// </summary>
		public Func<T> Constructor
		{
			get => constructor;
			set => constructor = value ?? throw new ArgumentNullException("Cannot use null delegate");
		}

		/// <summary>
		/// Add the constructor as a builder task.
		/// </summary>
		protected override void OnBuild(XType<T> xType, IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder)
		{
			if (!objectBuilder.IsConstructed)
			{
				objectBuilder.AddTask(() =>
				{
					if (!objectBuilder.IsConstructed)
					{
						objectBuilder.Object = constructor();
					}
					return true;
				});
			}
		}
	}
}
