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
	/// An implementation of <see cref="CachedXNamer"/> that maps XML to <see cref="Type"/> objects using
	/// <see cref="Type"/> names and the <see cref="XName"/>s of XML elements.
	/// </summary>
	public sealed class DefaultXNamer : CachedXNamer
	{
		private const string arrayName = "Array";

		private static readonly Regex arrayPattern = new Regex(string.Concat("^", arrayName, "([0-9]+)"));

		private static readonly IDictionary<Type, XName> defaultNameOverrides = new Dictionary<Type, XName>
		{
			{ typeof(object), "object" },
			{ typeof(bool), "bool" },
			{ typeof(byte), "byte" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(char), "char" },
			{ typeof(decimal), "decimal" },
			{ typeof(double), "double" },
			{ typeof(float), "float" },
			{ typeof(int), "int" },
			{ typeof(uint), "uint" },
			{ typeof(long), "long" },
			{ typeof(ulong), "ulong" },
			{ typeof(short), "short" },
			{ typeof(ushort), "ushort" },
			{ typeof(string), "string" }
		};

		private readonly bool includesDeclaring, includesNamespace, includesAssembly;

		/// <summary>
		/// Create a new instance of <see cref="DefaultXNamer"/>.
		/// </summary>
		/// <param name="includeDeclaring">If element names of nested types should be prepended with the name of the 
		/// declaring type.</param>
		/// <param name="includeNamespace">If element names should be prepended with types' namespaces.</param>
		/// <param name="includeAssembly">If element names should be prepended with typess assembly names.</param>
		public DefaultXNamer(bool includeDeclaring = false, bool includeNamespace = false, bool includeAssembly = false) :
			this(null, includeDeclaring, includeNamespace, includeAssembly)
		{ }

		/// <summary>
		/// Create a new instance of <see cref="DefaultXNamer"/>.
		/// </summary>
		/// <param name="nameOverrides">An <see cref="IDictionary{TKey, TValue}"/> of <see cref="Type"/>-<see cref="XName"/>
		/// mappings that should override the defaults.</param>
		/// <param name="includeDeclaring">If element names of nested types should be prepended with the name of the 
		/// declaring type.</param>
		/// <param name="includeNamespace">If element names should be prepended with types' namespaces.</param>
		/// <param name="includeAssembly">If element names should be prepended with typess assembly names.</param>
		public DefaultXNamer(IDictionary<Type, XName> nameOverrides, bool includeDeclaring = false, bool includeNamespace = false,
			bool includeAssembly = false)
		{
			includesDeclaring = includeDeclaring;
			includesNamespace = includeNamespace;
			includesAssembly = includeAssembly;

			foreach (KeyValuePair<Type, XName> kv in defaultNameOverrides)
			{
				this[kv.Key] = kv.Value;
			}

			if (nameOverrides != null)
			{
				foreach (KeyValuePair<Type, XName> kv in nameOverrides)
				{
					this[kv.Key] = kv.Value;
				}
			}
		}

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

		protected override Type ParseXName(XName xName) => ParseTypeName(TypeNameLexer.Parse(xName));

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
			// Special handling for arrays

			if (type.IsArray)
			{
				return string.Concat(arrayName, type.GetArrayRank(), TypeNameLexer.JoinDefinitionParts,
					TypeNameLexer.BraceGeneric, this[type.GetElementType()], TypeNameLexer.BraceGeneric);
			}

			// Prepend with assembly / namespace

			StringBuilder sb = new StringBuilder(64);

			if (includesAssembly)
			{
				_ = sb.Append(type.Assembly.GetName().Name).Append(TypeNameLexer.JoinDefinitionParts);
			}
			if (includesNamespace)
			{
				_ = sb.Append(type.Namespace).Append(TypeNameLexer.JoinDefinitionParts);
			}
			if (includesDeclaring)
			{
				Type declaring = type;

				while ((declaring = declaring.DeclaringType) != null)
				{
					string declaringName = declaring.Name.Split('`')[0];
					_ = sb.Append(declaringName).Append(TypeNameLexer.JoinDefinitionParts);
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
					_ = sb.Append(this[args[i]]);
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
	}
}
