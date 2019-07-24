using System;
using System.Collections.Generic;

namespace XMachine.Namers
{
	internal class TypeArgSequence
	{
		private readonly string stringValue;

		internal TypeArgSequence(CharEnumerator chars)
		{
			if (chars == null)
			{
				throw new ArgumentException("Cannot parse null characters.");
			}

			List<TypeName> typeNames = new List<TypeName>();

			while (true)
			{
				TypeName tn = new TypeName(chars);
				typeNames.Add(tn);

				if (chars.Current == TypeNameLexer.JoinSequence)
				{
					if (!chars.MoveNext())
					{
						throw new InvalidOperationException(
							$"Unexpected end of sequence at '{string.Join(TypeNameLexer.JoinSequence.ToString(), typeNames)}_'");
					}

					// More types in the sequence
					continue;
				}
				else if (chars.Current == TypeNameLexer.BraceGeneric)
				{
					// Finished parsing this set of args
					break;
				}
				else
				{
					throw new InvalidOperationException(
						$"Unexpected character '{chars.Current}' in argument sequence, at " +
						$"'{string.Join(TypeNameLexer.JoinSequence.ToString(), typeNames)}{chars.Current}'");
				}
			}

			TypeNames = typeNames;
			Count = typeNames.Count;

			stringValue = string.Join(TypeNameLexer.JoinSequence.ToString(), TypeNames);
		}

		internal int Count { get; }

		internal int Length => stringValue.Length;

		internal IEnumerable<TypeName> TypeNames { get; }

		public override string ToString() => stringValue;
	}
}
