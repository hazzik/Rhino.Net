/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Debug;
using Sharpen;

namespace Rhino.Debug
{
	/// <summary>
	/// Interface to implement if the application is interested in receiving debug
	/// information.
	/// </summary>
	/// <remarks>
	/// Interface to implement if the application is interested in receiving debug
	/// information.
	/// </remarks>
	public interface Debugger
	{
		// API class
		/// <summary>
		/// Called when compilation of a particular function or script into internal
		/// bytecode is done.
		/// </summary>
		/// <remarks>
		/// Called when compilation of a particular function or script into internal
		/// bytecode is done.
		/// </remarks>
		/// <param name="cx">current Context for this thread</param>
		/// <param name="fnOrScript">object describing the function or script</param>
		/// <param name="source">the function or script source</param>
		void HandleCompilationDone(Context cx, DebuggableScript fnOrScript, string source);

		/// <summary>Called when execution entered a particular function or script.</summary>
		/// <remarks>Called when execution entered a particular function or script.</remarks>
		/// <returns>
		/// implementation of DebugFrame which receives debug information during
		/// the function or script execution or null otherwise
		/// </returns>
		DebugFrame GetFrame(Context cx, DebuggableScript fnOrScript);
	}
}
