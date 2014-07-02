namespace Sharpen
{
    public class Arrays
	{
	    public static bool Equals<T> (T[] a1, T[] a2)
		{
			if (a1.Length != a2.Length) {
				return false;
			}
			for (int i = 0; i < a1.Length; i++) {
				if (!a1[i].Equals (a2[i])) {
					return false;
				}
			}
			return true;
		}
	}
}
