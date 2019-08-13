using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using XMachine.Reflection;

namespace XMachine.Components.Rules
{
	/// <summary>
	/// An <see cref="XMachineComponent"/> that applies rules to <see cref="XDomain"/> and <see cref="XType{T}"/> objects 
	/// when they are generated.
	/// </summary>
	public sealed class XRules : XMachineComponent
	{
		private readonly ICollection<Action<XDomain>> staticDomainRules = new HashSet<Action<XDomain>>();

		private readonly ICollection<MethodArgumentsMap> staticTypeRules = new HashSet<MethodArgumentsMap>();

		private readonly ICollection<Tuple<object, MethodArgumentsMap>> rtTypeRules =
			new HashSet<Tuple<object, MethodArgumentsMap>>();

		internal XRules() { }

		/// <summary>
		/// Use the given method and instance target (null for static methods) as an XTypeRule.
		/// </summary>
		/// <param name="method">A <see cref="MethodInfo"/> object reflecting the method to use.</param>
		/// <param name="target">The invocation target of the method.</param>
		public void AddTypeRule(MethodInfo method, object target = null)
		{
			if (method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}
			if (target == null && !method.IsStatic)
			{
				throw new ArgumentNullException(nameof(target));
			}
			rtTypeRules.Add(new Tuple<object, MethodArgumentsMap>(target, new MethodArgumentsMap(method)));
		}

		/// <summary>
		/// Use the given method and instance target (null for static methods) as an XTypeRule.
		/// </summary>
		/// <param name="methodExpression">An expression displaying the invocation of the desired method on the desired
		/// target object.</param>
		public void AddTypeRule<T>(Expression<Action<XType<T>>> methodExpression)
		{
			if (methodExpression == null)
			{
				throw new ArgumentNullException(nameof(methodExpression));
			}
			if (!(methodExpression.Body is MethodCallExpression mce))
			{
				throw new ArgumentException("Not a method", nameof(methodExpression));
			}
			rtTypeRules.Add(new Tuple<object, MethodArgumentsMap>(mce.Object, new MethodArgumentsMap(mce.Method)));
		}

		/// <summary>
		/// Remove the XTypeRule defined by the given <see cref="MethodInfo"/> and <paramref name="target"/> from
		/// the runtime rules collection.
		/// </summary>
		/// <param name="method">A <see cref="MethodInfo"/> object reflecting the method to use.</param>
		/// <param name="target">The invocation target of the method.</param>
		public void RemoveTypeRule(MethodInfo method, object target = null)
		{
			if (method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}

			Tuple<object, MethodArgumentsMap> rule = null;

			foreach (Tuple<object, MethodArgumentsMap> r in rtTypeRules)
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
				_ = rtTypeRules.Remove(rule);
			}
		}


		/// <summary>
		/// Remove the XTypeRule defined by the given expression from the runtime rules collection.
		/// </summary>
		/// <param name="methodExpression">An expression displaying the invocation of the desired method on the desired
		/// target object.</param>
		public void RemoveTypeRule<T>(Expression<Action<XType<T>>> methodExpression)
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

			foreach (Tuple<object, MethodArgumentsMap> r in rtTypeRules)
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
				_ = rtTypeRules.Remove(rule);
			}
		}

		protected override void OnCreateXTypeLate<T>(XType<T> xType)
		{
			object[] args = new object[] { xType };

			foreach (MethodArgumentsMap rule in staticTypeRules)
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

			foreach (Tuple<object, MethodArgumentsMap> rule in rtTypeRules)
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

		protected override void OnCreateDomain(XDomain domain)
		{
			foreach (Action<XDomain> rule in staticDomainRules)
			{
				rule(domain);
			}
		}

		protected override void OnInspectType(Type type)
		{
			if (!type.IsClass)
			{
				return;
			}

			foreach (MethodInfo method in type
				.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
				.Where(x => (!x.IsGenericMethod || x.IsGenericMethodDefinition) &&
					x.ReturnType == typeof(void)))
			{
				if (method.HasCustomAttribute<XTypeRuleAttribute>())
				{
					ParameterInfo[] parameters = method.GetParameters();

					if (parameters.Length == 1 &&
						parameters[0].ParameterType.IsGenericType &&
						parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(XType<>))
					{
						try
						{
							staticTypeRules.Add(new MethodArgumentsMap(method));
						}
						catch (Exception e)
						{
							// Method probably doesn't have a signature that allows discovery of the
							// generic method arguments from the method parameters alone
							ExceptionHandler(e);
						}
					}
				}
				else if (method.HasCustomAttribute<XDomainRuleAttribute>())
				{
					ParameterInfo[] parameters = method.GetParameters();

					if (parameters.Length == 1 &&
						parameters[0].ParameterType == typeof(XDomain))
					{
						staticDomainRules.Add((Action<XDomain>)Delegate.CreateDelegate(typeof(Action<XDomain>), method));
					}
				}
			}
		}
	}
}
