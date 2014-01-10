/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// The class of error objects
	/// ECMA 15.11
	/// </summary>
	[System.Serializable]
	internal sealed class NativeError : IdScriptableObject
	{
		private static readonly object ERROR_TAG = "Error";

		private RhinoException stackProvider;

		internal static void Init(Scriptable scope, bool @sealed)
		{
			NativeError obj = new NativeError();
			ScriptableObject.PutProperty(obj, "name", "Error");
			ScriptableObject.PutProperty(obj, "message", string.Empty);
			ScriptableObject.PutProperty(obj, "fileName", string.Empty);
			ScriptableObject.PutProperty(obj, "lineNumber", 0);
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		internal static NativeError Make(Context cx, Scriptable scope, IdFunctionObject ctorObj, object[] args)
		{
			Scriptable proto = (Scriptable)(ctorObj.Get("prototype", ctorObj));
			NativeError obj = new NativeError();
			obj.Prototype = proto;
			obj.ParentScope = scope;
			int arglen = args.Length;
			if (arglen >= 1)
			{
				ScriptableObject.PutProperty(obj, "message", ScriptRuntime.ToString(args[0]));
				if (arglen >= 2)
				{
					ScriptableObject.PutProperty(obj, "fileName", args[1]);
					if (arglen >= 3)
					{
						int line = ScriptRuntime.ToInt32(args[2]);
						ScriptableObject.PutProperty(obj, "lineNumber", line);
					}
				}
			}
			return obj;
		}

		public override string GetClassName()
		{
			return "Error";
		}

		public override string ToString()
		{
			// According to spec, Error.prototype.toString() may return undefined.
			object toString = Js_toString(this);
			return toString is string ? (string)toString : base.ToString();
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 1;
					s = "constructor";
					break;
				}

				case Id_toString:
				{
					arity = 0;
					s = "toString";
					break;
				}

				case Id_toSource:
				{
					arity = 0;
					s = "toSource";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(ERROR_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(ERROR_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_constructor:
				{
					return Make(cx, scope, f, args);
				}

				case Id_toString:
				{
					return Js_toString(thisObj);
				}

				case Id_toSource:
				{
					return Js_toSource(cx, scope, thisObj);
				}
			}
			throw new ArgumentException(id.ToString());
		}

		public void SetStackProvider(RhinoException re)
		{
			// We go some extra miles to make sure the stack property is only
			// generated on demand, is cached after the first access, and is
			// overwritable like an ordinary property. Hence this setup with
			// the getter and setter below.
			if (stackProvider == null)
			{
				stackProvider = re;
				DefineProperty("stack", null, typeof (NativeError).GetMethod("GetStack"), typeof (NativeError).GetMethod("SetStack", new[] { typeof (object) }), 0);
			}
		}

		public object GetStack()
		{
			object value = stackProvider == null ? ScriptableConstants.NOT_FOUND : stackProvider.GetScriptStackTrace();
			// We store the stack as local property both to cache it
			// and to make the property writable
			SetStack(value);
			return value;
		}

		public void SetStack(object value)
		{
			if (stackProvider != null)
			{
				stackProvider = null;
				Delete("stack");
			}
			Put("stack", this, value);
		}

		private static object Js_toString(Scriptable thisObj)
		{
			object name = ScriptableObject.GetProperty(thisObj, "name");
			if (name == ScriptableConstants.NOT_FOUND || name == Undefined.instance)
			{
				name = "Error";
			}
			else
			{
				name = ScriptRuntime.ToString(name);
			}
			object msg = ScriptableObject.GetProperty(thisObj, "message");
			object result;
			if (msg == ScriptableConstants.NOT_FOUND || msg == Undefined.instance)
			{
				result = Undefined.instance;
			}
			else
			{
				result = ((string)name) + ": " + ScriptRuntime.ToString(msg);
			}
			return result;
		}

		private static string Js_toSource(Context cx, Scriptable scope, Scriptable thisObj)
		{
			// Emulation of SpiderMonkey behavior
			object name = ScriptableObject.GetProperty(thisObj, "name");
			object message = ScriptableObject.GetProperty(thisObj, "message");
			object fileName = ScriptableObject.GetProperty(thisObj, "fileName");
			object lineNumber = ScriptableObject.GetProperty(thisObj, "lineNumber");
			StringBuilder sb = new StringBuilder();
			sb.Append("(new ");
			if (name == ScriptableConstants.NOT_FOUND)
			{
				name = Undefined.instance;
			}
			sb.Append(ScriptRuntime.ToString(name));
			sb.Append("(");
			if (message != ScriptableConstants.NOT_FOUND || fileName != ScriptableConstants.NOT_FOUND || lineNumber != ScriptableConstants.NOT_FOUND)
			{
				if (message == ScriptableConstants.NOT_FOUND)
				{
					message = string.Empty;
				}
				sb.Append(ScriptRuntime.Uneval(cx, scope, message));
				if (fileName != ScriptableConstants.NOT_FOUND || lineNumber != ScriptableConstants.NOT_FOUND)
				{
					sb.Append(", ");
					if (fileName == ScriptableConstants.NOT_FOUND)
					{
						fileName = string.Empty;
					}
					sb.Append(ScriptRuntime.Uneval(cx, scope, fileName));
					if (lineNumber != ScriptableConstants.NOT_FOUND)
					{
						int line = ScriptRuntime.ToInt32(lineNumber);
						if (line != 0)
						{
							sb.Append(", ");
							sb.Append(ScriptRuntime.ToString(line));
						}
					}
				}
			}
			sb.Append("))");
			return sb.ToString();
		}

		private static string GetString(Scriptable obj, string id)
		{
			object value = ScriptableObject.GetProperty(obj, id);
			if (value == ScriptableConstants.NOT_FOUND)
			{
				return string.Empty;
			}
			return ScriptRuntime.ToString(value);
		}

		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #string_id_map#
			// #generated# Last update: 2007-05-09 08:15:45 EDT
			id = 0;
			string X = null;
			int c;
			int s_length = s.Length;
			if (s_length == 8)
			{
				c = s[3];
				if (c == 'o')
				{
					X = "toSource";
					id = Id_toSource;
				}
				else
				{
					if (c == 't')
					{
						X = "toString";
						id = Id_toString;
					}
				}
			}
			else
			{
				if (s_length == 11)
				{
					X = "constructor";
					id = Id_constructor;
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

		private const int Id_toString = 2;

		private const int Id_toSource = 3;

		private const int MAX_PROTOTYPE_ID = 3;
		// #/string_id_map#
	}
}
