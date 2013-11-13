/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Tools.Debugger;
using Sharpen;

namespace Rhino.Tools.Debugger
{
	/// <summary>Interface to provide a scope object for script evaluation to the debugger.</summary>
	/// <remarks>Interface to provide a scope object for script evaluation to the debugger.</remarks>
	public interface ScopeProvider
	{
		/// <summary>Returns the scope object to be used for script evaluation.</summary>
		/// <remarks>Returns the scope object to be used for script evaluation.</remarks>
		Scriptable GetScope();
	}
}
