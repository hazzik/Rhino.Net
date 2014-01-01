/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using Rhino.CommonJS.Module;
using Rhino.CommonJS.Module.Provider;
using Sharpen;

namespace Rhino.CommonJS.Module.Provider
{
	/// <summary>
	/// A module script provider that uses a module source provider to load modules
	/// and caches the loaded modules.
	/// </summary>
	/// <remarks>
	/// A module script provider that uses a module source provider to load modules
	/// and caches the loaded modules. It strongly references the loaded modules,
	/// thus a module once loaded will not be eligible for garbage collection before
	/// the module provider itself becomes eligible.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: StrongCachingModuleScriptProvider.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[System.Serializable]
	public class StrongCachingModuleScriptProvider : CachingModuleScriptProviderBase
	{
		private const long serialVersionUID = 1L;

		private readonly IDictionary<string, CachingModuleScriptProviderBase.CachedModuleScript> modules = new ConcurrentHashMap<string, CachingModuleScriptProviderBase.CachedModuleScript>(16, .75f, GetConcurrencyLevel());

		/// <summary>Creates a new module provider with the specified module source provider.</summary>
		/// <remarks>Creates a new module provider with the specified module source provider.</remarks>
		/// <param name="moduleSourceProvider">provider for modules' source code</param>
		public StrongCachingModuleScriptProvider(ModuleSourceProvider moduleSourceProvider) : base(moduleSourceProvider)
		{
		}

		protected internal override CachingModuleScriptProviderBase.CachedModuleScript GetLoadedModule(string moduleId)
		{
			return modules.Get(moduleId);
		}

		protected internal override void PutLoadedModule(string moduleId, ModuleScript moduleScript, object validator)
		{
			modules [moduleId] = new CachingModuleScriptProviderBase.CachedModuleScript(moduleScript, validator);
		}
	}
}
