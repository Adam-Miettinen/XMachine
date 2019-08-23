using System;
using System.Xml.Linq;

namespace XMachine.Components.Properties
{
	internal sealed class DelegatedXProperty<TType, TProperty> : XProperty<TType, TProperty>
	{
		private readonly Func<TType, TProperty> get;
		private readonly Action<TType, TProperty> set;

		internal DelegatedXProperty(XName name, Func<TType, TProperty> get, Action<TType, TProperty> set = null)
			: base(name)
		{
			this.get = get;
			this.set = set;
		}

		public override TProperty Get(TType obj) => get == null ? default : get(obj);

		public override void Set(TType obj, TProperty value) => set?.Invoke(obj, value);
	}
}
