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

namespace Rhino.CommonJS.Module
{
	/// <summary>A top-level module scope.</summary>
	/// <remarks>
	/// A top-level module scope. This class provides methods to retrieve the
	/// module's source and base URIs in order to resolve relative module IDs
	/// and check sandbox constraints.
	/// </remarks>
	[System.Serializable]
	public class ModuleScope : TopLevel
	{
		private const long serialVersionUID = 1L;

		private readonly Uri uri;

		private readonly Uri @base;

		public ModuleScope(Scriptable prototype, Uri uri, Uri @base)
		{
			this.uri = uri;
			this.@base = @base;
			SetPrototype(prototype);
			CacheBuiltins();
		}

		public virtual Uri GetUri()
		{
			return uri;
		}

		public virtual Uri GetBase()
		{
			return @base;
		}
	}
}
