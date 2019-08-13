using System;
using System.Linq;

namespace XMachine.Components
{
	internal sealed class XDefaultTypes : XMachineComponent
	{
		private static readonly Type[] defaultTypes = new Type[]
		{
			typeof(bool),
			typeof(byte),
			typeof(sbyte),
			typeof(char),
			typeof(decimal),
			typeof(double),
			typeof(float),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(short),
			typeof(ushort),
			typeof(string),

			typeof(DateTime),
			typeof(Version),
			typeof(Guid)
		};

		internal static bool IsDefaultType(Type type) =>
			defaultTypes.Contains(type) || type.IsEnum;

		protected override void OnCreateDomain(XDomain domain)
		{
			// Write char, char[], and string as strings
			RegisterTexter(domain.Reflect<char>(), x => XmlTools.ReadChar(x), (obj) => XmlTools.Write(obj));
			RegisterTexter(domain.Reflect<char[]>(), x => x.ToCharArray(), (obj) => new string(obj));
			RegisterTexter(domain.Reflect<string>(), x => x, x => x);

			// Write bools as case-insensitive true/false
			RegisterTexter(domain.Reflect<bool>(), x => XmlTools.ReadBool(x), (obj) => XmlTools.Write(obj));

			// Numbers be numbers
			RegisterTexter(domain.Reflect<byte>(), x => XmlTools.ReadByte(x), (obj) => XmlTools.Write(obj));
			RegisterTexter(domain.Reflect<sbyte>(), x => XmlTools.ReadSByte(x), (obj) => XmlTools.Write(obj));
			RegisterTexter(domain.Reflect<decimal>(), x => XmlTools.ReadDecimal(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<double>(), x => XmlTools.ReadDouble(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<float>(), x => XmlTools.ReadFloat(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<int>(), x => XmlTools.ReadInt(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<uint>(), x => XmlTools.ReadUInt(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<long>(), x => XmlTools.ReadLong(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<ulong>(), x => XmlTools.ReadULong(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<short>(), x => XmlTools.ReadShort(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<ushort>(), x => XmlTools.ReadUShort(x), x => XmlTools.Write(x));

			// Other common texter types
			RegisterTexter(domain.Reflect<DateTime>(), x => XmlTools.ReadDateTime(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<Version>(), x => XmlTools.ReadVersion(x), x => XmlTools.Write(x));
			RegisterTexter(domain.Reflect<Guid>(), x => XmlTools.ReadGuid(x), x => XmlTools.Write(x));
		}

		protected override void OnCreateXType<T>(XType<T> xType)
		{
			Type type = typeof(T);

			// Special enum behaviour
			if (type.IsEnum)
			{
				RegisterTexter(xType, x => (T)Enum.Parse(typeof(T), x), x => x.ToString());
			}
		}

		private void RegisterTexter<T>(XType<T> xType, Func<string, T> reader, Func<T, string> writer) =>
			xType.Register(new XTexter<T>(xType, reader, writer));
	}
}
