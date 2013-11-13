/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Commonjs.Module.Provider;
using Sharpen;

namespace Rhino.Commonjs.Module.Provider
{
	/// <summary>Interface for URL connection based security domain providers.</summary>
	/// <remarks>
	/// Interface for URL connection based security domain providers. Used by
	/// <see cref="UrlModuleSourceProvider">UrlModuleSourceProvider</see>
	/// to create Rhino security domain objects for
	/// newly loaded scripts (see
	/// <see cref="Rhino.Context.CompileReader(System.IO.StreamReader, string, int, object)">Rhino.Context.CompileReader(System.IO.StreamReader, string, int, object)</see>
	/// ) based on the properties obtainable through their URL
	/// connection.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: UrlConnectionSecurityDomainProvider.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	public interface UrlConnectionSecurityDomainProvider
	{
		/// <summary>
		/// Create a new security domain object for a script source identified by
		/// its URL connection.
		/// </summary>
		/// <remarks>
		/// Create a new security domain object for a script source identified by
		/// its URL connection.
		/// </remarks>
		/// <param name="urlConnection">the URL connection of the script source</param>
		/// <returns>
		/// the security domain object for the script source. Can be null if
		/// no security domain object can be created, although it is advisable for
		/// the implementations to be able to create a security domain object for
		/// any URL connection.
		/// </returns>
		object GetSecurityDomain(URLConnection urlConnection);
	}
}
