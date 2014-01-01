using System;
using System.Linq;
using System.Reflection;

namespace Rhino
{
	public static class MethodBaseEx
	{
		public static Type[] GetParameterTypes(this MethodBase @this)
		{
			return @this.GetParameters().Select(t => t.ParameterType).ToArray();
		}

		public static bool IsAnnotationPresent(this MethodBase method, Type attributeType)
		{
			return method.GetCustomAttributes(attributeType).Any();
		}

		public static object NewInstance(this ConstructorInfo constructor, params object[] parameters)
		{
			return constructor.Invoke(parameters);
		}
	}
}