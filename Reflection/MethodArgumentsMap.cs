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
	public sealed class MethodArgumentsMap : IEquatable<MethodArgumentsMap>
	{
		private readonly Type[] parameterTypes;

		private readonly Stack<Func<Type[], Type[]>>[] maps;

		/// <summary>
		/// Create a new <see cref="MethodArgumentsMap"/> for the given method, generating a mapping
		/// between its parameters and its generic type arguments. Throws an <see cref="InvalidOperationException"/>
		/// if the method signature is such that the generic type arguments cannot be discovered from the
		/// parameter types alone.
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
				parameterTypes = MethodDefinition.GetParameters()
					.Select(x => x.ParameterType).ToArray();
				return;
			}

			// For generic methods, create a mapping function:

			MethodDefinition = method.GetGenericMethodDefinition();
			parameterTypes = MethodDefinition.GetParameters()
				.Select(x => x.ParameterType).ToArray();

			Type[] genericParams = MethodDefinition.GetGenericArguments();
			maps = new Stack<Func<Type[], Type[]>>[genericParams.Length];

			for (int i = 0; i < maps.Length; i++)
			{
				maps[i] = MapParameter(genericParams[i]);
			}
		}

		/// <summary>
		/// Get the <see cref="MethodInfo"/> object that represents either this method or, for a generic method,
		/// its generic method definition.
		/// </summary>
		public MethodInfo MethodDefinition { get; }

		/// <summary>
		/// Checks that the given argument types satisfy all constraints on the method's parameters, meaning that
		/// an invokable generic method can be constructed from arguments of these types.
		/// </summary>
		/// <param name="arguments">The <see cref="Type"/>s of the arguments being passed. Must be non-null.</param>
		/// <returns>True if all constraints are satisfied, false otherwise.</returns>
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
			}

			return true;

			// Recursive method to check assignability
			bool checkParameter(Type parameter, Type arg)
			{
				if (parameter.IsGenericParameter)
				{
					// Check that the argument can satisfy a generic type parameter's constraints
					if (ReflectionTools.CanCloseGenericParameter(parameter, arg,
						ReflectionTools.TypeContext.MethodParameter))
					{
						return true;
					}
				}
				else if (parameter.ContainsGenericParameters)
				{
					// Check that the argument is, or inherits from, a constructed generic of a generic type
					// definition
					if (ReflectionTools.InheritsFromOpenGenericType(parameter, arg, out Type[] _,
						ReflectionTools.TypeContext.MethodParameter))
					{
						return true;
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

		/// <summary>
		/// Constructs an invokable generic method, discovering the method's generic type arguments from the types
		/// of arguments given.
		/// </summary>
		/// <param name="arguments">The <see cref="Type"/>s of the arguments to be passed to the constructed method. 
		/// Must be non-null.</param>
		/// <returns>A <see cref="MethodInfo"/> representing the constructed method.</returns>
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
				Type[] i_args = arguments;

				foreach (Func<Type[], Type[]> f in maps[i])
				{
					i_args = f(i_args);
				}

				args[i] = i_args[0];
			}

			return MethodDefinition.MakeGenericMethod(args);
		}

		/// <summary>
		/// Constructs and invokes a generic method, discovering the method's generic type arguments from the types
		/// of arguments given.
		/// </summary>
		/// <param name="target">The target object to invoke the method on, or <c>null</c> for a static method.</param>
		/// <param name="arguments">The <see cref="Type"/>s of the arguments to be passed to the constructed method. 
		/// Must be non-null.</param>
		/// <returns>An <see cref="object"/> containing the return value of the invoked method.</returns>
		public object Invoke(object target, params object[] arguments)
		{
			if ((arguments == null && parameterTypes.Length > 0) ||
				(arguments != null && arguments.Length != parameterTypes.Length))
			{
				throw new ArgumentException($"Wrong number of arguments for method {MethodDefinition}", nameof(arguments));
			}

			MethodInfo constructedMethod;

			if (parameterTypes.Length == 0)
			{
				constructedMethod = MakeGenericMethod();
				if (constructedMethod == null)
				{
					throw new InvalidOperationException($"Cannot construct method from {MethodDefinition}.");
				}
			}
			else
			{
				Type[] argumentTypes = arguments.Select(x => x.GetType()).ToArray();
				constructedMethod = MakeGenericMethod(argumentTypes);
				if (constructedMethod == null)
				{
					throw new InvalidOperationException($"Cannot construct method from {MethodDefinition} using " +
						$"argument types {string.Join(", ", argumentTypes.Select(x => x.Name))}.");
				}
			}

			return constructedMethod.Invoke(target, arguments);
		}

		/// <summary>
		/// Try to construct and invoke a generic method, discovering the method's generic type arguments from the types
		/// of arguments given.
		/// </summary>
		/// <param name="returnValue">The return value of the method if invocation was successful.</param>
		/// <param name="target">The target object to invoke the method on, or <c>null</c> for a static method.</param>
		/// <param name="arguments">The <see cref="Type"/>s of the arguments to be passed to the constructed method. 
		/// Must be non-null.</param>
		/// <returns><c>True</c> if the method was successfully constructed and invoked, <c>false</c> otherwise.</returns>
		public bool TryInvoke(out object returnValue, object target, params object[] arguments)
		{
			if ((arguments == null && parameterTypes.Length > 0) ||
				(arguments != null && arguments.Length != parameterTypes.Length))
			{
				returnValue = null;
				return false;
			}

			MethodInfo constructedMethod;

			if (parameterTypes.Length == 0)
			{
				constructedMethod = MakeGenericMethod();
				if (constructedMethod == null)
				{
					throw new InvalidOperationException($"Cannot construct method from {MethodDefinition}.");
				}
			}
			else
			{
				Type[] argumentTypes = arguments.Select(x => x.GetType()).ToArray();
				constructedMethod = MakeGenericMethod(argumentTypes);
				if (constructedMethod == null)
				{
					throw new InvalidOperationException($"Cannot construct method from {MethodDefinition} using " +
						$"argument types {string.Join(", ", argumentTypes.Select(x => x.Name))}.");
				}
			}

			returnValue = constructedMethod.Invoke(target, arguments);
			return true;
		}

		/// <summary>
		/// Try to construct and invoke a generic method, discovering the method's generic type arguments from the types
		/// of arguments given.
		/// </summary>
		/// <param name="target">The target object to invoke the method on, or <c>null</c> for a static method.</param>
		/// <param name="arguments">The <see cref="Type"/>s of the arguments to be passed to the constructed method. 
		/// Must be non-null.</param>
		/// <returns><c>True</c> if the method was successfully constructed and invoked, <c>false</c> otherwise.</returns>
		public bool TryInvoke(object target, params object[] arguments)
		{
			if ((arguments == null && parameterTypes.Length > 0) ||
				(arguments != null && arguments.Length != parameterTypes.Length))
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
		/// True if <paramref name="other"/> has the same <see cref="MethodDefinition"/> property.
		/// </summary>
		/// <param name="other">A <see cref="MethodArgumentsMap"/> for comparison.</param>
		public bool Equals(MethodArgumentsMap other) => MethodDefinition == other?.MethodDefinition;

		/// <summary>
		/// True if <paramref name="obj"/> is a <see cref="MethodArgumentsMap"/> with an equal 
		/// <see cref="MethodDefinition"/> property.
		/// </summary>
		/// <param name="obj">An <see cref="object"/> for comparison.</param>
		public override bool Equals(object obj) => obj is MethodArgumentsMap map && Equals(map);

		/// <summary>
		/// Get a hashcode based on <see cref="MethodDefinition"/>.
		/// </summary>
		/// <returns>A hashcode based on <see cref="MethodDefinition"/>.</returns>
		public override int GetHashCode() => MethodDefinition.GetHashCode();

		/// <summary>
		/// Get the string representation of <see cref="MethodDefinition"/>.
		/// </summary>
		/// <returns>A string based on <see cref="MethodDefinition"/>.</returns>
		public override string ToString() => MethodDefinition.ToString();

		// Look for the method generic parameter in its arguments and create a delegate telling us how to navigate
		// there from the method signature
		private Stack<Func<Type[], Type[]>> MapParameter(Type param)
		{
			Stack<Func<Type[], Type[]>> stack = new Stack<Func<Type[], Type[]>>();

			if (!findArgument(parameterTypes))
			{
				// The method's generic parameter doesn't seem to meaningfully constrain the method's arguments,
				// so just assign it typeof(object)
				stack.Clear();
				stack.Push((args) => new Type[] { typeof(object) });
				return stack;
			}

			// This delegate tells us how to navigate through a set of argument types to find
			// the closed type to assign to the method's generic type parameter
			return stack;

			// This recursive local method creates the map
			bool findArgument(Type[] search)
			{
				for (int j = 0; j < search.Length; j++)
				{
					Type searchj = search[j];

					if (searchj == param)
					{
						// If we find the type parameter itself, the search is over
						stack.Push((args) => new Type[] { args[j] });
						return true;
					}
					else if (searchj.ContainsGenericParameters)
					{
						if (!searchj.IsGenericParameter)
						{
							// At an open generic type, check its type arguments to see if they contain our target parameter
							if (findArgument(searchj.GenericTypeArguments))
							{
								stack.Push((args) => args[j].GenericTypeArguments);
								return true;
							}
						}
						else
						{
							// At a generic parameter, we check its constraints to see if the type parameter is identified
							// within those constraints and can be inferred from how a different parameter satisfies them
							Type[] constraints = searchj.GetGenericParameterConstraints();

							for (int k = 0; k < constraints.Length; k++)
							{
								if (constraints[k].ContainsGenericParameters)
								{
									if (findArgument(constraints[k].GenericTypeArguments))
									{
										stack.Push((args) =>
										{
											_ = ReflectionTools.InheritsFromOpenGenericType(constraints[k], args[j], out Type[] withArgs);
											return withArgs;
										});
										return true;
									}
								}
							}
						}
					}
				}

				return false;
			}
		}
	}
}
