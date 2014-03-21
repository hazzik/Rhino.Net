using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using Rhino.Tools.Shell;
using Rhino.Utils;
using Xceed.Wpf.AvalonDock.Layout;

namespace Rhino.Tools.Debugger
{
	public partial class MainWindow : GuiCallback
	{
		internal readonly Dim dim = new Dim();

		/// <summary>
		/// The <see cref="FileWindow">FileWindow</see> that last had the focus.
		/// </summary>
		private FileWindow currentWindow;

		/// <summary>Hash table of internal frame names to the internal frames themselves.</summary>
		//private readonly IDictionary<string, JFrame> toplevels = new ConcurrentDictionary<string, JFrame>();

		/// <summary>Hash table of script URLs to their internal frames.</summary>
		private readonly IDictionary<string, FileWindow> fileWindows = new ConcurrentDictionary<string, FileWindow>();

		public MainWindow()
		{
			InitializeComponent();
            UpdateEnabled(false);
			dim.SetGuiCallback(this);
		    dim.AttachTo(Program.shellContextFactory);
		    Evaluator.Debugger = dim;
		    EvalTextArea.Debugger = dim;
		}

		private void BreakOnExceptionsToggle(object sender, RoutedEventArgs e)
		{
		}

		private void BreakOnFunctionEnterToggle(object sender, RoutedEventArgs e)
		{
		}

		private void BreakOnFunctionReturnToggle(object sender, RoutedEventArgs e)
		{
		}

		private void OpenClick(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Filter = "JavaScript Files (*.js)|*.js|All Files (*.*)|*.*"
			};
			if (dialog.ShowDialog() == true)
			{
				var fileName = dialog.FileName;
				string text = ReadFile(fileName);
				if (text != null)
				{
					new System.Threading.Thread(() => dim.CompileScript(fileName, text)).Start();
				}
			}
		}

		private void RunClick(object sender, RoutedEventArgs e)
		{
            var dialog = new OpenFileDialog
            {
                Filter = "JavaScript Files (*.js)|*.js|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                var fileName = dialog.FileName;
                string text = ReadFile(fileName);
                if (text != null)
                {
                    new System.Threading.Thread(() => dim.EvalScript(fileName, text)).Start();
                }
            }
        }

		private void ExitClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void CutClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void CopyClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void PasteClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void GoToFunctionClick(object sender, RoutedEventArgs e)
		{
			var dialog = new FindFunctionDialog { FunctionNames = dim.FunctionNames() };
			if (dialog.ShowDialog() == true)
			{
				FunctionSource item = dim.FunctionSourceByName(dialog.SelectedFunctionName);
				if (item != null)
				{
					SourceInfo si = item.SourceInfo;
					string url = si.Url();
					int lineNumber = item.FirstLine;
					ShowFileWindow(url, lineNumber);
				}
			}
		}

		private void BreakClick(object sender, RoutedEventArgs e)
		{
			dim.SetBreak();
		}

		private void GoClick(object sender, RoutedEventArgs e)
		{
			dim.SetReturnValue(Dim.GO);
            UpdateEnabled(false);
        }

		private void StepIntoClick(object sender, RoutedEventArgs e)
		{
			dim.SetReturnValue(Dim.STEP_INTO);
            UpdateEnabled(false);
        }

		private void StepOverClick(object sender, RoutedEventArgs e)
		{
			dim.SetReturnValue(Dim.STEP_OVER);
            UpdateEnabled(false);
        }

		private void StepOutClick(object sender, RoutedEventArgs e)
		{
			dim.SetReturnValue(Dim.STEP_OUT);
            UpdateEnabled(false);
        }

		private string ReadFile(string fileName)
		{
			using (var reader = File.OpenText(fileName))
			{
				return reader.ReadToEnd();
			}
		}

