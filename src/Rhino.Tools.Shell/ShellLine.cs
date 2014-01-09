/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Text;
using Rhino;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	/// <summary>
	/// Provides a specialized input stream for consoles to handle line
	/// editing, history and completion.
	/// </summary>
	/// <remarks>
	/// Provides a specialized input stream for consoles to handle line
	/// editing, history and completion. Relies on the JLine library (see
	/// <http://jline.sourceforge.net>).
	/// </remarks>
	public class ShellLine
	{
		[Obsolete]
		public static Stream GetStream(Scriptable scope)
		{
			ShellConsole console = ShellConsole.GetConsole(scope, Encoding.Default);
			return (console != null ? console.GetIn() : null);
		}
	}
}
