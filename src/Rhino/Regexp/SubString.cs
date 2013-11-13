/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Sharpen;

namespace Rhino.Regexp
{
	/// <summary>A utility class for lazily instantiated substrings.</summary>
	/// <remarks>A utility class for lazily instantiated substrings.</remarks>
	public class SubString
	{
		public SubString()
		{
		}

		public SubString(string str)
		{
			this.str = str;
			index = 0;
			length = str.Length;
		}

		public SubString(string source, int start, int len)
		{
			str = source;
			index = start;
			length = len;
		}

		public override string ToString()
		{
			return str == null ? string.Empty : Sharpen.Runtime.Substring(str, index, index + length);
		}

		public static readonly Rhino.Regexp.SubString emptySubString = new Rhino.Regexp.SubString();

		internal string str;

		internal int index;

		internal int length;
	}
}
