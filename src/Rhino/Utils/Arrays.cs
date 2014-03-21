using System.Collections.Generic;

namespace Rhino.Utils
{
	public static class Arrays
	{
		public static bool Equals<T>(T[] x, T[] y)
		{
			if (x.Length != y.Length)
				return false;
			
			for (var i = 0; i < x.Length; i++)
			{
				if (!x[i].Equals(y[i]))
				{
					return false;
				}
			}
			
			return true;
		}

		public static int HashCode<T>(IEnumerable<T> a)
		{
			if (a == null)
				return 0;

			var result = 1;
			
			foreach (var element in a)
			{
				result = element == null ? 31*result + 0 : 31*result + element.GetHashCode();
			}

			return result;
		}
	}
}
