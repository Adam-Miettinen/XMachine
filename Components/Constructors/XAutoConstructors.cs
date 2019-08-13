using System;
using System.Linq;
using System.Reflection;
using XMachine.Reflection;

namespace XMachine.Components.Constructors
{
	/// <summary>
	/// An <see cref="XMachineComponent"/> that searches for and assigns parameterless constructors
	/// to <see cref="XType{T}"/>s.
	/// </summary>
	public sealed class XAutoConstructors : XMachineComponent
	{
		private static readonly MethodInfo makePublicParameterlessConstructorDelegate =
			typeof(XAutoConstructors).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
				.FirstOrDefault(x => x.Name == nameof(MakePublicParameterlessConstructorDelegate) && x.IsGenericMethodDefinition);

		internal XAutoConstructors() { }

		/// <summary>
		/// Get or set a <see cref="MethodAccess"/> bitflag that determines the access level a parameterless
		/// constructor must have for it to be used to construct an object read from XML.
		/// </summary>
		public MethodAccess AccessIncluded { get; set; }

		/// <summary>
		/// Get the <see cref="MethodAccess"/> level used by this component for the type <typeparamref name="T"/>.
		/// </summary>
		/// <returns>A <see cref="MethodAccess"/> instance.</returns>
		public MethodAccess GetAccessLevel<T>()
		{
			Type type = typeof(T);
			if (type.Assembly == typeof(object).Assembly)
			{
				return MethodAccess.Public;
			}
			else
			{
				XMachineAssemblyAttribute xma = type.Assembly.GetCustomAttribute<XMachineAssemblyAttribute>();
				return xma == null ? AccessIncluded : xma.ConstructorAccess;
			}
		}

		protected override void OnCreateXType<T>(XType<T> xType)
		{
			base.OnCreateXType(xType);

			Type type = typeof(T);

			if (!ConstructorEligible(xType))
			{
				return;
			}

			if (type.GetConstructor(Type.EmptyTypes) != null)
			{
				xType.Register(new XConstructor<T>(xType,
					(Func<T>)makePublicParameterlessConstructorDelegate.MakeGenericMethod(typeof(T)).Invoke(this, null)));
				return;
			}

			MethodAccess ctorAccess = GetAccessLevel<T>();

			if (ctorAccess != MethodAccess.Public)
			{
				ConstructorInfo ci = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault(x =>
						x.GetParameters().Length == 0 &&
						((x.IsFamilyOrAssembly && ctorAccess.HasFlag(MethodAccess.ProtectedInternal)) ||
						(x.IsAssembly && ctorAccess.HasFlag(MethodAccess.Internal)) ||
						(x.IsFamily && ctorAccess.HasFlag(MethodAccess.Protected)) ||
						(x.IsFamilyAndAssembly && ctorAccess.HasFlag(MethodAccess.PrivateProtected)) ||
						(x.IsPrivate && ctorAccess.HasFlag(MethodAccess.Private))));

				if (ci != null)
				{
					xType.Register(new XConstructor<T>(xType, () => (T)ci.Invoke(null)));
				}
			}
		}

		protected override void OnCreateXTypeLate<T>(XType<T> xType)
		{
			if (xType.Components<XTexter<T>>().Any(x => x.Enabled) ||
				xType.Components<XBuilderComponent<T>>().Any(x => x.Enabled))
			{
				XConstructor<T> ctor = xType.Component<XConstructor<T>>();
				if (ctor != null)
				{
					ctor.Enabled = false;
				}
			}
		}

		internal bool ConstructorEligible<T>(XType<T> xType)
		{
			Type type = typeof(T);
			return type.CanCreateInstances() && !type.IsValueType && !type.IsArray && !XDefaultTypes.IsDefaultType(type) &&
				xType.Component<XTexter<T>>() == null && xType.Component<XBuilderComponent<T>>() == null;
		}

		private Func<T> MakePublicParameterlessConstructorDelegate<T>() where T : new() => () => new T();
	}
}
