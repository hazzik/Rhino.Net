/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
	/// and caches the loaded modules. It softly references the loaded modules'
	/// Rhino
	/// <see cref="Rhino.Script">Rhino.Script</see>
	/// objects, thus a module once loaded can become eligible
	/// for garbage collection if it is otherwise unused under memory pressure.
	/// Instances of this class are thread safe.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: SoftCachingModuleScriptProvider.java,v 1.3 2011/04/07 20:26:12 hannes%helma.at Exp $</version>
	[Serializable]
	public class SoftCachingModuleScriptProvider : CachingModuleScriptProviderBase
	{
		[NonSerialized]
		private ReferenceQueue<Script> scriptRefQueue = new ReferenceQueue<Script>();

		[NonSerialized]
		private ConcurrentMap<string, ScriptReference> scripts = new ConcurrentHashMap<string, ScriptReference>(16, .75f, GetConcurrencyLevel());

		/// <summary>Creates a new module provider with the specified module source provider.</summary>
		/// <remarks>Creates a new module provider with the specified module source provider.</remarks>
		/// <param name="moduleSourceProvider">provider for modules' source code</param>
		public SoftCachingModuleScriptProvider(ModuleSourceProvider moduleSourceProvider) : base(moduleSourceProvider)
		{
		}

		/// <exception cref="System.Exception"></exception>
		public override ModuleScript GetModuleScript(Context cx, string moduleId, Uri uri, Uri @base, Scriptable paths)
		{
			// Overridden to clear the reference queue before retrieving the
			// script.
			for (; ; )
			{
				var @ref = (ScriptReference) scriptRefQueue.Poll();
				if (@ref == null)
				{
					break;
				}
				scripts.Remove(@ref.GetModuleId(), @ref);
			}
			return base.GetModuleScript(cx, moduleId, uri, @base, paths);
		}

		protected internal override CachedModuleScript GetLoadedModule(string moduleId)
		{
			ScriptReference scriptRef = scripts.GetValueOrDefault(moduleId);
			return scriptRef != null ? scriptRef.GetCachedModuleScript() : null;
		}

		protected internal override void PutLoadedModule(string moduleId, ModuleScript moduleScript, object validator)
		{
			scripts[moduleId] = new ScriptReference(moduleScript.GetScript(), moduleId, moduleScript.GetUri(), moduleScript.GetBase(), validator, scriptRefQueue);
		}

		private class ScriptReference : SoftReference<Script>
		{
			private readonly string moduleId;

			private readonly Uri uri;

			private readonly Uri @base;

			private readonly object validator;

			internal ScriptReference(Script script, string moduleId, Uri uri, Uri @base, object validator, ReferenceQueue<Script> refQueue) : base(script, refQueue)
			{
				this.moduleId = moduleId;
				this.uri = uri;
				this.@base = @base;
				this.validator = validator;
			}

			internal virtual CachedModuleScript GetCachedModuleScript()
			{
				Script script = Get();
				if (script == null)
				{
					return null;
				}
				return new CachedModuleScript(new ModuleScript(script, uri, @base), validator);
			}

			internal virtual string GetModuleId()
			{
				return moduleId;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		[OnDeserializing]
		private void ReadObject(ObjectInputStream @in)
		{
			scriptRefQueue = new ReferenceQueue<Script>();
			scripts = new ConcurrentHashMap<string, ScriptReference>();
			IDictionary<string, CachedModuleScript> serScripts = (IDictionary<string, CachedModuleScript>) @in.ReadObject();
			foreach (KeyValuePair<string, CachedModuleScript> entry in serScripts)
			{
				CachedModuleScript cachedModuleScript = entry.Value;
				PutLoadedModule(entry.Key, cachedModuleScript.GetModule(), cachedModuleScript.GetValidator());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			IDictionary<string, CachedModuleScript> serScripts = new Dictionary<string, CachedModuleScript>();
			foreach (KeyValuePair<string, ScriptReference> entry in scripts)
			{
				CachedModuleScript cachedModuleScript = entry.Value.GetCachedModuleScript();
				if (cachedModuleScript != null)
				{
					serScripts[entry.Key] = cachedModuleScript;
				}
			}
			@out.WriteObject(serScripts);
		}
	}
}
