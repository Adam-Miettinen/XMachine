using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
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
	/// Provides utility methods for reading and writing XML.
	/// </summary>
	public static class XmlTools
	{
		#region Constants

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
		/// The invariant culture object, used in XML so that code can be portable across systems.
		/// </summary>
		public static readonly IFormatProvider InvariantCulture = CultureInfo.InvariantCulture;

		private static readonly XCollator defaultCollator = new XCollator();

		/// <summary>
		/// The XML reserved characters: { &lt;, &gt;, &quot;, &apos;, &amp; }
		/// </summary>
		public static char[] XmlReservedChars => new char[] { '<', '>', '"', '\'', '&' };

		#endregion

		#region File I/O

		/// <summary>
		/// Reads each of the given XML files, producing an <see cref="IEnumerable{T}"/> over the root
		/// elements of the files.
		/// </summary>
		public static IEnumerable<XElement> ReadFiles(params string[] files) =>
			defaultCollator.ReadFiles(files);

		/// <summary>
		/// Reads each of the given XML files, producing an <see cref="IEnumerable{T}"/> over the root
		/// elements of the files.
		/// </summary>
		public static IEnumerable<XElement> ReadFiles(IEnumerable<string> files) =>
			defaultCollator.ReadFiles(files);

		/// <summary>
		/// Reads each of the given XML files, producing an <see cref="IEnumerable{T}"/> over the root
		/// elements of the files.
		/// </summary>
		public static IEnumerable<XElement> ReadFiles(params Stream[] streams) =>
			defaultCollator.ReadFiles(streams);

		/// <summary>
		/// Reads each of the given XML files, producing an <see cref="IEnumerable{T}"/> over the root
		/// elements of the files.
		/// </summary>
		public static IEnumerable<XElement> ReadFiles(IEnumerable<Stream> streams) =>
			defaultCollator.ReadFiles(streams);

		/// <summary>
		/// Reads the given XML file and returns its root element.
		/// </summary>
		public static XElement ReadFile(string file) => defaultCollator.ReadFile(file);

		/// <summary>
		/// Reads XML from the given <see cref="Stream"/> and returns its root element.
		/// </summary>
		public static XElement ReadFile(Stream stream) => defaultCollator.ReadFile(stream);

		/// <summary>
		/// Reads XML from the given <see cref="XmlReader"/> and returns the root element.
		/// </summary>
		public static XElement ReadFile(XmlReader xmlReader) => defaultCollator.ReadFile(xmlReader);

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given file.
		/// </summary>
		public static void WriteFile(string file, XElement root) => defaultCollator.WriteFile(file, root);

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given <see cref="Stream"/>.
		/// </summary>
		public static void WriteFile(Stream stream, XElement root) => defaultCollator.WriteFile(stream, root);

		/// <summary>
		/// Writes the given <see cref="XElement"/> to the given <see cref="XmlWriter"/>.
		/// </summary>
		public static void WriteFile(XmlWriter xmlWriter, XElement root) => defaultCollator.WriteFile(xmlWriter, root);

		#endregion

		#region Utility methods

		private static readonly ISet<Assembly> ignoredAssemblies = new HashSet<Assembly>();

		private static bool ignoreAll;

		/// <summary>
		/// Get or set a value that determines whether <see cref="Assembly"/> objects are ignored by default.
		/// Changing this value resets the list of ignored/unignored assemblies.
		/// </summary>
		public static bool IgnoreAll
		{
			get => ignoreAll;
			set
			{
				if (ignoreAll != value)
				{
					ignoredAssemblies.Clear();
					XComponents.ResetStatic();
					ignoreAll = value;
				}
			}
		}

		/// <summary>
		/// Ignore the given <see cref="Assembly"/>, blocking <see cref="XMachine"/> from using it or its types.
		/// </summary>
		public static void Ignore(Assembly assembly) => 
			_ = IgnoreAll ? ignoredAssemblies.Remove(assembly) : ignoredAssemblies.Add(assembly);

		/// <summary>
		/// Unignore the given <see cref="Assembly"/>, allowing <see cref="XMachine"/> to scan it and its types.
		/// </summary>
		public static void UnIgnore(Assembly assembly)
		{
			if (IgnoreAll ? ignoredAssemblies.Add(assembly) : ignoredAssemblies.Remove(assembly))
			{
				XComponents.ScanAssembly(assembly, XComponents.Components());
			}
		}

		/// <summary>
		/// Returns the concatenated string value of the direct child <see cref="XText"/> nodes of the given
		/// <see cref="XElement"/>.
		/// </summary>
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

		internal static bool IsXIgnored(this Assembly assembly) =>
			assembly.IsDynamic ||
			assembly.ReflectionOnly ||
			assembly.GetCustomAttribute<XIgnoreAttribute>() != null ||
			ignoredAssemblies.Contains(assembly);

		internal static bool IsXIgnored(this Type type, bool checkAssembly = false) =>
			(checkAssembly && type.Assembly.IsXIgnored()) ||
			!type.IsPublic ||
			type.GetCustomAttribute<XIgnoreAttribute>() != null ||
			(type != type.DeclaringType && type.DeclaringType?.IsXIgnored() == true) ||
			(!type.IsGenericParameter && type.GenericTypeArguments.Any(x =>
				x != type && x.IsXIgnored(true)));

		internal static bool IsXIgnored(this PropertyInfo property, bool checkType = false, bool checkAssembly = false) =>
			(checkType && property.ReflectedType.IsXIgnored(checkAssembly)) ||
			property.GetCustomAttribute<XIgnoreAttribute>() != null ||
			property.GetCustomAttribute<XmlIgnoreAttribute>() != null ||
			property.PropertyType.IsXIgnored() ||
			property.DeclaringType.IsXIgnored();

		internal static bool IsXIgnored(this ConstructorInfo constructor, bool checkType = false, bool checkAssembly = false) =>
			(checkType && constructor.ReflectedType.IsXIgnored(checkAssembly)) ||
			constructor.GetCustomAttribute<XIgnoreAttribute>() != null;

		internal static bool IsXIgnored(this MethodInfo method, bool checkType = false, bool checkAssembly = false) =>
			(checkType && method.DeclaringType.IsXIgnored(checkAssembly)) ||
			(method.ReturnType != typeof(void) && method.ReturnType.IsXIgnored(true)) ||
			method.GetParameters().Any(x => x.ParameterType.IsXIgnored(true));

		internal static string GetXmlNameFromAttributes(this MemberInfo member)
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
		public static string ReadInnerXml(XElement element)
		{
			using (XmlReader reader = element.CreateReader())
			{
				_ = reader.MoveToContent();
				return reader.ReadInnerXml();
			}
		}

		/// <summary>
		/// Write a <see cref="string"/> to the given <see cref="XElement"/> as raw XML.
		/// </summary>
		public static void WriteInnerXml(XElement element, string xml)
		{
			using (XmlWriter writer = element.CreateWriter())
			{
				writer.WriteRaw(xml);
			}
		}

		/// <summary>
		/// Attempts to read a <see cref="bool"/> from text, returning <c>false</c> if unsuccessful. Performs
		/// a case-insensitive, culture-invariant comparison against <see cref="bool.TrueString"/>.
		/// </summary>
		public static bool ReadBool(string text) =>
			string.Compare(text, bool.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0;

		/// <summary>
		/// Attempts to read a <see cref="bool"/> from text, returning <c>false</c> if unsuccessful. Performs
		/// a case-insensitive, culture-invariant comparison against <see cref="bool.TrueString"/>, plus any 
		/// of the monikers you provide. If any match, this method returns true.
		/// </summary>
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
		public static byte ReadByte(string text, byte @default = default) => byte.TryParse(text, out byte val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="char"/> from text, returning a null character (\0) if unsuccessful.
		/// </summary>
		public static char ReadChar(string text, char @default = default) => char.TryParse(text, out char val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="DateTime"/> from text, returning <see cref="DateTime.MinValue"/> 
		/// if unsuccessful.
		/// </summary>
		public static DateTime ReadDateTime(string text, DateTime @default = default) =>
			DateTime.TryParse(text, InvariantCulture, DateTimeStyles, out DateTime val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="decimal"/> from text, returning zero (0M) if unsuccessful.
		/// </summary>
		public static decimal ReadDecimal(string text, decimal @default = default) =>
			decimal.TryParse(text, NumberStyles, InvariantCulture, out decimal val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="double"/> from text, returning zero (0.0D) if unsuccessful.
		/// </summary>
		public static double ReadDouble(string text, double @default = default) =>
			double.TryParse(text, NumberStyles, InvariantCulture, out double val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="short"/> from text, returning 0 if unsuccessful.
		/// </summary>
		public static short ReadShort(string text, short @default = default) =>
			short.TryParse(text, NumberStyles, InvariantCulture, out short val) ? val : @default;

		/// <summary>
		/// Attempts to read an <see cref="int"/> from text, returning 0 if unsuccessful.
		/// </summary>
		public static int ReadInt(string text, int @default = default) =>
			int.TryParse(text, NumberStyles, InvariantCulture, out int val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="long"/> from text, returning 0L if unsuccessful.
		/// </summary>
		public static long ReadLong(string text, long @default = default) =>
			long.TryParse(text, NumberStyles, InvariantCulture, out long val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="sbyte"/> from text, returning 0 if unsuccessful.
		/// </summary>
		public static sbyte ReadSByte(string text, sbyte @default = default) =>
			sbyte.TryParse(text, NumberStyles, InvariantCulture, out sbyte val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="float"/> from text, returning 0.0F if unsuccessful.
		/// </summary>
		public static float ReadFloat(string text, float @default = default) =>
			float.TryParse(text, NumberStyles, InvariantCulture, out float val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="ushort"/> from text, returning 0 if unsuccessful.
		/// </summary>
		public static ushort ReadUShort(string text, ushort @default = default) =>
			ushort.TryParse(text, NumberStyles, InvariantCulture, out ushort val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="uint"/> from text, returning 0 if unsuccessful.
		/// </summary>
		public static uint ReadUInt(string text, uint @default = default) =>
			uint.TryParse(text, NumberStyles, InvariantCulture, out uint val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="ulong"/> from text, returning 0 if unsuccessful.
		/// </summary>
		public static ulong ReadULong(string text, ulong @default = default) =>
			ulong.TryParse(text, NumberStyles, InvariantCulture, out ulong val) ? val : @default;

		/// <summary>
		/// Attempts to read a member of the enum <typeparamref name="T"/> from text, returning the default
		/// member if unsuccessful.
		/// </summary>
		public static T ReadEnum<T>(string text, T @default = default) where T : Enum => 
			Enum.Parse(typeof(T), text) is T tobj ? tobj : @default;

		/// <summary>
		/// Attempts to read a <see cref="Version"/> from text, returning <c>null</c> if unavailable.
		/// </summary>
		public static Version ReadVersion(string text, Version @default = default) => 
			Version.TryParse(text, out Version val) ? val : @default;

		/// <summary>
		/// Attempts to read a <see cref="Guid"/> from text, returning <see cref="Guid.Empty"/> if unsuccessful.
		/// </summary>
		public static Guid ReadGuid(string text, string format = "N") =>
			Guid.TryParseExact(text, format, out Guid val) ? val : Guid.Empty;

		/// <summary>
		/// Returns an <see cref="XText"/> node containing the given string. The string is checked for reserved XML 
		/// characters, and if any are found, the returned <see cref="XText"/> is an <see cref="XCData"/>.
		/// </summary>
		public static XText WriteText(string text) =>
			text.IndexOfAny(XmlReservedChars) >= 0 ? new XCData(text) : new XText(text);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(bool obj) => obj ? bool.TrueString : bool.FalseString;

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(byte obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(DateTime obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(decimal obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(double obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(short obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(int obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(long obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(sbyte obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(float obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(ushort obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(uint obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(ulong obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(Version obj) => obj.ToString();

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(BigInteger obj) => obj.ToString(InvariantCulture);

		/// <summary>
		/// Writes the given object as a string
		/// </summary>
		public static string Write(Guid obj, string format = "N") => obj.ToString(format, InvariantCulture);

		#endregion

		#region Static versions of XDomain extension methods

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static object Read(XElement element) => XExtensionMethods.Read(XDomain.Global, element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/> as an object assignable to <typeparamref name="T"/>, using a 
		/// new instance of <see cref="XReader"/>.
		/// </summary>
		public static T Read<T>(XElement element) => XExtensionMethods.Read<T>(XDomain.Global, element);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<object> ReadAll(IEnumerable<XElement> elements) =>
			XExtensionMethods.ReadAll(XDomain.Global, elements);

		/// <summary>
		/// Attempts to read the given <see cref="XElement"/>s as objects assignable to <typeparamref name="T"/>, using a 
		/// new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadAll<T>(IEnumerable<XElement> elements) =>
			XExtensionMethods.ReadAll<T>(XDomain.Global, elements);

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		public static object ReadFrom(string file) => XExtensionMethods.ReadFrom(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the root element of the given file as an object.
		/// </summary>
		public static object ReadFrom(Stream stream) => XExtensionMethods.ReadFrom(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(params string[] files) =>
			XExtensionMethods.ReadFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(IEnumerable<string> files) =>
			XExtensionMethods.ReadFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects.
		/// </summary>
		public static IEnumerable<object> ReadFrom(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadFrom(XDomain.Global, streams);

		/// <summary>
		/// Attempts to read the root element of the given file as an object as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		public static T ReadFrom<T>(string file) => XExtensionMethods.ReadFrom<T>(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the root element of the given file as an object as an object assignable to <typeparamref name="T"/>.
		/// </summary>
		public static T ReadFrom<T>(Stream stream) => XExtensionMethods.ReadFrom<T>(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(params string[] files) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(IEnumerable<string> files) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the root elements of the given files as objects assignable to <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadFrom<T>(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadFrom<T>(XDomain.Global, streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(string file) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(Stream stream) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(params string[] files) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(IEnumerable<string> files) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects.
		/// </summary>
		public static IEnumerable<object> ReadElementsFrom(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadElementsFrom(XDomain.Global, streams);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects assignable to
		/// <typeparamref name="T"/>, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(string file) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, file);

		/// <summary>
		/// Attempts to read the child elements of the root element of the given file as a collection of objects assignable to
		/// <typeparamref name="T"/>, using a new instance of <see cref="XReader"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(Stream stream) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, stream);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(params string[] files) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(IEnumerable<string> files) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, files);

		/// <summary>
		/// Attempts to read the child elements of the root elements of the given files as a collection of objects assignable to
		/// <typeparamref name="T"/>.
		/// </summary>
		public static IEnumerable<T> ReadElementsFrom<T>(IEnumerable<Stream> streams) =>
			XExtensionMethods.ReadElementsFrom<T>(XDomain.Global, streams);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith<TType, TId>(XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class where TId : class =>
			XExtensionMethods.ReadWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith<TType, TId>(Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			XExtensionMethods.ReadWith(XDomain.Global, identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith<TType, TId>(Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			XExtensionMethods.ReadWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new <see cref="XReader"/>, then return it.
		/// </summary>
		public static XReader ReadWith(IEnumerable contextObjects) =>
			XExtensionMethods.ReadWith(XDomain.Global, contextObjects);

		/// <summary>
		/// Attempts to write the given object as an <see cref="XElement"/>, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static XElement Write(object obj) => XExtensionMethods.Write(XDomain.Global, obj);

		/// <summary>
		/// Attempts to write the given collection of objects as <see cref="XElement"/>s, using a new instance of 
		/// <see cref="XWriter"/>.
		/// </summary>
		public static IEnumerable<XElement> WriteAll(IEnumerable objects) =>
			XExtensionMethods.WriteAll(XDomain.Global, objects);

		/// <summary>
		/// Attempts to write the given object as the root element of the given file, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static void WriteTo(object obj, string file) => XExtensionMethods.WriteTo(XDomain.Global, obj, file);

		/// <summary>
		/// Attempts to write the given object as the root element of the given file, using a new instance of <see cref="XWriter"/>.
		/// </summary>
		public static void WriteTo(object obj, Stream stream) => XExtensionMethods.WriteTo(XDomain.Global, obj, stream);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of a root element
		/// with the given name (default is 'XML').
		/// </summary>
		public static void WriteToElements(IEnumerable objects, string file, XName rootElement = null) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, file, rootElement);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of a root element
		/// with the given name (default is 'XML').
		/// </summary>
		public static void WriteToElements(IEnumerable objects, Stream stream, XName rootElement = null) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, stream, rootElement);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of the given root element.
		/// </summary>
		public static void WriteToElements(IEnumerable objects, string file, XElement rootElement) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, file, rootElement);

		/// <summary>
		/// Attempts to write the given collection of objects to the given file as child elements of the given root element.
		/// </summary>
		public static void WriteToElements(IEnumerable objects, Stream stream, XElement rootElement) =>
			XExtensionMethods.WriteToElements(XDomain.Global, objects, stream, rootElement);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new 
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith<TType, TId>(XIdentifier<TType, TId> identifier,
			IEnumerable contextObjects = null) where TType : class where TId : class =>
			XExtensionMethods.WriteWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new  
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith<TType, TId>(Func<TType, TId> identifier,
			IEqualityComparer<TId> keyComparer = null, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			XExtensionMethods.WriteWith(XDomain.Global, identifier, keyComparer, contextObjects);

		/// <summary>
		/// Add the given <see cref="XIdentifier{TType, TId}"/> and <see cref="IEnumerable"/> of contextual objects to a new  
		/// <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith<TType, TId>(Func<TType, TId> identifier, IEnumerable contextObjects = null)
			where TType : class where TId : class =>
			XExtensionMethods.WriteWith(XDomain.Global, identifier, contextObjects);

		/// <summary>
		/// Add the given <see cref="IEnumerable"/> of contextual objects to a new <see cref="XWriter"/>, then return it.
		/// </summary>
		public static XWriter WriteWith(IEnumerable contextObjects) =>
			XExtensionMethods.WriteWith(XDomain.Global, contextObjects);

		#endregion

		#region Other static methods

		/// <summary>
		/// Retrieve the collection of global identifiers in the <see cref="XIdentifiers"/> component, if it exists.
		/// </summary>
		public static XCompositeIdentifier Identifier() => XComponents.Component<XIdentifiers>()?.Identifier;

		/// <summary>
		/// Add an <see cref="XIdentifier{TType, TId}"/> characterized by the given delegate to the global identifiers 
		/// used by the <see cref="XIdentifiers"/> component, if it exists.
		/// </summary>
		public static void Identify<TType, TId>(Func<TType, TId> identifier, IEqualityComparer<TId> keyComparer = null)
			where TType : class =>
			XComponents.Component<XIdentifiers>()?.Identifier.Identify(XIdentifier<TType, TId>.Create(identifier, keyComparer));

		#endregion
	}
}