using System;

namespace Rhino.Tools.Debugger
{
	/// <summary>Class to store information about a function.</summary>
	public sealed class FunctionSource
	{
		/// <summary>Creates a new FunctionSource.</summary>
		internal FunctionSource(SourceInfo sourceInfo, int firstLine, string name)
		{
			if (name == null)
				throw new ArgumentException();
			SourceInfo = sourceInfo;
			FirstLine = firstLine;
			Name = name;
		}

		/// <summary> Returns the SourceInfo object that describes the source of the function. </summary>
		public SourceInfo SourceInfo { get; private set; }

		/// <summary>Returns the line number of the first line of the function.</summary>
		public int FirstLine { get; private set; }

		/// <summary>Returns the name of the function.</summary>
		public string Name { get; private set; }
	}
}