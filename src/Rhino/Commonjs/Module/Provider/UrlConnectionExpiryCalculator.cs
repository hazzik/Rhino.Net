/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Net;
using Rhino.CommonJS.Module.Provider;
using Sharpen;

namespace Rhino.CommonJS.Module.Provider
{
	/// <summary>
	/// Implemented by objects that can be used as heuristic strategies for
	/// calculating the expiry of a cached resource in cases where the server of the
	/// resource doesn't provide explicit expiry information.
	/// </summary>
	/// <remarks>
	/// Implemented by objects that can be used as heuristic strategies for
	/// calculating the expiry of a cached resource in cases where the server of the
	/// resource doesn't provide explicit expiry information.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: UrlConnectionExpiryCalculator.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	public interface UrlConnectionExpiryCalculator
	{
		/// <summary>
		/// Given a URL connection, returns a calculated heuristic expiry time (in
		/// terms of milliseconds since epoch) for the resource.
		/// </summary>
		/// <remarks>
		/// Given a URL connection, returns a calculated heuristic expiry time (in
		/// terms of milliseconds since epoch) for the resource.
		/// </remarks>
		/// <param name="response">the URL connection for the resource</param>
		/// <returns>the expiry for the resource</returns>
		long CalculateExpiry(HttpWebResponse response);
	}
}
