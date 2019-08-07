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
	internal sealed class MethodArgumentsMap : IEquatable<MethodArgumentsMap>
	{
		private readonly Type[] parameterTypes;

		private readonly Stack<Func<Type[], Type[]>>[] maps;

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
				parameterTypes = MethodDefinition.GetParameters()
					.Select(x => x.ParameterType).ToArray();
				return;
			}

			// Create a mapping function for generic methods

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
		/// Construct and invoke this method definition on the given arguments, return the result. Throws an
		/// <see cref="InvalidOperationException"/> if the method cannot be constructed from the given arguments'
		/// types.
		/// </summary>
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
		/// Attempt to discover the generic type arguments of <see cref="MethodDefinition"/> from the types of the
		/// supplied arguments, and if possible, invoke a constructed version of the method on the given target
		/// using the given arguments.
		/// </summary>
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
		/// Attempt to discover the generic type arguments of <see cref="MethodDefinition"/> from the types of the
		/// supplied arguments, and if possible, invoke a constructed version of the method on the given target
		/// using the given arguments.
		/// </summary>
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
		public bool Equals(MethodArgumentsMap other) => MethodDefinition == other?.MethodDefinition;

		/// <summary>
		/// True if <paramref name="obj"/> is a <see cref="MethodArgumentsMap"/> with an equal 
		/// <see cref="MethodDefinition"/> property.
		/// </summary>
		public override bool Equals(object obj) => obj is MethodArgumentsMap map && Equals(map);

		/// <summary>
		/// Return a hashcode based on <see cref="MethodDefinition"/>.
		/// </summary>
		public override int GetHashCode() => MethodDefinition.GetHashCode();

		/// <summary>
		/// Returns the string representation of <see cref="MethodDefinition"/>.
		/// </summary>
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
