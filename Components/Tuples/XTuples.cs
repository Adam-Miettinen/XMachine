using System;
using System.Xml.Linq;

namespace XMachine.Components.Tuples
{
	internal static class XTuples
	{
		internal static readonly XName
			Item1 = nameof(Tuple<object>.Item1),
			Item2 = nameof(Tuple<object, object>.Item2),
			Item3 = nameof(Tuple<object, object, object>.Item3),
			Item4 = nameof(Tuple<object, object, object, object>.Item4),
			Item5 = nameof(Tuple<object, object, object, object, object>.Item5),
			Item6 = nameof(Tuple<object, object, object, object, object, object>.Item6),
			Item7 = nameof(Tuple<object, object, object, object, object, object, object>.Item7),
			Rest = nameof(Tuple<object, object, object, object, object, object, object, object>.Rest);
	}
}
