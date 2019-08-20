using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using XMachine.Components.Identifiers;

namespace XMachine
{
	/// <summary>
	/// A static class providing utility methods for reading and writing to XML. Most of <see cref="XMachine"/>'s
	/// functionality can be accessed through this class.
	/// </summary>
	public static class XmlTools
	{
		#region Constants

		/// <summary>
		/// The default exception handler for <see cref="IExceptionHandler"/> implementations. Throws all generated exceptions.
		/// </summary>
		public static readonly Action<Exception> ThrowHandler = e => throw e;

		/// <summary>
		/// An instance of <see cref="object"/> used to indicate an unread object during some components' read operations.
		/// </summary>
		public static readonly object PlaceholderObject = new object();

		/// <summary>
		/// A regular expression pattern that matches the XML reserved characters: &lt;&gt;&quot;&apos;&amp;
		/// </summary>
		public static readonly Regex XmlReservedCharsPattern = new Regex(@"[<>""'&]");

		/// <summary>
		/// Culture-invariant number styles for XML: optional leading sign for negative numbers and periods for decimals.
		/// </summary>
		public static readonly NumberStyles NumberStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

		/// <summary>
		/// Culture-invariant default date/time styles for XML.
		/// </summary>
		public static readonly DateTimeStyles DateTimeStyles = DateTimeStyles.None;

		/// <summary>
		/// The invariant culture object, used so that serialized data can be portable across systems.
		/// </summary>
		public static readonly IFormatProvider InvariantCulture = CultureInfo.InvariantCulture;

		private static readonly char[] xmlReservedChars = new char[] { '<', '>', '"', '\'', '&' };

		private static readonly XCollator defaultCollator = new XCollator();

		#endregion

		#region File I/O

		/// <summary>
		/// Reads the given XML files.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the root elements of the files.</returns>
		public static IEnumerable<XElement> ReadFiles(params string[] files) =>
			defaultCollator.ReadFiles(files);

		/// <summary>
		/// Reads the given XML files.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the root elements of the files.</returns>
		public static IEnumerable<XElement> ReadFiles(IEnumerable<string> files) =>
			defaultCollator.ReadFiles(files);

		/// <summary>
		/// Reads the given XML files.
		/// </summary>
		/// <param name="streams">Streams containing readable XML.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the root elements of the files.</returns>
		public static IEnumerable<XElement> ReadFiles(params Stream[] streams) =>
			defaultCollator.ReadFiles(streams);

		/// <summary>
		/// Reads the given XML files.
		/// </summary>
		/// <param name="streams">Streams containing readable XML.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the root elements of the files.</returns>
		public static IEnumerable<XElement> ReadFiles(IEnumerable<Stream> streams) =>
			defaultCollator.ReadFiles(streams);

		/// <summary>
		/// Reads the given XML file.
		/// </summary>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>The root <see cref="XElement"/> of the document.</returns>
		public static XElement ReadFile(string file) => defaultCollator.ReadFile(file);

		/// <summary>
		/// Reads XML from the given <see cref="Stream"/> and returns its root element.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <returns>The root <see cref="XElement"/> of the document.</returns>
		public static XElement ReadFile(Stream stream) => defaultCollator.ReadFile(stream);

		/// <summary>
		/// Reads XML from the given <see cref="XmlReader"/> and returns the root element.
		/// </summary>
		/// <param name="xmlReader">The <see cref="XmlReader"/> to read from.</param>
		/// <returns>The root <see cref="XElement"/> of the document.</returns>
		public static XElement ReadFile(XmlReader xmlReader) => defaultCollator.ReadFile(xmlReader);

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given file.
		/// </summary>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="root">The root <see cref="XElement"/> of the XML tree to write.</param>
		public static void WriteFile(string file, XElement root) => defaultCollator.WriteFile(file, root);

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="root">The root <see cref="XElement"/> of the XML tree to write.</param>
		public static void WriteFile(Stream stream, XElement root) => defaultCollator.WriteFile(stream, root);

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given <see cref="XmlWriter"/>.
		/// </summary>
		/// <param name="xmlWriter">The <see cref="XmlWriter"/> to write to.</param>
		/// <param name="root">The root <see cref="XElement"/> of the XML tree to write.</param>
		public static void WriteFile(XmlWriter xmlWriter, XElement root) => defaultCollator.WriteFile(xmlWriter, root);

