/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Rhino;
using Rhino.CommonJS.Module;
using Sharpen;

namespace Rhino.CommonJS.Module
{
	/// <summary>
	/// Implements the require() function as defined by
	/// <a href="http://wiki.commonjs.org/wiki/Modules/1.1">Common JS modules</a>.
	/// </summary>
	/// <remarks>
	/// Implements the require() function as defined by
	/// <a href="http://wiki.commonjs.org/wiki/Modules/1.1">Common JS modules</a>.
	/// <h1>Thread safety</h1>
	/// You will ordinarily create one instance of require() for every top-level
	/// scope. This ordinarily means one instance per program execution, except if
	/// you use shared top-level scopes and installing most objects into them.
	/// Module loading is thread safe, so using a single require() in a shared
	/// top-level scope is also safe.
	/// <h1>Creation</h1>
	/// If you need to create many otherwise identical require() functions for
	/// different scopes, you might want to use
	/// <see cref="RequireBuilder">RequireBuilder</see>
	/// for
	/// convenience.
	/// <h1>Making it available</h1>
	/// In order to make the require() function available to your JavaScript
	/// program, you need to invoke either
	/// <see cref="Install(Scriptable)">Install(Rhino.Scriptable)</see>
	/// or
	/// <see cref="RequireMain(Rhino.Context, string)">RequireMain(Rhino.Context, string)</see>
	/// .
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: Require.java,v 1.4 2011/04/07 20:26:11 hannes%helma.at Exp $</version>
	[System.Serializable]
	public class Require : BaseFunction
	{
		private readonly ModuleScriptProvider moduleScriptProvider;

		private readonly Scriptable nativeScope;

		private readonly Scriptable paths;

		private readonly bool sandboxed;

		private readonly Script preExec;

		private readonly Script postExec;

		private string mainModuleId = null;

		private Scriptable mainExports;

		private readonly IDictionary<string, Scriptable> exportedModuleInterfaces = new ConcurrentHashMap<string, Scriptable>();

		private readonly object loadLock = new object();

		private static readonly ThreadLocal<IDictionary<string, Scriptable>> loadingModuleInterfaces = new ThreadLocal<IDictionary<string, Scriptable>>();

		/// <summary>Creates a new instance of the require() function.</summary>
		/// <remarks>
		/// Creates a new instance of the require() function. Upon constructing it,
		/// you will either want to install it in the global (or some other) scope
		/// using
		/// <see cref="Install(Scriptable)">Install(Rhino.Scriptable)</see>
		/// , or alternatively, you can load the
		/// program's main module using
		/// <see cref="RequireMain(Rhino.Context, string)">RequireMain(Rhino.Context, string)</see>
		/// and
		/// then act on the main module's exports.
		/// </remarks>
		/// <param name="cx">the current context</param>
		/// <param name="nativeScope">
		/// a scope that provides the standard native JavaScript
		/// objects.
		/// </param>
		/// <param name="moduleScriptProvider">a provider for module scripts</param>
		/// <param name="preExec">
		/// an optional script that is executed in every module's
		/// scope before its module script is run.
		/// </param>
		/// <param name="postExec">
		/// an optional script that is executed in every module's
		/// scope after its module script is run.
		/// </param>
		/// <param name="sandboxed">
		/// if set to true, the require function will be sandboxed.
		/// This means that it doesn't have the "paths" property, and also that the
		/// modules it loads don't export the "module.uri" property.
		/// </param>
		public Require(Context cx, Scriptable nativeScope, ModuleScriptProvider moduleScriptProvider, Script preExec, Script postExec, bool sandboxed)
		{
			// Modules that completed loading; visible to all threads
			// Modules currently being loaded on the thread. Used to resolve circular
			// dependencies while loading.
			this.moduleScriptProvider = moduleScriptProvider;
			this.nativeScope = nativeScope;
			this.sandboxed = sandboxed;
			this.preExec = preExec;
			this.postExec = postExec;
			Prototype = ScriptableObject.GetFunctionPrototype(nativeScope);
			if (!sandboxed)
			{
				paths = cx.NewArray(nativeScope, 0);
				DefineReadOnlyProperty(this, "paths", paths);
			}
			else
			{
				paths = null;
			}
		}

