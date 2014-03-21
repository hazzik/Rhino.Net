using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation.CSharp;

namespace Rhino.Tools.Debugger
{
	public partial class FileWindow : IProgramFlowInfo
	{
		private readonly MainWindow mainWindow;
		private readonly ProgramFlowMargin programFlowMargin;
		private int? clickedLineNumber;

		/// <summary>The current offset position.</summary>
		private int currentPos = -1;

		private SourceInfo sourceInfo;

		public FileWindow()
		{
			InitializeComponent();

			TextEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy();
			programFlowMargin = new ProgramFlowMargin(this);
			TextEditor.TextArea.LeftMargins.Add(programFlowMargin);
			TextEditor.TextArea.LeftMargins.Add(new LineNumberMargin());
			var manager = FoldingManager.Install(TextEditor.TextArea);
			var strategy = new BraceFoldingStrategy();
			Observable.FromEventPattern(x => TextEditor.TextArea.TextView.VisualLinesChanged += x, x => TextEditor.TextArea.TextView.VisualLinesChanged -= x)
				.Throttle(TimeSpan.FromMilliseconds(250))
				.ObserveOnDispatcher()
				.Subscribe(x => strategy.UpdateFoldings(manager, TextEditor.Document));
		}

		public FileWindow(MainWindow mainWindow, SourceInfo sourceInfo1) : this()
		{
			this.mainWindow = mainWindow;
			UpdateText(sourceInfo1);
		}

		/// <summary>Returns whether the given line has a breakpoint.</summary>
		public bool IsBreakPoint(int line)
		{
			return sourceInfo.BreakableLine(line) && sourceInfo.Breakpoint(line);
		}

		/// <summary>Toggles the breakpoint on the given line.</summary>
		public void ToggleBreakPoint(int line)
		{
			if (sourceInfo.BreakableLine(line))
				sourceInfo.Breakpoint(line, !sourceInfo.Breakpoint(line));
			programFlowMargin.InvalidateVisual();
		}

		public int? CurrentPosition
		{
			get { return currentPos; }
		}

		public void UpdateText(SourceInfo source)
		{
			sourceInfo = source;
			var newText = source.Source();
			var textArea = TextEditor;

			if (textArea.Text != newText)
			{
				textArea.Text = newText;
				var pos = 0;
				if (currentPos != -1)
				{
					pos = currentPos;
				}
				textArea.Select(pos, pos);
				//Select(textArea, pos);
			}
		}

		public void SetPosition(int pos)
		{
			Select(pos);
			SetLineNumber(TextEditor.Document.GetLineByOffset(pos).LineNumber);
		}

		public void Select(int pos)
		{
			TextEditor.Select(pos, pos);
		}

		public void SetLineNumber(int lineNumber)
		{
			currentPos = lineNumber;
			programFlowMargin.InvalidateVisual();
		}

		private void SetBreakPoint(int line, bool value)
		{
			if (sourceInfo.BreakableLine(line))
				sourceInfo.Breakpoint(line, value);
			programFlowMargin.InvalidateVisual();
		}

		private void SetBreakPointClick(object sender, RoutedEventArgs e)
		{
			if (clickedLineNumber != null)
			{
				SetBreakPoint(clickedLineNumber.Value, true);
				clickedLineNumber = null;
			}
		}

		private void ClearBreakPointClick(object sender, RoutedEventArgs e)
		{
			if (clickedLineNumber != null)
			{
				SetBreakPoint(clickedLineNumber.Value, false);
				clickedLineNumber = null;
			}
		}

		private void RunClick(object sender, RoutedEventArgs e)
		{
			LoadFile();
		}

		/// <summary>Loads the file.</summary>
		internal virtual void LoadFile()
		{
			string url = sourceInfo.Url();
			if (url != null)
			{
				new System.Threading.Thread(() =>
				{
					try
					{
						mainWindow.dim.EvalScript(url, sourceInfo.Source());
					}
					catch (Exception ex)
					{
						//MessageBox.Show(mainWindow, ex.Message, "Run error for " + url, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}).Start();
			}
		}

		private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			var visualLine = TextEditor.TextArea.TextView.GetVisualLineFromVisualTop(e.CursorTop + TextEditor.VerticalOffset);
			if (visualLine != null) clickedLineNumber = visualLine.FirstDocumentLine.LineNumber;
		}
	}
}
