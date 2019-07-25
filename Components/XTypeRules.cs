using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XMachine.Reflection;

namespace XMachine.Components
{
	/// <summary>
	/// A component that applies global rules to <see cref="XType{T}"/> objects when they are generated.
	/// </summary>
	public sealed class XTypeRules : XMachineComponent
	{
		private readonly ICollection<MethodArgumentsMap> rules = new HashSet<MethodArgumentsMap>();

		internal XTypeRules() { }

		/// <summary>
		/// Apply rules to qualifying <see cref="XType{T}"/>s.
		/// </summary>
		protected override void OnCreateXTypeLate<T>(XType<T> xType)
		{
			object[] args = new object[] { xType };

			foreach (MethodArgumentsMap rule in rules)
			{
				_ = rule.TryInvoke(null, args);
			}
		}

		/// <summary>
		/// Check for methods tagged with <see cref="XTypeRuleAttribute"/>.
		/// </summary>
		protected override void OnInspectType(Type type)
		{
			if (!type.IsClass)
			{
				return;
			}

			foreach (MethodInfo method in type
				.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
				.Where(x => x.HasCustomAttribute<XTypeRuleAttribute>() &&
					(!x.IsGenericMethod || x.IsGenericMethodDefinition) &&
					!x.IsXIgnored() &&
					x.ReturnType == typeof(void)))
			{
				ParameterInfo[] parameters = method.GetParameters();

				if (parameters.Length == 1 ||
					parameters[0].ParameterType.IsGenericType ||
					parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(XType<>))
				{
					try
					{
						rules.Add(new MethodArgumentsMap(method));
					}
					catch
					{
						// Method probably doesn't have a signature that allows discovery of the
						// generic method arguments from the method parameters alone
					}
				}
			}
		}
	}
}
