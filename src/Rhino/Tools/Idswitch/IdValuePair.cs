/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Sharpen;

namespace Rhino.Tools.Idswitch
{
	public class IdValuePair
	{
		public readonly int idLength;

		public readonly string id;

		public readonly string value;

		private int lineNumber;

		public IdValuePair(string id, string value)
		{
			this.idLength = id.Length;
			this.id = id;
			this.value = value;
		}

		public virtual int GetLineNumber()
		{
			return lineNumber;
		}

		public virtual void SetLineNumber(int value)
		{
			lineNumber = value;
		}
	}
}
