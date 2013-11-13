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
	/// <summary>
	/// A base implementation for all module script providers that actually load
	/// module scripts.
	/// </summary>
	/// <remarks>
	/// A base implementation for all module script providers that actually load
	/// module scripts. Performs validation of identifiers, allows loading from
	/// preferred locations (attempted before require.paths), from require.paths
	/// itself, and from fallback locations (attempted after require.paths). Note
	/// that while this base class strives to be as generic as possible, it does
	/// have loading from an URI built into its design, for the simple reason that
	/// the require.paths is defined in terms of URIs.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ModuleSourceProviderBase.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[System.Serializable]
	public abstract class ModuleSourceProviderBase : ModuleSourceProvider
	{
		private const long serialVersionUID = 1L;

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.URISyntaxException"></exception>
		public virtual ModuleSource LoadSource(string moduleId, Scriptable paths, object validator)
		{
			if (!EntityNeedsRevalidation(validator))
			{
				return ModuleSourceProviderConstants.NOT_MODIFIED;
			}
			ModuleSource moduleSource = LoadFromPrivilegedLocations(moduleId, validator);
			if (moduleSource != null)
			{
				return moduleSource;
			}
			if (paths != null)
			{
				moduleSource = LoadFromPathArray(moduleId, paths, validator);
				if (moduleSource != null)
				{
					return moduleSource;
				}
			}
			return LoadFromFallbackLocations(moduleId, validator);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.URISyntaxException"></exception>
		public virtual ModuleSource LoadSource(Uri uri, Uri @base, object validator)
		{
			return LoadFromUri(uri, @base, validator);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ModuleSource LoadFromPathArray(string moduleId, Scriptable paths, object validator)
		{
			long llength = ScriptRuntime.ToUint32(ScriptableObject.GetProperty(paths, "length"));
			// Yeah, I'll ignore entries beyond Integer.MAX_VALUE; so sue me.
			int ilength = llength > int.MaxValue ? int.MaxValue : (int)llength;
			for (int i = 0; i < ilength; ++i)
			{
				string path = EnsureTrailingSlash(ScriptableObject.GetTypedProperty<string>(paths, i));
				try
				{
					Uri uri = new Uri(path);
					if (!uri.IsAbsoluteUri)
					{
						uri = new FilePath(path).ToURI().Resolve(string.Empty);
					}
					ModuleSource moduleSource = LoadFromUri(uri.Resolve(moduleId), uri, validator);
					if (moduleSource != null)
					{
						return moduleSource;
					}
				}
				catch (URISyntaxException e)
				{
					throw new UriFormatException(e.Message);
				}
			}
			return null;
		}

		private static string EnsureTrailingSlash(string path)
		{
			return path.EndsWith("/") ? path : System.String.Concat(path, "/");
		}

		/// <summary>
		/// Override to determine whether according to the validator, the cached
		/// module script needs revalidation.
		/// </summary>
		/// <remarks>
		/// Override to determine whether according to the validator, the cached
		/// module script needs revalidation. A validator can carry expiry
		/// information. If the cached representation is not expired, it doesn'
		/// t need revalidation, otherwise it does. When no cache revalidation is
		/// required, the external resource will not be contacted at all, so some
		/// level of expiry (staleness tolerance) can greatly enhance performance.
		/// The default implementation always returns true so it will always require
		/// revalidation.
		/// </remarks>
		/// <param name="validator">the validator</param>
		/// <returns>returns true if the cached module needs revalidation.</returns>
		protected internal virtual bool EntityNeedsRevalidation(object validator)
		{
			return true;
		}

		/// <summary>Override in a subclass to load a module script from a logical URI.</summary>
		/// <remarks>
		/// Override in a subclass to load a module script from a logical URI. The
		/// URI is absolute but does not have a file name extension such as ".js".
		/// It is up to the ModuleSourceProvider implementation to add such an
		/// extension.
		/// </remarks>
		/// <param name="uri">the URI of the script, without file name extension.</param>
		/// <param name="base">the base URI the uri was resolved from.</param>
		/// <param name="validator">
		/// a validator that can be used to revalidate an existing
		/// cached source at the URI. Can be null if there is no cached source
		/// available.
		/// </param>
		/// <returns>
		/// the loaded module script, or null if it can't be found, or
		/// <see cref="ModuleSourceProvider.NOT_MODIFIED">ModuleSourceProvider.NOT_MODIFIED</see>
		/// if it revalidated the existing
		/// cached source against the URI.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// if the module script was found, but an I/O exception
		/// prevented it from being loaded.
		/// </exception>
		/// <exception cref="Sharpen.URISyntaxException">if the final URI could not be constructed</exception>
		protected internal abstract ModuleSource LoadFromUri(Uri uri, Uri @base, object validator);

		/// <summary>Override to obtain a module source from privileged locations.</summary>
		/// <remarks>
		/// Override to obtain a module source from privileged locations. This will
		/// be called before source is attempted to be obtained from URIs specified
		/// in require.paths.
		/// </remarks>
		/// <param name="moduleId">the ID of the module</param>
		/// <param name="validator">
		/// a validator that can be used to validate an existing
		/// cached script. Can be null if there is no cached script available.
		/// </param>
		/// <returns>
		/// the loaded module script, or null if it can't be found in the
		/// privileged locations, or
		/// <see cref="ModuleSourceProvider.NOT_MODIFIED">ModuleSourceProvider.NOT_MODIFIED</see>
		/// if
		/// the existing cached module script is still valid.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// if the module script was found, but an I/O exception
		/// prevented it from being loaded.
		/// </exception>
		/// <exception cref="Sharpen.URISyntaxException">if the final URI could not be constructed.</exception>
		protected internal virtual ModuleSource LoadFromPrivilegedLocations(string moduleId, object validator)
		{
			return null;
		}

		/// <summary>Override to obtain a module source from fallback locations.</summary>
		/// <remarks>
		/// Override to obtain a module source from fallback locations. This will
		/// be called after source is attempted to be obtained from URIs specified
		/// in require.paths.
		/// </remarks>
		/// <param name="moduleId">the ID of the module</param>
		/// <param name="validator">
		/// a validator that can be used to validate an existing
		/// cached script. Can be null if there is no cached script available.
		/// </param>
		/// <returns>
		/// the loaded module script, or null if it can't be found in the
		/// privileged locations, or
		/// <see cref="ModuleSourceProvider.NOT_MODIFIED">ModuleSourceProvider.NOT_MODIFIED</see>
		/// if
		/// the existing cached module script is still valid.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// if the module script was found, but an I/O exception
		/// prevented it from being loaded.
		/// </exception>
		/// <exception cref="Sharpen.URISyntaxException">if the final URI could not be constructed.</exception>
		protected internal virtual ModuleSource LoadFromFallbackLocations(string moduleId, object validator)
		{
			return null;
		}
	}
}
