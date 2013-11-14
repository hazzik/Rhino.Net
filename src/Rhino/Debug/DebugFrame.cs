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

namespace Rhino.Debug
{
	/// <summary>
	/// Interface to implement if the application is interested in receiving debug
	/// information during execution of a particular script or function.
	/// </summary>
	/// <remarks>
	/// Interface to implement if the application is interested in receiving debug
	/// information during execution of a particular script or function.
	/// </remarks>
	public interface DebugFrame
	{
		// API class
		/// <summary>Called when execution is ready to start bytecode interpretation for entered a particular function or script.</summary>
		/// <remarks>Called when execution is ready to start bytecode interpretation for entered a particular function or script.</remarks>
		/// <param name="cx">current Context for this thread</param>
		/// <param name="activation">the activation scope for the function or script.</param>
		/// <param name="thisObj">value of the JavaScript <code>this</code> object</param>
		/// <param name="args">the array of arguments</param>
		void OnEnter(Context cx, Scriptable activation, Scriptable thisObj, object[] args);

		/// <summary>Called when executed code reaches new line in the source.</summary>
		/// <remarks>Called when executed code reaches new line in the source.</remarks>
		/// <param name="cx">current Context for this thread</param>
		/// <param name="lineNumber">current line number in the script source</param>
		void OnLineChange(Context cx, int lineNumber);

		/// <summary>Called when thrown exception is handled by the function or script.</summary>
		/// <remarks>Called when thrown exception is handled by the function or script.</remarks>
		/// <param name="cx">current Context for this thread</param>
		/// <param name="ex">exception object</param>
		void OnExceptionThrown(Context cx, Exception ex);

		/// <summary>Called when the function or script for this frame is about to return.</summary>
		/// <remarks>Called when the function or script for this frame is about to return.</remarks>
		/// <param name="cx">current Context for this thread</param>
		/// <param name="byThrow">
		/// if true function will leave by throwing exception, otherwise it
		/// will execute normal return
		/// </param>
		/// <param name="resultOrException">
		/// function result in case of normal return or
		/// exception object if about to throw exception
		/// </param>
		void OnExit(Context cx, bool byThrow, object resultOrException);

		/// <summary>Called when the function or script executes a 'debugger' statement.</summary>
		/// <remarks>Called when the function or script executes a 'debugger' statement.</remarks>
		/// <param name="cx">current Context for this thread</param>
		void OnDebuggerStatement(Context cx);
	}
}
