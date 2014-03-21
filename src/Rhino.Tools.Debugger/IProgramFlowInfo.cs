namespace Rhino.Tools.Debugger
{
	public interface IProgramFlowInfo
	{
		/// <summary>The current offset position.</summary>
		int? CurrentPosition { get; }

		/// <summary>Returns whether the given line has a breakpoint.</summary>
		bool IsBreakPoint(int line);

		/// <summary>Toggles the breakpoint on the given line.</summary>
		void ToggleBreakPoint(int line);
	}
}