		/// <summary>
		/// Calling this method establishes a module as being the main module of the
		/// program to which this require() instance belongs.
		/// </summary>
		/// <remarks>
		/// Calling this method establishes a module as being the main module of the
		/// program to which this require() instance belongs. The module will be
		/// loaded as if require()'d and its "module" property will be set as the
		/// "main" property of this require() instance. You have to call this method
		/// before the module has been loaded (that is, the call to this method must
		/// be the first to require the module and thus trigger its loading). Note
		/// that the main module will execute in its own scope and not in the global
		/// scope. Since all other modules see the global scope, executing the main
		/// module in the global scope would open it for tampering by other modules.
		/// </remarks>
		/// <param name="cx">the current context</param>
		/// <param name="mainModuleId">the ID of the main module</param>
		/// <returns>the "exports" property of the main module</returns>
		/// <exception cref="System.InvalidOperationException">
		/// if the main module is already loaded when
		/// required, or if this require() instance already has a different main
		/// module set.
		/// </exception>
		public virtual Scriptable RequireMain(Context cx, string mainModuleId)
		{
			if (this.mainModuleId != null)
			{
				if (!this.mainModuleId.Equals(mainModuleId))
				{
					throw new InvalidOperationException("Main module already set to " + this.mainModuleId);
				}
				return mainExports;
			}
			// try to get the module script to see if it is on the module path
			ModuleScript moduleScript = moduleScriptProvider.GetModuleScript(cx, mainModuleId, null, null, paths);
			if (moduleScript != null)
			{
				mainExports = GetExportedModuleInterface(cx, mainModuleId, null, null, true);
			}
			else
			{
				if (!sandboxed)
				{
					Uri mainUri = null;
					// try to resolve to an absolute URI or file path
					try
					{
						mainUri = new Uri(mainModuleId);
					}
					catch (URISyntaxException)
					{
					}
					// fall through
					// if not an absolute uri resolve to a file path
					if (mainUri == null || !mainUri.IsAbsoluteUri)
					{
						FileInfo file = new FileInfo(mainModuleId);
						if (!file.Exists)
						{
							throw ScriptRuntime.ThrowError(cx, nativeScope, string.Format("Module \"{0}\" not found.", mainModuleId));
						}
						mainUri = new Uri(file.FullName);
					}
					mainExports = GetExportedModuleInterface(cx, mainUri.ToString(), mainUri, null, true);
				}
			}
			this.mainModuleId = mainModuleId;
			return mainExports;
		}

		/// <summary>
		/// Binds this instance of require() into the specified scope under the
		/// property name "require".
		/// </summary>
		/// <remarks>
		/// Binds this instance of require() into the specified scope under the
		/// property name "require".
		/// </remarks>
		/// <param name="scope">the scope where the require() function is to be installed.</param>
		public virtual void Install(Scriptable scope)
		{
			ScriptableObject.PutProperty(scope, "require", this);
		}

		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (args == null || args.Length < 1)
			{
				throw ScriptRuntime.ThrowError(cx, scope, "require() needs one argument");
			}
			string id = (string)Context.JsToJava(args[0], typeof(string));
			Uri uri = null;
			Uri @base = null;
			if (id.StartsWith("./") || id.StartsWith("../"))
			{
				var moduleScope = thisObj as ModuleScope;
				if (moduleScope == null)
				{
					throw ScriptRuntime.ThrowError(cx, scope, "Can't resolve relative module ID \"" + id + "\" when require() is used outside of a module");
				}

				@base = moduleScope.GetBase();
				Uri current = moduleScope.GetUri();
				uri = current.Resolve(id);
				if (@base == null)
				{
					// calling module is absolute, resolve to absolute URI
					// (but without file extension)
					id = uri.ToString();
				}
				else
				{
					// try to convert to a relative URI rooted on base
					id = @base.MakeRelativeUri(current).Resolve(id).ToString();
					if (id[0] == '.')
					{
						// resulting URI is not contained in base,
						// throw error or make absolute depending on sandbox flag.
						if (sandboxed)
						{
							throw ScriptRuntime.ThrowError(cx, scope, "Module \"" + id + "\" is not contained in sandbox.");
						}
						else
						{
							id = uri.ToString();
						}
					}
				}
			}
			return GetExportedModuleInterface(cx, id, uri, @base, false);
		}

