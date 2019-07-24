using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMachine
{
	internal sealed class XReaderImpl : XReader, IXReadOperation
	{
		private static readonly Func<object, bool> completedTask = x => true;
		private static readonly object placeHolder = new object();

		private readonly XDomain domain;

		private readonly LinkedList<ObjectBuilderBox> objectBuilders = new LinkedList<ObjectBuilderBox>();
		private readonly LinkedList<Func<bool>> compTasks = new LinkedList<Func<bool>>();

		internal XReaderImpl(XDomain domain)
		{
			this.domain = domain;
			ExceptionHandler = domain.ExceptionHandler;
		}

		public override void Submit(object obj)
		{
			if (obj != null)
			{
				foreach (Func<bool> task in ForEachComponent(x =>
					{
						x.Submit(this, obj, out Func<bool> compTask);
						return compTask;
					}))
				{
					if (task != null)
					{
						_ = compTasks.AddLast(task);
					}
				}
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
#if DEBUG
			Console.WriteLine($"{nameof(XReaderImpl)} arrived at element {element?.Name} expecting {expectedType?.FullName}.");
#endif
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

			// Start reading

			Func<bool> task = ForEachComponent(
				x => x.Read(this, element, expectedType, assign, out Func<bool> compTask) ? compTask : null,
				x => x != null);

			if (task != null)
			{
				compTasks.AddLast(task);
				return;
			}

			// No immediate success, resolve the type

			XTypeBox box = null;

			if (box == null && !hint.HasFlag(ReaderHints.IgnoreElementName))
			{
				box = domain.ReflectElement(element, expectedType, false);
			}

			// Check for an inner element that implements the expected type

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
			if (box == null)
			{
				box = domain.ReflectFromType(expectedType);
			}

			if (!box.Type.CanCreateInstances())
			{
				ExceptionHandler(new InvalidOperationException(
					$"Cannot create an instance of the ignored or abstract type {box.Type}."));
				return;
			}

			// Try reading again, invoking XType components

			if (box.OnRead(this, element, expectedType, out object result))
			{
				Submit(result);
				if (!assign(result))
				{
					compTasks.AddLast(() => assign(result));
				}
				return;
			}

			// Still no luck, move on to an ObjectBuilder

			ObjectBuilderBox objectBuilder = ObjectBuilderBox.Create(box.Type);
			objectBuilder.OnConstructed = () =>
			{
				Submit(objectBuilder.Object);
				if (!assign(objectBuilder.Object))
				{
					compTasks.AddLast(() => assign(objectBuilder.Object));
				}
			};

			// Invoke XType components that work on object builders

			box.OnBuild(this, element, objectBuilder);

			// Try to finish, clean up some memory

			_ = objectBuilder.TryFinish();
			if (!objectBuilder.IsFinished)
			{
				objectBuilders.AddLast(objectBuilder);
			}
			else if (!objectBuilder.IsConstructed)
			{
				ExceptionHandler(new InvalidOperationException($"Failed to construct an object."));
			}
		}

		public void Read(XAttribute attribute, Type expectedType, Func<object, bool> assign, ReaderHints hint = ReaderHints.Default)
		{
#if DEBUG
			Console.WriteLine($"{nameof(XReaderImpl)} arrived at attribute {attribute?.Name} expecting {expectedType?.FullName}.");
#endif
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

			// Start reading

			Func<bool> task = ForEachComponent(
				x => x.Read(this, attribute, expectedType, assign, out Func<bool> compTask) ? compTask : null,
				x => x != null);

			if (task != null)
			{
				if (!task())
				{
					compTasks.AddLast(task);
				}
				return;
			}

			// Get an XType

			XTypeBox box = domain.ReflectFromType(expectedType);
			if (box == null)
			{
				return;
			}

			// Try reading again, invoking XType components

			if (box.OnRead(this, attribute, expectedType, out object result))
			{
				Submit(result);
				if (!assign(result))
				{
					compTasks.AddLast(() => assign(result));
				}
				return;
			}

			// No luck

			ExceptionHandler(new InvalidOperationException($"Unable to read XAttribute {attribute.Name}."));
		}

		internal void Finish()
		{
			while (objectBuilders.Count > 0 || compTasks.Count > 0)
			{
				bool progress = false;

				// Try to finish object builders

				LinkedListNode<ObjectBuilderBox> obNode = objectBuilders.First;

				while (obNode != null)
				{
					try
					{
						if (obNode.Value.TryFinish())
						{
							progress = true;
						}
					}
					catch (Exception e)
					{
						ExceptionHandler(e);
					}

					if (obNode.Value.IsFinished)
					{
						if (!obNode.Value.IsConstructed)
						{
							ExceptionHandler(new InvalidOperationException($"Failed to construct an object."));
						}

						obNode.List.Remove(obNode);
					}
					obNode = obNode.Next;
				}

				// Try to finish tasks

				LinkedListNode<Func<bool>> taskNode = compTasks.First;

				while (taskNode != null)
				{
					try
					{
						if (taskNode.Value())
						{
							progress = true;
							taskNode.List.Remove(taskNode);
						}
					}
					catch (Exception e)
					{
						ExceptionHandler(e);
						taskNode.List.Remove(taskNode);
					}

					taskNode = taskNode.Next;
				}

				// Break if we haven't made any progress

				if (!progress && (objectBuilders.Count > 0 || compTasks.Count > 0))
				{
					ExceptionHandler(new InvalidOperationException(
						$"{typeof(XReaderImpl)} finished without completing {objectBuilders.Count} objects."));
					break;
				}
			}

			// Reset the reader

			objectBuilders.Clear();
			compTasks.Clear();
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

			ExceptionHandler(new InvalidCastException($"Read object as Type {result.GetType().FullName}, " +
				$"which is not assignable to expected type {typeof(T).FullName}."));
			return default;
		}
	}
}
