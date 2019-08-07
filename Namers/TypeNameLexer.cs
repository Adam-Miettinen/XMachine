using System;
using System.Xml.Linq;

namespace XMachine.Namers
{
	internal static class TypeNameLexer
	{
		internal const char
			JoinSequence = '_',
			BeginGeneric = '.',
			BraceGeneric = '-',
			JoinDefinitionParts = '.';

		internal static bool IsDefinitionCharacter(char c) => char.IsLetterOrDigit(c) || c == '.';

		internal static bool IsDefinitionEnd(char c) => c == JoinSequence || c == BraceGeneric;

		internal static TypeName Parse(XName xName)
		{
			CharEnumerator enumerator = xName.ToString().GetEnumerator();
			_ = enumerator.MoveNext();
			return new TypeName(enumerator);
		}

		internal static TypeName Parse(string name)
		{
			CharEnumerator enumerator = name.GetEnumerator();
			_ = enumerator.MoveNext();
			return new TypeName(enumerator);
		}
	}
}
