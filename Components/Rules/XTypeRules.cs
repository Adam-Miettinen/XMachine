using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using XMachine.Reflection;

namespace XMachine.Components.Rules
{
	/// <summary>
	/// A component that applies global rules to <see cref="XType{T}"/> objects when they are generated.
	/// </summary>
	public sealed class XTypeRules : XMachineComponent
	{
		private readonly ICollection<MethodArgumentsMap> staticRules = new HashSet<MethodArgumentsMap>();

		private readonly ICollection<Tuple<object, MethodArgumentsMap>> runtimeRules =
			new HashSet<Tuple<object, MethodArgumentsMap>>();

		internal XTypeRules() { }

		/// <summary>
		/// Use the given method and instance target (null for static methods) as an XTypeRule.
		/// </summary>
		public void Add(MethodInfo method, object target = null)
		{
			if (method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}
			if (target == null && !method.IsStatic)
			{
				throw new ArgumentNullException(nameof(target));
			}
			runtimeRules.Add(new Tuple<object, MethodArgumentsMap>(target, new MethodArgumentsMap(method)));
		}

		/// <summary>
		/// Translate the method identified by the given expression as an XTypeRule.
		/// </summary>
		public void Add<T>(Expression<Action<XType<T>>> methodExpression)
		{
			if (methodExpression == null)
			{
				throw new ArgumentNullException(nameof(methodExpression));
			}
			if (!(methodExpression.Body is MethodCallExpression mce))
			{
				throw new ArgumentException("Not a method", nameof(methodExpression));
			}
			runtimeRules.Add(new Tuple<object, MethodArgumentsMap>(mce.Object, new MethodArgumentsMap(mce.Method)));
		}

		/// <summary>
		/// Remove the XTypeRule defined by the given <see cref="MethodInfo"/> and <paramref name="target"/> from
		/// the runtime rules collection.
		/// </summary>
		public void Remove(MethodInfo method, object target = null)
		{
			if (method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}

			Tuple<object, MethodArgumentsMap> rule = null;

			foreach (Tuple<object, MethodArgumentsMap> r in runtimeRules)
			{
				if ((target == null || Equals(target, r.Item1)) &&
					(r.Item2.MethodDefinition == method ||
					(method.IsGenericMethod && method.GetGenericMethodDefinition() == r.Item2.MethodDefinition)))
				{
					rule = r;
					break;
				}
			}

			if (rule != null)
			{
				_ = runtimeRules.Remove(rule);
			}
		}


		/// <summary>
		/// Remove the XTypeRule defined by the given expression from the runtime rules collection.
		/// </summary>
		public void Remove<T>(Expression<Action<XType<T>>> methodExpression)
		{
			if (methodExpression == null)
			{
				throw new ArgumentNullException(nameof(methodExpression));
			}
			if (!(methodExpression.Body is MethodCallExpression mce))
			{
				throw new ArgumentException("Not a method", nameof(methodExpression));
			}

			Tuple<object, MethodArgumentsMap> rule = null;

			foreach (Tuple<object, MethodArgumentsMap> r in runtimeRules)
			{
				if ((mce.Object == null || Equals(mce.Object, r.Item1)) &&
					(r.Item2.MethodDefinition == mce.Method ||
					(mce.Method.IsGenericMethod && mce.Method.GetGenericMethodDefinition() == r.Item2.MethodDefinition)))
				{
					rule = r;
					break;
				}
			}

			if (rule != null)
			{
				_ = runtimeRules.Remove(rule);
			}
		}

		/// <summary>
		/// Apply rules to qualifying <see cref="XType{T}"/>s.
		/// </summary>
		protected override void OnCreateXTypeLate<T>(XType<T> xType)
		{
			object[] args = new object[] { xType };

			foreach (MethodArgumentsMap rule in staticRules)
			{
				try
				{
					_ = rule.TryInvoke(null, args);
				}
				catch (Exception e)
				{
					ExceptionHandler(e);
				}
			}

			foreach (Tuple<object, MethodArgumentsMap> rule in runtimeRules)
			{
				try
				{
					_ = rule.Item2.TryInvoke(rule.Item1, args);
				}
				catch (Exception e)
				{
					ExceptionHandler(e);
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
						staticRules.Add(new MethodArgumentsMap(method));
					}
					catch (Exception e)
					{
						// Method probably doesn't have a signature that allows discovery of the
						// generic method arguments from the method parameters alone
						ExceptionHandler(e);
					}
				}
			}
		}
	}
}