		#endregion

		#region Assembly management

		private static readonly IDictionary<Assembly, XMachineAssemblyAttribute> virtualAssemblyAttributes =
			new Dictionary<Assembly, XMachineAssemblyAttribute>
			{
				{
					typeof(object).Assembly,
					new XMachineAssemblyAttribute
					{
						ConstructorAccess = MethodAccess.Public,
						PropertyAccess = MemberAccess.PublicOnly
					}
				},
				{
					typeof(XmlTools).Assembly,
					new XMachineAssemblyAttribute
					{
						ConstructorAccess = MethodAccess.Public,
						PropertyAccess = MemberAccess.PublicOnly
					}
				}
			};

		private static bool scanUnknownAssemblies = false;

		/// <summary>
		/// Get or set a value that determines whether unknown assemblies are scanned. This is
		/// initialized to false, meaning types are non-serializable unless they are in mscorlib or in an
		/// assembly tagged with <see cref="XMachineAssemblyAttribute"/>. Set this property to true to
		/// scan all assemblies in the current application domain, allowing all public types to be
		/// serialized.
		/// </summary>
		public static bool ScanUnknownAssemblies
		{
			get => scanUnknownAssemblies;
			set
			{
				if (ScanUnknownAssemblies != value)
				{
					scanUnknownAssemblies = value;
					XComponents.ResetAllXDomains();
				}
			}
		}

		/// <summary>
		/// Get the value of <see cref="XMachineAssemblyAttribute"/> on the given <see cref="Assembly"/>.
		/// </summary>
		/// <param name="assembly">The assembly to check.</param>
		/// <returns>An <see cref="XMachineAssemblyAttribute"/> instance, or <c>null</c> if none has been set.</returns>
		public static XMachineAssemblyAttribute GetXMachineAssemblyAttribute(Assembly assembly) =>
			virtualAssemblyAttributes.TryGetValue(assembly, out XMachineAssemblyAttribute attr) ? attr : null;

