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
	/// Master for id-based functions that knows their properties and how to
	/// execute them.
	/// </summary>
	/// <remarks>
	/// Master for id-based functions that knows their properties and how to
	/// execute them.
	/// </remarks>
	public interface IdFunctionCall
	{
		/// <summary>
		/// 'thisObj' will be null if invoked as constructor, in which case
		/// instance of Scriptable should be returned
		/// </summary>
		object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args);
	}
}
