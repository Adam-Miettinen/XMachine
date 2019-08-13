using System;
using System.Collections.Generic;
using System.Linq;
using XMachine.Components;

namespace XMachine
{
	/// <summary>
	/// Represents an object within <see cref="XMachine"/> whose functionality is extensible by adding components
	/// of type <typeparamref name="T"/>.
	/// </summary>
	public abstract class XWithComponents<T> : IExceptionHandler, IXWithComponents<T> where T : IXComponent
	{
		private readonly ISet<T> registeredComponents = new HashSet<T>();
		private IEnumerator<T> enumerator;
		private Action<Exception> exceptionHandler;

		/// <summary>
		/// Create a new instance of <see cref="XWithComponents{T}"/>.
		/// </summary>
		public XWithComponents() => Enumerator = registeredComponents.GetEnumerator();

		/// <summary>
		/// Get an <see cref="IEnumerator{T}"/> over the components in this <see cref="XWithComponents{T}"/>. Enumerates
		/// both enabled and disabled components.
		/// </summary>
		protected IEnumerator<T> Enumerator
		{
			get
			{
				enumerator.Reset();
				return enumerator;
			}
			private set => enumerator = value;
		}

		/// <summary>
		/// Get or set the delegate that handles <see cref="Exception"/>s.
		/// </summary>
		public virtual Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? XmlTools.ThrowHandler;
			set => exceptionHandler = value;
		}

		/// <summary>
		/// Retrieve a component of type <typeparamref name="V"/>.
		/// </summary>
		public V Component<V>() where V : T => registeredComponents.OfType<V>().FirstOrDefault();

		/// <summary>
		/// Retrieve all components of type <typeparamref name="V"/>.
		/// </summary>
		public IEnumerable<V> Components<V>() where V : T => registeredComponents.OfType<V>();

		/// <summary>
		/// Retrieve all components on this <see cref="XWithComponents{T}"/>.
		/// </summary>
		public IEnumerable<T> Components() => registeredComponents;

		/// <summary>
		/// Register a component.
		/// </summary>
		/// <param name="component">The component to register.</param>
		public void Register(T component)
		{
			if (component == null)
			{
				throw new ArgumentNullException(nameof(component));
			}
			if (!registeredComponents.Contains(component))
			{
				OnComponentsRegistered(Enumerable.Repeat(component, 1));
			}
		}

		/// <summary>
		/// Register components.
		/// </summary>
		/// <param name="components">The components to register.</param>
		public void Register(params T[] components)
		{
			if (components != null && components.Length > 0)
			{
				List<T> registered = new List<T>(components.Length);

				foreach (T component in components)
				{
					if (component == null)
					{
						throw new ArgumentNullException("Component cannot be null");
					}
					else if (!registeredComponents.Contains(component))
					{
						registered.Add(component);
					}
				}
				OnComponentsRegistered(registered);
			}
		}

		/// <summary>
		/// Deregister a component.
		/// </summary>
		/// <param name="component">The component to deregister.</param>
		public void Deregister(T component) => registeredComponents.Remove(component);

		/// <summary>
		/// Deregister components.
		/// </summary>
		/// <param name="components">The components to deregister.</param>
		public void Deregister(params T[] components)
		{
			if (components != null && components.Length > 0)
			{
				foreach (T comp in components)
				{
					Deregister(comp);
				}
			}
		}

		/// <summary>
		/// Perform a delegate on each enabled component, catching exceptions and handling them with 
		/// <see cref="ExceptionHandler"/>.
		/// </summary>
		/// <param name="action">The delegate to be performed.</param>
		protected void ForEachComponent(Action<T> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			IEnumerator<T> enumerator = Enumerator;

			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Enabled)
				{
					try
					{
						action(enumerator.Current);
					}
					catch (Exception e)
					{
						ExceptionHandler(new ComponentException(enumerator.Current, e));
					}

				}
			}
		}

		/// <summary>
		/// Perform a delegate on each enabled component, catching exceptions and handling them with 
		/// <see cref="ExceptionHandler"/>.
		/// </summary>
		/// <param name="func">The delegate to be performed.</param>
		/// <returns>An <see cref="IEnumerable{TReturn}"/> over the values returned by enabled components
		/// that did not throw exceptions.</returns>
		protected IEnumerable<TReturn> ForEachComponent<TReturn>(Func<T, TReturn> func)
		{
			IEnumerator<T> enumerator = Enumerator;

			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Enabled)
				{
					TReturn result = default;
					try
					{
						result = func(enumerator.Current);
					}
					catch (Exception e)
					{
						ExceptionHandler(new ComponentException(enumerator.Current, e));
						continue;
					}
					yield return result;
				}
			}
		}

		/// <summary>
		/// Perform a delegate on each enabled component, catching exceptions and handling them with 
		/// <see cref="ExceptionHandler"/>, until the given <see cref="Predicate{TReturn}"/> returns
		/// <c>true</c>.
		/// </summary>
		/// <param name="func">The delegate to be performed.</param>
		/// <param name="until">A predicate that halts the execution of the delegate once it returns
		/// <c>true</c>.</param>
		/// <returns>The return value of the delegate when <paramref name="until"/> returned <c>true</c>,
		/// or a default value if <paramref name="until"/> was <c>false</c> for all components.</returns>
		protected TReturn ForEachComponent<TReturn>(Func<T, TReturn> func, Predicate<TReturn> until)
		{
			IEnumerator<T> enumerator = Enumerator;

			TReturn result = default;

			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Enabled)
				{
					try
					{
						result = func(enumerator.Current);
						if (until(result))
						{
							break;
						}
					}
					catch (Exception e)
					{
						ExceptionHandler(new ComponentException(enumerator.Current, e));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Perform a delegate on each enabled component, catching exceptions and handling them with 
		/// <see cref="ExceptionHandler"/>, until the delegate returns <c>true</c>.
		/// </summary>
		/// <param name="func">The delegate to be performed.</param>
		/// <returns>The return value of the delegate from the last component on which it was
		/// executed.</returns>
		protected bool ForEachComponent(Func<T, bool> func)
		{
			IEnumerator<T> enumerator = Enumerator;

			bool result = false;

			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Enabled)
				{
					try
					{
						result = func(enumerator.Current);
						if (result)
						{
							break;
						}
					}
					catch (Exception e)
					{
						ExceptionHandler(new ComponentException(enumerator.Current, e));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Updates the internal <see cref="IEnumerator{T}"/> used to iterate over components. Implementers
		/// can override this method to perform actions on newly-registered components.
		/// </summary>
		/// <param name="components">The component or components registered.</param>
		protected virtual void OnComponentsRegistered(IEnumerable<T> components)
		{
			if (components.Any())
			{
				Enumerator.Dispose();
				Enumerator = null;

				foreach (T component in components)
				{
					_ = registeredComponents.Add(component);
				}

				Enumerator = registeredComponents.GetEnumerator();
			}
		}
	}
}
