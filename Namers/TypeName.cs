using System;
using System.Text;

namespace XMachine.Namers
{
	internal class TypeName
	{
		private readonly string stringValue;

		internal TypeName(CharEnumerator chars)
		{
			if (chars == null)
			{
				throw new ArgumentException("Cannot parse null characters.");
			}

			Definition = new TypeDefinition(chars);

			if (!Definition.PrecedesGeneric)
			{
				stringValue = Definition.ToString();
			}
			else
			{
				char c;
				try
				{
					c = chars.Current;
				}
				catch (Exception e)
				{
					throw new InvalidOperationException(
						$"Generic arguments not opened: unexpected end of sequence after {Definition}.", e);
				}

				if (c != TypeNameLexer.BraceGeneric)
				{
					throw new InvalidOperationException(
						$"Generic arguments improperly opened: unexpected character '{chars.Current}' after {Definition}.");
				}

				if (!chars.MoveNext())
				{
					throw new InvalidOperationException(
						$"No content for generic arguments: unexpected end of sequence after '{Definition}.-'");
				}

				GenericArguments = new TypeArgSequence(chars);

				try
				{
					c = chars.Current;
				}
				catch (Exception e)
				{
					throw new InvalidOperationException(
						$"Generic arguments not closed: unexpected end of sequence after '{Definition}.-{GenericArguments}'", e);
				}

				if (c != TypeNameLexer.BraceGeneric)
				{
					throw new InvalidOperationException(
						$"Generic arguments improperly closed: unexpected character '{c}' after '{Definition}.-{GenericArguments}'");
				}
				chars.MoveNext();

				StringBuilder sb = new StringBuilder();
				sb.Append(Definition);
				if (IsGeneric)
				{
					sb.Append(TypeNameLexer.BeginGeneric)
						.Append(TypeNameLexer.BraceGeneric)
						.Append(GenericArguments)
						.Append(TypeNameLexer.BraceGeneric);
				}
				stringValue = sb.ToString();
			}
		}

		internal int Length => stringValue.Length;

		internal TypeDefinition Definition { get; }

		internal bool IsGeneric => GenericArguments != null;

		internal TypeArgSequence GenericArguments { get; }

		public override string ToString() => stringValue;
	}
}
