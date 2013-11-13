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
	/// A proxy for the regexp package, so that the regexp package can be
	/// loaded optionally.
	/// </summary>
	/// <remarks>
	/// A proxy for the regexp package, so that the regexp package can be
	/// loaded optionally.
	/// </remarks>
	/// <author>Norris Boyd</author>
	public interface RegExpProxy
	{
		// Types of regexp actions
		bool IsRegExp(Scriptable obj);

		object CompileRegExp(Context cx, string source, string flags);

		Scriptable WrapRegExp(Context cx, Scriptable scope, object compiled);

		object Action(Context cx, Scriptable scope, Scriptable thisObj, object[] args, int actionType);

		int Find_split(Context cx, Scriptable scope, string target, string separator, Scriptable re, int[] ip, int[] matchlen, bool[] matched, string[][] parensp);

		object Js_split(Context _cx, Scriptable _scope, string thisString, object[] _args);
	}

	public static class RegExpProxyConstants
	{
		public const int RA_MATCH = 1;

		public const int RA_REPLACE = 2;

		public const int RA_SEARCH = 3;
	}
}
