/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Rhino;
using Rhino.Commonjs.Module;
using Rhino.Commonjs.Module.Provider;
using Sharpen;

namespace Rhino.Commonjs.Module.Provider
{
	/// <summary>Abstract base class that implements caching of loaded module scripts.</summary>
	/// <remarks>
	/// Abstract base class that implements caching of loaded module scripts. It
	/// uses a
	/// <see cref="ModuleSourceProvider">ModuleSourceProvider</see>
	/// to obtain the source text of the
	/// scripts. It supports a cache revalidation mechanism based on validator
	/// objects returned from the
	/// <see cref="ModuleSourceProvider">ModuleSourceProvider</see>
	/// . Instances of this
	/// class and its subclasses are thread safe (and written to perform decently
	/// under concurrent access).
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: CachingModuleScriptProviderBase.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[System.Serializable]
	public abstract class CachingModuleScriptProviderBase : ModuleScriptProvider
	{
		private const long serialVersionUID = 1L;

		private static readonly int loadConcurrencyLevel = Runtime.GetRuntime().AvailableProcessors() * 8;

		private static readonly int loadLockShift;

		private static readonly int loadLockMask;

		private static readonly int loadLockCount;

		static CachingModuleScriptProviderBase()
		{
			int sshift = 0;
			int ssize = 1;
			while (ssize < loadConcurrencyLevel)
			{
				++sshift;
				ssize <<= 1;
			}
			loadLockShift = 32 - sshift;
			loadLockMask = ssize - 1;
			loadLockCount = ssize;
		}

		private readonly object[] loadLocks = new object[loadLockCount];

		private readonly ModuleSourceProvider moduleSourceProvider;

		/// <summary>Creates a new module script provider with the specified source.</summary>
		/// <remarks>Creates a new module script provider with the specified source.</remarks>
		/// <param name="moduleSourceProvider">provider for modules' source code</param>
		protected internal CachingModuleScriptProviderBase(ModuleSourceProvider moduleSourceProvider)
		{
			{
				for (int i = 0; i < loadLocks.Length; ++i)
				{
					loadLocks[i] = new object();
				}
			}
			this.moduleSourceProvider = moduleSourceProvider;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual ModuleScript GetModuleScript(Context cx, string moduleId, Uri moduleUri, Uri baseUri, Scriptable paths)
		{
			CachingModuleScriptProviderBase.CachedModuleScript cachedModule1 = GetLoadedModule(moduleId);
			object validator1 = GetValidator(cachedModule1);
			ModuleSource moduleSource = (moduleUri == null) ? moduleSourceProvider.LoadSource(moduleId, paths, validator1) : moduleSourceProvider.LoadSource(moduleUri, baseUri, validator1);
			if (moduleSource == ModuleSourceProviderConstants.NOT_MODIFIED)
			{
				return cachedModule1.GetModule();
			}
			if (moduleSource == null)
			{
				return null;
			}
			TextReader reader = moduleSource.GetReader();
			try
			{
				int idHash = moduleId.GetHashCode();
				lock (loadLocks[((int)(((uint)idHash) >> loadLockShift)) & loadLockMask])
				{
					CachingModuleScriptProviderBase.CachedModuleScript cachedModule2 = GetLoadedModule(moduleId);
					if (cachedModule2 != null)
					{
						if (!Equal(validator1, GetValidator(cachedModule2)))
						{
							return cachedModule2.GetModule();
						}
					}
					Uri sourceUri = moduleSource.GetUri();
					ModuleScript moduleScript = new ModuleScript(cx.CompileReader(reader, sourceUri.ToString(), 1, moduleSource.GetSecurityDomain()), sourceUri, moduleSource.GetBase());
					PutLoadedModule(moduleId, moduleScript, moduleSource.GetValidator());
					return moduleScript;
				}
			}
			finally
			{
				reader.Close();
			}
		}

		/// <summary>
		/// Store a loaded module script for later retrieval using
		/// <see cref="GetLoadedModule(string)">GetLoadedModule(string)</see>
		/// .
		/// </summary>
		/// <param name="moduleId">the ID of the module</param>
		/// <param name="moduleScript">the module script</param>
		/// <param name="validator">the validator for the module's source text entity</param>
		protected internal abstract void PutLoadedModule(string moduleId, ModuleScript moduleScript, object validator);

		/// <summary>
		/// Retrieves an already loaded moduleScript stored using
		/// <see cref="PutLoadedModule(string, Rhino.Commonjs.Module.ModuleScript, object)">PutLoadedModule(string, Rhino.Commonjs.Module.ModuleScript, object)</see>
		/// .
		/// </summary>
		/// <param name="moduleId">the ID of the module</param>
		/// <returns>a cached module script, or null if the module is not loaded.</returns>
		protected internal abstract CachingModuleScriptProviderBase.CachedModuleScript GetLoadedModule(string moduleId);

		/// <summary>Instances of this class represent a loaded and cached module script.</summary>
		/// <remarks>Instances of this class represent a loaded and cached module script.</remarks>
		/// <author>Attila Szegedi</author>
		/// <version>$Id: CachingModuleScriptProviderBase.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
		public class CachedModuleScript
		{
			private readonly ModuleScript moduleScript;

			private readonly object validator;

			/// <summary>Creates a new cached module script.</summary>
			/// <remarks>Creates a new cached module script.</remarks>
			/// <param name="moduleScript">the module script itself</param>
			/// <param name="validator">
			/// a validator for the moduleScript's source text
			/// entity.
			/// </param>
			public CachedModuleScript(ModuleScript moduleScript, object validator)
			{
				this.moduleScript = moduleScript;
				this.validator = validator;
			}

			/// <summary>Returns the module script.</summary>
			/// <remarks>Returns the module script.</remarks>
			/// <returns>the module script.</returns>
			internal virtual ModuleScript GetModule()
			{
				return moduleScript;
			}

			/// <summary>Returns the validator for the module script's source text entity.</summary>
			/// <remarks>Returns the validator for the module script's source text entity.</remarks>
			/// <returns>the validator for the module script's source text entity.</returns>
			internal virtual object GetValidator()
			{
				return validator;
			}
		}

		private static object GetValidator(CachingModuleScriptProviderBase.CachedModuleScript cachedModule)
		{
			return cachedModule == null ? null : cachedModule.GetValidator();
		}

		private static bool Equal(object o1, object o2)
		{
			return o1 == null ? o2 == null : o1.Equals(o2);
		}

		/// <summary>Returns the internal concurrency level utilized by caches in this JVM.</summary>
		/// <remarks>Returns the internal concurrency level utilized by caches in this JVM.</remarks>
		/// <returns>the internal concurrency level utilized by caches in this JVM.</returns>
		protected internal static int GetConcurrencyLevel()
		{
			return loadLockCount;
		}
	}
}
