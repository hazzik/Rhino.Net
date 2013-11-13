/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	public abstract class SecurityProxy : SecurityController
	{
		protected internal abstract void CallProcessFileSecure(Context cx, Scriptable scope, string filename);
	}
}
