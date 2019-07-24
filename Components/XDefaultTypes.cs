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
			XType<bool> xBool = domain.Reflect<bool>();
			xBool.Register(new XTexter<bool>(x => XmlTools.ReadBool(x), (obj) => XmlTools.Write(obj)));
			_ = domain.Reflect<bool[]>();

			XType<byte> xByte = domain.Reflect<byte>();
			xByte.Register(new XTexter<byte>(x => XmlTools.ReadByte(x), (obj) => XmlTools.Write(obj)));
			_ = domain.Reflect<byte[]>();

			XType<sbyte> xSByte = domain.Reflect<sbyte>();
			xSByte.Register(new XTexter<sbyte>(x => XmlTools.ReadSByte(x), (obj) => XmlTools.Write(obj)));
			_ = domain.Reflect<sbyte[]>();

			XType<char> xChar = domain.Reflect<char>();
			xChar.Register(new XTexter<char>(x => XmlTools.ReadChar(x), (obj) => XmlTools.Write(obj)));
			_ = domain.Reflect<char[]>();

			XType<decimal> xDecimal = domain.Reflect<decimal>();
			xDecimal.Register(new XTexter<decimal>(x => XmlTools.ReadDecimal(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<decimal[]>();

			XType<double> xDouble = domain.Reflect<double>();
			xDouble.Register(new XTexter<double>(x => XmlTools.ReadDouble(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<double[]>();

			XType<float> xFloat = domain.Reflect<float>();
			xFloat.Register(new XTexter<float>(x => XmlTools.ReadFloat(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<float[]>();

			XType<int> xInt = domain.Reflect<int>();
			xInt.Register(new XTexter<int>(x => XmlTools.ReadInt(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<int[]>();

			XType<uint> xUInt = domain.Reflect<uint>();
			xUInt.Register(new XTexter<uint>(x => XmlTools.ReadUInt(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<uint[]>();

			XType<long> xLong = domain.Reflect<long>();
			xLong.Register(new XTexter<long>(x => XmlTools.ReadLong(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<long[]>();

			XType<ulong> xULong = domain.Reflect<ulong>();
			xULong.Register(new XTexter<ulong>(x => XmlTools.ReadULong(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<ulong[]>();

			XType<short> xShort = domain.Reflect<short>();
			xShort.Register(new XTexter<short>(x => XmlTools.ReadShort(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<short[]>();

			XType<ushort> xUShort = domain.Reflect<ushort>();
			xUShort.Register(new XTexter<ushort>(x => XmlTools.ReadUShort(x), x => XmlTools.Write(x)));
			_ = domain.Reflect<ushort[]>();

			XType<string> xString = domain.Reflect<string>();
			xString.Register(new XTexter<string>(x => x, x => x));
			_ = domain.Reflect<string[]>();

			XType<DateTime> xDateTime = domain.Reflect<DateTime>();
			xDateTime.Register(new XTexter<DateTime>(x => XmlTools.ReadDateTime(x), x => XmlTools.Write(x)));

			XType<Version> xVersion = domain.Reflect<Version>();
			xVersion.Register(new XTexter<Version>(x => XmlTools.ReadVersion(x), x => XmlTools.Write(x)));

			XType<BigInteger> xBigInteger = domain.Reflect<BigInteger>();
			xBigInteger.Register(new XTexter<BigInteger>(x => XmlTools.ReadBigInteger(x), x => XmlTools.Write(x)));
		}

		protected override void OnCreateXType<T>(XType<T> xType)
		{
			Type type = typeof(T);

			// Special enum behaviour

			if (type.IsEnum)
			{
				xType.Register(new XTexter<T>(x => (T)Enum.Parse(type, x)));
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
