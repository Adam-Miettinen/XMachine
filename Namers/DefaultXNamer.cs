using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace XMachine.Namers
{
	/// <summary>
	/// An implementation of <see cref="XNamer"/> that generates <see cref="XName"/>s for <see cref="Type"/>s
	/// based on type names.
	/// </summary>
	public sealed class DefaultXNamer : AbstractXNamer
	{
		private const string arrayName = "Array";

		private static readonly Regex arrayPattern = new Regex(string.Concat("^", arrayName, "([0-9]+)"));

		/// <summary>
		/// Whether this namer includes the declaring type/namespace/assembly in <see cref="XName"/>s.
		/// </summary>
		public readonly bool IncludesDeclaring, IncludesNamespace, IncludesAssembly;

		/// <summary>
		/// Create a new <see cref="DefaultXNamer"/>.
		/// </summary>
		public DefaultXNamer(bool includeDeclaring = false, bool includeNamespace = false, bool includeAssembly = false)
		{
			IncludesDeclaring = includeDeclaring;
			IncludesNamespace = includeNamespace;
			IncludesAssembly = includeAssembly;
		}

		/// <summary>
		/// Create a new <see cref="DefaultXNamer"/>.
		/// </summary>
		public DefaultXNamer(IDictionary<Type, XName> nameOverrides, bool includeDeclaring = false, bool includeNamespace = false, 
			bool includeAssembly = false)
		{
			IncludesDeclaring = includeDeclaring;
			IncludesNamespace = includeNamespace;
			IncludesAssembly = includeAssembly;

			if (nameOverrides != null)
			{
				foreach (KeyValuePair<Type, XName> kv in nameOverrides)
				{
					this[kv.Key] = kv.Value;
				}
			}
		}

		/// <summary>
		/// Names the given type.
		/// </summary>
		protected override string GetName(Type type)
		{
			string name;

			// Check name from attributes

			if (!string.IsNullOrEmpty(name = type.GetXmlNameFromAttributes()) &&
				!string.IsNullOrEmpty(name = XmlConvert.EncodeLocalName(name)))
			{
				return name;
			}

			// If none or invalid, check name from our own method, BuildName(Type)

			if (!string.IsNullOrEmpty(name = BuildName(type)) &&
				!string.IsNullOrEmpty(name = XmlConvert.EncodeLocalName(name)))
			{
				return name;
			}

			return null;
		}

		/// <summary>
		/// Attempt to parse an XName backward to a Type.
		/// </summary>
		protected override Type ParseXName(XName xName) => ParseTypeName(TypeNameLexer.Parse(xName));

		/// <summary>
		/// Ignores collisions.
		/// </summary>
		protected override Type ResolveCollision(XName xName, Type type1, Type type2) => type1;

		private Type ParseTypeName(TypeName typeName)
		{
			string defName = typeName.Definition.ToString();

			// Check array

			Match arrayMatch = arrayPattern.Match(defName);
			if (arrayMatch.Success)
			{
				return ParseTypeName(typeName.GenericArguments.TypeNames.First())
					?.MakeArrayType(int.Parse(arrayMatch.Groups[1].Value));
			}

			// Check other type definitions

			Type typeDef = this[typeName.Definition.ToString()];
			if (typeDef == null)
			{
				return null;
			}

			// Make generic Type

			Type[] genericTypeArgs = typeName.GenericArguments.TypeNames
				.Select(x => ParseTypeName(typeName)).ToArray();

			return genericTypeArgs.All(x => x != null)
				? typeDef.MakeGenericType(genericTypeArgs)
				: null;
		}

		private string BuildName(Type type)
		{
			string specialName = type.GetXmlNameFromAttributes();
			if (specialName != null)
			{
				return specialName;
			}

			// Special handling for arrays

			if (type.IsArray)
			{
				return string.Concat(arrayName, type.GetArrayRank(), TypeNameLexer.JoinDefinitionParts,
					TypeNameLexer.BraceGeneric, BuildName(GetEssenceType(type)), TypeNameLexer.BraceGeneric);
			}

			// Prepend with assembly / namespace

			StringBuilder sb = new StringBuilder(64);

			if (IncludesAssembly)
			{
				_ = sb.Append(type.Assembly.GetName().Name).Append(TypeNameLexer.JoinDefinitionParts);
			}
			if (IncludesNamespace)
			{
				_ = sb.Append(type.Namespace).Append(TypeNameLexer.JoinDefinitionParts);
			}
			if (IncludesDeclaring)
			{
				Type declaring = type;

				while ((declaring = declaring.DeclaringType) != null)
				{
					_ = sb.Append(declaring.Name.Split('`')[0]).Append(TypeNameLexer.JoinDefinitionParts);
				}
			}

			// End with the local name

			if (type.IsGenericType)
			{
				_ = sb.Append(type.Name.Split('`')[0])
					.Append(TypeNameLexer.BeginGeneric).Append(TypeNameLexer.BraceGeneric);

				Type[] args = type.GenericTypeArguments;
				for (int i = 0; i < args.Length; i++)
				{
					_ = sb.Append(BuildName(args[i]));
					if (i < args.Length - 1)
					{
						_ = sb.Append(TypeNameLexer.JoinSequence);
					}
				}

				_ = sb.Append(TypeNameLexer.BraceGeneric);
			}
			else
			{
				_ = sb.Append(type.Name);
			}

			return sb.ToString();
		}

		private Type GetEssenceType(Type type) => type.GetElementType() ?? type;
	}
}
