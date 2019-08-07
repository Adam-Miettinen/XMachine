using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XMachine.Components.Collections
{
	internal sealed class XArray2<T> : XCollection<T[,], T>
	{
		internal XArray2() { }

		protected override void AddItem(T[,] collection, T item) { }

		protected override void OnBuild(XType<T[,]> xType, IXReadOperation reader, XElement element, ObjectBuilder<T[,]> objectBuilder)
		{
			// Read as a jagged array

			T[][] jagged = null;

			reader.Read<T[][]>(element, x =>
			{
				jagged = x;
				return true;
			});

			// Convert to a multidimensional array once read

			reader.AddTask(this, () =>
			{
				if (jagged == null)
				{
					return false;
				}

				int lb0 = jagged.GetLowerBound(0), n0 = jagged.Length;

				int lb1, n1;
				if (n0 > 0)
				{
					lb1 = jagged[0].GetLowerBound(0);
					n1 = jagged[0].Length;
				}
				else
				{
					lb1 = 0;
					n1 = 0;
				}

				objectBuilder.Object = (T[,])Array.CreateInstance(typeof(T), new int[2] { n0, n1 }, new int[2] { lb0, lb1 });

				for (int i = lb0, I = lb0 + n0; i < I; i++)
				{
					for (int j = lb1, J = lb1 + n1; j < J; j++)
					{
						objectBuilder.Object[i, j] = jagged[i][j];
					}
				}

				return true;
			});
		}

		protected override bool OnWrite(XType<T[,]> xType, IXWriteOperation writer, T[,] obj, XElement element)
		{
			// Get lower bound and length

			int lb = obj.GetLowerBound(0), n = obj.GetLength(0);
			int[] lb1 = new int[1] { obj.GetLowerBound(1) }, n1 = new int[1] { obj.GetLength(1) };

			if (lb != 0)
			{
				element.SetAttributeValue(XComponents.Component<XAutoCollections>().ArrayLowerBoundName, XmlTools.Write(lb));
			}

			// Copy each second rank into a new array and write them as 1-dimensional arrays

			for (int i = lb, I = lb + n; i < I; i++)
			{
				T[] dim1 = (T[])Array.CreateInstance(typeof(T), n1, lb1);

				for (int j = lb1[0], J = n1[0]; j < J; j++)
				{
					dim1[j] = obj[i, j];
				}

				XElement li = writer.WriteTo(new XElement(ItemName), dim1);
				if (lb1[0] != 0)
				{
					li.SetAttributeValue(XComponents.Component<XAutoCollections>().ArrayLowerBoundName, XmlTools.Write(lb1[0]));
				}
				element.Add(li);
			}

			return true;
		}
	}
}
