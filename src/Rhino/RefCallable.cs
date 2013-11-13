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
	/// <summary>Object that can allows assignments to the result of function calls.</summary>
	/// <remarks>Object that can allows assignments to the result of function calls.</remarks>
	public interface RefCallable : Callable
	{
		/// <summary>Perform function call in reference context.</summary>
		/// <remarks>
		/// Perform function call in reference context.
		/// The args array reference should not be stored in any object that is
		/// can be GC-reachable after this method returns. If this is necessary,
		/// for example, to implement
		/// <see cref="Ref">Ref</see>
		/// methods, then store args.clone(),
		/// not args array itself.
		/// </remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="thisObj">the JavaScript <code>this</code> object</param>
		/// <param name="args">the array of arguments</param>
		Ref RefCall(Context cx, Scriptable thisObj, object[] args);
	}
}
