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
		private static readonly Func<object, bool> completedTask = x => true;
		private static readonly object placeHolder = new object();

		private static readonly MethodInfo createObjectBuilderMethod = typeof(XReaderImpl)
			.GetMethod(nameof(CreateObjectBuilder), BindingFlags.Instance | BindingFlags.NonPublic);

		private readonly XDomain domain;

		private readonly LinkedList<ReadTask> tasks = new LinkedList<ReadTask>();

		private readonly IDictionary<Type, Func<Type, Action<object>, object>> createObjectBuilderDelegates =
			new Dictionary<Type, Func<Type, Action<object>, object>>();

		internal XReaderImpl(XDomain domain)
		{
			this.domain = domain;
			ExceptionHandler = domain.ExceptionHandler;
		}

		public override void Submit(object obj)
		{
			if (obj != null)
			{
				ForEachComponent(x => x.Submit(this, obj));
			}
		}

		public override void SubmitAll(IEnumerable objects)
		{
			if (objects != null)
			{
				foreach (object obj in objects)
				{
					Submit(obj);
				}
			}
		}

		public void AddTask(object source, Func<bool> task) =>
			tasks.AddLast(new ReadTask(source, task ?? throw new ArgumentNullException(nameof(task))));

		public override object Read(XElement element) => Read<object>(element);

		public override T Read<T>(XElement element)
		{
			if (element == null)
			{
				ExceptionHandler(new ArgumentNullException("Cannot read a null XElement."));
				return default;
			}

			object result = placeHolder;
			Read<T>(element, x =>
				{
					result = x;
					return true;
				});
			Finish();

			return CastResult<T>(result);
		}

		public override IEnumerable<object> ReadAll(IEnumerable<XElement> elements) => ReadAll<object>(elements);

		public override IEnumerable<T> ReadAll<T>(IEnumerable<XElement> elements)
		{
			if (elements == null)
			{
				ExceptionHandler(new ArgumentNullException("Cannot read null as an IEnumerable<XElement>."));
				return Enumerable.Empty<T>();
			}
			if (!elements.Any())
			{
				return Enumerable.Empty<T>();
			}

			List<object> results = new List<object>();

			foreach (XElement element in elements)
			{
				results.Add(placeHolder);

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

			Finish();

			return results.Select(x => CastResult<T>(x));
		}

		public void Read<T>(XElement element, Func<T, bool> assign, ReaderHints hint = ReaderHints.Default) =>
			Read(element, typeof(T), x => assign?.Invoke((T)x) != false, hint);

		public void Read(XElement element, Func<object, bool> assign, ReaderHints hint = ReaderHints.Default) =>
			Read(element, typeof(object), assign, hint);

		public void Read<T>(XAttribute attribute, Func<T, bool> assign, ReaderHints hint = ReaderHints.Default) =>
			Read(attribute, typeof(T), x => assign?.Invoke((T)x) != false, hint);

		public void Read(XElement element, Type expectedType, Func<object, bool> assign, ReaderHints hint = ReaderHints.Default)
		{
			if (element == null)
			{
				ExceptionHandler(new ArgumentNullException("Cannot read null XElement"));
				return;
			}
			if (expectedType == null)
			{
				ExceptionHandler(new ArgumentNullException("Cannot read without expected Type"));
				return;
			}
			if (expectedType.IsXIgnored())
			{
				ExceptionHandler(new InvalidOperationException($"Cannot reflect the ignored type {expectedType}."));
				return;
			}

			if (assign == null)
			{
				assign = completedTask;
			}

			// Resolve the type of the object at this element

			XTypeBox box = null;

			if (!hint.HasFlag(ReaderHints.IgnoreElementName))
			{
				box = domain.ReflectElement(element, expectedType, false);
			}

			// If the type can be subclassed, and the element consists of a single inner element that reflects to a subclass,
			// assume we're looking at an explicitly typed object

			Type expectedImpl = box == null ? expectedType : box.Type;

			if (!expectedImpl.IsSealed &&
				!element.HasAttributes &&
				element.Elements().Count() == 1)
			{
				XElement innerEl = element.Elements().First();

				XTypeBox boxImpl = domain.ReflectElement(innerEl, expectedImpl, false);
				if (boxImpl != null)
				{
					box = boxImpl;
					element = innerEl;
				}
			}

			// Ensure we have an XType<T> to work with

			if (box == null)
			{
				box = domain.ReflectFromType(expectedType);
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

			if (ForEachComponent(x => box.OnComponentRead(x, this, element, assign)))
			{
				return;
			}

			// Inform XTypeComponents

			if (box.OnRead(this, element, out object result))
			{
				Submit(result);
				if (!assign(result))
				{
					AddTask(this, () => assign(result));
				}
				return;
			}

			// Move on to advanced reading with ObjectBuilder


			if (!createObjectBuilderDelegates.TryGetValue(box.Type, out Func<Type, Action<object>, object> cobDelegate))
			{
				cobDelegate = (Func<Type, Action<object>, object>)Delegate
					.CreateDelegate(typeof(Func<Type, Action<object>, object>), this, createObjectBuilderMethod);
				createObjectBuilderDelegates.Add(box.Type, cobDelegate);
			}

			object objectBuilder = cobDelegate(box.Type, (obj) =>
			{
				Submit(obj);
				if (!assign(obj))
				{
					AddTask(this, () => assign(obj));
				}
			});

			// Invoke XType components that work on object builders

			box.OnBuild(this, element, objectBuilder);
		}

		public void Read(XAttribute attribute, Type expectedType, Func<object, bool> assign, ReaderHints hint = ReaderHints.Default)
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
			if (expectedType.IsXIgnored())
			{
				ExceptionHandler(new InvalidOperationException($"Cannot reflect the ignored type {expectedType}."));
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

			// Get an XType

			XTypeBox box = domain.ReflectFromType(expectedType);
			if (box == null)
			{
				return;
			}

			// Inform XReaderComponents

			if (ForEachComponent(x => box.OnComponentRead(x, this, attribute, assign)))
			{
				return;
			}

			// Inform XTypeComponents

			if (box.OnRead(this, attribute, out object result))
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

		internal void Finish()
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
						ExceptionHandler(new InvalidOperationException($"{tasks.Count} unfinished component tasks from sources: " +
							string.Join(";", tasks.Select(x => x.Source).Where(x => x != null))));
					}
					break;
				}
			}

			// Reset the reader

			tasks.Clear();

			createObjectBuilderDelegates.Clear();
		}

		private T CastResult<T>(object result)
		{
			if (result == placeHolder)
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

			ExceptionHandler(new InvalidCastException($"Read an object as Type {result.GetType().FullName}, " +
				$"which is not assignable to expected type {typeof(T).FullName}."));
			return default;
		}

		private object CreateObjectBuilder(Type type, Action<object> onConstructed) =>
			typeof(ObjectBuilder<>)
				.MakeGenericType(type)
				.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
				.FirstOrDefault()
				.Invoke(new object[] { onConstructed });
	}
}
