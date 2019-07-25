using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace XMachine.Reflection
{
	/// <summary>
	/// Utility methods for objects in the <see cref="System.Reflection"/> namespace.
	/// </summary>
	public static class ReflectionTools
	{
		/// <summary>
		/// An enum representing the context in which a <see cref="Type"/> object is being used, which 
		/// determines whether covariance/contravariance is acceptable.
		/// </summary>
		public enum TypeContext
		{
			/// <summary>
			/// The <see cref="Type"/> defines a variable, a base type, an interface, etc.
			/// </summary>
			Default = 0,

			/// <summary>
			/// The <see cref="Type"/> is being used as a method parameter: covariance (out) is not allowed.
			/// </summary>
			MethodParameter = 1,

			/// <summary>
			/// The <see cref="Type"/> is being used as the return type of a method: contravariance (in) is not allowed.
			/// </summary>
			MethodReturn = 2
		}

		/// <summary>
		/// Whether the given attribute is present.
		/// </summary>
		public static bool HasCustomAttribute<T>(this Assembly assembly) where T : Attribute =>
			assembly.GetCustomAttribute<T>() != null;

		/// <summary>
		/// Whether the given attribute is present.
		/// </summary>
		public static bool HasCustomAttribute<T>(this MemberInfo member) where T : Attribute =>
			member.GetCustomAttribute<T>() != null;

		/// <summary>
		/// Returns <c>true</c> if and only if instances of this <see cref="Type"/> can be created, i.e. if it is not an
		/// unassigned generic parameter, contains no generic parameters, and is not an abstract class or interface.
		/// </summary>
		public static bool CanCreateInstances(this Type type) =>
			!type.ContainsGenericParameters &&
			!type.IsGenericTypeDefinition &&
			!type.IsAbstract;

		/// <summary>
		/// Returns a lazy enumerable over the curren type, followed by all the types from which this type directly inherits.
		/// </summary>
		public static IEnumerable<Type> GetSelfAndBaseTypes(this Type type)
		{
			do
			{
				yield return type;
				type = type.BaseType;
			}
			while (type != null);
		}

		/// <summary>
		/// Checks if <paramref name="type"/> satisfies all constraints on <paramref name="parameter"/>.
		/// </summary>
		public static bool CanCloseGenericParameter(Type parameter, Type type, TypeContext context = TypeContext.Default)
		{
			if (parameter == null)
			{
				throw new ArgumentNullException(nameof(parameter));
			}
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}
			if (!parameter.IsGenericParameter)
			{
				throw new ArgumentException("Not a generic parameter", nameof(parameter));
			}
			if (type.ContainsGenericParameters)
			{
				throw new ArgumentException("Not a closed type", nameof(type));
			}

			// Check special attributes

			GenericParameterAttributes attributes = parameter.GenericParameterAttributes;

			if ((attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) &&
					!type.IsByRef) ||
				(attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) &&
					(!type.IsValueType || type.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(Nullable<>)))) ||
				(attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) &&
					type.GetConstructor(Type.EmptyTypes) == null) ||
				(attributes.HasFlag(GenericParameterAttributes.Covariant) && context == TypeContext.MethodParameter) ||
				(attributes.HasFlag(GenericParameterAttributes.Contravariant) && context == TypeContext.MethodReturn))
			{
				return false;
			}

			// Check type constraints

			Type[] constraints = type.GetGenericParameterConstraints();

			for (int i = 0; i < constraints.Length; i++)
			{
				if (!constraints[i].IsAssignableFrom(type))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Check if the closed <see cref="Type"/> <paramref name="type"/> or any of the classes or interfaces it 
		/// inherits or implements are a constructed generic type of the given generic type definition, 
		/// <paramref name="definition"/>.
		/// </summary>
		public static bool InheritsFromGenericTypeDefinition(Type definition, Type type, out Type[] withArgs)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}
			if (!definition.IsGenericTypeDefinition)
			{
				throw new ArgumentException("Not a generic type definition", nameof(definition));
			}
			if (type.ContainsGenericParameters)
			{
				throw new ArgumentException("Not a closed type", nameof(type));
			}

			if (definition.IsClass)
			{
				foreach (Type inheritedClass in type.GetSelfAndBaseTypes())
				{
					if (inheritedClass.IsGenericType && inheritedClass.GetGenericTypeDefinition() == definition)
					{
						withArgs = inheritedClass.GenericTypeArguments;
						return true;
					}
				}
			}
			else if (definition.IsInterface)
			{
				foreach (Type inheritedInterface in type.GetInterfaces())
				{
					if (inheritedInterface.IsGenericType && inheritedInterface.GetGenericTypeDefinition() == definition)
					{
						withArgs = inheritedInterface.GenericTypeArguments;
						return true;
					}
				}
			}

			withArgs = null;
			return false;
		}

		internal static PropertyInfo ParsePropertyExpression<TType, TProperty>(Expression<Func<TType, TProperty>> expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("Cannot parse a null expression");
			}
			if (!(expression.Body is MemberExpression me))
			{
				throw new ArgumentException(
					$"The expression {expression} does not identify a member.");
			}
			if (!(me.Member is PropertyInfo pi))
			{
				throw new ArgumentException(
					$"The expression {expression} identifies a member that is not a property.");
			}
			if (!pi.ReflectedType.IsAssignableFrom(typeof(TType)))
			{
				throw new ArgumentException($"The expression {expression} does not identify a property defined on " +
					$"the type {typeof(TType).Name}.");
			}
			return pi;
		}

		internal static MemberInfo ParseFieldOrPropertyExpression<TType, TMember>(Expression<Func<TType, TMember>> expression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException(nameof(expression));
			}
			if (!(expression.Body is MemberExpression me))
			{
				throw new ArgumentException(
					$"The expression {expression} does not identify a member.", nameof(expression));
			}
			if (!(me.Member is FieldInfo || me.Member is PropertyInfo))
			{
				throw new ArgumentException(
					   $"The expression {expression} does not identify a field or property.", nameof(expression));
			}
			if (!me.Member.ReflectedType.IsAssignableFrom(typeof(TType)))
			{
				throw new ArgumentException($"The expression {expression} does not identify a member defined on " +
					$"the type {typeof(TType).Name}.", nameof(expression));
			}
			return me.Member;
		}
	}
}
