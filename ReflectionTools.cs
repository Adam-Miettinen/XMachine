using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace XMachine
{
	/// <summary>
	/// Utility methods for objects in the <see cref="System.Reflection"/> namespace.
	/// </summary>
	public static class ReflectionTools
	{
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
		/// Returns a lazy enumerable over the types from which this type directly inherits.
		/// </summary>
		public static IEnumerable<Type> GetBaseTypes(this Type type)
		{
			while (type.BaseType != null)
			{
				type = type.BaseType;
				yield return type;
			}
		}

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
		/// Returns an array of all the unassigned generic parameters in this type, discovered recursively by checking
		/// every one of its arguments, its arguments' arguments, and so on.
		/// </summary>
		public static Type[] GetUnassignedGenericParameters(this Type type)
		{
			if (type.ContainsGenericParameters)
			{
				List<Type> parameters = new List<Type>();
				GetUnassignedGenericParametersPrivate(type, parameters);
				return parameters.ToArray();
			}
			return Type.EmptyTypes;
		}

		/// <summary>
		/// Returns an array of all the unassigned generic parameters in this method.
		/// </summary>
		public static Type[] GetUnassignedGenericParameters(this MethodInfo method)
		{
			if (!method.IsGenericMethod)
			{
				throw new InvalidOperationException("The method is not generic");
			}

			if (method.ContainsGenericParameters)
			{
				return method.GetGenericArguments().Where(x => x.IsGenericParameter).ToArray();
			}
			return Type.EmptyTypes;
		}

		private static void GetUnassignedGenericParametersPrivate(Type type, List<Type> parameters)
		{
			if (type.IsGenericParameter)
			{
				parameters.Add(type);
			}
			if (type.IsGenericType)
			{
				foreach (Type arg in type.GetGenericArguments())
				{
					GetUnassignedGenericParametersPrivate(arg, parameters);
				}
			}
		}

		/// <summary>
		/// Returns true if and only if the given type can be assigned to the current type or, if the current type
		/// has unassigned generic parameters, if the given type can be assigned to a closed generic type
		/// constructed from the current type.
		/// </summary>
		public static bool IsAssignableFromSelfOrConstructed(this Type type, Type closedType, out Type constructedType)
		{
			if (closedType.ContainsGenericParameters)
			{
				throw new InvalidOperationException("The closed type argument cannot contain generic parameters");
			}

			constructedType = null;

			if (!type.ContainsGenericParameters)
			{
				if (type.IsAssignableFrom(closedType))
				{
					constructedType = closedType;
					return true;
				}
				return false;
			}

			if (type.IsGenericParameter)
			{
				// Check that the given type meets any special attribute requirements

				GenericParameterAttributes attributes = type.GenericParameterAttributes;

				if ((attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) &&
						!closedType.IsByRef) ||
					(attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) &&
						(!closedType.IsValueType ||
							closedType.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(Nullable<>)))) ||
					(attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) &&
						closedType.GetConstructor(Type.EmptyTypes) == null))
				{
					return false;
				}

				// Then check that derived meets all the constraints

				foreach (Type constraint in type.GetGenericParameterConstraints())
				{
					if (!constraint.IsAssignableFrom(closedType))
					{
						return false;
					}
				}

				constructedType = closedType;
				return true;
			}

			// This is a generic type definition, possibly with some assigned arguments

			Type typeDefinition = type.GetGenericTypeDefinition(),
				constructedParent = null;

			if (typeDefinition.IsClass)
			{
				// Check class inheritance

				constructedParent = closedType.GetSelfAndBaseTypes().FirstOrDefault(x =>
					x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeDefinition);
			}
			else if (typeDefinition.IsInterface)
			{
				// Check interface inheritance

				constructedParent = closedType.GetInterfaces()
					.FirstOrDefault(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeDefinition);
			}

			if (constructedParent == null)
			{
				return false;
			}

			// If closedType is assignable to something constructed from this type, check that all the arguments match up

			Type[] typeArgs = type.GenericTypeArguments,
				constructedParentArgs = constructedParent.GenericTypeArguments,
				argsToSub = new Type[typeArgs.Length];

			for (int i = 0; i < constructedParentArgs.Length; i++)
			{
				if (typeArgs[i].IsAssignableFromSelfOrConstructed(constructedParentArgs[i], out Type constructedArg))
				{
					argsToSub[i] = constructedArg;
				}
				else
				{
					return false;
				}
			}

			constructedType = typeDefinition.MakeGenericType(argsToSub);
			return true;
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

		internal static MemberInfo ParseMemberExpression<TType, TMember>(Expression<Func<TType, TMember>> expression)
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
			if (!me.Member.ReflectedType.IsAssignableFrom(typeof(TType)))
			{
				throw new ArgumentException($"The expression {expression} does not identify a member defined on " +
					$"the type {typeof(TType).Name}.");
			}
			return me.Member;
		}
	}
}
