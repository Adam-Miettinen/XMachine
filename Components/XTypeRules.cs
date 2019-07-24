using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XMachine.Components
{
	/// <summary>
	/// A component that applies global rules to <see cref="XType{T}"/> objects when they are generated.
	/// </summary>
	public sealed class XTypeRules : XMachineComponent
	{
		private readonly IDictionary<Type, ICollection<MethodInfo>> staticRules =
			new Dictionary<Type, ICollection<MethodInfo>>();

		internal XTypeRules() { }

		/// <summary>
		/// Apply rules to qualifying <see cref="XType{T}"/>s.
		/// </summary>
		protected override void OnCreateXTypeLate<T>(XType<T> xType)
		{
			Type type = typeof(T);

			// For type == T
			// Match to: T

			if (staticRules.TryGetValue(type, out ICollection<MethodInfo> typeRules))
			{
				ApplyRules(xType, typeRules);
			}

			foreach (KeyValuePair<Type, ICollection<MethodInfo>> kv in staticRules)
			{
				// For type : BaseType
				// Match to: T where T : BaseType

				if (kv.Key.IsGenericParameter)
				{
					GenericParameterAttributes attributes = kv.Key.GenericParameterAttributes;

					if ((!attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) || type.IsByRef) &&
						(!attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) ||
							(type.IsValueType && type.GetInterface(nameof(Nullable)) == null)) &&
						(!attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) ||
							type.GetConstructor(Type.EmptyTypes) != null) &&
						kv.Key.GetGenericParameterConstraints().All(x => x.IsAssignableFrom(type)))
					{
						ApplyRules(xType, kv.Value.Select(x => x.MakeGenericMethod(type)));
					}
				}
			}

			// For type == GenericType<T> for some T
			// Match to: GenericType<T>

			if (type.IsConstructedGenericType)
			{
				Type genericTypeDef = type.GetGenericTypeDefinition();
				Type[] genericArgs = type.GenericTypeArguments;

				foreach (KeyValuePair<Type, ICollection<MethodInfo>> kv in staticRules)
				{
					if (kv.Key.IsGenericTypeDefinition && kv.Key == genericTypeDef)
					{
						ApplyRules(xType, kv.Value.Select(x => x.MakeGenericMethod(genericArgs)));
					}
				}
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

			foreach (MethodInfo method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
				.Where(x => x.GetCustomAttribute<XTypeRuleAttribute>() != null &&
					(!x.IsGenericMethod || x.IsGenericMethodDefinition) &&
					!x.IsXIgnored() &&
					x.ReturnType == typeof(void)))
			{

				ParameterInfo[] parameters = method.GetParameters();

				if (!(parameters.Length == 1 &&
					parameters[0].ParameterType.IsGenericType &&
					parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(XType<>)))
				{
					continue;
				}

				Type index = parameters[0].ParameterType.GenericTypeArguments[0];

				if (staticRules.TryGetValue(index, out ICollection<MethodInfo> tRules))
				{
					tRules.Add(method);
				}
				else
				{
					tRules = new List<MethodInfo>
					{
						method
					};
					staticRules.Add(index, tRules);
				}
			}
		}

		private void ApplyRules<T>(XType<T> xType, IEnumerable<MethodInfo> rules)
		{
			foreach (MethodInfo method in rules)
			{
				_ = method.Invoke(null, new object[] { xType });
			}
		}
	}
}
