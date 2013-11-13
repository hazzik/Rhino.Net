/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// Generic notion of callable object that can execute some script-related code
	/// upon request with specified values for script scope and this objects.
	/// </summary>
	/// <remarks>
	/// Generic notion of callable object that can execute some script-related code
	/// upon request with specified values for script scope and this objects.
	/// </remarks>
	public interface Callable
	{
		/// <summary>Perform the call.</summary>
		/// <remarks>Perform the call.</remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">the scope to use to resolve properties.</param>
		/// <param name="thisObj">the JavaScript <code>this</code> object</param>
		/// <param name="args">the array of arguments</param>
		/// <returns>the result of the call</returns>
		object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args);
	}
}
