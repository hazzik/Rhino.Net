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
	/// <summary>This is interface that all functions in JavaScript must implement.</summary>
	/// <remarks>
	/// This is interface that all functions in JavaScript must implement.
	/// The interface provides for calling functions and constructors.
	/// </remarks>
	/// <seealso cref="Scriptable">Scriptable</seealso>
	/// <author>Norris Boyd</author>
	public interface Function : Scriptable, Callable
	{
		// API class
		/// <summary>Call the function.</summary>
		/// <remarks>
		/// Call the function.
		/// Note that the array of arguments is not guaranteed to have
		/// length greater than 0.
		/// </remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">
		/// the scope to execute the function relative to. This is
		/// set to the value returned by getParentScope() except
		/// when the function is called from a closure.
		/// </param>
		/// <param name="thisObj">the JavaScript <code>this</code> object</param>
		/// <param name="args">the array of arguments</param>
		/// <returns>the result of the call</returns>
		object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args);

		/// <summary>Call the function as a constructor.</summary>
		/// <remarks>
		/// Call the function as a constructor.
		/// This method is invoked by the runtime in order to satisfy a use
		/// of the JavaScript <code>new</code> operator.  This method is
		/// expected to create a new object and return it.
		/// </remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">
		/// an enclosing scope of the caller except
		/// when the function is called from a closure.
		/// </param>
		/// <param name="args">the array of arguments</param>
		/// <returns>the allocated object</returns>
		Scriptable Construct(Context cx, Scriptable scope, object[] args);
	}
}
