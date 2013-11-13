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
	/// <summary>All compiled scripts implement this interface.</summary>
	/// <remarks>
	/// All compiled scripts implement this interface.
	/// <p>
	/// This class encapsulates script execution relative to an
	/// object scope.
	/// </remarks>
	/// <since>1.3</since>
	/// <author>Norris Boyd</author>
	public interface Script
	{
		// API class
		/// <summary>Execute the script.</summary>
		/// <remarks>
		/// Execute the script.
		/// <p>
		/// The script is executed in a particular runtime Context, which
		/// must be associated with the current thread.
		/// The script is executed relative to a scope--definitions and
		/// uses of global top-level variables and functions will access
		/// properties of the scope object. For compliant ECMA
		/// programs, the scope must be an object that has been initialized
		/// as a global object using <code>Context.initStandardObjects</code>.
		/// <p>
		/// </remarks>
		/// <param name="cx">the Context associated with the current thread</param>
		/// <param name="scope">the scope to execute relative to</param>
		/// <returns>the result of executing the script</returns>
		/// <seealso cref="Context.InitStandardObjects()">Context.InitStandardObjects()</seealso>
		object Exec(Context cx, Scriptable scope);
	}
}
