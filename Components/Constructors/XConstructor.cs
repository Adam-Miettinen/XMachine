using System;
using System.Xml.Linq;

namespace XMachine.Components.Constructors
{
	/// <summary>
	/// An <see cref="XTypeComponent{T}"/> that uses a parameterless constructor to construct
	/// instances of serialized objects of type <typeparamref name="T"/>.
	/// </summary>
	public sealed class XConstructor<T> : XTypeComponent<T>
	{
		private Func<T> constructor;

		/// <summary>
		/// Create a new <see cref="XConstructor{T}"/> using the given delegate.
		/// </summary>
		/// <param name="xType">The <see cref="XType{T}"/> object to which this <see cref="XTypeComponent{T}"/> belongs.</param>
		/// <param name="constructor">The delegate to use as a constructor.</param>
		public XConstructor(XType<T> xType, Func<T> constructor) : base(xType) =>
			Constructor = constructor;

		/// <summary>
		/// Get or set the constructor delegate.
		/// </summary>
		public Func<T> Constructor
		{
			get => constructor;
			set => constructor = value ?? throw new ArgumentNullException("Cannot use null delegate");
		}

		protected override void OnBuild(IXReadOperation reader, XElement element, ObjectBuilder<T> objectBuilder,
			XObjectArgs args)
		{
			if ((args == null || !args.Hints.HasFlag(ObjectHints.DontConstruct)) &&
				!objectBuilder.IsConstructed)
			{
				objectBuilder.Object = Constructor();
			}
		}
	}
}
