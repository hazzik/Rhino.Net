/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Net;
using Sharpen;

namespace Rhino.CommonJS.Module.Provider
{
	/// <summary>The default heuristic for calculating cache expiry of URL-based resources.</summary>
	/// <remarks>
	/// The default heuristic for calculating cache expiry of URL-based resources.
	/// It is simply configured with a default relative expiry, and each invocation
	/// of
	/// <see cref="CalculateExpiry">CalculateExpiry(Sharpen.URLConnection)</see>
	/// returns
	/// <see cref="Extensions.ToMillisecondsSinceEpoch(DateTime.UtcNow)">DateTime.UtcNow.ToMillisecondsSinceEpoch()</see>
	/// incremented with the relative expiry.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: DefaultUrlConnectionExpiryCalculator.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[System.Serializable]
	public class DefaultUrlConnectionExpiryCalculator : UrlConnectionExpiryCalculator
	{
		private readonly long relativeExpiry;

		/// <summary>Creates a new default expiry calculator with one minute relative expiry.</summary>
		/// <remarks>Creates a new default expiry calculator with one minute relative expiry.</remarks>
		public DefaultUrlConnectionExpiryCalculator() : this(60000L)
		{
		}

		/// <summary>
		/// Creates a new default expiry calculator with the specified relative
		/// expiry.
		/// </summary>
		/// <remarks>
		/// Creates a new default expiry calculator with the specified relative
		/// expiry.
		/// </remarks>
		/// <param name="relativeExpiry">the fixed relative expiry, in milliseconds.</param>
		public DefaultUrlConnectionExpiryCalculator(long relativeExpiry)
		{
			if (relativeExpiry < 0)
			{
				throw new ArgumentException("relativeExpiry < 0");
			}
			this.relativeExpiry = relativeExpiry;
		}

		public virtual long CalculateExpiry(HttpWebResponse response)
		{
			return DateTime.UtcNow.ToMillisecondsSinceEpoch() + relativeExpiry;
		}
	}
}
