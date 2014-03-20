/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Debug;
using Sharpen;

namespace Rhino
{
	[System.Serializable]
	internal sealed class InterpretedFunction : NativeFunction, Script
	{
		internal InterpreterData idata;
#if ENCHANCED_SECURITY
		internal SecurityController securityController;
		internal object securityDomain;
#endif

		private InterpretedFunction(InterpreterData idata, object staticSecurityDomain)
		{
			this.idata = idata;
			// Always get Context from the current thread to
			// avoid security breaches via passing mangled Context instances
			// with bogus SecurityController
#if ENCHANCED_SECURITY
			Context cx = Context.GetContext();
			SecurityController sc = cx.GetSecurityController();
			object dynamicDomain;
			if (sc != null)
			{
				dynamicDomain = sc.GetDynamicSecurityDomain(staticSecurityDomain);
			}
			else
			{
				if (staticSecurityDomain != null)
				{
					throw new ArgumentException();
				}
				dynamicDomain = null;
			}
			this.securityController = sc;
			this.securityDomain = dynamicDomain;
#endif
		}

		private InterpretedFunction(Rhino.InterpretedFunction parent, int index)
		{
			this.idata = parent.idata.itsNestedFunctions[index];
#if ENCHANCED_SECURITY
			this.securityController = parent.securityController;
			this.securityDomain = parent.securityDomain;
#endif
		}

		/// <summary>Create script from compiled bytecode.</summary>
		/// <remarks>Create script from compiled bytecode.</remarks>
		internal static Rhino.InterpretedFunction CreateScript(InterpreterData idata, object staticSecurityDomain)
		{
			Rhino.InterpretedFunction f;
			f = new Rhino.InterpretedFunction(idata, staticSecurityDomain);
			return f;
		}

		/// <summary>Create function compiled from Function(...) constructor.</summary>
		/// <remarks>Create function compiled from Function(...) constructor.</remarks>
		internal static Rhino.InterpretedFunction CreateFunction(Context cx, Scriptable scope, InterpreterData idata, object staticSecurityDomain)
		{
			Rhino.InterpretedFunction f;
			f = new Rhino.InterpretedFunction(idata, staticSecurityDomain);
			f.InitScriptFunction(cx, scope);
			return f;
		}

		/// <summary>Create function embedded in script or another function.</summary>
		/// <remarks>Create function embedded in script or another function.</remarks>
		internal static Rhino.InterpretedFunction CreateFunction(Context cx, Scriptable scope, Rhino.InterpretedFunction parent, int index)
		{
			Rhino.InterpretedFunction f = new Rhino.InterpretedFunction(parent, index);
			f.InitScriptFunction(cx, scope);
			return f;
		}

		public override string FunctionName
		{
			get { return idata.itsName ?? string.Empty; }
		}

		/// <summary>Calls the function.</summary>
		/// <remarks>Calls the function.</remarks>
		/// <param name="cx">the current context</param>
		/// <param name="scope">the scope used for the call</param>
		/// <param name="thisObj">the value of "this"</param>
		/// <param name="args">
		/// function arguments. Must not be null. You can use
		/// <see cref="ScriptRuntime.emptyArgs">ScriptRuntime.emptyArgs</see>
		/// to pass empty arguments.
		/// </param>
		/// <returns>the result of the function call.</returns>
		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!ScriptRuntime.HasTopCall(cx))
			{
				return ScriptRuntime.DoTopCall(this, cx, scope, thisObj, args);
			}
			return Interpreter.Interpret(this, cx, scope, thisObj, args);
		}

		public object Exec(Context cx, Scriptable scope)
		{
			if (!IsScript())
			{
				// Can only be applied to scripts
				throw new InvalidOperationException();
			}
			if (!ScriptRuntime.HasTopCall(cx))
			{
				// It will go through "call" path. but they are equivalent
				return ScriptRuntime.DoTopCall(this, cx, scope, scope, ScriptRuntime.emptyArgs);
			}
			return Interpreter.Interpret(this, cx, scope, scope, ScriptRuntime.emptyArgs);
		}

		public bool IsScript()
		{
			return idata.itsFunctionType == 0;
		}

		public override string GetEncodedSource()
		{
			return Interpreter.GetEncodedSource(idata);
		}

		public override DebuggableScript GetDebuggableView()
		{
			return idata;
		}

		public override object ResumeGenerator(Context cx, Scriptable scope, int operation, object state, object value)
		{
			return Interpreter.ResumeGenerator(cx, scope, operation, state, value);
		}

		protected internal override LanguageVersion GetLanguageVersion()
		{
			return idata.languageVersion;
		}

		protected internal override int GetParamCount()
		{
			return idata.argCount;
		}

		protected internal override int GetParamAndVarCount()
		{
			return idata.argNames.Length;
		}

		protected internal override string GetParamOrVarName(int index)
		{
			return idata.argNames[index];
		}

		protected internal override bool GetParamOrVarConst(int index)
		{
			return idata.argIsConst[index];
		}
	}
}
