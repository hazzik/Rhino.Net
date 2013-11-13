/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Commonjs.Module.Provider;
using Sharpen;

namespace Rhino.Commonjs.Module.Provider
{
	/// <summary>Implemented by objects that can provide the source text for the script.</summary>
	/// <remarks>
	/// Implemented by objects that can provide the source text for the script. The
	/// design of the interface supports cache revalidation.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ModuleSourceProvider.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	public interface ModuleSourceProvider
	{
		/// <summary>Returns the script source of the requested module.</summary>
		/// <remarks>
		/// Returns the script source of the requested module. More specifically, it
		/// resolves the module ID to a resource. If it can not resolve it, null is
		/// returned. If the caller passes a non-null validator, and the source
		/// provider recognizes it, and the validator applies to the same resource
		/// that the provider would use to load the source, and the validator
		/// validates the current cached representation of the resource (using
		/// whatever semantics for validation that this source provider implements),
		/// then
		/// <see cref="NOT_MODIFIED">NOT_MODIFIED</see>
		/// should be returned. Otherwise, it should
		/// return a
		/// <see cref="ModuleSource">ModuleSource</see>
		/// object with the actual source text of the
		/// module, preferrably a validator for it, and a security domain, where
		/// applicable.
		/// </remarks>
		/// <param name="moduleId">
		/// the ID of the module. An implementation must only accept
		/// an absolute ID, starting with a term.
		/// </param>
		/// <param name="paths">
		/// the value of the require() function's "paths" attribute. If
		/// the require() function is sandboxed, it will be null, otherwise it will
		/// be a JavaScript Array object. It is up to the provider implementation
		/// whether and how it wants to honor the contents of the array.
		/// </param>
		/// <param name="validator">
		/// a validator for an existing loaded and cached module.
		/// This will either be null, or an object that this source provider
		/// returned earlier as part of a
		/// <see cref="ModuleSource">ModuleSource</see>
		/// . It can be used to
		/// validate the existing cached module and avoid reloading it.
		/// </param>
		/// <returns>
		/// a script representing the code of the module. Null should be
		/// returned if the script is not found.
		/// <see cref="NOT_MODIFIED">NOT_MODIFIED</see>
		/// should be
		/// returned if the passed validator validates the current representation of
		/// the module (the currently cached module script).
		/// </returns>
		/// <exception cref="System.IO.IOException">if there was an I/O problem reading the script</exception>
		/// <exception cref="Sharpen.URISyntaxException">if the final URI could not be constructed.</exception>
		/// <exception cref="System.ArgumentException">
		/// if the module ID is syntactically not a
		/// valid absolute module identifier.
		/// </exception>
		ModuleSource LoadSource(string moduleId, Scriptable paths, object validator);

		/// <summary>Returns the script source of the requested module from the given URI.</summary>
		/// <remarks>
		/// Returns the script source of the requested module from the given URI.
		/// The URI is absolute but does not contain a file name extension such as
		/// ".js", which may be specific to the ModuleSourceProvider implementation.
		/// <p>
		/// If the resource is not found, null is returned. If the caller passes a
		/// non-null validator, and the source provider recognizes it, and the
		/// validator applies to the same resource that the provider would use to
		/// load the source, and the validator validates the current cached
		/// representation of the resource (using whatever semantics for validation
		/// that this source provider implements), then
		/// <see cref="NOT_MODIFIED">NOT_MODIFIED</see>
		/// should be returned. Otherwise, it should return a
		/// <see cref="ModuleSource">ModuleSource</see>
		/// object with the actual source text of the module, preferrably a
		/// validator for it, and a security domain, where applicable.
		/// </remarks>
		/// <param name="uri">
		/// the absolute URI from which to load the module source, but
		/// without an extension such as ".js".
		/// </param>
		/// <param name="baseUri">
		/// the module path base URI from which <code>uri</code>
		/// was derived.
		/// </param>
		/// <param name="validator">
		/// a validator for an existing loaded and cached module.
		/// This will either be null, or an object that this source provider
		/// returned earlier as part of a
		/// <see cref="ModuleSource">ModuleSource</see>
		/// . It can be used to
		/// validate the existing cached module and avoid reloading it.
		/// </param>
		/// <returns>
		/// a script representing the code of the module. Null should be
		/// returned if the script is not found.
		/// <see cref="NOT_MODIFIED">NOT_MODIFIED</see>
		/// should be
		/// returned if the passed validator validates the current representation of
		/// the module (the currently cached module script).
		/// </returns>
		/// <exception cref="System.IO.IOException">if there was an I/O problem reading the script</exception>
		/// <exception cref="Sharpen.URISyntaxException">if the final URI could not be constructed</exception>
		ModuleSource LoadSource(Uri uri, Uri baseUri, object validator);
	}

	public static class ModuleSourceProviderConstants
	{
		/// <summary>
		/// A special return value for
		/// <see cref="LoadSource(string, Rhino.Scriptable, object)">LoadSource(string, Rhino.Scriptable, object)</see>
		/// and
		/// <see cref="LoadSource(System.Uri, System.Uri, object)">LoadSource(System.Uri, System.Uri, object)</see>
		/// that signifies that the
		/// cached representation is still valid according to the passed validator.
		/// </summary>
		public const ModuleSource NOT_MODIFIED = new ModuleSource(null, null, null, null, null);
	}
}
