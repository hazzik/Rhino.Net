/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the Undefined value in JavaScript.</summary>
	/// <remarks>This class implements the Undefined value in JavaScript.</remarks>
	[System.Serializable]
	public class Undefined
	{
		internal const long serialVersionUID = 9195680630202616767L;

		public static readonly object instance = new Rhino.Undefined();

		private Undefined()
		{
		}

		public virtual object ReadResolve()
		{
			return instance;
		}
	}
}
