using System;

namespace XMachine.Components.Properties
{
	internal class XPropertyArgs : XObjectArgs
	{
		private readonly Func<Tuple<object, bool>> getter;

		internal XPropertyArgs(XObjectArgs args, Func<Tuple<object, bool>> getter) :
			base(args == null ? default : args.Hints) =>
			this.getter = getter ?? throw new ArgumentNullException(nameof(getter));

		internal bool GetFromOwner(out object obj)
		{
			Tuple<object, bool> value = getter();
			obj = value.Item1;
			return value.Item2;
		}
	}
}
