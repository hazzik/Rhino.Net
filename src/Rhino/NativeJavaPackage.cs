/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
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
	public class NativeJavaPackage : ScriptableObject
	{
		internal const long serialVersionUID = 7445054382212031523L;

		internal NativeJavaPackage(bool internalUsage, string packageName, ClassLoader classLoader)
		{
			this.packageName = packageName;
			this.classLoader = classLoader;
		}

		[System.ObsoleteAttribute(@"NativeJavaPackage is an internal class, do not use it directly.")]
		public NativeJavaPackage(string packageName, ClassLoader classLoader) : this(false, packageName, classLoader)
		{
		}

		[System.ObsoleteAttribute(@"NativeJavaPackage is an internal class, do not use it directly.")]
		public NativeJavaPackage(string packageName) : this(false, packageName, Context.GetCurrentContext().GetApplicationClassLoader())
		{
		}

		public override string GetClassName()
		{
			return "JavaPackage";
		}

		public override bool Has(string id, Scriptable start)
		{
			return true;
		}

		public override bool Has(int index, Scriptable start)
		{
			return false;
		}

		public override void Put(string id, Scriptable start, object value)
		{
		}

		// Can't add properties to Java packages.  Sorry.
		public override void Put(int index, Scriptable start, object value)
		{
			throw Context.ReportRuntimeError0("msg.pkg.int");
		}

		public override object Get(string id, Scriptable start)
		{
			return GetPkgProperty(id, start, true);
		}

		public override object Get(int index, Scriptable start)
		{
			return ScriptableConstants.NOT_FOUND;
		}

		// set up a name which is known to be a package so we don't
		// need to look for a class by that name
		internal virtual Rhino.NativeJavaPackage ForcePackage(string name, Scriptable scope)
		{
			object cached = base.Get(name, this);
			if (cached != null && cached is Rhino.NativeJavaPackage)
			{
				return (Rhino.NativeJavaPackage)cached;
			}
			else
			{
				string newPackage = packageName.Length == 0 ? name : packageName + "." + name;
				Rhino.NativeJavaPackage pkg = new Rhino.NativeJavaPackage(true, newPackage, classLoader);
				ScriptRuntime.SetObjectProtoAndParent(pkg, scope);
				base.Put(name, this, pkg);
				return pkg;
			}
		}

		internal virtual object GetPkgProperty(string name, Scriptable start, bool createPkg)
		{
			lock (this)
			{
				object cached = base.Get(name, start);
				if (cached != ScriptableConstants.NOT_FOUND)
				{
					return cached;
				}
				if (negativeCache != null && negativeCache.Contains(name))
				{
					// Performance optimization: see bug 421071
					return null;
				}
				string className = (packageName.Length == 0) ? name : packageName + '.' + name;
				Context cx = Context.GetContext();
				ClassShutter shutter = cx.GetClassShutter();
				Scriptable newValue = null;
				if (shutter == null || shutter.VisibleToScripts(className))
				{
					Type cl = null;
					if (classLoader != null)
					{
						cl = Kit.ClassOrNull(classLoader, className);
					}
					else
					{
						cl = Kit.ClassOrNull(className);
					}
					if (cl != null)
					{
						WrapFactory wrapFactory = cx.GetWrapFactory();
						newValue = wrapFactory.WrapJavaClass(cx, GetTopLevelScope(this), cl);
						newValue.SetPrototype(GetPrototype());
					}
				}
				if (newValue == null)
				{
					if (createPkg)
					{
						Rhino.NativeJavaPackage pkg;
						pkg = new Rhino.NativeJavaPackage(true, className, classLoader);
						ScriptRuntime.SetObjectProtoAndParent(pkg, GetParentScope());
						newValue = pkg;
					}
					else
					{
						// add to negative cache
						if (negativeCache == null)
						{
							negativeCache = new HashSet<string>();
						}
						negativeCache.AddItem(name);
					}
				}
				if (newValue != null)
				{
					// Make it available for fast lookup and sharing of
					// lazily-reflected constructors and static members.
					base.Put(name, start, newValue);
				}
				return newValue;
			}
		}

		public override object GetDefaultValue(Type ignored)
		{
			return ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			this.classLoader = Context.GetCurrentContext().GetApplicationClassLoader();
		}

		public override string ToString()
		{
			return "[JavaPackage " + packageName + "]";
		}

		public override bool Equals(object obj)
		{
			if (obj is Rhino.NativeJavaPackage)
			{
				Rhino.NativeJavaPackage njp = (Rhino.NativeJavaPackage)obj;
				return packageName.Equals(njp.packageName) && classLoader == njp.classLoader;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return packageName.GetHashCode() ^ (classLoader == null ? 0 : classLoader.GetHashCode());
		}

		private string packageName;

		[System.NonSerialized]
		private ClassLoader classLoader;

		private ICollection<string> negativeCache = null;
	}
}
