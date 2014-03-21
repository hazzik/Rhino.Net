using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace Rhino.Tools.Debugger
{
	public class BraceFoldingStrategy : AbstractFoldingStrategy
	{
		private const char OpeningBrace = '{';

		private const char ClosingBrace = '}';

		public override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset) 
		{
			firstErrorOffset = -1;
			var newFoldings = new List<NewFolding>();

			var startOffsets = new Stack<int>();
			int lastNewLineOffset = 0;
			for (int i = 0; i < document.TextLength; i++)
			{
				char c = document.GetCharAt(i);
				if (c == OpeningBrace)
				{
					startOffsets.Push(i);
				}
				else if (c == ClosingBrace && startOffsets.Count > 0)
				{
					int startOffset = startOffsets.Pop();
					if (startOffset < lastNewLineOffset)
					{
						newFoldings.Add(new NewFolding(startOffset, i + 1));
					}
				}
				else if (c == '\n' || c == '\r')
				{
					lastNewLineOffset = i + 1;
				}
			}
			return newFoldings.OrderBy(x => x.StartOffset);
		}
	}
}