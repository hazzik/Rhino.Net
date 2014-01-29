using System;
using System.Collections;

namespace Rhino
{
	public static class BitArrayEx
	{
		public static void Set(this BitArray array, int index)
		{
			array.Set(index, true);
		}

		public static int Cardinality(this BitArray array)
		{
			var result = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array.Get(i))
					result ++;
			}
			return result;
		}
	}
}