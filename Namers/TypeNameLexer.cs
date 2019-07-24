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

		internal static TypeName Parse(XName xName) => new TypeName(xName.ToString().GetEnumerator());

		internal static TypeName Parse(string name) => new TypeName(name.GetEnumerator());
	}
}
