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
	/// <summary>
	/// Class ImporterTopLevel
	/// This class defines a ScriptableObject that can be instantiated
	/// as a top-level ("global") object to provide functionality similar
	/// to Java's "import" statement.
	/// </summary>
	/// <remarks>
	/// Class ImporterTopLevel
	/// This class defines a ScriptableObject that can be instantiated
	/// as a top-level ("global") object to provide functionality similar
	/// to Java's "import" statement.
	/// <p>
	/// This class can be used to create a top-level scope using the following code:
	/// <pre>
	/// Scriptable scope = new ImporterTopLevel(cx);
	/// </pre>
	/// Then JavaScript code will have access to the following methods:
	/// <ul>
	/// <li>importClass - will "import" a class by making its unqualified name
	/// available as a property of the top-level scope
	/// <li>importPackage - will "import" all the classes of the package by
	/// searching for unqualified names as classes qualified
	/// by the given package.
	/// </ul>
	/// The following code from the shell illustrates this use:
	/// <pre>
	/// js&gt; importClass(java.io.File)
	/// js&gt; f = new File('help.txt')
	/// help.txt
	/// js&gt; importPackage(java.util)
	/// js&gt; v = new Vector()
	/// []
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public class ImporterTopLevel : TopLevel
	{
		private static readonly object IMPORTER_TAG = "Importer";

		public ImporterTopLevel()
		{
		}

		public ImporterTopLevel(Context cx) : this(cx, false)
		{
		}

		public ImporterTopLevel(Context cx, bool @sealed)
		{
			// API class
			InitStandardObjects(cx, @sealed);
		}

		public override string GetClassName()
		{
			return (topScopeFlag) ? "global" : "JavaImporter";
		}

		public static void Init(Context cx, Scriptable scope, bool @sealed)
		{
			Rhino.ImporterTopLevel obj = new Rhino.ImporterTopLevel();
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		public virtual void InitStandardObjects(Context cx, bool @sealed)
		{
			// Assume that Context.initStandardObjects initialize JavaImporter
			// property lazily so the above init call is not yet called
			cx.InitStandardObjects(this, @sealed);
			topScopeFlag = true;
			// If seal is true then exportAsJSClass(cx, seal) would seal
			// this obj. Since this is scope as well, it would not allow
			// to add variables.
			IdFunctionObject ctor = ExportAsJSClass(MAX_PROTOTYPE_ID, this, false);
			if (@sealed)
			{
				ctor.SealObject();
			}
			// delete "constructor" defined by exportAsJSClass so "constructor"
			// name would refer to Object.constructor
			// and not to JavaImporter.prototype.constructor.
			Delete("constructor");
		}

		public override bool Has(string name, Scriptable start)
		{
			return base.Has(name, start) || GetPackageProperty(name, start) != ScriptableConstants.NOT_FOUND;
		}

		public override object Get(string name, Scriptable start)
		{
			object result = base.Get(name, start);
			if (result != ScriptableConstants.NOT_FOUND)
			{
				return result;
			}
			result = GetPackageProperty(name, start);
			return result;
		}

		private object GetPackageProperty(string name, Scriptable start)
		{
			object result = ScriptableConstants.NOT_FOUND;
			object[] elements;
			lock (importedPackages)
			{
				elements = importedPackages.ToArray();
			}
			for (int i = 0; i < elements.Length; i++)
			{
				NativeJavaPackage p = (NativeJavaPackage)elements[i];
				object v = p.GetPkgProperty(name, start, false);
				if (v != null && !(v is NativeJavaPackage))
				{
					if (result == ScriptableConstants.NOT_FOUND)
					{
						result = v;
					}
					else
					{
						throw Context.ReportRuntimeError2("msg.ambig.import", result.ToString(), v.ToString());
					}
				}
			}
			return result;
		}

		private object Js_construct(Scriptable scope, object[] args)
		{
			Rhino.ImporterTopLevel result = new Rhino.ImporterTopLevel();
			for (int i = 0; i != args.Length; ++i)
			{
				object arg = args[i];
				var cl = arg as NativeJavaClass;
				if (cl != null)
				{
					result.ImportClass(cl);
				}
				else
				{
					var pkg = arg as NativeJavaPackage;
					if (pkg != null)
					{
						result.ImportPackage(pkg);
					}
					else
					{
						throw Context.ReportRuntimeError1("msg.not.class.not.pkg", Context.ToString(arg));
					}
				}
			}
			// set explicitly prototype and scope
			// as otherwise in top scope mode BaseFunction.construct
			// would keep them set to null. It also allow to use
			// JavaImporter without new and still get properly
			// initialized object.
			result.ParentScope = scope;
			result.Prototype = this;
			return result;
		}

		private object Js_importClass(object[] args)
		{
			for (int i = 0; i != args.Length; i++)
			{
				object arg = args[i];
				if (!(arg is NativeJavaClass))
				{
					throw Context.ReportRuntimeError1("msg.not.class", Context.ToString(arg));
				}
				ImportClass((NativeJavaClass)arg);
			}
			return Undefined.instance;
		}

		private object Js_importPackage(object[] args)
		{
			for (int i = 0; i != args.Length; i++)
			{
				object arg = args[i];
				if (!(arg is NativeJavaPackage))
				{
					throw Context.ReportRuntimeError1("msg.not.pkg", Context.ToString(arg));
				}
				ImportPackage((NativeJavaPackage)arg);
			}
			return Undefined.instance;
		}

		private void ImportPackage(NativeJavaPackage pkg)
		{
			if (pkg == null)
			{
				return;
			}
			lock (importedPackages)
			{
				for (int j = 0; j != importedPackages.Size(); j++)
				{
					if (pkg.Equals(importedPackages.Get(j)))
					{
						return;
					}
				}
				importedPackages.Add(pkg);
			}
		}

		private void ImportClass(NativeJavaClass cl)
		{
			string s = cl.GetClassObject().FullName;
			string n = s.Substring(s.LastIndexOf('.') + 1);
			object val = Get(n, this);
			if (val != ScriptableConstants.NOT_FOUND && val != cl)
			{
				throw Context.ReportRuntimeError1("msg.prop.defined", n);
			}
			//defineProperty(n, cl, DONTENUM);
			Put(n, this, cl);
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 0;
					s = "constructor";
					break;
				}

				case Id_importClass:
				{
					arity = 1;
					s = "importClass";
					break;
				}

				case Id_importPackage:
				{
					arity = 1;
					s = "importPackage";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(IMPORTER_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(IMPORTER_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_constructor:
				{
					return Js_construct(scope, args);
				}

				case Id_importClass:
				{
					return RealThis(thisObj, f).Js_importClass(args);
				}

				case Id_importPackage:
				{
					return RealThis(thisObj, f).Js_importPackage(args);
				}
			}
			throw new ArgumentException(id.ToString());
		}

		private Rhino.ImporterTopLevel RealThis(Scriptable thisObj, IdFunctionObject f)
		{
			if (topScopeFlag)
			{
				// when used as top scope importPackage and importClass are global
				// function that ignore thisObj
				return this;
			}
			if (!(thisObj is Rhino.ImporterTopLevel))
			{
				throw IncompatibleCallError(f);
			}
			return (Rhino.ImporterTopLevel)thisObj;
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:15:24 EDT
			id = 0;
			string X = null;
			int c;
			int s_length = s.Length;
			if (s_length == 11)
			{
				c = s[0];
				if (c == 'c')
				{
					X = "constructor";
					id = Id_constructor;
				}
				else
				{
					if (c == 'i')
					{
						X = "importClass";
						id = Id_importClass;
					}
				}
			}
			else
			{
				if (s_length == 13)
				{
					X = "importPackage";
					id = Id_importPackage;
				}
			}
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		private const int Id_constructor = 1;

		private const int Id_importClass = 2;

		private const int Id_importPackage = 3;

		private const int MAX_PROTOTYPE_ID = 3;

		private ObjArray importedPackages = new ObjArray();

		private bool topScopeFlag;
		// #/string_id_map#
	}
}