		public void UpdateSourceText(SourceInfo sourceInfo)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				string fileName = sourceInfo.Url();
				if (!UpdateFileWindow(sourceInfo) && fileName != "<stdin>")
				{
					CreateFileWindow(sourceInfo, -1);
				}
			}));

		}

		private bool UpdateFileWindow(SourceInfo sourceInfo)
		{
			string fileName = sourceInfo.Url();
			FileWindow w = GetFileWindow(fileName);
			if (w != null)
			{
				w.UpdateText(sourceInfo);
				//w.Show();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Creates and shows a new
		/// <see cref="FileWindow">FileWindow</see>
		/// for the given source.
		/// </summary>
		private void CreateFileWindow(SourceInfo sourceInfo, int line)
		{
			const bool activate = true;
			string url = sourceInfo.Url();
			FileWindow w = new FileWindow(this, sourceInfo);
			fileWindows[url] = w;

			if (line != -1)
			{
				if (currentWindow != null)
				{
					currentWindow.Select(-1);
					currentWindow.SetLineNumber(-1);
				}
				try
				{
					w.Select(w.TextEditor.Document.GetLineByNumber(line).Offset);
					w.SetLineNumber(line);
				}
				catch (Exception)
				{
					try
					{
						w.Select(w.TextEditor.Document.GetLineByNumber(0).Offset);
						w.SetLineNumber(0);
					}
					catch (Exception)
					{
						w.Select(-1);
						w.SetLineNumber(-1);
					}
				}
			}
			var firstDocumentPane = DockingManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
			var document = new LayoutDocument
			{
				Title = Path.GetFileName(sourceInfo.Url()),
				IsActive = true,
				Content = w,
			};
			firstDocumentPane.Children.Add(document);
			if (line != -1)
			{
				currentWindow = w;
			}
//            menubar.AddFile(url);
//            w.SetVisible(true);
			if (activate)
			{
				try
				{
//                    w.SetMaximum(true);
//                    w.SetSelected(true);
//                    w.MoveToFront();
				}
				catch (Exception)
				{
				}
			}
		}
		
		/// <summary>
		/// Returns the <see cref="FileWindow">FileWindow</see>for the given URL.
		/// </summary>
		private FileWindow GetFileWindow(string url)
		{
			if (url == null || url == "<stdin>")
				return null;
			return fileWindows.GetValueOrDefault(url);
		}

		/// <summary>
		/// Shows a <see cref="FileWindow">FileWindow</see> for the given source, creating it
		/// if it doesn't exist yet. if <code>lineNumber</code> is greater
		/// than -1, it indicates the line number to select and display.
		/// </summary>
		/// <param name="sourceUrl">the source URL</param>
		/// <param name="lineNumber">the line number to select, or -1</param>
		private void ShowFileWindow(string sourceUrl, int lineNumber)
		{
			var w = GetFileWindow(sourceUrl);
			if (w == null)
			{
				SourceInfo si = dim.SourceInfo(sourceUrl);
				CreateFileWindow(si, -1);
				w = GetFileWindow(sourceUrl);
			}
			if (lineNumber > -1)
			{
				w.TextEditor.ScrollToLine(lineNumber);
				var line = w.TextEditor.Document.GetLineByNumber(lineNumber);
				w.TextEditor.Select(line.Offset, line.EndOffset - line.Offset);
			}
			//w.Parent.IsActive = true;
		}

		public void EnterInterrupt(StackFrame lastFrame, string threadTitle, string alertMessage)
		{
			if (IsGuiEventThread())
			{
				EnterInterruptImpl(lastFrame, threadTitle, alertMessage);
			}
			else
			{
				Dispatcher.BeginInvoke(new Action(() => EnterInterruptImpl(lastFrame, threadTitle, alertMessage)));
			}
		}

		public bool IsGuiEventThread()
		{
			return System.Threading.Thread.CurrentThread.ManagedThreadId == Dispatcher.Thread.ManagedThreadId;
		}

		public void DispatchNextGuiEvent()
		{
			throw new NotImplementedException();
		}

	    private List<string> toolTips = new List<string>();
 
		/// <summary>Handles script interruption.</summary>
		internal virtual void EnterInterruptImpl(StackFrame lastFrame, string threadTitle, string alertMessage)
		{
			//statusBar.Text = "Thread: " + threadTitle;
			ShowStopLine(lastFrame);
		    if (!string.IsNullOrEmpty(alertMessage))
		        MessageBox.Show(this, alertMessage, "Exception in Script", MessageBoxButton.OK, MessageBoxImage.Error);

		    UpdateEnabled(true);
 
            ContextData contextData = lastFrame.ContextData();
            var ctx = Context;
            int frameCount = contextData.FrameCount();
		    ctx.Items.Clear();
		    ctx.SelectedItem = null;
            toolTips.Clear();
            for (int i = 0; i < frameCount; i++)
            {
                StackFrame frame = contextData.GetFrame(i);
                string url = frame.GetUrl();
                int lineNumber = frame.GetLineNumber();
                string shortName = url;
                if (url.Length > 20)
                {
                    shortName = "..." + url.Substring(url.Length - 17);
                }
                string location = "\"" + shortName + "\", line " + lineNumber;
                ctx.Items.Insert(i, location);
                location = "\"" + url + "\", line " + lineNumber;
                toolTips.Add(location);
            }
            ctx.SelectedIndex = 0;
		}

	    private void UpdateEnabled(bool interrupted)
	    {
	        Context.IsEnabled = interrupted;

	        MiBreak.IsEnabled = !interrupted;
	        MiGo.IsEnabled = interrupted;
	        MiStepInto.IsEnabled = interrupted;
	        MiStepOver.IsEnabled = interrupted;
	        MiStepOut.IsEnabled = interrupted;

	        BtnBreak.IsEnabled = !interrupted;
	        BtnGo.IsEnabled = interrupted;
	        BtnStepInto.IsEnabled = interrupted;
	        BtnStepOver.IsEnabled = interrupted;
	        BtnStepOut.IsEnabled = interrupted;
	    }

	    /// <summary>Shows the line at which execution in the given stack frame just stopped.</summary>
		/// <remarks>Shows the line at which execution in the given stack frame just stopped.</remarks>
		internal virtual void ShowStopLine(StackFrame frame)
		{
			string sourceName = frame.GetUrl();
			if (sourceName == null || sourceName == "<stdin>")
			{
				if (Console.IsVisible)
					Console.Focus();
			}
			else
			{
				ShowFileWindow(sourceName, -1);
				int lineNumber = frame.GetLineNumber();
				FileWindow w = GetFileWindow(sourceName);
				if (w != null)
				{
					SetFilePosition(w, lineNumber);
				}
			}
		}

		/// <summary>
		/// Moves the current position in the given
		/// <see cref="FileWindow">FileWindow</see>
		/// to the
		/// given line.
		/// </summary>
		private void SetFilePosition(FileWindow w, int line)
		{
			bool activate = true;
			TextEditor ta = w.TextEditor;
			try
			{
				if (line == -1)
				{
					w.SetPosition(-1);
					if (currentWindow == w)
					{
						currentWindow = null;
					}
				}
				else
				{
					int loc = ta.Document.GetLineByNumber(line).Offset;
					if (currentWindow != null && currentWindow != w)
					{
						currentWindow.SetPosition(-1);
					}
				    //w.Select(loc);
                    w.SetLineNumber(line);
				    currentWindow = w;
				}
			}
			catch (Exception)
			{
			}
			// fix me
			if (activate)
			{
				w.Focus();
			}
		}

		private void ConsoleClick(object sender, RoutedEventArgs e)
		{
			Console.Focus();
		}

        private void ContextSwitch(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ContextData contextData = dim.CurrentContextData();
            if (contextData == null)
                return;
            
            int frameIndex = Context.SelectedIndex;
            int frameCount = contextData.FrameCount();
            if (frameIndex < 0 || frameIndex >= frameCount)
                return;
            
            Context.ToolTip = toolTips[frameIndex];
            
            StackFrame frame = contextData.GetFrame(frameIndex);
            object scope = frame.Scope();
            object thisObj = frame.ThisObj();
            ThisTable.Model = new VariableModel(dim, thisObj);
            LocalsTable.Model = scope != thisObj
                ? new VariableModel(dim, scope)
                : new VariableModel();
            dim.ContextSwitch(frameIndex);
            ShowStopLine(frame);
        }

	    private void MainWindowInitialized(object sender, EventArgs eventArgs)
	    {
	        dim.SetBreak();
	        //            main.SetExitAction(() => Environment.Exit(0));
	        System.Console.SetIn(Console.ConsoleTextArea.In);
	        System.Console.SetOut(Console.ConsoleTextArea.Out);
	        System.Console.SetError(Console.ConsoleTextArea.Error);
	        Global global = Shell.Program.GetGlobal();
	        global.SetIn(Console.ConsoleTextArea.In);
	        global.SetOut(Console.ConsoleTextArea.Out);
	        global.SetError(Console.ConsoleTextArea.Error);
	        dim.AttachTo(Program.shellContextFactory);
	        dim.SetScopeProvider(new ScopeProviderImpl(global));
	        var thread = new System.Threading.Thread(() => Shell.Program.Exec(new string[] { }))
	        {
	            IsBackground = true,
	        };
	        thread.Start();
	    }
	}
}