		/// <summary>
		/// Tag an assembly at runtime with <see cref="XMachineAssemblyAttribute"/>, allowing its <see cref="Type"/>s to 
		/// be scanned and setting its default <see cref="MemberAccess"/> and <see cref="MethodAccess"/> levels.
		/// </summary>
		/// <param name="assembly">The assembly to be tagged.</param>
		/// <param name="attribute">The <see cref="XMachineAssemblyAttribute"/> instance to add.</param>
		public static void SetXMachineAssemblyAttribute(Assembly assembly, XMachineAssemblyAttribute attribute)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException(nameof(assembly));
			}
			if (attribute == null)
			{
				throw new ArgumentNullException(nameof(attribute));
			}
			if (virtualAssemblyAttributes.ContainsKey(assembly))
			{
				virtualAssemblyAttributes[assembly] = attribute;
			}
			else
			{
				virtualAssemblyAttributes.Add(assembly, attribute);
			}
		}

		#endregion

		#region Utility methods

		/// <summary>
		/// Get the concatenated string value of the direct child <see cref="XText"/> nodes of the given
		/// <see cref="XElement"/>. Unlike <see cref="XElement.Value"/>, this method is not recursive.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to get text from.</param>
		/// <returns>A <see cref="string"/> containing direct text contents.</returns>
		public static string GetElementText(XElement element)
		{
			StringBuilder sb = new StringBuilder();

			foreach (XNode node in element.Nodes())
			{
				if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
				{
					_ = sb.Append(((XText)node).Value);
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Searches the direct members of an <see cref="XElement"/> for an attribute, then an element, with 
		/// the given <see cref="XName"/>. If found, its text content is returned; if not, <c>null</c>.
		/// </summary>
		public static string GetAttributeOrElementValue(this XElement element, XName xName)
		{
			XAttribute attribute = element.Attribute(xName);
			return attribute != null ? attribute.Value : element.Element(xName)?.Value;
		}

		/// <summary>
		/// Check <paramref name="member"/> for name overrides assigned by <see cref="XNameAttribute"/>,
		/// <see cref="XmlTypeAttribute"/> or <see cref="XmlAttributeAttribute"/>.
		/// </summary>
		/// <param name="member">An instance of <see cref="MemberInfo"/>.</param>
		/// <returns>A <see cref="string"/> containing the override name, or <c>null</c> if <paramref name="member"/> has
		/// none of the override attributes.</returns>
		public static string GetXmlNameFromAttributes(this MemberInfo member)
		{
			XNameAttribute xna = member.GetCustomAttribute<XNameAttribute>();
			if (xna != null)
			{
				return xna.Name;
			}

			XmlTypeAttribute xta = member.GetCustomAttribute<XmlTypeAttribute>();
			if (xta != null && !string.IsNullOrEmpty(xta.TypeName))
			{
				return xta.TypeName;
			}

			XmlAttributeAttribute xaa = member.GetCustomAttribute<XmlAttributeAttribute>();
			if (xaa != null && !string.IsNullOrEmpty(xaa.AttributeName))
			{
				return xaa.AttributeName;
			}

			return null;
		}

		#endregion

		#region Primitive reader/writers

		/// <summary>
		/// Read the inner XML of the given <see cref="XElement"/> as a <see cref="string"/>.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to read</param>.
		/// <returns>A <see cref="string"/> containing all text and markup within <paramref name="element"/>. Does not include
		/// the enclosing element tags of <paramref name="element"/> itself. (For that, use XElement.ToString()).</returns>
		public static string ReadInnerXml(XElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}
			using (XmlReader reader = element.CreateReader())
			{
				_ = reader.MoveToContent();
				return reader.ReadInnerXml();
			}
		}

		/// <summary>
		/// Write a <see cref="string"/> to the given <see cref="XElement"/> as raw inner XML.
		/// </summary>
		/// <param name="element">The <see cref="XElement"/> to write to.</param>
		/// <param name="xml">A string containing the text and XML markup to write.</param>
		public static void WriteInnerXml(XElement element, string xml)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}
			using (XmlWriter writer = element.CreateWriter())
			{
				writer.WriteRaw(xml);
			}
		}

		/// <summary>
		/// Attempts to read a <see cref="bool"/> from text, returning <c>false</c> if unsuccessful. Performs
		/// a case-insensitive, culture-invariant comparison against <see cref="bool.TrueString"/>.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <returns>A <see cref="bool"/>.</returns>
		public static bool ReadBool(string text) =>
			string.Compare(text, bool.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0;

		/// <summary>
		/// Attempts to read a <see cref="bool"/> from text, returning <c>false</c> if unsuccessful. Performs
		/// a case-insensitive, culture-invariant comparison against <see cref="bool.TrueString"/>, plus any 
		/// of the monikers you provide. If any match, this method returns true.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="trueMonikers">A collection of <see cref="string"/> objects that will be interpreted as true.</param>
		/// <returns>A <see cref="bool"/>.</returns>
		public static bool ReadBool(string text, params string[] trueMonikers)
		{
			if (!ReadBool(text) && trueMonikers != null && trueMonikers.Length > 0)
			{
				for (int i = 0; i < trueMonikers.Length; i++)
				{
					if (string.Compare(text, trueMonikers[i], StringComparison.InvariantCultureIgnoreCase) == 0)
					{
						return true;
					}
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Attempts to read a <see cref="byte"/> from text, returning <c>0</c> if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="byte"/>.</returns>
		public static byte ReadByte(string text, byte @default = default) => byte.TryParse(text, out byte val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="char"/> from text, returning a null character (\0) if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="char"/>.</returns>
		public static char ReadChar(string text, char @default = default) => char.TryParse(text, out char val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="DateTime"/> from text, returning <see cref="DateTime.MinValue"/> 
		/// if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="DateTime"/>.</returns>
		public static DateTime ReadDateTime(string text, DateTime @default = default) =>
			DateTime.TryParse(text, InvariantCulture, DateTimeStyles, out DateTime val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="decimal"/> from text, returning zero (0M) if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="decimal"/>.</returns>
		public static decimal ReadDecimal(string text, decimal @default = default) =>
			decimal.TryParse(text, NumberStyles, InvariantCulture, out decimal val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="double"/> from text, returning zero (0.0D) if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="double"/>.</returns>
		public static double ReadDouble(string text, double @default = default) =>
			double.TryParse(text, NumberStyles, InvariantCulture, out double val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="short"/> from text, returning 0 if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="short"/>.</returns>
		public static short ReadShort(string text, short @default = default) =>
			short.TryParse(text, NumberStyles, InvariantCulture, out short val) ? val : @default;

		/// <summary>
		/// Attempts to read an <see cref="int"/> from text, returning 0 if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="int"/>.</returns>
		public static int ReadInt(string text, int @default = default) =>
			int.TryParse(text, NumberStyles, InvariantCulture, out int val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="long"/> from text, returning 0L if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="long"/>.</returns>
		public static long ReadLong(string text, long @default = default) =>
			long.TryParse(text, NumberStyles, InvariantCulture, out long val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="sbyte"/> from text, returning 0 if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="sbyte"/>.</returns>
		public static sbyte ReadSByte(string text, sbyte @default = default) =>
			sbyte.TryParse(text, NumberStyles, InvariantCulture, out sbyte val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="float"/> from text, returning 0.0F if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="float"/>.</returns>
		public static float ReadFloat(string text, float @default = default) =>
			float.TryParse(text, NumberStyles, InvariantCulture, out float val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="ushort"/> from text, returning 0 if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="ushort"/>.</returns>
		public static ushort ReadUShort(string text, ushort @default = default) =>
			ushort.TryParse(text, NumberStyles, InvariantCulture, out ushort val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="uint"/> from text, returning 0 if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="uint"/>.</returns>
		public static uint ReadUInt(string text, uint @default = default) =>
			uint.TryParse(text, NumberStyles, InvariantCulture, out uint val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="ulong"/> from text, returning 0 if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="ulong"/>.</returns>
		public static ulong ReadULong(string text, ulong @default = default) =>
			ulong.TryParse(text, NumberStyles, InvariantCulture, out ulong val) ? val : @default;

		/// <summary>
		/// Attempts to read a member of the enum <typeparamref name="T"/> from text, returning the default
		/// member if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <typeparamref name="T"/> member.</returns>
		public static T ReadEnum<T>(string text, T @default = default) where T : Enum =>
			Enum.Parse(typeof(T), text) is T tobj ? tobj : @default;

		/// <summary>
		/// Attempts to read a <see cref="Version"/> from text, returning <c>null</c> if unavailable.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="default">An optional default value to return if the text cannot be parsed.</param>
		/// <returns>A <see cref="Version"/>.</returns>
		public static Version ReadVersion(string text, Version @default = default) =>
			Version.TryParse(text, out Version val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="Guid"/> from text, returning <see cref="Guid.Empty"/> if unsuccessful.
		/// </summary>
		/// <param name="text">The text to read from.</param>
		/// <param name="format">The format string to be used by <see cref="Guid.TryParseExact(string, string, out Guid)"/>,
		/// default 'N'.</param>
		/// <returns>A <see cref="Guid"/>.</returns>
		public static Guid ReadGuid(string text, string format = "N") =>
			Guid.TryParseExact(text, format, out Guid val) ? val : Guid.Empty;

		/// <summary>
		/// Returns an <see cref="XText"/> node containing the given string. The string is checked for reserved XML 
		/// characters, and if any are found, the returned <see cref="XText"/> is an <see cref="XCData"/>.
		/// </summary>
		/// <param name="text">The text to be written.</param>
		/// <returns>An <see cref="XText"/> instance.</returns>
		public static XText WriteText(string text) =>
			text.IndexOfAny(xmlReservedChars) >= 0 ? new XCData(text) : new XText(text);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="bool"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(bool obj) => obj ? bool.TrueString : bool.FalseString;

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="byte"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(byte obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="DateTime"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(DateTime obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="decimal"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(decimal obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="double"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(double obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="short"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(short obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(int obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="long"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(long obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="sbyte"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(sbyte obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="float"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(float obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="ushort"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(ushort obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="uint"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(uint obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="ulong"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(ulong obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string.
		/// </summary>
		/// <param name="obj">The <see cref="Version"/> to be written.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(Version obj) => obj.ToString();

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		/// <param name="obj">The <see cref="Guid"/> to be written.</param>
		/// <param name="format">The format string to be used by <see cref="Guid.ToString(string, IFormatProvider)"/>,
		/// default 'N'.</param>
		/// <returns>A <see cref="string"/>.</returns>
		public static string Write(Guid obj, string format = "N") => obj.ToString(format, InvariantCulture);

		#endregion

		#region Other static methods

		/// <summary>
		/// Retrieve the collection of global identifiers used by the the <see cref="XIdentifiers"/> component, if it exists.
		/// </summary>
		public static XCompositeIdentifier Identifiers() => XComponents.Component<XIdentifiers>()?.Identifier;

		/// <summary>
		/// Add an <see cref="XIdentifier{TType, TId}"/> characterized by the given delegate to the global identifiers 
		/// used by the <see cref="XIdentifiers"/> component, if it exists.
		/// </summary>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> defining equality between <typeparamref name="TId"/>s.</param>
		public static void Identify<TType, TId>(Func<TType, TId> identifier, IEqualityComparer<TId> keyComparer = null)
			where TType : class =>
			XComponents.Component<XIdentifiers>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));

		#endregion

		#region Static versions of XDomain extension methods

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object.
		/// </summary>
		/// <param name="element">An <see cref="XElement"/> containing a serialized object.</param>
		/// <returns>The deserialized object.</returns>
		public static object Read(XElement element) =>
			XExtensionMethods.Read(XDomain.Global, element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		/// <param name="element">An <see cref="XElement"/> containing a serialized object.</param>
		/// <returns>The deserialized object.</returns>
		public static T Read<T>(XElement element) =>
			XExtensionMethods.Read<T>(XDomain.Global, element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects.
		/// </summary>
		/// <param name="elements">An <see cref="IEnumerable{XElement}"/> that contain serialized objects.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> containing the deserialized objects.</returns>
		public static IEnumerable<object> ReadAll(IEnumerable<XElement> elements) =>
			XExtensionMethods.ReadAll(XDomain.Global, elements);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		/// <param name="elements">An <see cref="IEnumerable{XElement}"/> that contain serialized objects.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> containing the deserialized objects.</returns>
		public static IEnumerable<T> ReadAll<T>(IEnumerable<XElement> elements) =>
			XExtensionMethods.ReadAll<T>(XDomain.Global, elements);

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>A deserialized <see cref="object"/>.</returns>
		public static object ReadFrom(string file) =>
			XExtensionMethods.ReadFrom(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the root element of the given stream as an object.
		/// </summary>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>A deserialized <see cref="object"/>.</returns>
		public static object ReadFrom(Stream stream) =>
			XExtensionMethods.ReadFrom(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(params string[] files) =>
			XExtensionMethods.ReadFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		/// <param name="domain">The current <see cref="XDomain"/>.</param>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(this XDomain domain, IEnumerable<string> files) =>
			XExtensionMethods.ReadFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given streams as objects.
		/// </summary>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> over the deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadFrom(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadFrom(XDomain.Global, streams);

		/// <summary>
		/// Attempts to read the root element of the given file as an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>A deserialized <typeparamref name="T"/>.</returns>
		public static T ReadFrom<T>(string file) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the root element of the given stream as an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>A deserialized <typeparamref name="T"/>.</returns>
		public static T ReadFrom<T>(Stream stream) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(params string[] files) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(IEnumerable<string> files) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given streams as objects of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> over the deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadFrom<T>(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(string file) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given stream as a collection of objects.
		/// </summary>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(Stream stream) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(params string[] files) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(IEnumerable<string> files) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given streams as a collection of objects.
		/// </summary>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{Object}"/> of deserialized <see cref="object"/>s.</returns>
		public static IEnumerable<object> ReadElementsFrom(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="file">The path of the file to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(string file) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="stream">A stream containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(Stream stream) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(params string[] files) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="files">The paths of the files to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(IEnumerable<string> files) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given streams as a collection of objects of type
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <param name="streams">Streams containing XML to read.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of deserialized <typeparamref name="T"/>s.</returns>
		public static IEnumerable<T> ReadElementsFrom<T>(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, streams);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="identifier">The <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith<TType, TId>(XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class =>
			XExtensionMethods.ReadWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> defining equality between <typeparamref name="TId"/>s.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith<TType, TId>(Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class =>
			XExtensionMethods.ReadWith(XDomain.Global, identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>.
		/// </summary>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith<TType, TId>(Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class =>
			XExtensionMethods.ReadWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new <see cref="XReader"/>.
		/// </summary>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XReader"/>.</returns>
		public static XReader ReadWith(IEnumerable contextObjects) =>
			XExtensionMethods.ReadWith(XDomain.Global, contextObjects);

		/// <summary>
		/// Write the given object as an <see cref="XElement"/>.
		/// </summary>
		/// <param name="obj">The object to be written.</param>
		/// <returns>An <see cref="XElement"/> containing the serialized <paramref name="obj"/>.</returns>
		public static XElement Write(object obj) =>
			XExtensionMethods.Write(XDomain.Global, obj);

		/// <summary>
		/// Write the given collection of objects as a collection of <see cref="XElement"/>s.
		/// </summary>
		/// <param name="objects">The objects to be written.</param>
		/// <returns>An <see cref="IEnumerable{XElement}"/> containing the serialized <paramref name="objects"/>.</returns>
		public static IEnumerable<XElement> WriteAll(IEnumerable objects) =>
			XExtensionMethods.WriteAll(XDomain.Global, objects);

		/// <summary>
		/// Attempts to write the given object as the root element of the given file.
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		public static void WriteTo(object obj, string file) =>
			XExtensionMethods.WriteTo(XDomain.Global, obj, file);

		/// <summary>
		/// Attempts to write the given object as the root element of the given file.
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		public static void WriteTo(object obj, Stream stream) =>
			XExtensionMethods.WriteTo(XDomain.Global, obj, stream);

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="rootElement">The <see cref="XName"/> of the file's root element. If null, 'xml' is used.</param>
		public static void WriteToElements(IEnumerable objects, string file, XName rootElement = null) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, file, rootElement);

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="rootElement">The <see cref="XName"/> of the file's root element. If null, 'xml' is used.</param>
		public static void WriteToElements(IEnumerable objects, Stream stream, XName rootElement = null) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, stream, rootElement);

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="file">The path of the file to write to.</param>
		/// <param name="rootElement">The root <see cref="XElement"/> of the file.</param>
		public static void WriteToElements(IEnumerable objects, string file, XElement rootElement) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, file, rootElement);

		/// <summary>
		/// Attempts to write a collection of objects to file as the child elements of the root element.
		/// </summary>
		/// <param name="objects">The objects to serialize.</param>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="rootElement">The root <see cref="XElement"/> of the file.</param>
		public static void WriteToElements(IEnumerable objects, Stream stream, XElement rootElement) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, stream, rootElement);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new
		/// instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="identifier">The <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith<TType, TId>(XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class =>
			XExtensionMethods.WriteWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new
		/// instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> defining equality between <typeparamref name="TId"/>s.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith<TType, TId>(Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class =>
			XExtensionMethods.WriteWith(XDomain.Global, identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="identifier">A delegate defining the <see cref="XIdentifier{TType, TId}"/> to add.</param>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith<TType, TId>(Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class =>
			XExtensionMethods.WriteWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new instance of <see cref="XWriter"/>.
		/// </summary>
		/// <param name="contextObjects">An <see cref="IEnumerable"/> of contextual objects.</param>
		/// <returns>A new instance of <see cref="XWriter"/>.</returns>
		public static XWriter WriteWith(IEnumerable contextObjects) =>
			XExtensionMethods.WriteWith(XDomain.Global, contextObjects);

		#endregion
	}
}