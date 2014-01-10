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
	/// <summary>The JavaScript Script object.</summary>
	/// <remarks>
	/// The JavaScript Script object.
	/// Note that the C version of the engine uses XDR as the format used
	/// by freeze and thaw. Since this depends on the internal format of
	/// structures in the C runtime, we cannot duplicate it.
	/// Since we cannot replace 'this' as a result of the compile method,
	/// will forward requests to execute to the nonnull 'script' field.
	/// </remarks>
	/// <since>1.3</since>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	internal class NativeScript : BaseFunction
	{
		private static readonly object SCRIPT_TAG = "Script";

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeScript obj = new Rhino.NativeScript(null);
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		private NativeScript(Script script)
		{
			this.script = script;
		}

		/// <summary>Returns the name of this JavaScript class, "Script".</summary>
		/// <remarks>Returns the name of this JavaScript class, "Script".</remarks>
		public override string GetClassName()
		{
			return "Script";
		}

		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (script != null)
			{
				return script.Exec(cx, scope);
			}
			return Undefined.instance;
		}

		public override Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			throw Context.ReportRuntimeError0("msg.script.is.not.constructor");
		}

		public override int Length
		{
			get { return 0; }
		}

		public override int Arity
		{
			get { return 0; }
		}

		internal override string Decompile(int indent, int flags)
		{
			if (script is NativeFunction)
			{
				return ((NativeFunction)script).Decompile(indent, flags);
			}
			return base.Decompile(indent, flags);
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

				case Id_exec:
				{
					arity = 0;
					s = "exec";
					break;
				}

				case Id_compile:
				{
					arity = 1;
					s = "compile";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(SCRIPT_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(SCRIPT_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_constructor:
				{
					string source = (args.Length == 0) ? string.Empty : ScriptRuntime.ToString(args[0]);
					Script script = Compile(cx, source);
					Rhino.NativeScript nscript = new Rhino.NativeScript(script);
					ScriptRuntime.SetObjectProtoAndParent(nscript, scope);
					return nscript;
				}

				case Id_toString:
				{
					Rhino.NativeScript real = RealThis(thisObj, f);
					Script realScript = real.script;
					if (realScript == null)
					{
						return string.Empty;
					}
					return cx.DecompileScript(realScript, 0);
				}

				case Id_exec:
				{
					throw Context.ReportRuntimeError1("msg.cant.call.indirect", "exec");
				}

				case Id_compile:
				{
					Rhino.NativeScript real = RealThis(thisObj, f);
					string source = ScriptRuntime.ToString(args, 0);
					real.script = Compile(cx, source);
					return real;
				}
			}
			throw new ArgumentException(id.ToString());
		}

		private static Rhino.NativeScript RealThis(Scriptable thisObj, IdFunctionObject f)
		{
			if (!(thisObj is Rhino.NativeScript))
			{
				throw IncompatibleCallError(f);
			}
			return (Rhino.NativeScript)thisObj;
		}

		private static Script Compile(Context cx, string source)
		{
			int[] linep = new int[] { 0 };
			string filename = Context.GetSourcePositionFromStack(linep);
			if (filename == null)
			{
				filename = "<Script object>";
				linep[0] = 1;
			}
			ErrorReporter reporter;
			reporter = DefaultErrorReporter.ForEval(cx.GetErrorReporter());
			return cx.CompileString(source, null, reporter, filename, linep[0], null);
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:16:01 EDT
			id = 0;
			string X = null;
			switch (s.Length)
			{
				case 4:
				{
					X = "exec";
					id = Id_exec;
					goto L_break;
				}

				case 7:
				{
					X = "compile";
					id = Id_compile;
					goto L_break;
				}

				case 8:
				{
					X = "toString";
					id = Id_toString;
					goto L_break;
				}

				case 11:
				{
					X = "constructor";
					id = Id_constructor;
					goto L_break;
				}
			}
L_break: ;
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

		private const int Id_compile = 3;

		private const int Id_exec = 4;

		private const int MAX_PROTOTYPE_ID = 4;

		private Script script;
		// #/string_id_map#
	}
}
