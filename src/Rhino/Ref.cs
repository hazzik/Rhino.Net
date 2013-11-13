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
	/// Generic notion of reference object that know how to query/modify the
	/// target objects based on some property/index.
	/// </summary>
	/// <remarks>
	/// Generic notion of reference object that know how to query/modify the
	/// target objects based on some property/index.
	/// </remarks>
	[System.Serializable]
	public abstract class Ref
	{
		internal const long serialVersionUID = 4044540354730911424L;

		public virtual bool Has(Context cx)
		{
			return true;
		}

		public abstract object Get(Context cx);

		public abstract object Set(Context cx, object value);

		public virtual bool Delete(Context cx)
		{
			return false;
		}
	}
}
