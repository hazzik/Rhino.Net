using System;

namespace Rhino.Tools.Debugger
{
	/// <summary>Class to store information about a stack.</summary>
	/// <remarks>Class to store information about a stack.</remarks>
	public sealed class ContextData
	{
		/// <summary>The stack frames.</summary>
		/// <remarks>The stack frames.</remarks>
		private readonly ObjArray frameStack = new ObjArray();

		/// <summary>Whether the debugger should break at the next line in this context.</summary>
		/// <remarks>Whether the debugger should break at the next line in this context.</remarks>
		internal bool breakNextLine;

		/// <summary>The frame depth the debugger should stop at.</summary>
		/// <remarks>
		/// The frame depth the debugger should stop at.  Used to implement
		/// "step over" and "step out".
		/// </remarks>
		internal int stopAtFrameDepth = -1;

		/// <summary>Whether this context is in the event thread.</summary>
		/// <remarks>Whether this context is in the event thread.</remarks>
		internal bool eventThreadFlag;

		/// <summary>The last exception that was processed.</summary>
		/// <remarks>The last exception that was processed.</remarks>
		internal Exception lastProcessedException;

		/// <summary>Returns the ContextData for the given Context.</summary>
		/// <remarks>Returns the ContextData for the given Context.</remarks>
		public static ContextData Get(Context cx)
		{
			return (ContextData)cx.GetDebuggerContextData();
		}

		/// <summary>Returns the number of stack frames.</summary>
		/// <remarks>Returns the number of stack frames.</remarks>
		public int FrameCount()
		{
			return frameStack.Size();
		}

		/// <summary>Returns the stack frame with the given index.</summary>
		/// <remarks>Returns the stack frame with the given index.</remarks>
		public StackFrame GetFrame(int frameNumber)
		{
			int num = frameStack.Size() - frameNumber - 1;
			return (StackFrame)frameStack.Get(num);
		}

		/// <summary>Pushes a stack frame on to the stack.</summary>
		/// <remarks>Pushes a stack frame on to the stack.</remarks>
		internal void PushFrame(StackFrame frame)
		{
			frameStack.Push(frame);
		}

		/// <summary>Pops a stack frame from the stack.</summary>
		/// <remarks>Pops a stack frame from the stack.</remarks>
		internal void PopFrame()
		{
			frameStack.Pop();
		}
	}
}