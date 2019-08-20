using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using XMachine.Reflection;

namespace XMachine
{
	internal sealed class XReaderImpl : XReader, IXReadOperation
	{
		private readonly struct ReadTask
		{
			private readonly Func<bool> task;

			internal ReadTask(object source, Func<bool> task)
			{
				Source = source ?? throw new ArgumentNullException(nameof(source));
				this.task = task ?? throw new ArgumentNullException(nameof(task));
			}

			internal object Source { get; }

			internal bool Invoke() => task();
		}

		private static readonly Func<object, bool> completedTask = x => true;

		// Cache bound delegates to create object builders (speed things up if there are a lot of complex objects)

		private static readonly MethodInfo createObjectBuilderMethod = typeof(XReaderImpl)
			.GetMethod(nameof(CreateObjectBuilderDelegate), BindingFlags.Instance | BindingFlags.NonPublic);

		private readonly IDictionary<Type, Func<Action<object>, object>> createObjectBuilderDelegates =
			new Dictionary<Type, Func<Action<object>, object>>();

		// Task queue

		private readonly LinkedList<ReadTask> tasks = new LinkedList<ReadTask>();

		internal XReaderImpl(XDomain domain) : base(domain) { }

		/*
		 * API methods (XReader)
		 */

		public override void Submit(object obj)
		{
			if (obj != null)
			{
				ForEachComponent(x => x.Submit(this, obj));
			}
		}

		public override void SubmitAll(IEnumerable objects)
		{
			if (objects == null)
			{
				throw new ArgumentNullException(nameof(objects));
			}

			foreach (object obj in objects)
			{
				Submit(obj);
			}
		}

		public override object Read(XElement element) => Read<object>(element);

		public override T Read<T>(XElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			// Read root object

			object result = XmlTools.PlaceholderObject;
			Read<T>(element, x =>
				{
					result = x;
					return true;
				});

			// Finish task queue

			Finish();

			return CastResult<T>(result);
		}

		public override IEnumerable<object> ReadAll(IEnumerable<XElement> elements) =>
			ReadAll<object>(elements);

		public override IEnumerable<T> ReadAll<T>(IEnumerable<XElement> elements)
		{
			if (elements == null)
			{
				throw new ArgumentNullException(nameof(elements));
			}

			// Read objects

			List<object> results = new List<object>();

			foreach (XElement element in elements)
			{
				results.Add(XmlTools.PlaceholderObject);

				if (element == null)
				{
					ExceptionHandler(new ArgumentNullException("Cannot read null element"));
					continue;
				}

				int idx = results.Count - 1;
				Read<T>(element, x =>
					{
						results[idx] = x;
						return true;
					});
			}

			// Finish task queue

			Finish();

			return results.Select(x => CastResult<T>(x)).ToArray();
		}

		/*
		 * Backend methods (IXReadOperation)
		 */

		public void AddTask(object source, Func<bool> task) =>
			tasks.AddLast(new ReadTask(source ?? this, task ?? throw new ArgumentNullException(nameof(task))));

		public void Read<T>(XElement element, Func<T, bool> assign, XObjectArgs args = null) =>
			Read(element, typeof(T), BoxDelegate(assign), args);

		public void Read(XElement element, Func<object, bool> assign, XObjectArgs args = null) =>
			Read(element, typeof(object), assign, args);

		public void Read<T>(XAttribute attribute, Func<T, bool> assign, XObjectArgs args = null) =>
			Read(attribute, typeof(T), BoxDelegate(assign), args);

		public void Read(XElement element, Type expectedType, Func<object, bool> assign, XObjectArgs args = null)
		{
			if (element == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(element)));
				return;
			}
			if (expectedType == null)
			{
				ExceptionHandler(new ArgumentNullException(nameof(expectedType)));
				return;
			}

			if (assign == null)
			{
				assign = completedTask;
			}

			if (args == null)
			{
				args = XObjectArgs.Default;
			}

			// Resolve the type of the object at this element

			XTypeBox box = null;

			if (!args.Hints.HasFlag(ObjectHints.IgnoreElementName))
			{
				box = Domain.ReflectElement(element, expectedType, false);
			}

			// If the type can be subclassed, and the element consists of a single inner element that reflects to a subclass,
			// assume we're looking at an explicitly typed object

			Type expectedImpl = box == null ? expectedType : box.Type;

			if ((expectedImpl.IsInterface || (expectedImpl.IsClass && !expectedImpl.IsSealed)) &&
				!element.HasAttributes &&
				element.HasElements &&
				element.Elements().Count() == 1)
			{
				XElement innerEl = element.Elements().First();

				XTypeBox boxImpl = Domain.ReflectElement(innerEl, expectedImpl, false);
				if (boxImpl != null)
				{
					box = boxImpl;
					element = innerEl;
				}
			}

			// Ensure we have an XType<T> to work with

