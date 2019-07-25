using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XMachine.Reflection
{
	/// <summary>
	/// An object that wraps a <see cref="MethodInfo"/> that may contain generic type arguments. Allows you to
	/// check provided arguments against that method's parameter constraints and to construct generic methods
	/// with type arguments discovered from provided argument types.
	/// </summary>
	internal sealed class MethodArgumentsMap
	{
		private readonly Type[] parameterTypes;

		private readonly Func<Type[], Type>[] maps;

		/// <summary>
		/// Create a new <see cref="MethodArgumentsMap"/> for the given method, generating a mapping
		/// between its parameters and its generic type arguments. Throws an <see cref="InvalidOperationException"/>
		/// if the method signature is such that the generic type arguments cannot be discovered from the
		/// parameter types alone -- for example, if one of the generic type arguments appears only in a 
		/// constraint on another generic type argument.
		/// </summary>
		public MethodArgumentsMap(MethodInfo method)
		{
			if (method == null)
			{
				throw new ArgumentNullException(nameof(method));
			}

			// Nongeneric methods are simple

			if (!method.IsGenericMethod)
			{
				MethodDefinition = method;
				return;
			}

			// Create a mapping function for generic methods
			// Extracts the type arguments for the method from the parameters

			MethodDefinition = method.GetGenericMethodDefinition();

			parameterTypes = MethodDefinition.GetParameters()
				.Select(x => x.ParameterType).ToArray();
			Type[] genericParams = MethodDefinition.GetGenericArguments();

			maps = new Func<Type[], Type>[genericParams.Length];

			for (int i = 0; i < maps.Length; i++)
			{
				// Create a stack to navigate from the arguments list to the type we substitute into each
				// generic method parameter

				Stack<int> stack = new Stack<int>();

				bool findArgument(Type arg, Type[] search)
				{
					for (int j = 0; j < search.Length; j++)
					{
						if (search[j] == arg ||
							(search[j].IsGenericType && findArgument(arg, search[j].GenericTypeArguments)))
						{
							// Found the method's type parameter, or a generic type definition whose
							// arguments contain it (or whose arguments' arguments' contain it, etc.)
							stack.Push(j);
							return true;
						}
					}
					return false;
				}

				if (!findArgument(genericParams[i], parameterTypes))
				{
					throw new InvalidOperationException(
						$"Cannot recover generic method arguments from parameters in method {method.Name}");
				}

				// The returned delegate iterates through the stack and returns the type to substitute

				maps[i] = (args) =>
				{
					Type current = null;

					foreach (int nav in stack)
					{
						current = args[nav];

						if (current.IsGenericTypeDefinition)
						{
							// Continue digging
							args = current.GenericTypeArguments;
						}
						else
						{
							// Finished digging
							return current;
						}
					}

					return current;
				};
			}
		}

		/// <summary>
		/// Get the <see cref="MethodInfo"/> object that represents this method (for a non-generic method)
		/// or its generic method definition.
		/// </summary>
		public MethodInfo MethodDefinition { get; }

		/// <summary>
		/// Checks that the given argument types satisfy all constraints on the method's parameters, meaning that
		/// an invokable generic method can be constructed from arguments of these types.
		/// </summary>
		public bool CanConstructMethodFor(params Type[] arguments)
		{
			if (arguments == null)
			{
				throw new ArgumentNullException(nameof(arguments));
			}
			if (arguments.Length != parameterTypes.Length)
			{
				return false;
			}

			for (int i = 0; i < parameterTypes.Length; i++)
			{
				if (arguments[i] == null)
				{
					throw new ArgumentNullException(nameof(arguments));
				}

				if (!checkParameter(parameterTypes[i], arguments[i]))
				{
					return false;
				}

				// Recursive method to check assignability
				bool checkParameter(Type parameter, Type arg)
				{
					if (parameter.IsGenericParameter)
					{
						// Check that the argument can satisfy a generic type parameter's constraints
						return ReflectionTools.CanCloseGenericParameter(parameter, arg,
							ReflectionTools.TypeContext.MethodParameter);
					}
					else if (parameter.ContainsGenericParameters)
					{
						// Check that the argument is, or inherits from, a constructed generic of a generic type
						// definition
						if (!ReflectionTools.InheritsFromGenericTypeDefinition(parameter, arg, out Type[] withArgs))
						{
							return false;
						}
						Type[] defArgs = parameter.GenericTypeArguments;
						for (int j = 0; j < defArgs.Length; j++)
						{
							if (!checkParameter(defArgs[j], withArgs[i]))
							{
								return false;
							}
						}
					}
					else
					{
						// For a closed type, just use IsAssignableFrom
						return parameter.IsAssignableFrom(arg);
					}

					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Constructs an invokable generic method, discovering the method's generic type arguments from the types
		/// of arguments given. If <see cref="CanConstructMethodFor(Type[])"/> returns <c>false</c> on these
		/// argument types, this method returns null.
		/// </summary>
		public MethodInfo MakeGenericMethod(params Type[] arguments)
		{
			if (!CanConstructMethodFor(arguments))
			{
				return null;
			}

			// The arguments meet all parameter constraints:
			// Use the map to construct the generic method

			if (maps == null)
			{
				return MethodDefinition;
			}

			Type[] args = new Type[maps.Length];

			for (int i = 0; i < args.Length; i++)
			{
				args[i] = maps[i](arguments);
			}

			return MethodDefinition.MakeGenericMethod(args);
		}

		/// <summary>
		/// Construct and invoke this method definition on the given arguments, return the result. Throws an
		/// <see cref="InvalidOperationException"/> if the method cannot be constructed from the given arguments'
		/// types.
		/// </summary>
		public object Invoke(object target, params object[] arguments)
		{
			if (arguments == null)
			{
				throw new ArgumentNullException(nameof(arguments));
			}
			if (arguments.Length != parameterTypes.Length)
			{
				throw new ArgumentException($"Wrong number of arguments for method {MethodDefinition}", nameof(arguments));
			}

			Type[] argumentTypes = arguments.Select(x => x.GetType()).ToArray();

			MethodInfo constructedMethod = MakeGenericMethod(argumentTypes);
			if (constructedMethod == null)
			{
				throw new InvalidOperationException($"Cannot construct method from {MethodDefinition} using " +
					$"argument types {string.Join(", ", argumentTypes.Select(x => x.Name))}.");
			}

			return constructedMethod.Invoke(target, arguments);
		}

		/// <summary>
		/// Attempt to discover the generic type arguments of <see cref="MethodDefinition"/> from the types of the
		/// supplied arguments, and if possible, invoke a constructed version of the method on the given target
		/// using the given arguments.
		/// </summary>
		public bool TryInvoke(out object returnValue, object target, params object[] arguments)
		{
			if (arguments == null || arguments.Length != parameterTypes.Length)
			{
				returnValue = null;
				return false;
			}

			MethodInfo constructedMethod = MakeGenericMethod(arguments.Select(x => x.GetType()).ToArray());
			if (constructedMethod == null)
			{
				returnValue = null;
				return false;
			}

			returnValue = constructedMethod.Invoke(target, arguments);
			return true;
		}

		/// <summary>
		/// Attempt to discover the generic type arguments of <see cref="MethodDefinition"/> from the types of the
		/// supplied arguments, and if possible, invoke a constructed version of the method on the given target
		/// using the given arguments.
		/// </summary>
		public bool TryInvoke(object target, params object[] arguments)
		{
			if (arguments == null || arguments.Length != parameterTypes.Length)
			{
				return false;
			}

			MethodInfo constructedMethod = MakeGenericMethod(arguments.Select(x => x.GetType()).ToArray());
			if (constructedMethod == null)
			{
				return false;
			}

			_ = constructedMethod.Invoke(target, arguments);
			return true;
		}

		/// <summary>
		/// True if <paramref name="obj"/> is a <see cref="MethodArgumentsMap"/> with an equal 
		/// <see cref="MethodDefinition"/> property.
		/// </summary>
		public override bool Equals(object obj) => 
			obj is MethodArgumentsMap map && Equals(MethodDefinition, map.MethodDefinition);

		/// <summary>
		/// Return a hashcode based on <see cref="MethodDefinition"/>.
		/// </summary>
		public override int GetHashCode() => MethodDefinition.GetHashCode();

		/// <summary>
		/// Returns the string representation of <see cref="MethodDefinition"/>.
		/// </summary>
		public override string ToString() => MethodDefinition.ToString();
	}
}
