using System;
using System.Linq;
using System.Reflection;

namespace XMachine
{
	internal sealed class ObjectBuilderBox
	{
		private static readonly MethodInfo createMethod = typeof(ObjectBuilderBox)
			.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
			.FirstOrDefault(x => x.IsGenericMethodDefinition && x.Name == nameof(Create));

		private static readonly MethodInfo boxMethod = typeof(ObjectBuilderBox)
			.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
			.FirstOrDefault(x => x.IsGenericMethodDefinition && x.Name == nameof(Box));

		internal static ObjectBuilderBox Create(Type type) =>
			(ObjectBuilderBox)createMethod.MakeGenericMethod(type).Invoke(null, null);

		internal static ObjectBuilderBox Create<T>() =>
			Box<T>(new ObjectBuilder<T>());

		internal static ObjectBuilderBox Box(object objectBuilder, Type builderType) =>
			(ObjectBuilderBox)boxMethod.MakeGenericMethod(builderType).Invoke(null, new object[] { objectBuilder });

		internal static ObjectBuilderBox Box<T>(ObjectBuilder<T> objectBuilder) =>
			new ObjectBuilderBox(objectBuilder)
			{
				getObject = () => objectBuilder.Object,
				isConstructed = () => objectBuilder.IsConstructed,
				isFinished = () => objectBuilder.IsFinished,
				onConstructedGet = () => objectBuilder.OnConstructed,
				onConstructedSet = x => objectBuilder.OnConstructed = x,
				tryFinish = () => objectBuilder.TryFinish()
			};

		internal static ObjectBuilder<T> Unbox<T>(ObjectBuilderBox objectBuilderBox) =>
			objectBuilderBox?.objectBuilder as ObjectBuilder<T>;

		private readonly object objectBuilder;

		private Func<object> getObject;
		private Func<bool> isConstructed;
		private Func<bool> isFinished;

		private Func<Action> onConstructedGet;
		private Action<Action> onConstructedSet;

		private Func<bool> tryFinish;

		private ObjectBuilderBox(object objectBuilder) => this.objectBuilder = objectBuilder;

		internal object Object => getObject();

		public bool IsConstructed => isConstructed();

		internal bool IsFinished => isFinished();

		internal Action OnConstructed
		{
			get => onConstructedGet();
			set => onConstructedSet(value);
		}

		internal bool TryFinish() => tryFinish();
	}
}