		public override Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			throw ScriptRuntime.ThrowError(cx, scope, "require() can not be invoked as a constructor");
		}

		private Scriptable GetExportedModuleInterface(Context cx, string id, Uri uri, Uri @base, bool isMain)
		{
			// Check if the requested module is already completely loaded
			Scriptable exports = exportedModuleInterfaces.Get(id);
			if (exports != null)
			{
				if (isMain)
				{
					throw new InvalidOperationException("Attempt to set main module after it was loaded");
				}
				return exports;
			}
			// Check if it is currently being loaded on the current thread
			// (supporting circular dependencies).
			IDictionary<string, Scriptable> threadLoadingModules = loadingModuleInterfaces.Value;
			if (threadLoadingModules != null)
			{
				exports = threadLoadingModules.Get(id);
				if (exports != null)
				{
					return exports;
				}
			}
			// The requested module is neither already loaded, nor is it being
			// loaded on the current thread. End of fast path. We must synchronize
			// now, as we have to guarantee that at most one thread can load
			// modules at any one time. Otherwise, two threads could end up
			// attempting to load two circularly dependent modules in opposite
			// order, which would lead to either unacceptable non-determinism or
			// deadlock, depending on whether we underprotected or overprotected it
			// with locks.
			lock (loadLock)
			{
				// Recheck if it is already loaded - other thread might've
				// completed loading it just as we entered the synchronized block.
				exports = exportedModuleInterfaces.Get(id);
				if (exports != null)
				{
					return exports;
				}
				// Nope, still not loaded; we're loading it then.
				ModuleScript moduleScript = GetModule(cx, id, uri, @base);
				if (sandboxed && !moduleScript.IsSandboxed())
				{
					throw ScriptRuntime.ThrowError(cx, nativeScope, "Module \"" + id + "\" is not contained in sandbox.");
				}
				exports = cx.NewObject(nativeScope);
				// Are we the outermost locked invocation on this thread?
				bool outermostLocked = threadLoadingModules == null;
				if (outermostLocked)
				{
					threadLoadingModules = new Dictionary<string, Scriptable>();
					loadingModuleInterfaces.Value = threadLoadingModules;
				}
				// Must make the module exports available immediately on the
				// current thread, to satisfy the CommonJS Modules/1.1 requirement
				// that "If there is a dependency cycle, the foreign module may not
				// have finished executing at the time it is required by one of its
				// transitive dependencies; in this case, the object returned by
				// "require" must contain at least the exports that the foreign
				// module has prepared before the call to require that led to the
				// current module's execution."
				threadLoadingModules[id] = exports;
				try
				{
					// Support non-standard Node.js feature to allow modules to
					// replace the exports object by setting module.exports.
					Scriptable newExports = ExecuteModuleScript(cx, id, exports, moduleScript, isMain);
					if (exports != newExports)
					{
						threadLoadingModules[id] = newExports;
						exports = newExports;
					}
				}
				catch (Exception e)
				{
					// Throw loaded module away if there was an exception
					threadLoadingModules.Remove(id);
					throw;
				}
				finally
				{
					if (outermostLocked)
					{
						// Make loaded modules visible to other threads only after
						// the topmost triggering load has completed. This strategy
						// (compared to the one where we'd make each module
						// globally available as soon as it loads) prevents other
						// threads from observing a partially loaded circular
						// dependency of a module that completed loading.
						foreach (var val in threadLoadingModules)
							exportedModuleInterfaces[val.Key] = val.Value;
						loadingModuleInterfaces.Value = null;
					}
				}
			}
			return exports;
		}

		private Scriptable ExecuteModuleScript(Context cx, string id, Scriptable exports, ModuleScript moduleScript, bool isMain)
		{
			ScriptableObject moduleObject = (ScriptableObject)cx.NewObject(nativeScope);
			Uri uri = moduleScript.GetUri();
			Uri @base = moduleScript.GetBase();
			DefineReadOnlyProperty(moduleObject, "id", id);
			if (!sandboxed)
			{
				DefineReadOnlyProperty(moduleObject, "uri", uri.ToString());
			}
			Scriptable executionScope = new ModuleScope(nativeScope, uri, @base);
			// Set this so it can access the global JS environment objects.
			// This means we're currently using the "MGN" approach (ModuleScript
			// with Global Natives) as specified here:
			// <http://wiki.commonjs.org/wiki/Modules/ProposalForNativeExtension>
			executionScope.Put("exports", executionScope, exports);
			executionScope.Put("module", executionScope, moduleObject);
			moduleObject.Put("exports", moduleObject, exports);
			Install(executionScope);
			if (isMain)
			{
				DefineReadOnlyProperty(this, "main", moduleObject);
			}
			ExecuteOptionalScript(preExec, cx, executionScope);
			moduleScript.GetScript().Exec(cx, executionScope);
			ExecuteOptionalScript(postExec, cx, executionScope);
			return ScriptRuntime.ToObject(nativeScope, ScriptableObject.GetProperty(moduleObject, "exports"));
		}

		private static void ExecuteOptionalScript(Script script, Context cx, Scriptable executionScope)
		{
			if (script != null)
			{
				script.Exec(cx, executionScope);
			}
		}

		private static void DefineReadOnlyProperty(ScriptableObject obj, string name, object value)
		{
			ScriptableObject.PutProperty(obj, name, value);
			obj.SetAttributes(name, PropertyAttributes.READONLY | PropertyAttributes.PERMANENT);
		}

		private ModuleScript GetModule(Context cx, string id, Uri uri, Uri @base)
		{
			try
			{
				ModuleScript moduleScript = moduleScriptProvider.GetModuleScript(cx, id, uri, @base, paths);
				if (moduleScript == null)
				{
					throw ScriptRuntime.ThrowError(cx, nativeScope, "Module \"" + id + "\" not found.");
				}
				return moduleScript;
			}
			catch (Exception e)
			{
				throw Context.ThrowAsScriptRuntimeEx(e);
			}
		}

		public override string GetFunctionName()
		{
			return "require";
		}

		public override int Arity
		{
			get { return 1; }
		}

		public override int Length
		{
			get { return 1; }
		}
	}
}
