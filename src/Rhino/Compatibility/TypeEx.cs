using System;
using System.Reflection;

namespace Rhino
{
	public static class TypeEx
	{
		public static Uri GetResource(this Type type, string name)
		{
			throw new NotImplementedException();
		}

		public static ClassLoader GetClassLoader(this Type type)
		{
			return new ClassLoader();
		}

	    public static FieldInfo GetDeclaredField(this Type type, string value)
		{
			return type.GetField(value, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static ConstructorInfo[] GetDeclaredConstructors(this Type type)
		{
			return type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}
	}
}