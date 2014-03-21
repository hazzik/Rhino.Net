using System;
using Rhino.Debug;

namespace Rhino.Tools.Debugger
{
	/// <summary>Class to store information about a script source.</summary>
	/// <remarks>Class to store information about a script source.</remarks>
	public sealed class SourceInfo
	{
		/// <summary>An empty array of booleans.</summary>
		/// <remarks>An empty array of booleans.</remarks>
		private static readonly bool[] EMPTY_BOOLEAN_ARRAY = new bool[0];

		/// <summary>The script.</summary>
		/// <remarks>The script.</remarks>
		private readonly string source;

		/// <summary>The URL of the script.</summary>
		/// <remarks>The URL of the script.</remarks>
		private readonly string url;

		/// <summary>Array indicating which lines can have breakpoints set.</summary>
		/// <remarks>Array indicating which lines can have breakpoints set.</remarks>
		private readonly bool[] breakableLines;

		/// <summary>Array indicating whether a breakpoint is set on the line.</summary>
		/// <remarks>Array indicating whether a breakpoint is set on the line.</remarks>
		internal readonly bool[] breakpoints;

		/// <summary>Array of FunctionSource objects for the functions in the script.</summary>
		/// <remarks>Array of FunctionSource objects for the functions in the script.</remarks>
		private readonly FunctionSource[] functionSources;

		/// <summary>Creates a new SourceInfo object.</summary>
		/// <remarks>Creates a new SourceInfo object.</remarks>
		internal SourceInfo(string source, DebuggableScript[] functions, string normilizedUrl)
		{
			this.source = source;
			this.url = normilizedUrl;
			int N = functions.Length;
			int[][] lineArrays = new int[N][];
			for (int i = 0; i != N; ++i)
			{
				lineArrays[i] = functions[i].GetLineNumbers();
			}
			int minAll = 0;
			int maxAll = -1;
			int[] firstLines = new int[N];
			for (int i_1 = 0; i_1 != N; ++i_1)
			{
				int[] lines = lineArrays[i_1];
				if (lines == null || lines.Length == 0)
				{
					firstLines[i_1] = -1;
				}
				else
				{
					int max;
					int min = max = lines[0];
					for (int j = 1; j != lines.Length; ++j)
					{
						int line = lines[j];
						if (line < min)
						{
							min = line;
						}
						else
						{
							if (line > max)
							{
								max = line;
							}
						}
					}
					firstLines[i_1] = min;
					if (minAll > maxAll)
					{
						minAll = min;
						maxAll = max;
					}
					else
					{
						if (min < minAll)
						{
							minAll = min;
						}
						if (max > maxAll)
						{
							maxAll = max;
						}
					}
				}
			}
			if (minAll > maxAll)
			{
				// No line information
				this.breakableLines = EMPTY_BOOLEAN_ARRAY;
				this.breakpoints = EMPTY_BOOLEAN_ARRAY;
			}
			else
			{
				if (minAll < 0)
				{
					// Line numbers can not be negative
					throw new InvalidOperationException(minAll.ToString());
				}
				int linesTop = maxAll + 1;
				this.breakableLines = new bool[linesTop];
				this.breakpoints = new bool[linesTop];
				for (int i_2 = 0; i_2 != N; ++i_2)
				{
					int[] lines = lineArrays[i_2];
					if (lines != null && lines.Length != 0)
					{
						for (int j = 0; j != lines.Length; ++j)
						{
							int line = lines[j];
							this.breakableLines[line] = true;
						}
					}
				}
			}
			this.functionSources = new FunctionSource[N];
			for (int i_3 = 0; i_3 != N; ++i_3)
			{
				string name = functions[i_3].GetFunctionName();
				if (name == null)
				{
					name = String.Empty;
				}
				this.functionSources[i_3] = new FunctionSource(this, firstLines[i_3], name);
			}
		}

		/// <summary>Returns the source text.</summary>
		/// <remarks>Returns the source text.</remarks>
		public string Source()
		{
			return source;
		}

		/// <summary>Returns the script's origin URL.</summary>
		/// <remarks>Returns the script's origin URL.</remarks>
		public string Url()
		{
			return url;
		}

		/// <summary>Returns the number of FunctionSource objects stored in this object.</summary>
		/// <remarks>Returns the number of FunctionSource objects stored in this object.</remarks>
		public int FunctionSourcesTop()
		{
			return functionSources.Length;
		}

		/// <summary>Returns the FunctionSource object with the given index.</summary>
		/// <remarks>Returns the FunctionSource object with the given index.</remarks>
		public FunctionSource GetFunctionSource(int i)
		{
			return functionSources[i];
		}

		/// <summary>
		/// Copies the breakpoints from the given SourceInfo object into this
		/// one.
		/// </summary>
		/// <remarks>
		/// Copies the breakpoints from the given SourceInfo object into this
		/// one.
		/// </remarks>
		internal void CopyBreakpointsFrom(SourceInfo old)
		{
			int end = old.breakpoints.Length;
			if (end > breakpoints.Length)
			{
				end = breakpoints.Length;
			}
			for (int line = 0; line != end; ++line)
			{
				if (old.breakpoints[line])
				{
					breakpoints[line] = true;
				}
			}
		}

		/// <summary>
		/// Returns whether the given line number can have a breakpoint set on
		/// it.
		/// </summary>
		/// <remarks>
		/// Returns whether the given line number can have a breakpoint set on
		/// it.
		/// </remarks>
		public bool BreakableLine(int line)
		{
			return (line < breakableLines.Length) && breakableLines[line];
		}

		/// <summary>Returns whether there is a breakpoint set on the given line.</summary>
		/// <remarks>Returns whether there is a breakpoint set on the given line.</remarks>
		public bool Breakpoint(int line)
		{
			if (!BreakableLine(line))
			{
				throw new ArgumentException(line.ToString());
			}
			return line < breakpoints.Length && breakpoints[line];
		}

		/// <summary>Sets or clears the breakpoint flag for the given line.</summary>
		/// <remarks>Sets or clears the breakpoint flag for the given line.</remarks>
		public bool Breakpoint(int line, bool value)
		{
			if (!BreakableLine(line))
			{
				throw new ArgumentException(line.ToString());
			}
			bool changed;
			lock (breakpoints)
			{
				if (breakpoints[line] != value)
				{
					breakpoints[line] = value;
					changed = true;
				}
				else
				{
					changed = false;
				}
			}
			return changed;
		}

		/// <summary>Removes all breakpoints from the script.</summary>
		/// <remarks>Removes all breakpoints from the script.</remarks>
		public void RemoveAllBreakpoints()
		{
			lock (breakpoints)
			{
				for (int line = 0; line != breakpoints.Length; ++line)
				{
					breakpoints[line] = false;
				}
			}
		}
	}
}