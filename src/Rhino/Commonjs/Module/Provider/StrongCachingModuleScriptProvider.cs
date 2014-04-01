/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino.Utils;
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
	[Serializable]
	public class StrongCachingModuleScriptProvider : CachingModuleScriptProviderBase
	{
		private readonly IDictionary<string, CachedModuleScript> modules = new ConcurrentHashMap<string, CachedModuleScript>(16, .75f, GetConcurrencyLevel());

		/// <summary>Creates a new module provider with the specified module source provider.</summary>
		/// <remarks>Creates a new module provider with the specified module source provider.</remarks>
		/// <param name="moduleSourceProvider">provider for modules' source code</param>
		public StrongCachingModuleScriptProvider(ModuleSourceProvider moduleSourceProvider) : base(moduleSourceProvider)
		{
		}

		protected internal override CachedModuleScript GetLoadedModule(string moduleId)
		{
			return modules.GetValueOrDefault(moduleId);
		}

		protected internal override void PutLoadedModule(string moduleId, ModuleScript moduleScript, object validator)
		{
			modules[moduleId] = new CachedModuleScript(moduleScript, validator);
		}
	}
}
