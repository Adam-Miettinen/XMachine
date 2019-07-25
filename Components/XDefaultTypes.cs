using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using XMachine.Components.Tuples;

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
			typeof(BigInteger)
		};

		private static readonly Type[] defaultTypeDefinitions = new Type[]
		{
			typeof(KeyValuePair<,>),
			typeof(Tuple<>),
			typeof(Tuple<,>),
			typeof(Tuple<,,>),
			typeof(Tuple<,,,>),
			typeof(Tuple<,,,,>),
			typeof(Tuple<,,,,,>),
			typeof(Tuple<,,,,,,>),
			typeof(Tuple<,,,,,,,>)
		};

		internal static bool IsDefaultType(Type type) =>
			defaultTypes.Contains(type) ||
			type.IsEnum ||
			(type.IsConstructedGenericType && defaultTypeDefinitions.Contains(type.GetGenericTypeDefinition()));

		protected override void OnCreateDomain(XDomain domain)
		{
			// Write char, char[], and string as strings
			domain.Reflect<char>().Register(new XTexter<char>(x => XmlTools.ReadChar(x), (obj) => XmlTools.Write(obj)));
			domain.Reflect<char[]>().Register(new XTexter<char[]>(x => x.ToCharArray(), (obj) => new string(obj)));
			domain.Reflect<string>().Register(new XTexter<string>(x => x, x => x));
			_ = domain.Reflect<string[]>();

			// Write bools as case-insensitive true/false
			domain.Reflect<bool>().Register(new XTexter<bool>(x => XmlTools.ReadBool(x), (obj) => XmlTools.Write(obj)));
			_ = domain.Reflect<bool[]>();

			// Numbers be numbers
			domain.Reflect<byte>().Register(new XTexter<byte>(x => XmlTools.ReadByte(x), (obj) => XmlTools.Write(obj)));
			_ = domain.Reflect<byte[]>();

			domain.Reflect<sbyte>().Register(new XTexter<sbyte>(x => XmlTools.ReadSByte(x), (obj) => XmlTools.Write(obj)));
			_ = domain.Reflect<sbyte[]>();

			domain.Reflect<decimal>().Register(new XTexter<decimal>(x => XmlTools.ReadDecimal(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<decimal[]>();

			domain.Reflect<double>().Register(new XTexter<double>(x => XmlTools.ReadDouble(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<double[]>();

			domain.Reflect<float>().Register(new XTexter<float>(x => XmlTools.ReadFloat(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<float[]>();

			domain.Reflect<int>().Register(new XTexter<int>(x => XmlTools.ReadInt(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<int[]>();

			domain.Reflect<uint>().Register(new XTexter<uint>(x => XmlTools.ReadUInt(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<uint[]>();

			domain.Reflect<long>().Register(new XTexter<long>(x => XmlTools.ReadLong(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<long[]>();

			domain.Reflect<ulong>().Register(new XTexter<ulong>(x => XmlTools.ReadULong(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<ulong[]>();

			domain.Reflect<short>().Register(new XTexter<short>(x => XmlTools.ReadShort(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<short[]>();

			domain.Reflect<ushort>().Register(new XTexter<ushort>(x => XmlTools.ReadUShort(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<ushort[]>();

			// Other common texter types

			domain.Reflect<DateTime>().Register(new XTexter<DateTime>(x => XmlTools.ReadDateTime(x), x => XmlTools.Write(x)));

			domain.Reflect<Version>().Register(new XTexter<Version>(x => XmlTools.ReadVersion(x), x => XmlTools.Write(x)));

			domain.Reflect<BigInteger>().Register(new XTexter<BigInteger>(x => XmlTools.ReadBigInteger(x), x => XmlTools.Write(x)));
		}

		protected override void OnCreateXType<T>(XType<T> xType)
		{
			Type type = typeof(T);

			// Special enum behaviour

			if (type.IsEnum)
			{
				xType.Register(new XTexter<T>(x => (T)Enum.Parse(typeof(T), x)));
				return;
			}

			Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			Type[] args = type.IsGenericType ? type.GenericTypeArguments : null;

			// Tuples

			if (definition != null)
			{
				if (definition == typeof(Tuple<>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
				else if (definition == typeof(Tuple<,>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<,>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
				else if (definition == typeof(Tuple<,,>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<,,>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
				else if (definition == typeof(Tuple<,,,>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<,,,>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
				else if (definition == typeof(Tuple<,,,,>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<,,,,>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
				else if (definition == typeof(Tuple<,,,,,>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<,,,,,>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
				else if (definition == typeof(Tuple<,,,,,,>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<,,,,,,>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
				else if (definition == typeof(Tuple<,,,,,,,>))
				{
					xType.Register((XTypeComponent<T>)typeof(XTuple<,,,,,,,>).MakeGenericType(args)
						.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
						.FirstOrDefault()
						.Invoke(null));
				}
			}
		}
	}
}
