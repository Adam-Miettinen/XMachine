using System;
using System.Collections.Generic;
using System.Text;

namespace XMachine.Namers
{
	internal class TypeDefinition
	{
		private readonly string name;

		internal TypeDefinition(CharEnumerator chars)
		{
			if (chars == null)
			{
				throw new ArgumentException("Cannot parse null characters.");
			}

			StringBuilder sb = new StringBuilder();

			do
			{
				char c = chars.Current;

				if (TypeNameLexer.IsDefinitionCharacter(c))
				{
					sb.Append(c);
				}
				else if (TypeNameLexer.IsDefinitionEnd(c))
				{
					if (sb.Length == 0)
					{
						throw new InvalidOperationException(
							$"Could not parse type name, unexpected initial character {c}.");
					}

					if (sb[sb.Length - 1] == TypeNameLexer.BeginGeneric && c == TypeNameLexer.BraceGeneric)
					{
						sb.Remove(sb.Length - 1, 1);
						PrecedesGeneric = true;
					}

					break;
				}
				else
				{
					throw new InvalidOperationException(
						$"Could not parse type name, terminated at: {sb}. Unexpected character {c}.");
				}
			}
			while (chars.MoveNext());

			name = sb.ToString();
		}

		internal bool PrecedesGeneric { get; }

		internal int Length => name.Length;

		internal IEnumerable<string> Parts => name.Split(TypeNameLexer.JoinDefinitionParts);

		public override string ToString() => name;
	}
}
