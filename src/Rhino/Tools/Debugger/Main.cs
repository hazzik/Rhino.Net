/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Java.Awt;
using Javax.Swing;
using Rhino;
using Rhino.Tools.Debugger;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Debugger
{
	/// <summary>Rhino script debugger main class.</summary>
	/// <remarks>
	/// Rhino script debugger main class.  This class links together a
	/// debugger object (
	/// <see cref="Dim">Dim</see>
	/// ) and a debugger GUI object (
	/// <see cref="SwingGui">SwingGui</see>
	/// ).
	/// </remarks>
	public class Main
	{
		/// <summary>The debugger.</summary>
		/// <remarks>The debugger.</remarks>
		private Dim dim;

		/// <summary>The debugger frame.</summary>
		/// <remarks>The debugger frame.</remarks>
		private SwingGui debugGui;

		/// <summary>Creates a new Main.</summary>
		/// <remarks>Creates a new Main.</remarks>
		public Main(string title)
		{
			dim = new Dim();
			debugGui = new SwingGui(dim, title);
		}

		/// <summary>
		/// Returns the debugger window
		/// <see cref="Javax.Swing.JFrame">Javax.Swing.JFrame</see>
		/// .
		/// </summary>
		public virtual JFrame GetDebugFrame()
		{
			return debugGui;
		}

		/// <summary>Breaks execution of the script.</summary>
		/// <remarks>Breaks execution of the script.</remarks>
		public virtual void DoBreak()
		{
			dim.SetBreak();
		}

		/// <summary>Sets whether execution should break when a script exception is thrown.</summary>
		/// <remarks>Sets whether execution should break when a script exception is thrown.</remarks>
		public virtual void SetBreakOnExceptions(bool value)
		{
			dim.SetBreakOnExceptions(value);
			debugGui.GetMenubar().GetBreakOnExceptions().SetSelected(value);
		}

		/// <summary>Sets whether execution should break when a function is entered.</summary>
		/// <remarks>Sets whether execution should break when a function is entered.</remarks>
		public virtual void SetBreakOnEnter(bool value)
		{
			dim.SetBreakOnEnter(value);
			debugGui.GetMenubar().GetBreakOnEnter().SetSelected(value);
		}

		/// <summary>Sets whether execution should break when a function is left.</summary>
		/// <remarks>Sets whether execution should break when a function is left.</remarks>
		public virtual void SetBreakOnReturn(bool value)
		{
			dim.SetBreakOnReturn(value);
			debugGui.GetMenubar().GetBreakOnReturn().SetSelected(value);
		}

		/// <summary>Removes all breakpoints.</summary>
		/// <remarks>Removes all breakpoints.</remarks>
		public virtual void ClearAllBreakpoints()
		{
			dim.ClearAllBreakpoints();
		}

		/// <summary>Resumes execution of the script.</summary>
		/// <remarks>Resumes execution of the script.</remarks>
		public virtual void Go()
		{
			dim.Go();
		}

		/// <summary>Sets the scope to be used for script evaluation.</summary>
		/// <remarks>Sets the scope to be used for script evaluation.</remarks>
		public virtual void SetScope(Scriptable scope)
		{
			SetScopeProvider(Main.IProxy.NewScopeProvider(scope));
		}

		/// <summary>
		/// Sets the
		/// <see cref="ScopeProvider">ScopeProvider</see>
		/// that provides a scope to be used
		/// for script evaluation.
		/// </summary>
		public virtual void SetScopeProvider(ScopeProvider p)
		{
			dim.SetScopeProvider(p);
		}

		/// <summary>
		/// Sets the
		/// <see cref="SourceProvider">SourceProvider</see>
		/// that provides the source to be displayed
		/// for script evaluation.
		/// </summary>
		public virtual void SetSourceProvider(SourceProvider sourceProvider)
		{
			dim.SetSourceProvider(sourceProvider);
		}

		/// <summary>
		/// Assign a Runnable object that will be invoked when the user
		/// selects "Exit..." or closes the Debugger main window.
		/// </summary>
		/// <remarks>
		/// Assign a Runnable object that will be invoked when the user
		/// selects "Exit..." or closes the Debugger main window.
		/// </remarks>
		public virtual void SetExitAction(Runnable r)
		{
			debugGui.SetExitAction(r);
		}

		/// <summary>
		/// Returns an
		/// <see cref="System.IO.InputStream">System.IO.InputStream</see>
		/// for stdin from the debugger's internal
		/// Console window.
		/// </summary>
		public virtual InputStream GetIn()
		{
			return debugGui.GetConsole().GetIn();
		}

		/// <summary>
		/// Returns a
		/// <see cref="System.IO.TextWriter">System.IO.TextWriter</see>
		/// for stdout to the debugger's internal
		/// Console window.
		/// </summary>
		public virtual TextWriter GetOut()
		{
			return debugGui.GetConsole().GetOut();
		}

		/// <summary>
		/// Returns a
		/// <see cref="System.IO.TextWriter">System.IO.TextWriter</see>
		/// for stderr in the Debugger's internal
		/// Console window.
		/// </summary>
		public virtual TextWriter GetErr()
		{
			return debugGui.GetConsole().GetErr();
		}

		/// <summary>Packs the debugger GUI frame.</summary>
		/// <remarks>Packs the debugger GUI frame.</remarks>
		public virtual void Pack()
		{
			debugGui.Pack();
		}

		/// <summary>Sets the debugger GUI frame dimensions.</summary>
		/// <remarks>Sets the debugger GUI frame dimensions.</remarks>
		public virtual void SetSize(int w, int h)
		{
			debugGui.SetSize(w, h);
		}

		/// <summary>Sets the visibility of the debugger GUI frame.</summary>
		/// <remarks>Sets the visibility of the debugger GUI frame.</remarks>
		public virtual void SetVisible(bool flag)
		{
			debugGui.SetVisible(flag);
		}

		/// <summary>Returns whether the debugger GUI frame is visible.</summary>
		/// <remarks>Returns whether the debugger GUI frame is visible.</remarks>
		public virtual bool IsVisible()
		{
			return debugGui.IsVisible();
		}

		/// <summary>Frees any resources held by the debugger.</summary>
		/// <remarks>Frees any resources held by the debugger.</remarks>
		public virtual void Dispose()
		{
			ClearAllBreakpoints();
			dim.Go();
			debugGui.Dispose();
			dim = null;
		}

		/// <summary>
		/// Attaches the debugger to the given
		/// <see cref="Rhino.ContextFactory">Rhino.ContextFactory</see>
		/// .
		/// </summary>
		public virtual void AttachTo(ContextFactory factory)
		{
			dim.AttachTo(factory);
		}

		/// <summary>
		/// Detaches from the current
		/// <see cref="Rhino.ContextFactory">Rhino.ContextFactory</see>
		/// .
		/// </summary>
		public virtual void Detach()
		{
			dim.Detach();
		}

		/// <summary>Main entry point.</summary>
		/// <remarks>
		/// Main entry point.  Creates a debugger attached to a Rhino
		/// <see cref="Rhino.Tools.Shell.Main">Rhino.Tools.Shell.Main</see>
		/// shell session.
		/// </remarks>
		public static void Main(string[] args)
		{
			Rhino.Tools.Debugger.Main main = new Rhino.Tools.Debugger.Main("Rhino JavaScript Debugger");
			main.DoBreak();
			main.SetExitAction(new Main.IProxy(Main.IProxy.EXIT_ACTION));
			Runtime.SetIn(main.GetIn());
			Runtime.SetOut(main.GetOut());
			Runtime.SetErr(main.GetErr());
			Global global = Rhino.Tools.Shell.Main.GetGlobal();
			global.SetIn(main.GetIn());
			global.SetOut(main.GetOut());
			global.SetErr(main.GetErr());
			main.AttachTo(Rhino.Tools.Shell.Main.shellContextFactory);
			main.SetScope(global);
			main.Pack();
			main.SetSize(600, 460);
			main.SetVisible(true);
			Rhino.Tools.Shell.Main.Exec(args);
		}

		/// <summary>Entry point for embedded applications.</summary>
		/// <remarks>
		/// Entry point for embedded applications.  This method attaches
		/// to the global
		/// <see cref="Rhino.ContextFactory">Rhino.ContextFactory</see>
		/// with a scope of a newly
		/// created
		/// <see cref="Rhino.Tools.Shell.Global">Rhino.Tools.Shell.Global</see>
		/// object.  No I/O redirection is performed
		/// as with
		/// <see cref="Main(string[])">Main(string[])</see>
		/// .
		/// </remarks>
		public static Rhino.Tools.Debugger.Main MainEmbedded(string title)
		{
			ContextFactory factory = ContextFactory.GetGlobal();
			Global global = new Global();
			global.Init(factory);
			return MainEmbedded(factory, global, title);
		}

		/// <summary>Entry point for embedded applications.</summary>
		/// <remarks>
		/// Entry point for embedded applications.  This method attaches
		/// to the given
		/// <see cref="Rhino.ContextFactory">Rhino.ContextFactory</see>
		/// with the given scope.  No
		/// I/O redirection is performed as with
		/// <see cref="Main(string[])">Main(string[])</see>
		/// .
		/// </remarks>
		public static Rhino.Tools.Debugger.Main MainEmbedded(ContextFactory factory, Scriptable scope, string title)
		{
			return MainEmbeddedImpl(factory, scope, title);
		}

		/// <summary>Entry point for embedded applications.</summary>
		/// <remarks>
		/// Entry point for embedded applications.  This method attaches
		/// to the given
		/// <see cref="Rhino.ContextFactory">Rhino.ContextFactory</see>
		/// with the given scope.  No
		/// I/O redirection is performed as with
		/// <see cref="Main(string[])">Main(string[])</see>
		/// .
		/// </remarks>
		public static Rhino.Tools.Debugger.Main MainEmbedded(ContextFactory factory, ScopeProvider scopeProvider, string title)
		{
			return MainEmbeddedImpl(factory, scopeProvider, title);
		}

		/// <summary>
		/// Helper method for
		/// <see cref="MainEmbedded(string)">MainEmbedded(string)</see>
		/// , etc.
		/// </summary>
		private static Rhino.Tools.Debugger.Main MainEmbeddedImpl(ContextFactory factory, object scopeProvider, string title)
		{
			if (title == null)
			{
				title = "Rhino JavaScript Debugger (embedded usage)";
			}
			Rhino.Tools.Debugger.Main main = new Rhino.Tools.Debugger.Main(title);
			main.DoBreak();
			main.SetExitAction(new Main.IProxy(Main.IProxy.EXIT_ACTION));
			main.AttachTo(factory);
			if (scopeProvider is ScopeProvider)
			{
				main.SetScopeProvider((ScopeProvider)scopeProvider);
			}
			else
			{
				Scriptable scope = (Scriptable)scopeProvider;
				if (scope is Global)
				{
					Global global = (Global)scope;
					global.SetIn(main.GetIn());
					global.SetOut(main.GetOut());
					global.SetErr(main.GetErr());
				}
				main.SetScope(scope);
			}
			main.Pack();
			main.SetSize(600, 460);
			main.SetVisible(true);
			return main;
		}

		// Deprecated methods
		[System.ObsoleteAttribute(@"Use SetSize(int, int) instead.")]
		public virtual void SetSize(Dimension dimension)
		{
			debugGui.SetSize(dimension.width, dimension.height);
		}

		[System.ObsoleteAttribute(@"The method does nothing and is only present for compatibility.")]
		public virtual void SetOptimizationLevel(int level)
		{
		}

		[System.ObsoleteAttribute(@"The method is only present for compatibility and should not be called.")]
		public virtual void ContextEntered(Context cx)
		{
			throw new InvalidOperationException();
		}

		[System.ObsoleteAttribute(@"The method is only present for compatibility and should not be called.")]
		public virtual void ContextExited(Context cx)
		{
			throw new InvalidOperationException();
		}

		[System.ObsoleteAttribute(@"The method is only present for compatibility and should not be called.")]
		public virtual void ContextCreated(Context cx)
		{
			throw new InvalidOperationException();
		}

		[System.ObsoleteAttribute(@"The method is only present for compatibility and should not be called.")]
		public virtual void ContextReleased(Context cx)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Class to consolidate all internal implementations of interfaces
		/// to avoid class generation bloat.
		/// </summary>
		/// <remarks>
		/// Class to consolidate all internal implementations of interfaces
		/// to avoid class generation bloat.
		/// </remarks>
		private class IProxy : Runnable, ScopeProvider
		{
			public const int EXIT_ACTION = 1;

			public const int SCOPE_PROVIDER = 2;

			/// <summary>The type of interface.</summary>
			/// <remarks>The type of interface.</remarks>
			private readonly int type;

			/// <summary>
			/// The scope object to expose when
			/// <see cref="type">type</see>
			/// =
			/// <see cref="SCOPE_PROVIDER">SCOPE_PROVIDER</see>
			/// .
			/// </summary>
			private Scriptable scope;

			/// <summary>Creates a new IProxy.</summary>
			/// <remarks>Creates a new IProxy.</remarks>
			public IProxy(int type)
			{
				// Constants for 'type'.
				this.type = type;
			}

			/// <summary>
			/// Creates a new IProxy that acts as a
			/// <see cref="ScopeProvider">ScopeProvider</see>
			/// .
			/// </summary>
			public static ScopeProvider NewScopeProvider(Scriptable scope)
			{
				Main.IProxy scopeProvider = new Main.IProxy(SCOPE_PROVIDER);
				scopeProvider.scope = scope;
				return scopeProvider;
			}

			// ContextAction
			/// <summary>Exit action.</summary>
			/// <remarks>Exit action.</remarks>
			public virtual void Run()
			{
				if (type != EXIT_ACTION)
				{
					Kit.CodeBug();
				}
				System.Environment.Exit(0);
			}

			// ScopeProvider
			/// <summary>Returns the scope for script evaluations.</summary>
			/// <remarks>Returns the scope for script evaluations.</remarks>
			public virtual Scriptable GetScope()
			{
				if (type != SCOPE_PROVIDER)
				{
					Kit.CodeBug();
				}
				if (scope == null)
				{
					Kit.CodeBug();
				}
				return scope;
			}
		}
	}
}
