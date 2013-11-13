/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino.Commonjs.Module
{
	/// <summary>Represents a compiled CommonJS module script.</summary>
	/// <remarks>
	/// Represents a compiled CommonJS module script. The
	/// <see cref="Require">Require</see>
	/// functions
	/// use them and obtain them through a
	/// <see cref="ModuleScriptProvider">ModuleScriptProvider</see>
	/// . Instances
	/// are immutable.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ModuleScript.java,v 1.3 2011/04/07 20:26:11 hannes%helma.at Exp $</version>
	[System.Serializable]
	public class ModuleScript
	{
		private const long serialVersionUID = 1L;

		private readonly Script script;

		private readonly Uri uri;

		private readonly Uri @base;

		/// <summary>Creates a new CommonJS module.</summary>
		/// <remarks>Creates a new CommonJS module.</remarks>
		/// <param name="script">the script representing the code of the module.</param>
		/// <param name="uri">the URI of the module.</param>
		/// <param name="base">the base URI, or null.</param>
		public ModuleScript(Script script, Uri uri, Uri @base)
		{
			this.script = script;
			this.uri = uri;
			this.@base = @base;
		}

		/// <summary>Returns the script object representing the code of the module.</summary>
		/// <remarks>Returns the script object representing the code of the module.</remarks>
		/// <returns>the script object representing the code of the module.</returns>
		public virtual Script GetScript()
		{
			return script;
		}

		/// <summary>Returns the URI of the module.</summary>
		/// <remarks>Returns the URI of the module.</remarks>
		/// <returns>the URI of the module.</returns>
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
		/// Returns true if this script has a base URI and has a source URI that
		/// is contained within that base URI.
		/// </summary>
		/// <remarks>
		/// Returns true if this script has a base URI and has a source URI that
		/// is contained within that base URI.
		/// </remarks>
		/// <returns>true if this script is contained within its sandbox base URI.</returns>
		public virtual bool IsSandboxed()
		{
			return @base != null && uri != null && !@base.MakeRelativeUri(uri).IsAbsoluteUri;
		}
	}
}
