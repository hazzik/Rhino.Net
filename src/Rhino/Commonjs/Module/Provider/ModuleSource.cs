/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Sharpen;

namespace Rhino.CommonJS.Module.Provider
{
	/// <summary>
	/// Represents the source text of the module as a tuple of a reader, a URI, a
	/// security domain, and a cache validator.
	/// </summary>
	/// <remarks>
	/// Represents the source text of the module as a tuple of a reader, a URI, a
	/// security domain, and a cache validator.
	/// <h1>Cache validators</h1>
	/// Validators are used by caches subclassed from
	/// <see cref="CachingModuleScriptProviderBase">CachingModuleScriptProviderBase</see>
	/// to avoid repeated loading of
	/// unmodified resources as well as automatic reloading of modified resources.
	/// Such a validator can be any value that can be used to detect modification or
	/// non-modification of the resource that provided the source of the module. It
	/// can be as simple as a tuple of a URI or a file path, and a last-modified
	/// date, or an ETag (in case of HTTP). It is left to the implementation. It is
	/// also allowed to carry expiration information (i.e. in case of HTTP
	/// expiration header, or if a default expiration is used by the source provider
	/// to avoid too frequent lookup of the resource), and to short-circuit the
	/// validation in case the validator indicates the cached representation has not
	/// yet expired. All these are plainly recommendations; the validators are
	/// considered opaque and should only make sure to implement
	/// <see cref="object.Equals(object)">object.Equals(object)</see>
	/// as caches themselves can rely on it to compare
	/// them semantically. Also, it is advisable to have them be serializable.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ModuleSource.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[System.Serializable]
	public class ModuleSource
	{
		private readonly TextReader reader;

		private readonly object securityDomain;

		private readonly Uri uri;

		private readonly Uri @base;

		private readonly object validator;

		/// <summary>Creates a new module source.</summary>
		/// <remarks>Creates a new module source.</remarks>
		/// <param name="reader">the reader returning the source text of the module.</param>
		/// <param name="securityDomain">
		/// the object representing the security domain for
		/// the module's source (passed to Rhino script compiler).
		/// </param>
		/// <param name="uri">the URI of the module's source text</param>
		/// <param name="validator">
		/// a validator that can be used for subsequent cache
		/// validation of the source text.
		/// </param>
		public ModuleSource(TextReader reader, object securityDomain, Uri uri, Uri @base, object validator)
		{
			this.reader = reader;
			this.securityDomain = securityDomain;
			this.uri = uri;
			this.@base = @base;
			this.validator = validator;
		}

		/// <summary>Returns the reader returning the source text of the module.</summary>
		/// <remarks>
		/// Returns the reader returning the source text of the module. Note that
		/// subsequent calls to this method return the same object, thus it is not
		/// possible to read the source twice.
		/// </remarks>
		/// <returns>the reader returning the source text of the module.</returns>
		public virtual TextReader GetReader()
		{
			return reader;
		}

		/// <summary>
		/// Returns the object representing the security domain for the module's
		/// source.
		/// </summary>
		/// <remarks>
		/// Returns the object representing the security domain for the module's
		/// source.
		/// </remarks>
		/// <returns>
		/// the object representing the security domain for the module's
		/// source.
		/// </returns>
		public virtual object GetSecurityDomain()
		{
			return securityDomain;
		}

		/// <summary>Returns the URI of the module source text.</summary>
		/// <remarks>Returns the URI of the module source text.</remarks>
		/// <returns>the URI of the module source text.</returns>
		public virtual Uri GetUri()
		{
			return uri;
		}

		/// <summary>
		/// Returns the base URI from which this module source was loaded, or null
		/// if it was loaded from an absolute URI.
		/// </summary>
		/// <remarks>
		/// Returns the base URI from which this module source was loaded, or null
		/// if it was loaded from an absolute URI.
		/// </remarks>
		/// <returns>the base URI, or null.</returns>
		public virtual Uri GetBase()
		{
			return @base;
		}

		/// <summary>
		/// Returns the validator that can be used for subsequent cache validation
		/// of the source text.
		/// </summary>
		/// <remarks>
		/// Returns the validator that can be used for subsequent cache validation
		/// of the source text.
		/// </remarks>
		/// <returns>
		/// the validator that can be used for subsequent cache validation
		/// of the source text.
		/// </returns>
		public virtual object GetValidator()
		{
			return validator;
		}
	}
}
