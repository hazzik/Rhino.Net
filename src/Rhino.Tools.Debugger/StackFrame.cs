using System;
using Rhino.Debug;

namespace Rhino.Tools.Debugger
{
	/// <summary>Object to represent one stack frame.</summary>
	/// <remarks>Object to represent one stack frame.</remarks>
	public sealed class StackFrame : DebugFrame
	{
		/// <summary>The debugger.</summary>
		/// <remarks>The debugger.</remarks>
		private Dim dim;

		/// <summary>The ContextData for the Context being debugged.</summary>
		/// <remarks>The ContextData for the Context being debugged.</remarks>
		private ContextData contextData;

		/// <summary>The scope.</summary>
		/// <remarks>The scope.</remarks>
		public Scriptable scope;

		/// <summary>The 'this' object.</summary>
		/// <remarks>The 'this' object.</remarks>
		public Scriptable thisObj;

		/// <summary>Information about the function.</summary>
		/// <remarks>Information about the function.</remarks>
		private FunctionSource fsource;

		/// <summary>Array of breakpoint state for each source line.</summary>
		/// <remarks>Array of breakpoint state for each source line.</remarks>
		private bool[] breakpoints;

		/// <summary>Current line number.</summary>
		/// <remarks>Current line number.</remarks>
		private int lineNumber;

		/// <summary>Creates a new StackFrame.</summary>
		/// <remarks>Creates a new StackFrame.</remarks>
		internal StackFrame(Context cx, Dim dim, FunctionSource fsource)
		{
			this.dim = dim;
			this.contextData = Debugger.ContextData.Get(cx);
			this.fsource = fsource;
			this.breakpoints = fsource.SourceInfo.breakpoints;
			this.lineNumber = fsource.FirstLine;
		}

		/// <summary>Called when the stack frame is entered.</summary>
		/// <remarks>Called when the stack frame is entered.</remarks>
		public void OnEnter(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			contextData.PushFrame(this);
			this.scope = scope;
			this.thisObj = thisObj;
			if (dim.breakOnEnter)
			{
				dim.HandleBreakpointHit(this, cx);
			}
		}

		/// <summary>Called when the current position has changed.</summary>
		/// <remarks>Called when the current position has changed.</remarks>
		public void OnLineChange(Context cx, int lineno)
		{
			this.lineNumber = lineno;
			if (!breakpoints[lineno] && !dim.breakFlag)
			{
				bool lineBreak = contextData.breakNextLine;
				if (lineBreak && contextData.stopAtFrameDepth >= 0)
				{
					lineBreak = (contextData.FrameCount() <= contextData.stopAtFrameDepth);
				}
				if (!lineBreak)
				{
					return;
				}
				contextData.stopAtFrameDepth = -1;
				contextData.breakNextLine = false;
			}
			dim.HandleBreakpointHit(this, cx);
		}

		/// <summary>Called when an exception has been thrown.</summary>
		/// <remarks>Called when an exception has been thrown.</remarks>
		public void OnExceptionThrown(Context cx, Exception exception)
		{
			dim.HandleExceptionThrown(cx, exception, this);
		}

		/// <summary>Called when the stack frame has been left.</summary>
		/// <remarks>Called when the stack frame has been left.</remarks>
		public void OnExit(Context cx, bool byThrow, object resultOrException)
		{
			if (dim.breakOnReturn && !byThrow)
			{
				dim.HandleBreakpointHit(this, cx);
			}
			contextData.PopFrame();
		}

		/// <summary>Called when a 'debugger' statement is executed.</summary>
		/// <remarks>Called when a 'debugger' statement is executed.</remarks>
		public void OnDebuggerStatement(Context cx)
		{
			dim.HandleBreakpointHit(this, cx);
		}

		/// <summary>Returns the SourceInfo object for the function.</summary>
		/// <remarks>Returns the SourceInfo object for the function.</remarks>
		public SourceInfo SourceInfo()
		{
			return fsource.SourceInfo;
		}

		/// <summary>Returns the ContextData object for the Context.</summary>
		/// <remarks>Returns the ContextData object for the Context.</remarks>
		public ContextData ContextData()
		{
			return contextData;
		}

		/// <summary>Returns the scope object for this frame.</summary>
		/// <remarks>Returns the scope object for this frame.</remarks>
		public object Scope()
		{
			return scope;
		}

		/// <summary>Returns the 'this' object for this frame.</summary>
		/// <remarks>Returns the 'this' object for this frame.</remarks>
		public object ThisObj()
		{
			return thisObj;
		}

		/// <summary>Returns the source URL.</summary>
		/// <remarks>Returns the source URL.</remarks>
		public string GetUrl()
		{
			return fsource.SourceInfo.Url();
		}

		/// <summary>Returns the current line number.</summary>
		/// <remarks>Returns the current line number.</remarks>
		public int GetLineNumber()
		{
			return lineNumber;
		}

		/// <summary>Returns the current function name.</summary>
		/// <remarks>Returns the current function name.</remarks>
		public string GetFunctionName()
		{
			return fsource.Name;
		}
	}
}