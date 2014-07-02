using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Text;

namespace Sharpen
{
	public class Runtime
	{
	    private static Hashtable properties;

		public static Hashtable GetProperties()
		{
			if (properties == null)
			{
				properties = new Hashtable();
				properties["jgit.fs.debug"] = "false";
				var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Trim();
				if (string.IsNullOrEmpty(home))
					home = Environment.GetFolderPath(Environment.SpecialFolder.Personal).Trim();
				properties["user.home"] = home;
				properties["java.library.path"] = Environment.GetEnvironmentVariable("PATH");
				if (Path.DirectorySeparatorChar != '\\')
					properties["os.name"] = "Unix";
				else
					properties["os.name"] = "Windows";
			}
			return properties;
		}

		public static string GetProperty(string key)
		{
			return ((string) GetProperties()[key]);
		}

		public static int IdentityHashCode(object ob)
		{
			return RuntimeHelpers.GetHashCode(ob);
		}

		public static byte[] GetBytesForString(string str)
		{
			return Encoding.UTF8.GetBytes(str);
		}

		public static byte[] GetBytesForString(string str, string encoding)
		{
			return Encoding.GetEncoding(encoding).GetBytes(str);
		}

		public static FieldInfo[] GetDeclaredFields(Type t)
		{
			return t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		}


	    public static Type GetType(string name)
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type t = a.GetType(name);
				if (t != null)
					return t;
			}
			throw new InvalidOperationException("Type not found: " + name);
		}

	    public static Encoding GetEncoding(string name)
		{
			//			Encoding e = Encoding.GetEncoding (name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
			Encoding e = Encoding.GetEncoding(name.Replace('_', '-'));
			if (e is UTF8Encoding)
				return new UTF8Encoding(false, true);
			return e;
		}

		public static MethodInfo[] GetDeclaredMethods(Type type)
		{
			return type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		}
	}
}
