/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.CommonJS.Module;
using Sharpen;

namespace Rhino.CommonJS.Module
{
	/// <summary>
	/// Should be implemented by Rhino embeddings to allow the require() function to
	/// obtain
	/// <see cref="ModuleScript">ModuleScript</see>
	/// objects. We provide two default implementations,
	/// but you can of course roll your own if they don't suit your needs.
	/// </summary>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ModuleScriptProvider.java,v 1.4 2011/04/07 20:26:11 hannes%helma.at Exp $</version>
	public interface ModuleScriptProvider
	{
		/// <summary>Returns a module script.</summary>
		/// <remarks>
		/// Returns a module script. It should attempt to load the module script if
		/// it is not already available to it, or return an already loaded module
		/// script instance if it is available to it.
		/// </remarks>
		/// <param name="cx">current context. Can be used to compile module scripts.</param>
		/// <param name="moduleId">
		/// the ID of the module. An implementation must only accept
		/// an absolute ID, starting with a term.
		/// </param>
		/// <param name="moduleUri">
		/// the URI of the module. If this is not null, resolution
		/// of <code>moduleId</code> is bypassed and the script is directly loaded
		/// from <code>moduleUri</code>
		/// </param>
		/// <param name="baseUri">
		/// the module path base URI from which <code>moduleUri</code>
		/// was derived.
		/// </param>
		/// <param name="paths">
		/// the value of the require() function's "paths" attribute. If
		/// the require() function is sandboxed, it will be null, otherwise it will
		/// be a JavaScript Array object. It is up to the provider implementation
		/// whether and how it wants to honor the contents of the array.
		/// </param>
		/// <returns>
		/// a module script representing the compiled code of the module.
		/// Null should be returned if the script could not found.
		/// </returns>
		/// <exception cref="System.Exception">
		/// if there was an unrecoverable problem obtaining the
		/// script
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// if the module ID is syntactically not a
		/// valid absolute module identifier.
		/// </exception>
		ModuleScript GetModuleScript(Context cx, string moduleId, Uri moduleUri, Uri baseUri, Scriptable paths);
	}
}
