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
	/// <summary>This class implements the activation object.</summary>
	/// <remarks>
	/// This class implements the activation object.
	/// See ECMA 10.1.6
	/// </remarks>
	/// <seealso cref="Arguments">Arguments</seealso>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public sealed class NativeCall : IdScriptableObject
	{
		private static readonly object CALL_TAG = "Call";

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeCall obj = new Rhino.NativeCall();
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		internal NativeCall()
		{
		}

		internal NativeCall(NativeFunction function, Scriptable scope, object[] args)
		{
			this.function = function;
			ParentScope = scope;
			// leave prototype null
			this.originalArgs = (args == null) ? ScriptRuntime.emptyArgs : args;
			// initialize values of arguments
			int paramAndVarCount = function.GetParamAndVarCount();
			int paramCount = function.GetParamCount();
			if (paramAndVarCount != 0)
			{
				for (int i = 0; i < paramCount; ++i)
				{
					string name = function.GetParamOrVarName(i);
					object val = i < args.Length ? args[i] : Undefined.instance;
					DefineProperty(name, val, PropertyAttributes.PERMANENT);
				}
			}
			// initialize "arguments" property but only if it was not overridden by
			// the parameter with the same name
			if (!base.Has("arguments", this))
			{
				DefineProperty("arguments", new Arguments(this), PropertyAttributes.PERMANENT);
			}
			if (paramAndVarCount != 0)
			{
				for (int i = paramCount; i < paramAndVarCount; ++i)
				{
					string name = function.GetParamOrVarName(i);
					if (!base.Has(name, this))
					{
						if (function.GetParamOrVarConst(i))
						{
							DefineProperty(name, Undefined.instance, PropertyAttributes.CONST);
						}
						else
						{
							DefineProperty(name, Undefined.instance, PropertyAttributes.PERMANENT);
						}
					}
				}
			}
		}

		public override string GetClassName()
		{
			return "Call";
		}

		protected internal override int FindPrototypeId(string s)
		{
			return s.Equals("constructor") ? Id_constructor : 0;
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			if (id == Id_constructor)
			{
				arity = 1;
				s = "constructor";
			}
			else
			{
				throw new ArgumentException(id.ToString());
			}
			InitPrototypeMethod(CALL_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(CALL_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			if (id == Id_constructor)
			{
				if (thisObj != null)
				{
					throw Context.ReportRuntimeError1("msg.only.from.new", "Call");
				}
				ScriptRuntime.CheckDeprecated(cx, "Call");
				Rhino.NativeCall result = new Rhino.NativeCall();
				result.SetPrototype(GetObjectPrototype(scope));
				return result;
			}
			throw new ArgumentException(id.ToString());
		}

		private const int Id_constructor = 1;

		private const int MAX_PROTOTYPE_ID = 1;

		internal NativeFunction function;

		internal object[] originalArgs;

		[System.NonSerialized]
		internal Rhino.NativeCall parentActivationCall;
	}
}
