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
	[System.Serializable]
	public sealed class NativeContinuation : IdScriptableObject, Function
	{
		internal const long serialVersionUID = 1794167133757605367L;

		private static readonly object FTAG = "Continuation";

		private object implementation;

		public static void Init(Context cx, Scriptable scope, bool @sealed)
		{
			NativeContinuation obj = new NativeContinuation();
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		public object GetImplementation()
		{
			return implementation;
		}

		public void InitImplementation(object implementation)
		{
			this.implementation = implementation;
		}

		public override string GetClassName()
		{
			return "Continuation";
		}

		public Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			throw Context.ReportRuntimeError("Direct call is not supported");
		}

		public object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			return Interpreter.RestartContinuation(this, cx, scope, args);
		}

		public static bool IsContinuationConstructor(IdFunctionObject f)
		{
			if (f.HasTag(FTAG) && f.MethodId() == Id_constructor)
			{
				return true;
			}
			return false;
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

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(FTAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(FTAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_constructor:
				{
					throw Context.ReportRuntimeError("Direct call is not supported");
				}
			}
			throw new ArgumentException(id.ToString());
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:16:40 EDT
			id = 0;
			string X = null;
			if (s.Length == 11)
			{
				X = "constructor";
				id = Id_constructor;
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

		private const int MAX_PROTOTYPE_ID = 1;
		// #/string_id_map#
	}
}
