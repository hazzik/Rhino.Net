/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino;
using Rhino.CommonJS.Module;
using Sharpen;

namespace Rhino.CommonJS.Module.Provider
{
	/// <summary>A multiplexer for module script providers.</summary>
	/// <remarks>A multiplexer for module script providers.</remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: MultiModuleScriptProvider.java,v 1.4 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	public class MultiModuleScriptProvider : ModuleScriptProvider
	{
		private readonly ModuleScriptProvider[] providers;

		/// <summary>
		/// Creates a new multiplexing module script provider tht gathers the
		/// specified providers
		/// </summary>
		/// <param name="providers">the providers to multiplex.</param>
		public MultiModuleScriptProvider(IEnumerable<ModuleScriptProvider> providers)
		{
			IList<ModuleScriptProvider> l = new List<ModuleScriptProvider>();
			foreach (ModuleScriptProvider provider in providers)
			{
				l.Add(provider);
			}
			this.providers = Sharpen.Collections.ToArray(l, new ModuleScriptProvider[l.Count]);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual ModuleScript GetModuleScript(Context cx, string moduleId, Uri uri, Uri @base, Scriptable paths)
		{
			foreach (ModuleScriptProvider provider in providers)
			{
				ModuleScript script = provider.GetModuleScript(cx, moduleId, uri, @base, paths);
				if (script != null)
				{
					return script;
				}
			}
			return null;
		}
	}
}
