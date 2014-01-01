/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class reflects Java packages into the JavaScript environment.</summary>
	/// <remarks>
	/// This class reflects Java packages into the JavaScript environment.  We
	/// lazily reflect classes and subpackages, and use a caching/sharing
	/// system to ensure that members reflected into one JavaPackage appear
	/// in all other references to the same package (as with Packages.java.lang
	/// and java.lang).
	/// </remarks>
	/// <author>Mike Shaver</author>
	/// <seealso cref="NativeJavaArray">NativeJavaArray</seealso>
	/// <seealso cref="NativeJavaObject">NativeJavaObject</seealso>
	/// <seealso cref="NativeJavaClass">NativeJavaClass</seealso>
	[System.Serializable]
	public class NativeJavaTopPackage : NativeJavaPackage, Function, IdFunctionCall
	{
		internal const long serialVersionUID = -1455787259477709999L;

		private static readonly string[][] commonPackages = new string[][] { new string[] { "java", "lang", "reflect" }, new string[] { "java", "io" }, new string[] { "java", "math" }, new string[] { "java", "net" }, new string[] { "java", "util", "zip" }, new string[] { "java", "text", "resources" }, new string[] { "java", "applet" }, new string[] { "javax", "swing" } };

		internal NativeJavaTopPackage(ClassLoader loader) : base(true, string.Empty, loader)
		{
		}

		// we know these are packages so we can skip the class check
		// note that this is ok even if the package isn't present.
		public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			return Construct(cx, scope, args);
		}

		public virtual Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			ClassLoader loader = null;
			if (args.Length != 0)
			{
				object arg = args[0];
				if (arg is Wrapper)
				{
					arg = ((Wrapper)arg).Unwrap();
				}
				if (arg is ClassLoader)
				{
					loader = (ClassLoader)arg;
				}
			}
			if (loader == null)
			{
				Context.ReportRuntimeError0("msg.not.classloader");
				return null;
			}
			NativeJavaPackage pkg = new NativeJavaPackage(true, string.Empty, loader);
			ScriptRuntime.SetObjectProtoAndParent(pkg, scope);
			return pkg;
		}

		public static void Init(Context cx, Scriptable scope, bool @sealed)
		{
//			ClassLoader loader = cx.GetApplicationClassLoader();
			Rhino.NativeJavaTopPackage top = new Rhino.NativeJavaTopPackage(new ClassLoader());
			top.SetPrototype(GetObjectPrototype(scope));
			top.SetParentScope(scope);
			for (int i = 0; i != commonPackages.Length; i++)
			{
				NativeJavaPackage parent = top;
				for (int j = 0; j != commonPackages[i].Length; j++)
				{
					parent = parent.ForcePackage(commonPackages[i][j], scope);
				}
			}
			// getClass implementation
			IdFunctionObject getClass = new IdFunctionObject(top, FTAG, Id_getClass, "getClass", 1, scope);
			// We want to get a real alias, and not a distinct JavaPackage
			// with the same packageName, so that we share classes and top
			// that are underneath.
			string[] topNames = ScriptRuntime.GetTopPackageNames();
			NativeJavaPackage[] topPackages = new NativeJavaPackage[topNames.Length];
			for (int i_1 = 0; i_1 < topNames.Length; i_1++)
			{
				topPackages[i_1] = (NativeJavaPackage)top.Get(topNames[i_1], top);
			}
			// It's safe to downcast here since initStandardObjects takes
			// a ScriptableObject.
			ScriptableObject global = (ScriptableObject)scope;
			if (@sealed)
			{
				getClass.SealObject();
			}
			getClass.ExportAsScopeProperty();
			global.DefineProperty("Packages", top, ScriptableObject.DONTENUM);
			for (int i_2 = 0; i_2 < topNames.Length; i_2++)
			{
				global.DefineProperty(topNames[i_2], topPackages[i_2], ScriptableObject.DONTENUM);
			}
		}

		public virtual object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (f.HasTag(FTAG))
			{
				if (f.MethodId() == Id_getClass)
				{
					return Js_getClass(cx, scope, args);
				}
			}
			throw f.Unknown();
		}

		private Scriptable Js_getClass(Context cx, Scriptable scope, object[] args)
		{
			if (args.Length > 0 && args[0] is Wrapper)
			{
				Scriptable result = this;
				Type cl = ((Wrapper)args[0]).Unwrap().GetType();
				// Evaluate the class name by getting successive properties of
				// the string to find the appropriate NativeJavaClass object
				string name = cl.FullName;
				int offset = 0;
				for (; ; )
				{
					int index = name.IndexOf('.', offset);
					string propName = index == -1 ? name.Substring(offset) : name.Substring(offset, index - offset);
					object prop = result.Get(propName, result);
					if (!(prop is Scriptable))
					{
						break;
					}
					// fall through to error
					result = (Scriptable)prop;
					if (index == -1)
					{
						return result;
					}
					offset = index + 1;
				}
			}
			throw Context.ReportRuntimeError0("msg.not.java.obj");
		}

		private static readonly object FTAG = "JavaTopPackage";

		private const int Id_getClass = 1;
	}
}
