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
	/// Interface to represent arbitrary action that requires to have Context
	/// object associated with the current thread for its execution.
	/// </summary>
	/// <remarks>
	/// Interface to represent arbitrary action that requires to have Context
	/// object associated with the current thread for its execution.
	/// </remarks>
	public interface ContextAction
	{
		// API class
		/// <summary>Execute action using the supplied Context instance.</summary>
		/// <remarks>
		/// Execute action using the supplied Context instance.
		/// When Rhino runtime calls the method, <tt>cx</tt> will be associated
		/// with the current thread as active context.
		/// </remarks>
		/// <seealso cref="ContextFactory.Call(ContextAction)">ContextFactory.Call(ContextAction)</seealso>
		object Run(Context cx);
	}
}