			if (box == null)
			{
				box = Domain.ReflectFromType(expectedType);
			}

			if (box == null)
			{
				ExceptionHandler(new InvalidOperationException(
					$"Cannot determine the type of the object at {element.Name}, expecting {expectedType.Name}."));
				return;
			}
			else if (!box.Type.CanCreateInstances())
			{
				ExceptionHandler(new InvalidOperationException(
					$"Cannot create an instance of the ignored or abstract type {box.Type}."));
				return;
			}

			// Inform XReaderComponents

			if (ForEachComponent(x => box.OnComponentRead(x, this, element, assign, args)))
			{
				return;
			}

			// Inform XTypeComponents

			if (box.OnRead(this, element, out object result, args))
			{
				Submit(result);
				if (!assign(result))
				{
					AddTask(this, () => assign(result));
				}
				return;
			}

			// Move on to advanced reading with ObjectBuilder

			if (!createObjectBuilderDelegates.TryGetValue(box.Type, out Func<Action<object>, object> cobDelegate))
			{
				cobDelegate = (Func<Action<object>, object>)createObjectBuilderMethod
					.MakeGenericMethod(box.Type)
					.Invoke(this, null);
				createObjectBuilderDelegates.Add(box.Type, cobDelegate);
			}

			object objectBuilder = cobDelegate(obj =>
			{
				Submit(obj);
				if (!assign(obj))
				{
					AddTask(this, () => assign(obj));
				}
			});

			// Invoke XType components that work on object builders

			box.OnBuild(this, element, objectBuilder, args);
		}

		public void Read(XAttribute attribute, Type expectedType, Func<object, bool> assign, XObjectArgs args = null)
		{
			if (attribute == null)
			{
				ExceptionHandler(new ArgumentNullException("Cannot read null XAttribute"));
				return;
			}
			if (expectedType == null)
			{
				ExceptionHandler(new ArgumentNullException("Cannot read without expected Type"));
				return;
			}
			if (!expectedType.CanCreateInstances())
			{
				ExceptionHandler(new InvalidOperationException($"Cannot create an instance of the abstract type {expectedType}."));
				return;
			}

			if (assign == null)
			{
				assign = completedTask;
			}
			if (args == null)
			{
				args = XObjectArgs.Default;
			}

			// Get an XType

			XTypeBox box = Domain.ReflectFromType(expectedType);
			if (box == null)
			{
				return;
			}

			// Inform XReaderComponents

			if (ForEachComponent(x => box.OnComponentRead(x, this, attribute, assign, args)))
			{
				return;
			}

			// Inform XTypeComponents

			if (box.OnRead(this, attribute, out object result, args))
			{
				Submit(result);
				if (!assign(result))
				{
					AddTask(this, () => assign(result));
				}
				return;
			}

			// No luck

			ExceptionHandler(new InvalidOperationException($"Unable to read XAttribute {attribute.Name}."));
		}

		private void Finish()
		{
			while (tasks.Count > 0)
			{
				bool progress = false;

				// Try to finish tasks

				LinkedListNode<ReadTask> taskNode = tasks.First;

				while (taskNode != null)
				{
					try
					{
						if (taskNode.Value.Invoke())
						{
							progress = true;
							taskNode.List.Remove(taskNode);
						}
					}
					catch (Exception e)
					{
						ExceptionHandler(e);
						taskNode.List.Remove(taskNode);
						progress = true;
					}

					taskNode = taskNode.Next;
				}

				// Break if we haven't made any progress

				if (!progress && tasks.Count > 0)
				{
					ExceptionHandler(new InvalidOperationException(
						$"{typeof(XReaderImpl)} finished without completing all tasks."));
					if (tasks.Count > 0)
					{
						ExceptionHandler(new InvalidOperationException($"{tasks.Count} has unfinished component tasks from sources: " +
							string.Join(";", tasks.Select(x => x.Source).Where(x => x != null))));
					}
					break;
				}
			}

			// Reset the reader

			tasks.Clear();
			createObjectBuilderDelegates.Clear();
		}

		private Func<object, bool> BoxDelegate<T>(Func<T, bool> assign) =>
			x => assign?.Invoke((T)x) != false;

		private T CastResult<T>(object result)
		{
			if (ReferenceEquals(result, XmlTools.PlaceholderObject))
			{
				return default;
			}
			if (result == null)
			{
				return default;
			}
			if (typeof(T).IsAssignableFrom(result.GetType()))
			{
				return (T)result;
			}

			ExceptionHandler(new InvalidCastException($"Deserialized object type {result.GetType().FullName}, " +
				$"is not assignable to expected type {typeof(T).FullName}."));
			return default;
		}

		private Func<Action<object>, object> CreateObjectBuilderDelegate<T>() =>
			(onConstructed) => new ObjectBuilder<T>(onConstructed);
	}
}
