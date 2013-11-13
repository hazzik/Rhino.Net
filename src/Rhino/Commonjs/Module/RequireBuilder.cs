/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Commonjs.Module;
using Sharpen;

namespace Rhino.Commonjs.Module
{
	/// <summary>
	/// A builder for
	/// <see cref="Require">Require</see>
	/// instances. Useful when you're creating many
	/// instances of
	/// <see cref="Require">Require</see>
	/// that are identical except for their top-level
	/// scope and current
	/// <see cref="Rhino.Context">Rhino.Context</see>
	/// . Also useful if you prefer configuring it
	/// using named setters instead of passing many parameters in a constructor.
	/// Every setter returns "this", so you can easily chain their invocations for
	/// additional convenience.
	/// </summary>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: RequireBuilder.java,v 1.4 2011/04/07 20:26:11 hannes%helma.at Exp $</version>
	[System.Serializable]
	public class RequireBuilder
	{
		private const long serialVersionUID = 1L;

		private bool sandboxed = true;

		private ModuleScriptProvider moduleScriptProvider;

		private Script preExec;

		private Script postExec;

		/// <summary>
		/// Sets the
		/// <see cref="ModuleScriptProvider">ModuleScriptProvider</see>
		/// for the
		/// <see cref="Require">Require</see>
		/// instances
		/// that this builder builds.
		/// </summary>
		/// <param name="moduleScriptProvider">
		/// the module script provider for the
		/// <see cref="Require">Require</see>
		/// instances that this builder builds.
		/// </param>
		/// <returns>this, so you can chain ("fluidize") setter invocations</returns>
		public virtual RequireBuilder SetModuleScriptProvider(ModuleScriptProvider moduleScriptProvider)
		{
			this.moduleScriptProvider = moduleScriptProvider;
			return this;
		}

		/// <summary>
		/// Sets the script that should execute in every module's scope after the
		/// module's own script has executed.
		/// </summary>
		/// <remarks>
		/// Sets the script that should execute in every module's scope after the
		/// module's own script has executed.
		/// </remarks>
		/// <param name="postExec">the post-exec script.</param>
		/// <returns>this, so you can chain ("fluidize") setter invocations</returns>
		public virtual RequireBuilder SetPostExec(Script postExec)
		{
			this.postExec = postExec;
			return this;
		}

		/// <summary>
		/// Sets the script that should execute in every module's scope before the
		/// module's own script has executed.
		/// </summary>
		/// <remarks>
		/// Sets the script that should execute in every module's scope before the
		/// module's own script has executed.
		/// </remarks>
		/// <param name="preExec">the pre-exec script.</param>
		/// <returns>this, so you can chain ("fluidize") setter invocations</returns>
		public virtual RequireBuilder SetPreExec(Script preExec)
		{
			this.preExec = preExec;
			return this;
		}

		/// <summary>Sets whether the created require() instances will be sandboxed.</summary>
		/// <remarks>
		/// Sets whether the created require() instances will be sandboxed.
		/// See
		/// <see cref="Require.Require(Rhino.Context, Rhino.Scriptable, ModuleScriptProvider, Rhino.Script, Rhino.Script, bool)">Require.Require(Rhino.Context, Rhino.Scriptable, ModuleScriptProvider, Rhino.Script, Rhino.Script, bool)</see>
		/// for explanation.
		/// </remarks>
		/// <param name="sandboxed">
		/// true if the created require() instances will be
		/// sandboxed.
		/// </param>
		/// <returns>this, so you can chain ("fluidize") setter invocations</returns>
		public virtual RequireBuilder SetSandboxed(bool sandboxed)
		{
			this.sandboxed = sandboxed;
			return this;
		}

		/// <summary>Creates a new require() function.</summary>
		/// <remarks>
		/// Creates a new require() function. You are still responsible for invoking
		/// either
		/// <see cref="Require.Install(Rhino.Scriptable)">Require.Install(Rhino.Scriptable)</see>
		/// or
		/// <see cref="Require.RequireMain(Rhino.Context, string)">Require.RequireMain(Rhino.Context, string)</see>
		/// to effectively make it
		/// available to its JavaScript program.
		/// </remarks>
		/// <param name="cx">the current context</param>
		/// <param name="globalScope">the global scope containing the JS standard natives.</param>
		/// <returns>a new Require instance.</returns>
		public virtual Require CreateRequire(Context cx, Scriptable globalScope)
		{
			return new Require(cx, globalScope, moduleScriptProvider, preExec, postExec, sandboxed);
		}
	}
}
