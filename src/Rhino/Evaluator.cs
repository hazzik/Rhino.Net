/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino.Ast;

namespace Rhino
{
	/// <summary>
	/// Abstraction of evaluation, which can be implemented either by an
	/// interpreter or compiler.
	/// </summary>
	/// <remarks>
	/// Abstraction of evaluation, which can be implemented either by an
	/// interpreter or compiler.
	/// </remarks>
	public interface Evaluator
	{
		/// <summary>
		/// Compile the script or function from intermediate representation
		/// tree into an executable form.
		/// </summary>
		/// <remarks>
		/// Compile the script or function from intermediate representation
		/// tree into an executable form.
		/// </remarks>
		/// <param name="compilerEnv">Compiler environment</param>
		/// <param name="tree">parse tree</param>
		/// <param name="cx">Current context</param>
		/// <param name="scope">scope of the function</param>
		/// <param name="staticSecurityDomain">security domain</param>
		/// <param name="debug"></param>
		/// <param name="encodedSource">encoding of the source code for decompilation</param>
		/// <param name="returnFunction">if true, compiling a function</param>
		/// <returns>
		/// an opaque object that can be passed to either
		/// createFunctionObject or createScriptObject, depending on the
		/// value of returnFunction
		/// </returns>
		/// <summary>Create a function object.</summary>
		/// <remarks>Create a function object.</remarks>
		/// <param name="bytecode">opaque object returned by compile</param>
		/// <returns>Function object that can be called</returns>
		Function CreateFunctionObject(CompilerEnvirons compilerEnv, ScriptNode tree, Context cx, Scriptable scope, object staticSecurityDomain, Action<object> debug);

		/// <summary>
		/// Compile the script or function from intermediate representation
		/// tree into an executable form.
		/// </summary>
		/// <remarks>
		/// Compile the script or function from intermediate representation
		/// tree into an executable form.
		/// </remarks>
		/// <param name="compilerEnv">Compiler environment</param>
		/// <param name="tree">parse tree</param>
		/// <param name="staticSecurityDomain"></param>
		/// <param name="debug"></param>
		/// <param name="encodedSource">encoding of the source code for decompilation</param>
		/// <param name="returnFunction">if true, compiling a function</param>
		/// <returns>
		/// an opaque object that can be passed to either
		/// createFunctionObject or createScriptObject, depending on the
		/// value of returnFunction
		/// </returns>
		/// <summary>Create a script object.</summary>
		/// <remarks>Create a script object.</remarks>
		/// <param name="bytecode">opaque object returned by compile</param>
		/// <param name="staticSecurityDomain">security domain</param>
		/// <returns>Script object that can be evaluated</returns>
		Script CreateScriptObject(CompilerEnvirons compilerEnv, ScriptNode tree, object staticSecurityDomain, Action<object> debug);

		/// <summary>Capture stack information from the given exception.</summary>
		/// <remarks>Capture stack information from the given exception.</remarks>
		/// <param name="ex">an exception thrown during execution</param>
		void CaptureStackInfo(RhinoException ex);

		/// <summary>Get the source position information by examining the stack.</summary>
		/// <remarks>Get the source position information by examining the stack.</remarks>
		/// <param name="cx">Context</param>
		/// <param name="linep">
		/// Array object of length &gt;= 1; getSourcePositionFromStack
		/// will assign the line number to linep[0].
		/// </param>
		/// <returns>the name of the file or other source container</returns>
		string GetSourcePositionFromStack(Context cx, int[] linep);

		/// <summary>
		/// Given a native stack trace, patch it with script-specific source
		/// and line information
		/// </summary>
		/// <param name="ex">exception</param>
		/// <param name="nativeStackTrace">the native stack trace</param>
		/// <returns>patched stack trace</returns>
		string GetPatchedStack(RhinoException ex, string nativeStackTrace);

		/// <summary>Get the script stack for the given exception</summary>
		/// <param name="ex">exception from execution</param>
		/// <returns>list of strings for the stack trace</returns>
		IList<string> GetScriptStack(RhinoException ex);

		/// <summary>
		/// Mark the given script to indicate it was created by a call to
		/// eval() or to a Function constructor.
		/// </summary>
		/// <remarks>
		/// Mark the given script to indicate it was created by a call to
		/// eval() or to a Function constructor.
		/// </remarks>
		/// <param name="script">script to mark as from eval</param>
		void SetEvalScriptFlag(Script script);
	}
}
