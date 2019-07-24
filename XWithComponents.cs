using System;
using System.Collections.Generic;
using System.Linq;

namespace XMachine
{
	/// <summary>
	/// Represents an object within <see cref="XMachine"/> whose functionality is represented by
	/// components, which are objects of type <typeparamref name="T"/>.
	/// </summary>
	public abstract class XWithComponents<T> : IExceptionHandler, IXWithComponents<T> where T : IXComponent
	{
		private readonly ISet<T> registeredComponents = new HashSet<T>();
		private IEnumerator<T> enumerator;
		private Action<Exception> exceptionHandler;

		/// <summary>
		/// Create a new instance of <see cref="XWithComponents{T}"/> to manage components of the given
		/// type.
		/// </summary>
		public XWithComponents() => Enumerator = registeredComponents.GetEnumerator();

		private IEnumerator<T> Enumerator
		{
			get
			{
				enumerator.Reset();
				return enumerator;
			}
			set => enumerator = value;
		}

		/// <summary>
		/// The delegate that handles exceptions thrown by components.
		/// </summary>
		public Action<Exception> ExceptionHandler
		{
			get => exceptionHandler ?? XComponents.ThrowHandler;
			set => exceptionHandler = value;
		}

		/// <summary>
		/// Retrieve a component of the given type.
		/// </summary>
		public V Component<V>() where V : T => registeredComponents.OfType<V>().FirstOrDefault();

		/// <summary>
		/// Retrieve all components of the given type.
		/// </summary>
		public IEnumerable<V> Components<V>() where V : T => registeredComponents.OfType<V>();

		/// <summary>
		/// Retrieve all components.
		/// </summary>
		public IEnumerable<T> Components() => registeredComponents;

		/// <summary>
		/// Register a component.
		/// </summary>
		public void Register(T component)
		{
			if (component == null)
			{
				ExceptionHandler(new ArgumentNullException("Component cannot be null"));
				return;
			}
			if (!registeredComponents.Contains(component))
			{
				OnComponentsRegistered(Enumerable.Repeat(component, 1));
			}
		}

		/// <summary>
		/// Register components.
		/// </summary>
		public void Register(params T[] components)
		{
			if (components != null && components.Length > 0)
			{
				List<T> registered = new List<T>(components.Length);

				foreach (T component in components)
				{
					if (component == null)
					{
						ExceptionHandler(new ArgumentNullException("Component cannot be null"));
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
		public void Deregister(T component) => registeredComponents.Remove(component);

		/// <summary>
		/// Deregister components.
		/// </summary>
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
		/// Perform a delegate on each enabled component. Any exceptions thrown by delegates will be caught and handled 
		/// by <see cref="ExceptionHandler"/>.
		/// </summary>
		protected void ForEachComponent(Action<T> action)
		{
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
						ExceptionHandler(e);
					}

				}
			}
		}

		/// <summary>
		/// Perform a delegate on each enabled component and return a lazy enumeration of the return values. 
		/// Any exceptions thrown by delegates will be caught and handled by <see cref="ExceptionHandler"/>;
		/// their return values will be excluded from the enumeration.
		/// </summary>
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
						ExceptionHandler(e);
						continue;
					}
					yield return result;
				}
			}
		}

		/// <summary>
		/// Perform a delegate on each enabled component, stopping when the given predicate over the 
		/// delegate's return values evaluates to true. The return value is the return value at which
		/// this method stopped or, if the predicate was never true, a default value. Any exceptions thrown 
		/// by delegates will be caught and handled by <see cref="ExceptionHandler"/>.
		/// </summary>
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
						ExceptionHandler(e);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Perform a delegate on each enabled component, stopping when the first of them returns true. The return
		/// value is true if any component return true. Any exceptions thrown by delegates will be
		/// caught and handled by <see cref="ExceptionHandler"/>.
		/// </summary>
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
						ExceptionHandler(e);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Extend this method to perform additional behaviour when new components are registered.
		/// </summary>
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
