using System;
using System.Linq;
using System.Reflection;
using XMachine.Reflection;

namespace XMachine.Components.Constructors
{
	/// <summary>
	/// This extension manages the selection of parameterless constructors for types.
	/// </summary>
	public sealed class XAutoConstructors : XMachineComponent
	{
		private static readonly MethodInfo makePublicParameterlessConstructorDelegate =
			typeof(XAutoConstructors).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
				.FirstOrDefault(x => x.Name == nameof(MakePublicParameterlessConstructorDelegate) && x.IsGenericMethodDefinition);

		internal XAutoConstructors() { }

		/// <summary>
		/// Get or set a <see cref="ConstructorAccess"/> bitflag that determines the access level a parameterless
		/// constructor must have for it to be used to construct an object read from XML. By default,
		/// parameterless constructors must be public. Altering this value will not change the treatment of
		/// types defined in Microsoft core libraries, types defined in an assemly tagged with 
		/// <see cref="XMachineAssemblyAttribute"/>, or any types that have already been reflected (read or
		/// written).
		/// </summary>
		public ConstructorAccess AccessIncluded { get; set; }

		/// <summary>
		/// Finds a suitable constructor and assigns it
		/// </summary>
		protected override void OnCreateXType<T>(XType<T> xType)
		{
			base.OnCreateXType(xType);

			Type type = typeof(T);

			if (!ConstructorEligible(xType))
			{
				return;
			}

			ConstructorAccess ctorAccess = GetAccessLevel<T>();

			if (type.GetConstructor(Type.EmptyTypes) != null || type.IsValueType)
			{
				xType.Register(new XConstructor<T>(
					(Func<T>)makePublicParameterlessConstructorDelegate.MakeGenericMethod(typeof(T)).Invoke(this, null)));
			}
			else if (ctorAccess != ConstructorAccess.Public)
			{
				ConstructorInfo ci = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
					.FirstOrDefault(x =>
						!x.IsXIgnored(true, true) &&
						x.GetParameters().Length == 0 &&
						((x.IsFamilyOrAssembly && ctorAccess.HasFlag(ConstructorAccess.ProtectedInternal)) ||
						(x.IsAssembly && ctorAccess.HasFlag(ConstructorAccess.Internal)) ||
						(x.IsFamily && ctorAccess.HasFlag(ConstructorAccess.Protected)) ||
						(x.IsFamilyAndAssembly && ctorAccess.HasFlag(ConstructorAccess.PrivateProtected)) ||
						(x.IsPrivate && ctorAccess.HasFlag(ConstructorAccess.Private))));

				if (ci != null)
				{
					xType.Register(new XConstructor<T>(() => (T)ci.Invoke(null)));
				}
			}
		}

		/// <summary>
		/// Disable <see cref="XConstructor{T}"/> component for types that have an <see cref="XTexter{T}"/> or
		/// <see cref="XBuilderComponent{T}"/>.
		/// </summary>
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

			return type.CanCreateInstances() && !type.IsArray && !XDefaultTypes.IsDefaultType(type) &&
				xType.Component<XTexter<T>>() == null && xType.Component<XBuilderComponent<T>>() == null;
		}

		internal ConstructorAccess GetAccessLevel<T>()
		{
			Type type = typeof(T);
			if (type.Assembly == typeof(object).Assembly)
			{
				return ConstructorAccess.Public;
			}
			else
			{
				XMachineAssemblyAttribute xma = type.Assembly.GetCustomAttribute<XMachineAssemblyAttribute>();
				return xma == null ? AccessIncluded : xma.ConstructorAccess;
			}
		}

		private Func<T> MakePublicParameterlessConstructorDelegate<T>() where T : new() => () => new T();
	}
}
