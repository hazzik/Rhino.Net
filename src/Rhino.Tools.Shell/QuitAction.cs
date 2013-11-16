/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace Rhino.Tools.Shell
{
	/// <summary>Defines action to perform in response to quit command.</summary>
	/// <remarks>Defines action to perform in response to quit command.</remarks>
	public delegate void QuitAction(Context cx, int exitCode);
}
