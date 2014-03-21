using Rhino.Debug;

namespace Rhino.Tools.Debugger
{
	internal sealed class DebuggerImpl : Debug.Debugger
	{
		/// <summary>The debugger.</summary>
		private readonly Dim dim;

		internal DebuggerImpl(Dim dim)
		{
			this.dim = dim;
		}

		/// <summary>Returns a StackFrame for the given function or script.</summary>
		/// <remarks>Returns a StackFrame for the given function or script.</remarks>
		public DebugFrame GetFrame(Context cx, DebuggableScript fnOrScript)
		{
			var item = dim.GetFunctionSource(fnOrScript);
			if (item == null) // Can not debug if source is not available
				return null;

			return new StackFrame(cx, dim, item);
		}

		/// <summary>Called when compilation is finished.</summary>
		/// <remarks>Called when compilation is finished.</remarks>
		public void HandleCompilationDone(Context cx, DebuggableScript fnOrScript, string source)
		{
			if (!fnOrScript.IsTopLevel())
				return;
				
			dim.RegisterTopScript(fnOrScript, source);
		}
	}
}