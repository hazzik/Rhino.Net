using System.Reflection;

namespace Rhino
{
	internal static class Modifier
	{
		public static bool IsPrivate(object v)
		{
			var field = v as FieldInfo;
			if (field != null)
				return field.IsPrivate;
			var methodBase = v as MethodBase;
			if (methodBase != null)
				return methodBase.IsPrivate;
			return false;
		}
	}
}