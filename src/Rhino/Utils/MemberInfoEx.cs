using System.Reflection;

namespace Rhino.Utils
{
	internal static class MemberInfoEx
	{
		public static bool IsPrivate(this MemberInfo member)
		{
			var field = member as FieldInfo;
			if (field != null)
				return field.IsPrivate;
            var methodBase = member as MethodBase;
			if (methodBase != null)
				return methodBase.IsPrivate;
			return false;
		}
	}
}