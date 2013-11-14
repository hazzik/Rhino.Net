/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using Java.Awt;
using Java.Awt.Event;
using Javax.Swing;
using Javax.Swing.Event;
using Javax.Swing.Filechooser;
using Javax.Swing.Table;
using Javax.Swing.Text;
using Javax.Swing.Tree;
using Rhino;
using Rhino.Tools.Debugger;
using Rhino.Tools.Debugger.Treetable;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Debugger
{
	/// <summary>GUI for the Rhino debugger.</summary>
	/// <remarks>GUI for the Rhino debugger.</remarks>
	[System.Serializable]
	public class SwingGui : JFrame, GuiCallback
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = -8217029773456711621L;

		/// <summary>The debugger.</summary>
		/// <remarks>The debugger.</remarks>
		internal Dim dim;

		/// <summary>
		/// The action to run when the 'Exit' menu item is chosen or the
		/// frame is closed.
		/// </summary>
		/// <remarks>
		/// The action to run when the 'Exit' menu item is chosen or the
		/// frame is closed.
		/// </remarks>
		private Runnable exitAction;

		/// <summary>
		/// The
		/// <see cref="Javax.Swing.JDesktopPane">Javax.Swing.JDesktopPane</see>
		/// that holds the script windows.
		/// </summary>
		private JDesktopPane desk;

		/// <summary>
		/// The
		/// <see cref="Javax.Swing.JPanel">Javax.Swing.JPanel</see>
		/// that shows information about the context.
		/// </summary>
		private ContextWindow context;

		/// <summary>The menu bar.</summary>
		/// <remarks>The menu bar.</remarks>
		private Menubar menubar;

		/// <summary>The tool bar.</summary>
		/// <remarks>The tool bar.</remarks>
		private JToolBar toolBar;

		/// <summary>The console that displays I/O from the script.</summary>
		/// <remarks>The console that displays I/O from the script.</remarks>
		private JSInternalConsole console;

		/// <summary>
		/// The
		/// <see cref="Javax.Swing.JSplitPane">Javax.Swing.JSplitPane</see>
		/// that separates
		/// <see cref="desk">desk</see>
		/// from
		/// <see cref="Rhino.Context">Rhino.Context</see>
		/// .
		/// </summary>
		private JSplitPane split1;

		/// <summary>The status bar.</summary>
		/// <remarks>The status bar.</remarks>
		private JLabel statusBar;

		/// <summary>Hash table of internal frame names to the internal frames themselves.</summary>
		/// <remarks>Hash table of internal frame names to the internal frames themselves.</remarks>
		private readonly IDictionary<string, JFrame> toplevels = Sharpen.Collections.SynchronizedMap(new Dictionary<string, JFrame>());

		/// <summary>Hash table of script URLs to their internal frames.</summary>
		/// <remarks>Hash table of script URLs to their internal frames.</remarks>
		private readonly IDictionary<string, FileWindow> fileWindows = Sharpen.Collections.SynchronizedMap(new Dictionary<string, FileWindow>());

		/// <summary>
		/// The
		/// <see cref="FileWindow">FileWindow</see>
		/// that last had the focus.
		/// </summary>
		private FileWindow currentWindow;

		/// <summary>File choose dialog for loading a script.</summary>
		/// <remarks>File choose dialog for loading a script.</remarks>
		internal JFileChooser dlg;

		/// <summary>The AWT EventQueue.</summary>
		/// <remarks>
		/// The AWT EventQueue.  Used for manually pumping AWT events from
		/// <see cref="DispatchNextGuiEvent()">DispatchNextGuiEvent()</see>
		/// .
		/// </remarks>
		private EventQueue awtEventQueue;

		/// <summary>Creates a new SwingGui.</summary>
		/// <remarks>Creates a new SwingGui.</remarks>
		public SwingGui(Dim dim, string title) : base(title)
		{
			this.dim = dim;
			Init();
			dim.SetGuiCallback(this);
		}

		/// <summary>Returns the Menubar of this debugger frame.</summary>
		/// <remarks>Returns the Menubar of this debugger frame.</remarks>
		public virtual Menubar GetMenubar()
		{
			return menubar;
		}

		/// <summary>
		/// Sets the
		/// <see cref="Sharpen.Runnable">Sharpen.Runnable</see>
		/// that will be run when the "Exit" menu
		/// item is chosen.
		/// </summary>
		public virtual void SetExitAction(Runnable r)
		{
			exitAction = r;
		}

		/// <summary>Returns the debugger console component.</summary>
		/// <remarks>Returns the debugger console component.</remarks>
		public virtual JSInternalConsole GetConsole()
		{
			return console;
		}

		/// <summary>Sets the visibility of the debugger GUI.</summary>
		/// <remarks>Sets the visibility of the debugger GUI.</remarks>
		public override void SetVisible(bool b)
		{
			base.SetVisible(b);
			if (b)
			{
				// this needs to be done after the window is visible
				console.consoleTextArea.RequestFocus();
				context.split.SetDividerLocation(0.5);
				try
				{
					console.SetMaximum(true);
					console.SetSelected(true);
					console.Show();
					console.consoleTextArea.RequestFocus();
				}
				catch (Exception)
				{
				}
			}
		}

		/// <summary>Records a new internal frame.</summary>
		/// <remarks>Records a new internal frame.</remarks>
		internal virtual void AddTopLevel(string key, JFrame frame)
		{
			if (frame != this)
			{
				toplevels.Put(key, frame);
			}
		}

		/// <summary>Constructs the debugger GUI.</summary>
		/// <remarks>Constructs the debugger GUI.</remarks>
		private void Init()
		{
			menubar = new Menubar(this);
			SetJMenuBar(menubar);
			toolBar = new JToolBar();
			JButton button;
			JButton breakButton;
			JButton goButton;
			JButton stepIntoButton;
			JButton stepOverButton;
			JButton stepOutButton;
			string[] toolTips = new string[] { "Break (Pause)", "Go (F5)", "Step Into (F11)", "Step Over (F7)", "Step Out (F8)" };
			int count = 0;
			button = breakButton = new JButton("Break");
			button.SetToolTipText("Break");
			button.SetActionCommand("Break");
			button.AddActionListener(menubar);
			button.SetEnabled(true);
			button.SetToolTipText(toolTips[count++]);
			button = goButton = new JButton("Go");
			button.SetToolTipText("Go");
			button.SetActionCommand("Go");
			button.AddActionListener(menubar);
			button.SetEnabled(false);
			button.SetToolTipText(toolTips[count++]);
			button = stepIntoButton = new JButton("Step Into");
			button.SetToolTipText("Step Into");
			button.SetActionCommand("Step Into");
			button.AddActionListener(menubar);
			button.SetEnabled(false);
			button.SetToolTipText(toolTips[count++]);
			button = stepOverButton = new JButton("Step Over");
			button.SetToolTipText("Step Over");
			button.SetActionCommand("Step Over");
			button.SetEnabled(false);
			button.AddActionListener(menubar);
			button.SetToolTipText(toolTips[count++]);
			button = stepOutButton = new JButton("Step Out");
			button.SetToolTipText("Step Out");
			button.SetActionCommand("Step Out");
			button.SetEnabled(false);
			button.AddActionListener(menubar);
			button.SetToolTipText(toolTips[count++]);
			Dimension dim = stepOverButton.GetPreferredSize();
			breakButton.SetPreferredSize(dim);
			breakButton.SetMinimumSize(dim);
			breakButton.SetMaximumSize(dim);
			breakButton.SetSize(dim);
			goButton.SetPreferredSize(dim);
			goButton.SetMinimumSize(dim);
			goButton.SetMaximumSize(dim);
			stepIntoButton.SetPreferredSize(dim);
			stepIntoButton.SetMinimumSize(dim);
			stepIntoButton.SetMaximumSize(dim);
			stepOverButton.SetPreferredSize(dim);
			stepOverButton.SetMinimumSize(dim);
			stepOverButton.SetMaximumSize(dim);
			stepOutButton.SetPreferredSize(dim);
			stepOutButton.SetMinimumSize(dim);
			stepOutButton.SetMaximumSize(dim);
			toolBar.Add(breakButton);
			toolBar.Add(goButton);
			toolBar.Add(stepIntoButton);
			toolBar.Add(stepOverButton);
			toolBar.Add(stepOutButton);
			JPanel contentPane = new JPanel();
			contentPane.SetLayout(new BorderLayout());
			GetContentPane().Add(toolBar, BorderLayout.NORTH);
			GetContentPane().Add(contentPane, BorderLayout.CENTER);
			desk = new JDesktopPane();
			desk.SetPreferredSize(new Dimension(600, 300));
			desk.SetMinimumSize(new Dimension(150, 50));
			desk.Add(console = new JSInternalConsole("JavaScript Console"));
			context = new ContextWindow(this);
			context.SetPreferredSize(new Dimension(600, 120));
			context.SetMinimumSize(new Dimension(50, 50));
			split1 = new JSplitPane(JSplitPane.VERTICAL_SPLIT, desk, context);
			split1.SetOneTouchExpandable(true);
			Rhino.Tools.Debugger.SwingGui.SetResizeWeight(split1, 0.66);
			contentPane.Add(split1, BorderLayout.CENTER);
			statusBar = new JLabel();
			statusBar.SetText("Thread: ");
			contentPane.Add(statusBar, BorderLayout.SOUTH);
			dlg = new JFileChooser();
			FileFilter filter = new _FileFilter_303();
			dlg.AddChoosableFileFilter(filter);
			AddWindowListener(new _WindowAdapter_326(this));
		}

		private sealed class _FileFilter_303 : FileFilter
		{
			public _FileFilter_303()
			{
			}

			public override bool Accept(FilePath f)
			{
				if (f.IsDirectory())
				{
					return true;
				}
				string n = f.GetName();
				int i = n.LastIndexOf('.');
				if (i > 0 && i < n.Length - 1)
				{
					string ext = Sharpen.Runtime.Substring(n, i + 1).ToLower();
					if (ext.Equals("js"))
					{
						return true;
					}
				}
				return false;
			}

			public override string GetDescription()
			{
				return "JavaScript Files (*.js)";
			}
		}

		private sealed class _WindowAdapter_326 : WindowAdapter
		{
			public _WindowAdapter_326(SwingGui _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void WindowClosing(WindowEvent e)
			{
				this._enclosing.Exit();
			}

			private readonly SwingGui _enclosing;
		}

		/// <summary>
		/// Runs the
		/// <see cref="exitAction">exitAction</see>
		/// .
		/// </summary>
		private void Exit()
		{
			if (exitAction != null)
			{
				SwingUtilities.InvokeLater(exitAction);
			}
			dim.SetReturnValue(Dim.EXIT);
		}

		/// <summary>
		/// Returns the
		/// <see cref="FileWindow">FileWindow</see>
		/// for the given URL.
		/// </summary>
		internal virtual FileWindow GetFileWindow(string url)
		{
			if (url == null || url.Equals("<stdin>"))
			{
				return null;
			}
			return fileWindows.Get(url);
		}

		/// <summary>Returns a short version of the given URL.</summary>
		/// <remarks>Returns a short version of the given URL.</remarks>
		internal static string GetShortName(string url)
		{
			int lastSlash = url.LastIndexOf('/');
			if (lastSlash < 0)
			{
				lastSlash = url.LastIndexOf('\\');
			}
			string shortName = url;
			if (lastSlash >= 0 && lastSlash + 1 < url.Length)
			{
				shortName = Sharpen.Runtime.Substring(url, lastSlash + 1);
			}
			return shortName;
		}

		/// <summary>
		/// Closes the given
		/// <see cref="FileWindow">FileWindow</see>
		/// .
		/// </summary>
		internal virtual void RemoveWindow(FileWindow w)
		{
			Sharpen.Collections.Remove(fileWindows, w.GetUrl());
			JMenu windowMenu = GetWindowMenu();
			int count = windowMenu.GetItemCount();
			JMenuItem lastItem = windowMenu.GetItem(count - 1);
			string name = GetShortName(w.GetUrl());
			for (int i = 5; i < count; i++)
			{
				JMenuItem item = windowMenu.GetItem(i);
				if (item == null)
				{
					continue;
				}
				// separator
				string text = item.GetText();
				//1 D:\foo.js
				//2 D:\bar.js
				int pos = text.IndexOf(' ');
				if (Sharpen.Runtime.Substring(text, pos + 1).Equals(name))
				{
					windowMenu.Remove(item);
					// Cascade    [0]
					// Tile       [1]
					// -------    [2]
					// Console    [3]
					// -------    [4]
					if (count == 6)
					{
						// remove the final separator
						windowMenu.Remove(4);
					}
					else
					{
						int j = i - 4;
						for (; i < count - 1; i++)
						{
							JMenuItem thisItem = windowMenu.GetItem(i);
							if (thisItem != null)
							{
								//1 D:\foo.js
								//2 D:\bar.js
								text = thisItem.GetText();
								if (text.Equals("More Windows..."))
								{
									break;
								}
								else
								{
									pos = text.IndexOf(' ');
									thisItem.SetText((char)('0' + j) + " " + Sharpen.Runtime.Substring(text, pos + 1));
									thisItem.SetMnemonic('0' + j);
									j++;
								}
							}
						}
						if (count - 6 == 0 && lastItem != item)
						{
							if (lastItem.GetText().Equals("More Windows..."))
							{
								windowMenu.Remove(lastItem);
							}
						}
					}
					break;
				}
			}
			windowMenu.Revalidate();
		}

		/// <summary>Shows the line at which execution in the given stack frame just stopped.</summary>
		/// <remarks>Shows the line at which execution in the given stack frame just stopped.</remarks>
		internal virtual void ShowStopLine(Dim.StackFrame frame)
		{
			string sourceName = frame.GetUrl();
			if (sourceName == null || sourceName.Equals("<stdin>"))
			{
				if (console.IsVisible())
				{
					console.Show();
				}
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
		/// Shows a
		/// <see cref="FileWindow">FileWindow</see>
		/// for the given source, creating it
		/// if it doesn't exist yet. if <code>lineNumber</code> is greater
		/// than -1, it indicates the line number to select and display.
		/// </summary>
		/// <param name="sourceUrl">the source URL</param>
		/// <param name="lineNumber">the line number to select, or -1</param>
		protected internal virtual void ShowFileWindow(string sourceUrl, int lineNumber)
		{
			FileWindow w = GetFileWindow(sourceUrl);
			if (w == null)
			{
				Dim.SourceInfo si = dim.SourceInfo(sourceUrl);
				CreateFileWindow(si, -1);
				w = GetFileWindow(sourceUrl);
			}
			if (lineNumber > -1)
			{
				int start = w.GetPosition(lineNumber - 1);
				int end = w.GetPosition(lineNumber) - 1;
				w.textArea.Select(start);
				w.textArea.SetCaretPosition(start);
				w.textArea.MoveCaretPosition(end);
			}
			try
			{
				if (w.IsIcon())
				{
					w.SetIcon(false);
				}
				w.SetVisible(true);
				w.MoveToFront();
				w.SetSelected(true);
				RequestFocus();
				w.RequestFocus();
				w.textArea.RequestFocus();
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// Creates and shows a new
		/// <see cref="FileWindow">FileWindow</see>
		/// for the given source.
		/// </summary>
		protected internal virtual void CreateFileWindow(Dim.SourceInfo sourceInfo, int line)
		{
			bool activate = true;
			string url = sourceInfo.Url();
			FileWindow w = new FileWindow(this, sourceInfo);
			fileWindows.Put(url, w);
			if (line != -1)
			{
				if (currentWindow != null)
				{
					currentWindow.SetPosition(-1);
				}
				try
				{
					w.SetPosition(w.textArea.GetLineStartOffset(line - 1));
				}
				catch (BadLocationException)
				{
					try
					{
						w.SetPosition(w.textArea.GetLineStartOffset(0));
					}
					catch (BadLocationException)
					{
						w.SetPosition(-1);
					}
				}
			}
			desk.Add(w);
			if (line != -1)
			{
				currentWindow = w;
			}
			menubar.AddFile(url);
			w.SetVisible(true);
			if (activate)
			{
				try
				{
					w.SetMaximum(true);
					w.SetSelected(true);
					w.MoveToFront();
				}
				catch (Exception)
				{
				}
			}
		}

		/// <summary>Update the source text for <code>sourceInfo</code>.</summary>
		/// <remarks>
		/// Update the source text for <code>sourceInfo</code>. This returns true
		/// if a
		/// <see cref="FileWindow">FileWindow</see>
		/// for the given source exists and could be updated.
		/// Otherwise, this does nothing and returns false.
		/// </remarks>
		/// <param name="sourceInfo">the source info</param>
		/// <returns>
		/// true if a
		/// <see cref="FileWindow">FileWindow</see>
		/// for the given source exists
		/// and could be updated, false otherwise.
		/// </returns>
		protected internal virtual bool UpdateFileWindow(Dim.SourceInfo sourceInfo)
		{
			string fileName = sourceInfo.Url();
			FileWindow w = GetFileWindow(fileName);
			if (w != null)
			{
				w.UpdateText(sourceInfo);
				w.Show();
				return true;
			}
			return false;
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
			JTextArea ta = w.textArea;
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
					int loc = ta.GetLineStartOffset(line - 1);
					if (currentWindow != null && currentWindow != w)
					{
						currentWindow.SetPosition(-1);
					}
					w.SetPosition(loc);
					currentWindow = w;
				}
			}
			catch (BadLocationException)
			{
			}
			// fix me
			if (activate)
			{
				if (w.IsIcon())
				{
					desk.GetDesktopManager().DeiconifyFrame(w);
				}
				desk.GetDesktopManager().ActivateFrame(w);
				try
				{
					w.Show();
					w.ToFront();
					// required for correct frame layering (JDK 1.4.1)
					w.SetSelected(true);
				}
				catch (Exception)
				{
				}
			}
		}

		/// <summary>Handles script interruption.</summary>
		/// <remarks>Handles script interruption.</remarks>
		internal virtual void EnterInterruptImpl(Dim.StackFrame lastFrame, string threadTitle, string alertMessage)
		{
			statusBar.SetText("Thread: " + threadTitle);
			ShowStopLine(lastFrame);
			if (alertMessage != null)
			{
				MessageDialogWrapper.ShowMessageDialog(this, alertMessage, "Exception in Script", JOptionPane.ERROR_MESSAGE);
			}
			UpdateEnabled(true);
			Dim.ContextData contextData = lastFrame.ContextData();
			JComboBox ctx = context.context;
			IList<string> toolTips = context.toolTips;
			context.DisableUpdate();
			int frameCount = contextData.FrameCount();
			ctx.RemoveAllItems();
			// workaround for JDK 1.4 bug that caches selected value even after
			// removeAllItems() is called
			ctx.SetSelectedItem(null);
			toolTips.Clear();
			for (int i = 0; i < frameCount; i++)
			{
				Dim.StackFrame frame = contextData.GetFrame(i);
				string url = frame.GetUrl();
				int lineNumber = frame.GetLineNumber();
				string shortName = url;
				if (url.Length > 20)
				{
					shortName = "..." + Sharpen.Runtime.Substring(url, url.Length - 17);
				}
				string location = "\"" + shortName + "\", line " + lineNumber;
				ctx.InsertItemAt(location, i);
				location = "\"" + url + "\", line " + lineNumber;
				toolTips.AddItem(location);
			}
			context.EnableUpdate();
			ctx.SetSelectedIndex(0);
			ctx.SetMinimumSize(new Dimension(50, ctx.GetMinimumSize().height));
		}

		/// <summary>Returns the 'Window' menu.</summary>
		/// <remarks>Returns the 'Window' menu.</remarks>
		private JMenu GetWindowMenu()
		{
			return menubar.GetMenu(3);
		}

		/// <summary>
		/// Displays a
		/// <see cref="Javax.Swing.JFileChooser">Javax.Swing.JFileChooser</see>
		/// and returns the selected filename.
		/// </summary>
		private string ChooseFile(string title)
		{
			dlg.SetDialogTitle(title);
			FilePath CWD = null;
			string dir = SecurityUtilities.GetSystemProperty("user.dir");
			if (dir != null)
			{
				CWD = new FilePath(dir);
			}
			if (CWD != null)
			{
				dlg.SetCurrentDirectory(CWD);
			}
			int returnVal = dlg.ShowOpenDialog(this);
			if (returnVal == JFileChooser.APPROVE_OPTION)
			{
				try
				{
					string result = dlg.GetSelectedFile().GetCanonicalPath();
					CWD = dlg.GetSelectedFile().GetParentFile();
					Properties props = Runtime.GetProperties();
					props.Put("user.dir", CWD.GetPath());
					Runtime.SetProperties(props);
					return result;
				}
				catch (IOException)
				{
				}
				catch (SecurityException)
				{
				}
			}
			return null;
		}

		/// <summary>Returns the current selected internal frame.</summary>
		/// <remarks>Returns the current selected internal frame.</remarks>
		private JInternalFrame GetSelectedFrame()
		{
			JInternalFrame[] frames = desk.GetAllFrames();
			for (int i = 0; i < frames.Length; i++)
			{
				if (frames[i].IsShowing())
				{
					return frames[i];
				}
			}
			return frames[frames.Length - 1];
		}

		/// <summary>
		/// Enables or disables the menu and tool bars with respect to the
		/// state of script execution.
		/// </summary>
		/// <remarks>
		/// Enables or disables the menu and tool bars with respect to the
		/// state of script execution.
		/// </remarks>
		private void UpdateEnabled(bool interrupted)
		{
			((Menubar)GetJMenuBar()).UpdateEnabled(interrupted);
			for (int ci = 0, cc = toolBar.GetComponentCount(); ci < cc; ci++)
			{
				bool enableButton;
				if (ci == 0)
				{
					// Break
					enableButton = !interrupted;
				}
				else
				{
					enableButton = interrupted;
				}
				toolBar.GetComponent(ci).SetEnabled(enableButton);
			}
			if (interrupted)
			{
				toolBar.SetEnabled(true);
				// raise the debugger window
				int state = GetExtendedState();
				if (state == Frame.ICONIFIED)
				{
					SetExtendedState(Frame.NORMAL);
				}
				ToFront();
				context.SetEnabled(true);
			}
			else
			{
				if (currentWindow != null)
				{
					currentWindow.SetPosition(-1);
				}
				context.SetEnabled(false);
			}
		}

		/// <summary>
		/// Calls
		/// <see cref="Javax.Swing.JSplitPane.SetResizeWeight(double)">Javax.Swing.JSplitPane.SetResizeWeight(double)</see>
		/// via reflection.
		/// For compatibility, since JDK &lt; 1.3 does not have this method.
		/// </summary>
		internal static void SetResizeWeight(JSplitPane pane, double weight)
		{
			try
			{
				MethodInfo m = typeof(JSplitPane).GetMethod("setResizeWeight", new Type[] { typeof(double) });
				m.Invoke(pane, new object[] { weight });
			}
			catch (MissingMethodException)
			{
			}
			catch (MemberAccessException)
			{
			}
			catch (TargetInvocationException)
			{
			}
		}

		/// <summary>Reads the file with the given name and returns its contents as a String.</summary>
		/// <remarks>Reads the file with the given name and returns its contents as a String.</remarks>
		private string ReadFile(string fileName)
		{
			string text;
			try
			{
				StreamReader r = new FileReader(fileName);
				try
				{
					text = Kit.ReadReader(r);
				}
				finally
				{
					r.Close();
				}
			}
			catch (IOException ex)
			{
				MessageDialogWrapper.ShowMessageDialog(this, ex.Message, "Error reading " + fileName, JOptionPane.ERROR_MESSAGE);
				text = null;
			}
			return text;
		}

		// GuiCallback
		/// <summary>Called when the source text for a script has been updated.</summary>
		/// <remarks>Called when the source text for a script has been updated.</remarks>
		public virtual void UpdateSourceText(Dim.SourceInfo sourceInfo)
		{
			RunProxy proxy = new RunProxy(this, RunProxy.UPDATE_SOURCE_TEXT);
			proxy.sourceInfo = sourceInfo;
			SwingUtilities.InvokeLater(proxy);
		}

		/// <summary>Called when the interrupt loop has been entered.</summary>
		/// <remarks>Called when the interrupt loop has been entered.</remarks>
		public virtual void EnterInterrupt(Dim.StackFrame lastFrame, string threadTitle, string alertMessage)
		{
			if (SwingUtilities.IsEventDispatchThread())
			{
				EnterInterruptImpl(lastFrame, threadTitle, alertMessage);
			}
			else
			{
				RunProxy proxy = new RunProxy(this, RunProxy.ENTER_INTERRUPT);
				proxy.lastFrame = lastFrame;
				proxy.threadTitle = threadTitle;
				proxy.alertMessage = alertMessage;
				SwingUtilities.InvokeLater(proxy);
			}
		}

		/// <summary>Returns whether the current thread is the GUI event thread.</summary>
		/// <remarks>Returns whether the current thread is the GUI event thread.</remarks>
		public virtual bool IsGuiEventThread()
		{
			return SwingUtilities.IsEventDispatchThread();
		}

		/// <summary>Processes the next GUI event.</summary>
		/// <remarks>Processes the next GUI event.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void DispatchNextGuiEvent()
		{
			EventQueue queue = awtEventQueue;
			if (queue == null)
			{
				queue = Toolkit.GetDefaultToolkit().GetSystemEventQueue();
				awtEventQueue = queue;
			}
			AWTEvent @event = queue.GetNextEvent();
			if (@event is ActiveEvent)
			{
				((ActiveEvent)@event).Dispatch();
			}
			else
			{
				object source = @event.GetSource();
				if (source is Component)
				{
					Component comp = (Component)source;
					comp.DispatchEvent(@event);
				}
				else
				{
					if (source is MenuComponent)
					{
						((MenuComponent)source).DispatchEvent(@event);
					}
				}
			}
		}

		// ActionListener
		/// <summary>Performs an action from the menu or toolbar.</summary>
		/// <remarks>Performs an action from the menu or toolbar.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			int returnValue = -1;
			if (cmd.Equals("Cut") || cmd.Equals("Copy") || cmd.Equals("Paste"))
			{
				JInternalFrame f = GetSelectedFrame();
				if (f != null && f is ActionListener)
				{
					((ActionListener)f).ActionPerformed(e);
				}
			}
			else
			{
				if (cmd.Equals("Step Over"))
				{
					returnValue = Dim.STEP_OVER;
				}
				else
				{
					if (cmd.Equals("Step Into"))
					{
						returnValue = Dim.STEP_INTO;
					}
					else
					{
						if (cmd.Equals("Step Out"))
						{
							returnValue = Dim.STEP_OUT;
						}
						else
						{
							if (cmd.Equals("Go"))
							{
								returnValue = Dim.GO;
							}
							else
							{
								if (cmd.Equals("Break"))
								{
									dim.SetBreak();
								}
								else
								{
									if (cmd.Equals("Exit"))
									{
										Exit();
									}
									else
									{
										if (cmd.Equals("Open"))
										{
											string fileName = ChooseFile("Select a file to compile");
											if (fileName != null)
											{
												string text = ReadFile(fileName);
												if (text != null)
												{
													RunProxy proxy = new RunProxy(this, RunProxy.OPEN_FILE);
													proxy.fileName = fileName;
													proxy.text = text;
													new Sharpen.Thread(proxy).Start();
												}
											}
										}
										else
										{
											if (cmd.Equals("Load"))
											{
												string fileName = ChooseFile("Select a file to execute");
												if (fileName != null)
												{
													string text = ReadFile(fileName);
													if (text != null)
													{
														RunProxy proxy = new RunProxy(this, RunProxy.LOAD_FILE);
														proxy.fileName = fileName;
														proxy.text = text;
														new Sharpen.Thread(proxy).Start();
													}
												}
											}
											else
											{
												if (cmd.Equals("More Windows..."))
												{
													MoreWindows dlg = new MoreWindows(this, fileWindows, "Window", "Files");
													dlg.ShowDialog(this);
												}
												else
												{
													if (cmd.Equals("Console"))
													{
														if (console.IsIcon())
														{
															desk.GetDesktopManager().DeiconifyFrame(console);
														}
														console.Show();
														desk.GetDesktopManager().ActivateFrame(console);
														console.consoleTextArea.RequestFocus();
													}
													else
													{
														if (cmd.Equals("Cut"))
														{
														}
														else
														{
															if (cmd.Equals("Copy"))
															{
															}
															else
															{
																if (cmd.Equals("Paste"))
																{
																}
																else
																{
																	if (cmd.Equals("Go to function..."))
																	{
																		FindFunction dlg = new FindFunction(this, "Go to function", "Function");
																		dlg.ShowDialog(this);
																	}
																	else
																	{
																		if (cmd.Equals("Tile"))
																		{
																			JInternalFrame[] frames = desk.GetAllFrames();
																			int count = frames.Length;
																			int rows;
																			int cols;
																			rows = cols = (int)Math.Sqrt(count);
																			if (rows * cols < count)
																			{
																				cols++;
																				if (rows * cols < count)
																				{
																					rows++;
																				}
																			}
																			Dimension size = desk.GetSize();
																			int w = size.width / cols;
																			int h = size.height / rows;
																			int x = 0;
																			int y = 0;
																			for (int i = 0; i < rows; i++)
																			{
																				for (int j = 0; j < cols; j++)
																				{
																					int index = (i * cols) + j;
																					if (index >= frames.Length)
																					{
																						break;
																					}
																					JInternalFrame f = frames[index];
																					try
																					{
																						f.SetIcon(false);
																						f.SetMaximum(false);
																					}
																					catch (Exception)
																					{
																					}
																					desk.GetDesktopManager().SetBoundsForFrame(f, x, y, w, h);
																					x += w;
																				}
																				y += h;
																				x = 0;
																			}
																		}
																		else
																		{
																			if (cmd.Equals("Cascade"))
																			{
																				JInternalFrame[] frames = desk.GetAllFrames();
																				int count = frames.Length;
																				int x;
																				int y;
																				int w;
																				int h;
																				x = y = 0;
																				h = desk.GetHeight();
																				int d = h / count;
																				if (d > 30)
																				{
																					d = 30;
																				}
																				for (int i = count - 1; i >= 0; i--, x += d, y += d)
																				{
																					JInternalFrame f = frames[i];
																					try
																					{
																						f.SetIcon(false);
																						f.SetMaximum(false);
																					}
																					catch (Exception)
																					{
																					}
																					Dimension dimen = f.GetPreferredSize();
																					w = dimen.width;
																					h = dimen.height;
																					desk.GetDesktopManager().SetBoundsForFrame(f, x, y, w, h);
																				}
																			}
																			else
																			{
																				object obj = GetFileWindow(cmd);
																				if (obj != null)
																				{
																					FileWindow w = (FileWindow)obj;
																					try
																					{
																						if (w.IsIcon())
																						{
																							w.SetIcon(false);
																						}
																						w.SetVisible(true);
																						w.MoveToFront();
																						w.SetSelected(true);
																					}
																					catch (Exception)
																					{
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			if (returnValue != -1)
			{
				UpdateEnabled(false);
				dim.SetReturnValue(returnValue);
			}
		}
	}

	/// <summary>Helper class for showing a message dialog.</summary>
	/// <remarks>Helper class for showing a message dialog.</remarks>
	internal class MessageDialogWrapper
	{
		/// <summary>
		/// Shows a message dialog, wrapping the <code>msg</code> at 60
		/// columns.
		/// </summary>
		/// <remarks>
		/// Shows a message dialog, wrapping the <code>msg</code> at 60
		/// columns.
		/// </remarks>
		public static void ShowMessageDialog(Component parent, string msg, string title, int flags)
		{
			if (msg.Length > 60)
			{
				StringBuilder buf = new StringBuilder();
				int len = msg.Length;
				int j = 0;
				int i;
				for (i = 0; i < len; i++, j++)
				{
					char c = msg[i];
					buf.Append(c);
					if (char.IsWhiteSpace(c))
					{
						int k;
						for (k = i + 1; k < len; k++)
						{
							if (char.IsWhiteSpace(msg[k]))
							{
								break;
							}
						}
						if (k < len)
						{
							int nextWordLen = k - i;
							if (j + nextWordLen > 60)
							{
								buf.Append('\n');
								j = 0;
							}
						}
					}
				}
				msg = buf.ToString();
			}
			JOptionPane.ShowMessageDialog(parent, msg, title, flags);
		}
	}

	/// <summary>Extension of JTextArea for script evaluation input.</summary>
	/// <remarks>Extension of JTextArea for script evaluation input.</remarks>
	[System.Serializable]
	internal class EvalTextArea : JTextArea, KeyListener, DocumentListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = -3918033649601064194L;

		/// <summary>The debugger GUI.</summary>
		/// <remarks>The debugger GUI.</remarks>
		private SwingGui debugGui;

		/// <summary>History of expressions that have been evaluated</summary>
		private IList<string> history;

		/// <summary>Index of the selected history item.</summary>
		/// <remarks>Index of the selected history item.</remarks>
		private int historyIndex = -1;

		/// <summary>Position in the display where output should go.</summary>
		/// <remarks>Position in the display where output should go.</remarks>
		private int outputMark;

		/// <summary>Creates a new EvalTextArea.</summary>
		/// <remarks>Creates a new EvalTextArea.</remarks>
		public EvalTextArea(SwingGui debugGui)
		{
			this.debugGui = debugGui;
			history = Sharpen.Collections.SynchronizedList(new AList<string>());
			Document doc = GetDocument();
			doc.AddDocumentListener(this);
			AddKeyListener(this);
			SetLineWrap(true);
			SetFont(new Font("Monospaced", 0, 12));
			Append("% ");
			outputMark = doc.GetLength();
		}

		/// <summary>Selects a subrange of the text.</summary>
		/// <remarks>Selects a subrange of the text.</remarks>
		public override void Select(int start, int end)
		{
			//requestFocus();
			base.Select(start, end);
		}

		/// <summary>Called when Enter is pressed.</summary>
		/// <remarks>Called when Enter is pressed.</remarks>
		private void ReturnPressed()
		{
			lock (this)
			{
				Document doc = GetDocument();
				int len = doc.GetLength();
				Segment segment = new Segment();
				try
				{
					doc.GetText(outputMark, len - outputMark, segment);
				}
				catch (BadLocationException ignored)
				{
					Sharpen.Runtime.PrintStackTrace(ignored);
				}
				string text = segment.ToString();
				if (debugGui.dim.StringIsCompilableUnit(text))
				{
					if (text.Trim().Length > 0)
					{
						history.AddItem(text);
						historyIndex = history.Count;
					}
					Append("\n");
					string result = debugGui.dim.Eval(text);
					if (result.Length > 0)
					{
						Append(result);
						Append("\n");
					}
					Append("% ");
					outputMark = doc.GetLength();
				}
				else
				{
					Append("\n");
				}
			}
		}

		/// <summary>Writes output into the text area.</summary>
		/// <remarks>Writes output into the text area.</remarks>
		public virtual void Write(string str)
		{
			lock (this)
			{
				Insert(str, outputMark);
				int len = str.Length;
				outputMark += len;
				Select(outputMark, outputMark);
			}
		}

		// KeyListener
		/// <summary>Called when a key is pressed.</summary>
		/// <remarks>Called when a key is pressed.</remarks>
		public virtual void KeyPressed(KeyEvent e)
		{
			int code = e.GetKeyCode();
			if (code == KeyEvent.VK_BACK_SPACE || code == KeyEvent.VK_LEFT)
			{
				if (outputMark == GetCaretPosition())
				{
					e.Consume();
				}
			}
			else
			{
				if (code == KeyEvent.VK_HOME)
				{
					int caretPos = GetCaretPosition();
					if (caretPos == outputMark)
					{
						e.Consume();
					}
					else
					{
						if (caretPos > outputMark)
						{
							if (!e.IsControlDown())
							{
								if (e.IsShiftDown())
								{
									MoveCaretPosition(outputMark);
								}
								else
								{
									SetCaretPosition(outputMark);
								}
								e.Consume();
							}
						}
					}
				}
				else
				{
					if (code == KeyEvent.VK_ENTER)
					{
						ReturnPressed();
						e.Consume();
					}
					else
					{
						if (code == KeyEvent.VK_UP)
						{
							historyIndex--;
							if (historyIndex >= 0)
							{
								if (historyIndex >= history.Count)
								{
									historyIndex = history.Count - 1;
								}
								if (historyIndex >= 0)
								{
									string str = history[historyIndex];
									int len = GetDocument().GetLength();
									ReplaceRange(str, outputMark, len);
									int caretPos = outputMark + str.Length;
									Select(caretPos, caretPos);
								}
								else
								{
									historyIndex++;
								}
							}
							else
							{
								historyIndex++;
							}
							e.Consume();
						}
						else
						{
							if (code == KeyEvent.VK_DOWN)
							{
								int caretPos = outputMark;
								if (history.Count > 0)
								{
									historyIndex++;
									if (historyIndex < 0)
									{
										historyIndex = 0;
									}
									int len = GetDocument().GetLength();
									if (historyIndex < history.Count)
									{
										string str = history[historyIndex];
										ReplaceRange(str, outputMark, len);
										caretPos = outputMark + str.Length;
									}
									else
									{
										historyIndex = history.Count;
										ReplaceRange(string.Empty, outputMark, len);
									}
								}
								Select(caretPos, caretPos);
								e.Consume();
							}
						}
					}
				}
			}
		}

		/// <summary>Called when a key is typed.</summary>
		/// <remarks>Called when a key is typed.</remarks>
		public virtual void KeyTyped(KeyEvent e)
		{
			int keyChar = e.GetKeyChar();
			if (keyChar == unchecked((int)(0x8)))
			{
				if (outputMark == GetCaretPosition())
				{
					e.Consume();
				}
			}
			else
			{
				if (GetCaretPosition() < outputMark)
				{
					SetCaretPosition(outputMark);
				}
			}
		}

		/// <summary>Called when a key is released.</summary>
		/// <remarks>Called when a key is released.</remarks>
		public virtual void KeyReleased(KeyEvent e)
		{
			lock (this)
			{
			}
		}

		// DocumentListener
		/// <summary>Called when text was inserted into the text area.</summary>
		/// <remarks>Called when text was inserted into the text area.</remarks>
		public virtual void InsertUpdate(DocumentEvent e)
		{
			lock (this)
			{
				int len = e.GetLength();
				int off = e.GetOffset();
				if (outputMark > off)
				{
					outputMark += len;
				}
			}
		}

		/// <summary>Called when text was removed from the text area.</summary>
		/// <remarks>Called when text was removed from the text area.</remarks>
		public virtual void RemoveUpdate(DocumentEvent e)
		{
			lock (this)
			{
				int len = e.GetLength();
				int off = e.GetOffset();
				if (outputMark > off)
				{
					if (outputMark >= off + len)
					{
						outputMark -= len;
					}
					else
					{
						outputMark = off;
					}
				}
			}
		}

		/// <summary>
		/// Attempts to clean up the damage done by
		/// <see cref="Javax.Swing.Text.JTextComponent.UpdateUI()">Javax.Swing.Text.JTextComponent.UpdateUI()</see>
		/// .
		/// </summary>
		public virtual void PostUpdateUI()
		{
			lock (this)
			{
				//requestFocus();
				SetCaret(GetCaret());
				Select(outputMark, outputMark);
			}
		}

		/// <summary>Called when text has changed in the text area.</summary>
		/// <remarks>Called when text has changed in the text area.</remarks>
		public virtual void ChangedUpdate(DocumentEvent e)
		{
			lock (this)
			{
			}
		}
	}

	/// <summary>An internal frame for evaluating script.</summary>
	/// <remarks>An internal frame for evaluating script.</remarks>
	[System.Serializable]
	internal class EvalWindow : JInternalFrame, ActionListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = -2860585845212160176L;

		/// <summary>The text area into which expressions can be typed.</summary>
		/// <remarks>The text area into which expressions can be typed.</remarks>
		private EvalTextArea evalTextArea;

		/// <summary>Creates a new EvalWindow.</summary>
		/// <remarks>Creates a new EvalWindow.</remarks>
		public EvalWindow(string name, SwingGui debugGui) : base(name, true, false, true, true)
		{
			evalTextArea = new EvalTextArea(debugGui);
			evalTextArea.SetRows(24);
			evalTextArea.SetColumns(80);
			JScrollPane scroller = new JScrollPane(evalTextArea);
			SetContentPane(scroller);
			//scroller.setPreferredSize(new Dimension(600, 400));
			Pack();
			SetVisible(true);
		}

		/// <summary>Sets whether the text area is enabled.</summary>
		/// <remarks>Sets whether the text area is enabled.</remarks>
		public override void SetEnabled(bool b)
		{
			base.SetEnabled(b);
			evalTextArea.SetEnabled(b);
		}

		// ActionListener
		/// <summary>Performs an action on the text area.</summary>
		/// <remarks>Performs an action on the text area.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			if (cmd.Equals("Cut"))
			{
				evalTextArea.Cut();
			}
			else
			{
				if (cmd.Equals("Copy"))
				{
					evalTextArea.Copy();
				}
				else
				{
					if (cmd.Equals("Paste"))
					{
						evalTextArea.Paste();
					}
				}
			}
		}
	}

	/// <summary>Internal frame for the console.</summary>
	/// <remarks>Internal frame for the console.</remarks>
	[System.Serializable]
	internal class JSInternalConsole : JInternalFrame, ActionListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = -5523468828771087292L;

		/// <summary>Creates a new JSInternalConsole.</summary>
		/// <remarks>Creates a new JSInternalConsole.</remarks>
		public JSInternalConsole(string name) : base(name, true, false, true, true)
		{
			consoleTextArea = new ConsoleTextArea(null);
			consoleTextArea.SetRows(24);
			consoleTextArea.SetColumns(80);
			JScrollPane scroller = new JScrollPane(consoleTextArea);
			SetContentPane(scroller);
			Pack();
			AddInternalFrameListener(new _InternalFrameAdapter_1287(this));
		}

		private sealed class _InternalFrameAdapter_1287 : InternalFrameAdapter
		{
			public _InternalFrameAdapter_1287(JSInternalConsole _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void InternalFrameActivated(InternalFrameEvent e)
			{
				// hack
				if (this._enclosing.consoleTextArea.HasFocus())
				{
					this._enclosing.consoleTextArea.GetCaret().SetVisible(false);
					this._enclosing.consoleTextArea.GetCaret().SetVisible(true);
				}
			}

			private readonly JSInternalConsole _enclosing;
		}

		/// <summary>The console text area.</summary>
		/// <remarks>The console text area.</remarks>
		internal ConsoleTextArea consoleTextArea;

		/// <summary>Returns the input stream of the console text area.</summary>
		/// <remarks>Returns the input stream of the console text area.</remarks>
		public virtual InputStream GetIn()
		{
			return consoleTextArea.GetIn();
		}

		/// <summary>Returns the output stream of the console text area.</summary>
		/// <remarks>Returns the output stream of the console text area.</remarks>
		public virtual TextWriter GetOut()
		{
			return consoleTextArea.GetOut();
		}

		/// <summary>Returns the error stream of the console text area.</summary>
		/// <remarks>Returns the error stream of the console text area.</remarks>
		public virtual TextWriter GetErr()
		{
			return consoleTextArea.GetErr();
		}

		// ActionListener
		/// <summary>Performs an action on the text area.</summary>
		/// <remarks>Performs an action on the text area.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			if (cmd.Equals("Cut"))
			{
				consoleTextArea.Cut();
			}
			else
			{
				if (cmd.Equals("Copy"))
				{
					consoleTextArea.Copy();
				}
				else
				{
					if (cmd.Equals("Paste"))
					{
						consoleTextArea.Paste();
					}
				}
			}
		}
	}

	/// <summary>
	/// Popup menu class for right-clicking on
	/// <see cref="FileTextArea">FileTextArea</see>
	/// s.
	/// </summary>
	[System.Serializable]
	internal class FilePopupMenu : JPopupMenu
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 3589525009546013565L;

		/// <summary>The popup x position.</summary>
		/// <remarks>The popup x position.</remarks>
		internal int x;

		/// <summary>The popup y position.</summary>
		/// <remarks>The popup y position.</remarks>
		internal int y;

		/// <summary>Creates a new FilePopupMenu.</summary>
		/// <remarks>Creates a new FilePopupMenu.</remarks>
		public FilePopupMenu(FileTextArea w)
		{
			JMenuItem item;
			Add(item = new JMenuItem("Set Breakpoint"));
			item.AddActionListener(w);
			Add(item = new JMenuItem("Clear Breakpoint"));
			item.AddActionListener(w);
			Add(item = new JMenuItem("Run"));
			item.AddActionListener(w);
		}

		/// <summary>Displays the menu at the given coordinates.</summary>
		/// <remarks>Displays the menu at the given coordinates.</remarks>
		public virtual void Show(JComponent comp, int x, int y)
		{
			this.x = x;
			this.y = y;
			base.Show(comp, x, y);
		}
	}

	/// <summary>Text area to display script source.</summary>
	/// <remarks>Text area to display script source.</remarks>
	[System.Serializable]
	internal class FileTextArea : JTextArea, ActionListener, PopupMenuListener, KeyListener, MouseListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = -25032065448563720L;

		/// <summary>
		/// The owning
		/// <see cref="FileWindow">FileWindow</see>
		/// .
		/// </summary>
		private FileWindow w;

		/// <summary>The popup menu.</summary>
		/// <remarks>The popup menu.</remarks>
		private FilePopupMenu popup;

		/// <summary>Creates a new FileTextArea.</summary>
		/// <remarks>Creates a new FileTextArea.</remarks>
		public FileTextArea(FileWindow w)
		{
			this.w = w;
			popup = new FilePopupMenu(this);
			popup.AddPopupMenuListener(this);
			AddMouseListener(this);
			AddKeyListener(this);
			SetFont(new Font("Monospaced", 0, 12));
		}

		/// <summary>Moves the selection to the given offset.</summary>
		/// <remarks>Moves the selection to the given offset.</remarks>
		public virtual void Select(int pos)
		{
			if (pos >= 0)
			{
				try
				{
					int line = GetLineOfOffset(pos);
					Rectangle rect = ModelToView(pos);
					if (rect == null)
					{
						Select(pos, pos);
					}
					else
					{
						try
						{
							Rectangle nrect = ModelToView(GetLineStartOffset(line + 1));
							if (nrect != null)
							{
								rect = nrect;
							}
						}
						catch (Exception)
						{
						}
						JViewport vp = (JViewport)GetParent();
						Rectangle viewRect = vp.GetViewRect();
						if (viewRect.y + viewRect.height > rect.y)
						{
							// need to scroll up
							Select(pos, pos);
						}
						else
						{
							// need to scroll down
							rect.y += (viewRect.height - rect.height) / 2;
							ScrollRectToVisible(rect);
							Select(pos, pos);
						}
					}
				}
				catch (BadLocationException)
				{
					Select(pos, pos);
				}
			}
		}

		//exc.printStackTrace();
		/// <summary>Checks if the popup menu should be shown.</summary>
		/// <remarks>Checks if the popup menu should be shown.</remarks>
		private void CheckPopup(MouseEvent e)
		{
			if (e.IsPopupTrigger())
			{
				popup.Show(this, e.GetX(), e.GetY());
			}
		}

		// MouseListener
		/// <summary>Called when a mouse button is pressed.</summary>
		/// <remarks>Called when a mouse button is pressed.</remarks>
		public virtual void MousePressed(MouseEvent e)
		{
			CheckPopup(e);
		}

		/// <summary>Called when the mouse is clicked.</summary>
		/// <remarks>Called when the mouse is clicked.</remarks>
		public virtual void MouseClicked(MouseEvent e)
		{
			CheckPopup(e);
			RequestFocus();
			GetCaret().SetVisible(true);
		}

		/// <summary>Called when the mouse enters the component.</summary>
		/// <remarks>Called when the mouse enters the component.</remarks>
		public virtual void MouseEntered(MouseEvent e)
		{
		}

		/// <summary>Called when the mouse exits the component.</summary>
		/// <remarks>Called when the mouse exits the component.</remarks>
		public virtual void MouseExited(MouseEvent e)
		{
		}

		/// <summary>Called when a mouse button is released.</summary>
		/// <remarks>Called when a mouse button is released.</remarks>
		public virtual void MouseReleased(MouseEvent e)
		{
			CheckPopup(e);
		}

		// PopupMenuListener
		/// <summary>Called before the popup menu will become visible.</summary>
		/// <remarks>Called before the popup menu will become visible.</remarks>
		public virtual void PopupMenuWillBecomeVisible(PopupMenuEvent e)
		{
		}

		/// <summary>Called before the popup menu will become invisible.</summary>
		/// <remarks>Called before the popup menu will become invisible.</remarks>
		public virtual void PopupMenuWillBecomeInvisible(PopupMenuEvent e)
		{
		}

		/// <summary>Called when the popup menu is cancelled.</summary>
		/// <remarks>Called when the popup menu is cancelled.</remarks>
		public virtual void PopupMenuCanceled(PopupMenuEvent e)
		{
		}

		// ActionListener
		/// <summary>Performs an action.</summary>
		/// <remarks>Performs an action.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			int pos = ViewToModel(new Point(popup.x, popup.y));
			popup.SetVisible(false);
			string cmd = e.GetActionCommand();
			int line = -1;
			try
			{
				line = GetLineOfOffset(pos);
			}
			catch (Exception)
			{
			}
			if (cmd.Equals("Set Breakpoint"))
			{
				w.SetBreakPoint(line + 1);
			}
			else
			{
				if (cmd.Equals("Clear Breakpoint"))
				{
					w.ClearBreakPoint(line + 1);
				}
				else
				{
					if (cmd.Equals("Run"))
					{
						w.Load();
					}
				}
			}
		}

		// KeyListener
		/// <summary>Called when a key is pressed.</summary>
		/// <remarks>Called when a key is pressed.</remarks>
		public virtual void KeyPressed(KeyEvent e)
		{
			switch (e.GetKeyCode())
			{
				case KeyEvent.VK_BACK_SPACE:
				case KeyEvent.VK_ENTER:
				case KeyEvent.VK_DELETE:
				case KeyEvent.VK_TAB:
				{
					e.Consume();
					break;
				}
			}
		}

		/// <summary>Called when a key is typed.</summary>
		/// <remarks>Called when a key is typed.</remarks>
		public virtual void KeyTyped(KeyEvent e)
		{
			e.Consume();
		}

		/// <summary>Called when a key is released.</summary>
		/// <remarks>Called when a key is released.</remarks>
		public virtual void KeyReleased(KeyEvent e)
		{
			e.Consume();
		}
	}

	/// <summary>Dialog to list the available windows.</summary>
	/// <remarks>Dialog to list the available windows.</remarks>
	[System.Serializable]
	internal class MoreWindows : JDialog, ActionListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 5177066296457377546L;

		/// <summary>Last selected value.</summary>
		/// <remarks>Last selected value.</remarks>
		private string value;

		/// <summary>The list component.</summary>
		/// <remarks>The list component.</remarks>
		private JList list;

		/// <summary>Our parent frame.</summary>
		/// <remarks>Our parent frame.</remarks>
		private SwingGui swingGui;

		/// <summary>The "Select" button.</summary>
		/// <remarks>The "Select" button.</remarks>
		private JButton setButton;

		/// <summary>The "Cancel" button.</summary>
		/// <remarks>The "Cancel" button.</remarks>
		private JButton cancelButton;

		/// <summary>Creates a new MoreWindows.</summary>
		/// <remarks>Creates a new MoreWindows.</remarks>
		internal MoreWindows(SwingGui frame, IDictionary<string, FileWindow> fileWindows, string title, string labelText) : base(frame, title, true)
		{
			this.swingGui = frame;
			//buttons
			cancelButton = new JButton("Cancel");
			setButton = new JButton("Select");
			cancelButton.AddActionListener(this);
			setButton.AddActionListener(this);
			GetRootPane().SetDefaultButton(setButton);
			//dim part of the dialog
			list = new JList(new DefaultListModel());
			DefaultListModel model = (DefaultListModel)list.GetModel();
			model.Clear();
			//model.fireIntervalRemoved(model, 0, size);
			foreach (string data in fileWindows.Keys)
			{
				model.AddElement(data);
			}
			list.SetSelectedIndex(0);
			//model.fireIntervalAdded(model, 0, data.length);
			setButton.SetEnabled(true);
			list.SetSelectionMode(ListSelectionModelConstants.SINGLE_INTERVAL_SELECTION);
			list.AddMouseListener(new MoreWindows.MouseHandler(this));
			JScrollPane listScroller = new JScrollPane(list);
			listScroller.SetPreferredSize(new Dimension(320, 240));
			//XXX: Must do the following, too, or else the scroller thinks
			//XXX: it's taller than it is:
			listScroller.SetMinimumSize(new Dimension(250, 80));
			listScroller.SetAlignmentX(LEFT_ALIGNMENT);
			//Create a container so that we can add a title around
			//the scroll pane.  Can't add a title directly to the
			//scroll pane because its background would be white.
			//Lay out the label and scroll pane from top to button.
			JPanel listPane = new JPanel();
			listPane.SetLayout(new BoxLayout(listPane, BoxLayout.Y_AXIS));
			JLabel label = new JLabel(labelText);
			label.SetLabelFor(list);
			listPane.Add(label);
			listPane.Add(Box.CreateRigidArea(new Dimension(0, 5)));
			listPane.Add(listScroller);
			listPane.SetBorder(BorderFactory.CreateEmptyBorder(10, 10, 10, 10));
			//Lay out the buttons from left to right.
			JPanel buttonPane = new JPanel();
			buttonPane.SetLayout(new BoxLayout(buttonPane, BoxLayout.X_AXIS));
			buttonPane.SetBorder(BorderFactory.CreateEmptyBorder(0, 10, 10, 10));
			buttonPane.Add(Box.CreateHorizontalGlue());
			buttonPane.Add(cancelButton);
			buttonPane.Add(Box.CreateRigidArea(new Dimension(10, 0)));
			buttonPane.Add(setButton);
			//Put everything together, using the content pane's BorderLayout.
			Container contentPane = GetContentPane();
			contentPane.Add(listPane, BorderLayout.CENTER);
			contentPane.Add(buttonPane, BorderLayout.SOUTH);
			Pack();
			AddKeyListener(new _KeyAdapter_1673(this));
		}

		private sealed class _KeyAdapter_1673 : KeyAdapter
		{
			public _KeyAdapter_1673(MoreWindows _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void KeyPressed(KeyEvent ke)
			{
				int code = ke.GetKeyCode();
				if (code == KeyEvent.VK_ESCAPE)
				{
					ke.Consume();
					this._enclosing.value = null;
					this._enclosing.SetVisible(false);
				}
			}

			private readonly MoreWindows _enclosing;
		}

		/// <summary>Shows the dialog.</summary>
		/// <remarks>Shows the dialog.</remarks>
		public virtual string ShowDialog(Component comp)
		{
			value = null;
			SetLocationRelativeTo(comp);
			SetVisible(true);
			return value;
		}

		// ActionListener
		/// <summary>Performs an action.</summary>
		/// <remarks>Performs an action.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			if (cmd.Equals("Cancel"))
			{
				SetVisible(false);
				value = null;
			}
			else
			{
				if (cmd.Equals("Select"))
				{
					value = (string)list.GetSelectedValue();
					SetVisible(false);
					swingGui.ShowFileWindow(value, -1);
				}
			}
		}

		/// <summary>
		/// MouseListener implementation for
		/// <see cref="MoreWindows.list">MoreWindows.list</see>
		/// .
		/// </summary>
		private class MouseHandler : MouseAdapter
		{
			public override void MouseClicked(MouseEvent e)
			{
				if (e.GetClickCount() == 2)
				{
					this._enclosing.setButton.DoClick();
				}
			}

			internal MouseHandler(MoreWindows _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly MoreWindows _enclosing;
		}
	}

	/// <summary>Find function dialog.</summary>
	/// <remarks>Find function dialog.</remarks>
	[System.Serializable]
	internal class FindFunction : JDialog, ActionListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 559491015232880916L;

		/// <summary>Last selected function.</summary>
		/// <remarks>Last selected function.</remarks>
		private string value;

		/// <summary>List of functions.</summary>
		/// <remarks>List of functions.</remarks>
		private JList list;

		/// <summary>The debug GUI frame.</summary>
		/// <remarks>The debug GUI frame.</remarks>
		private SwingGui debugGui;

		/// <summary>The "Select" button.</summary>
		/// <remarks>The "Select" button.</remarks>
		private JButton setButton;

		/// <summary>The "Cancel" button.</summary>
		/// <remarks>The "Cancel" button.</remarks>
		private JButton cancelButton;

		/// <summary>Creates a new FindFunction.</summary>
		/// <remarks>Creates a new FindFunction.</remarks>
		public FindFunction(SwingGui debugGui, string title, string labelText) : base(debugGui, title, true)
		{
			this.debugGui = debugGui;
			cancelButton = new JButton("Cancel");
			setButton = new JButton("Select");
			cancelButton.AddActionListener(this);
			setButton.AddActionListener(this);
			GetRootPane().SetDefaultButton(setButton);
			list = new JList(new DefaultListModel());
			DefaultListModel model = (DefaultListModel)list.GetModel();
			model.Clear();
			string[] a = debugGui.dim.FunctionNames();
			Arrays.Sort(a);
			for (int i = 0; i < a.Length; i++)
			{
				model.AddElement(a[i]);
			}
			list.SetSelectedIndex(0);
			setButton.SetEnabled(a.Length > 0);
			list.SetSelectionMode(ListSelectionModelConstants.SINGLE_INTERVAL_SELECTION);
			list.AddMouseListener(new FindFunction.MouseHandler(this));
			JScrollPane listScroller = new JScrollPane(list);
			listScroller.SetPreferredSize(new Dimension(320, 240));
			listScroller.SetMinimumSize(new Dimension(250, 80));
			listScroller.SetAlignmentX(LEFT_ALIGNMENT);
			//Create a container so that we can add a title around
			//the scroll pane.  Can't add a title directly to the
			//scroll pane because its background would be white.
			//Lay out the label and scroll pane from top to button.
			JPanel listPane = new JPanel();
			listPane.SetLayout(new BoxLayout(listPane, BoxLayout.Y_AXIS));
			JLabel label = new JLabel(labelText);
			label.SetLabelFor(list);
			listPane.Add(label);
			listPane.Add(Box.CreateRigidArea(new Dimension(0, 5)));
			listPane.Add(listScroller);
			listPane.SetBorder(BorderFactory.CreateEmptyBorder(10, 10, 10, 10));
			//Lay out the buttons from left to right.
			JPanel buttonPane = new JPanel();
			buttonPane.SetLayout(new BoxLayout(buttonPane, BoxLayout.X_AXIS));
			buttonPane.SetBorder(BorderFactory.CreateEmptyBorder(0, 10, 10, 10));
			buttonPane.Add(Box.CreateHorizontalGlue());
			buttonPane.Add(cancelButton);
			buttonPane.Add(Box.CreateRigidArea(new Dimension(10, 0)));
			buttonPane.Add(setButton);
			//Put everything together, using the content pane's BorderLayout.
			Container contentPane = GetContentPane();
			contentPane.Add(listPane, BorderLayout.CENTER);
			contentPane.Add(buttonPane, BorderLayout.SOUTH);
			Pack();
			AddKeyListener(new _KeyAdapter_1820(this));
		}

		private sealed class _KeyAdapter_1820 : KeyAdapter
		{
			public _KeyAdapter_1820(FindFunction _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override void KeyPressed(KeyEvent ke)
			{
				int code = ke.GetKeyCode();
				if (code == KeyEvent.VK_ESCAPE)
				{
					ke.Consume();
					this._enclosing.value = null;
					this._enclosing.SetVisible(false);
				}
			}

			private readonly FindFunction _enclosing;
		}

		/// <summary>Shows the dialog.</summary>
		/// <remarks>Shows the dialog.</remarks>
		public virtual string ShowDialog(Component comp)
		{
			value = null;
			SetLocationRelativeTo(comp);
			SetVisible(true);
			return value;
		}

		// ActionListener
		/// <summary>Performs an action.</summary>
		/// <remarks>Performs an action.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			if (cmd.Equals("Cancel"))
			{
				SetVisible(false);
				value = null;
			}
			else
			{
				if (cmd.Equals("Select"))
				{
					if (list.GetSelectedIndex() < 0)
					{
						return;
					}
					try
					{
						value = (string)list.GetSelectedValue();
					}
					catch (IndexOutOfRangeException)
					{
						return;
					}
					SetVisible(false);
					Dim.FunctionSource item = debugGui.dim.FunctionSourceByName(value);
					if (item != null)
					{
						Dim.SourceInfo si = item.SourceInfo();
						string url = si.Url();
						int lineNumber = item.FirstLine();
						debugGui.ShowFileWindow(url, lineNumber);
					}
				}
			}
		}

		/// <summary>
		/// MouseListener implementation for
		/// <see cref="FindFunction.list">FindFunction.list</see>
		/// .
		/// </summary>
		internal class MouseHandler : MouseAdapter
		{
			public override void MouseClicked(MouseEvent e)
			{
				if (e.GetClickCount() == 2)
				{
					this._enclosing.setButton.DoClick();
				}
			}

			internal MouseHandler(FindFunction _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly FindFunction _enclosing;
		}
	}

	/// <summary>Gutter for FileWindows.</summary>
	/// <remarks>Gutter for FileWindows.</remarks>
	[System.Serializable]
	internal class FileHeader : JPanel, MouseListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = -2858905404778259127L;

		/// <summary>The line that the mouse was pressed on.</summary>
		/// <remarks>The line that the mouse was pressed on.</remarks>
		private int pressLine = -1;

		/// <summary>The owning FileWindow.</summary>
		/// <remarks>The owning FileWindow.</remarks>
		private FileWindow fileWindow;

		/// <summary>Creates a new FileHeader.</summary>
		/// <remarks>Creates a new FileHeader.</remarks>
		public FileHeader(FileWindow fileWindow)
		{
			this.fileWindow = fileWindow;
			AddMouseListener(this);
			Update();
		}

		/// <summary>Updates the gutter.</summary>
		/// <remarks>Updates the gutter.</remarks>
		public virtual void Update()
		{
			FileTextArea textArea = fileWindow.textArea;
			Font font = textArea.GetFont();
			SetFont(font);
			FontMetrics metrics = GetFontMetrics(font);
			int h = metrics.GetHeight();
			int lineCount = textArea.GetLineCount() + 1;
			string dummy = Sharpen.Extensions.ToString(lineCount);
			if (dummy.Length < 2)
			{
				dummy = "99";
			}
			Dimension d = new Dimension();
			d.width = metrics.StringWidth(dummy) + 16;
			d.height = lineCount * h + 100;
			SetPreferredSize(d);
			SetSize(d);
		}

		/// <summary>Paints the component.</summary>
		/// <remarks>Paints the component.</remarks>
		public override void Paint(Graphics g)
		{
			base.Paint(g);
			FileTextArea textArea = fileWindow.textArea;
			Font font = textArea.GetFont();
			g.SetFont(font);
			FontMetrics metrics = GetFontMetrics(font);
			Rectangle clip = g.GetClipBounds();
			g.SetColor(GetBackground());
			g.FillRect(clip.x, clip.y, clip.width, clip.height);
			int ascent = metrics.GetMaxAscent();
			int h = metrics.GetHeight();
			int lineCount = textArea.GetLineCount() + 1;
			string dummy = Sharpen.Extensions.ToString(lineCount);
			if (dummy.Length < 2)
			{
				dummy = "99";
			}
			int startLine = clip.y / h;
			int endLine = (clip.y + clip.height) / h + 1;
			int width = GetWidth();
			if (endLine > lineCount)
			{
				endLine = lineCount;
			}
			for (int i = startLine; i < endLine; i++)
			{
				string text;
				int pos = -2;
				try
				{
					pos = textArea.GetLineStartOffset(i);
				}
				catch (BadLocationException)
				{
				}
				bool isBreakPoint = fileWindow.IsBreakPoint(i + 1);
				text = Sharpen.Extensions.ToString(i + 1) + " ";
				int y = i * h;
				g.SetColor(Color.blue);
				g.DrawString(text, 0, y + ascent);
				int x = width - ascent;
				if (isBreakPoint)
				{
					g.SetColor(new Color(unchecked((int)(0x80)), unchecked((int)(0x00)), unchecked((int)(0x00))));
					int dy = y + ascent - 9;
					g.FillOval(x, dy, 9, 9);
					g.DrawOval(x, dy, 8, 8);
					g.DrawOval(x, dy, 9, 9);
				}
				if (pos == fileWindow.currentPos)
				{
					Polygon arrow = new Polygon();
					int dx = x;
					y += ascent - 10;
					int dy = y;
					arrow.AddPoint(dx, dy + 3);
					arrow.AddPoint(dx + 5, dy + 3);
					for (x = dx + 5; x <= dx + 10; x++, y++)
					{
						arrow.AddPoint(x, y);
					}
					for (x = dx + 9; x >= dx + 5; x--, y++)
					{
						arrow.AddPoint(x, y);
					}
					arrow.AddPoint(dx + 5, dy + 7);
					arrow.AddPoint(dx, dy + 7);
					g.SetColor(Color.yellow);
					g.FillPolygon(arrow);
					g.SetColor(Color.black);
					g.DrawPolygon(arrow);
				}
			}
		}

		// MouseListener
		/// <summary>Called when the mouse enters the component.</summary>
		/// <remarks>Called when the mouse enters the component.</remarks>
		public virtual void MouseEntered(MouseEvent e)
		{
		}

		/// <summary>Called when a mouse button is pressed.</summary>
		/// <remarks>Called when a mouse button is pressed.</remarks>
		public virtual void MousePressed(MouseEvent e)
		{
			Font font = fileWindow.textArea.GetFont();
			FontMetrics metrics = GetFontMetrics(font);
			int h = metrics.GetHeight();
			pressLine = e.GetY() / h;
		}

		/// <summary>Called when the mouse is clicked.</summary>
		/// <remarks>Called when the mouse is clicked.</remarks>
		public virtual void MouseClicked(MouseEvent e)
		{
		}

		/// <summary>Called when the mouse exits the component.</summary>
		/// <remarks>Called when the mouse exits the component.</remarks>
		public virtual void MouseExited(MouseEvent e)
		{
		}

		/// <summary>Called when a mouse button is released.</summary>
		/// <remarks>Called when a mouse button is released.</remarks>
		public virtual void MouseReleased(MouseEvent e)
		{
			if (e.GetComponent() == this && (e.GetModifiers() & MouseEvent.BUTTON1_MASK) != 0)
			{
				int y = e.GetY();
				Font font = fileWindow.textArea.GetFont();
				FontMetrics metrics = GetFontMetrics(font);
				int h = metrics.GetHeight();
				int line = y / h;
				if (line == pressLine)
				{
					fileWindow.ToggleBreakPoint(line + 1);
				}
				else
				{
					pressLine = -1;
				}
			}
		}
	}

	/// <summary>An internal frame for script files.</summary>
	/// <remarks>An internal frame for script files.</remarks>
	[System.Serializable]
	internal class FileWindow : JInternalFrame, ActionListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = -6212382604952082370L;

		/// <summary>The debugger GUI.</summary>
		/// <remarks>The debugger GUI.</remarks>
		private SwingGui debugGui;

		/// <summary>The SourceInfo object that describes the file.</summary>
		/// <remarks>The SourceInfo object that describes the file.</remarks>
		private Dim.SourceInfo sourceInfo;

		/// <summary>The FileTextArea that displays the file.</summary>
		/// <remarks>The FileTextArea that displays the file.</remarks>
		internal FileTextArea textArea;

		/// <summary>
		/// The FileHeader that is the gutter for
		/// <see cref="textArea">textArea</see>
		/// .
		/// </summary>
		private FileHeader fileHeader;

		/// <summary>
		/// Scroll pane for containing
		/// <see cref="textArea">textArea</see>
		/// .
		/// </summary>
		private JScrollPane p;

		/// <summary>The current offset position.</summary>
		/// <remarks>The current offset position.</remarks>
		internal int currentPos;

		/// <summary>Loads the file.</summary>
		/// <remarks>Loads the file.</remarks>
		internal virtual void Load()
		{
			string url = GetUrl();
			if (url != null)
			{
				RunProxy proxy = new RunProxy(debugGui, RunProxy.LOAD_FILE);
				proxy.fileName = url;
				proxy.text = sourceInfo.Source();
				new Sharpen.Thread(proxy).Start();
			}
		}

		/// <summary>Returns the offset position for the given line.</summary>
		/// <remarks>Returns the offset position for the given line.</remarks>
		public virtual int GetPosition(int line)
		{
			int result = -1;
			try
			{
				result = textArea.GetLineStartOffset(line);
			}
			catch (BadLocationException)
			{
			}
			return result;
		}

		/// <summary>Returns whether the given line has a breakpoint.</summary>
		/// <remarks>Returns whether the given line has a breakpoint.</remarks>
		public virtual bool IsBreakPoint(int line)
		{
			return sourceInfo.BreakableLine(line) && sourceInfo.Breakpoint(line);
		}

		/// <summary>Toggles the breakpoint on the given line.</summary>
		/// <remarks>Toggles the breakpoint on the given line.</remarks>
		public virtual void ToggleBreakPoint(int line)
		{
			if (!IsBreakPoint(line))
			{
				SetBreakPoint(line);
			}
			else
			{
				ClearBreakPoint(line);
			}
		}

		/// <summary>Sets a breakpoint on the given line.</summary>
		/// <remarks>Sets a breakpoint on the given line.</remarks>
		public virtual void SetBreakPoint(int line)
		{
			if (sourceInfo.BreakableLine(line))
			{
				bool changed = sourceInfo.Breakpoint(line, true);
				if (changed)
				{
					fileHeader.Repaint();
				}
			}
		}

		/// <summary>Clears a breakpoint from the given line.</summary>
		/// <remarks>Clears a breakpoint from the given line.</remarks>
		public virtual void ClearBreakPoint(int line)
		{
			if (sourceInfo.BreakableLine(line))
			{
				bool changed = sourceInfo.Breakpoint(line, false);
				if (changed)
				{
					fileHeader.Repaint();
				}
			}
		}

		/// <summary>Creates a new FileWindow.</summary>
		/// <remarks>Creates a new FileWindow.</remarks>
		public FileWindow(SwingGui debugGui, Dim.SourceInfo sourceInfo) : base(SwingGui.GetShortName(sourceInfo.Url()), true, true, true, true)
		{
			this.debugGui = debugGui;
			this.sourceInfo = sourceInfo;
			UpdateToolTip();
			currentPos = -1;
			textArea = new FileTextArea(this);
			textArea.SetRows(24);
			textArea.SetColumns(80);
			p = new JScrollPane();
			fileHeader = new FileHeader(this);
			p.SetViewportView(textArea);
			p.SetRowHeaderView(fileHeader);
			SetContentPane(p);
			Pack();
			UpdateText(sourceInfo);
			textArea.Select(0);
		}

		/// <summary>Updates the tool tip contents.</summary>
		/// <remarks>Updates the tool tip contents.</remarks>
		private void UpdateToolTip()
		{
			// Try to set tool tip on frame. On Mac OS X 10.5,
			// the number of components is different, so try to be safe.
			int n = GetComponentCount() - 1;
			if (n > 1)
			{
				n = 1;
			}
			else
			{
				if (n < 0)
				{
					return;
				}
			}
			Component c = GetComponent(n);
			// this will work at least for Metal L&F
			if (c != null && c is JComponent)
			{
				((JComponent)c).SetToolTipText(GetUrl());
			}
		}

		/// <summary>Returns the URL of the source.</summary>
		/// <remarks>Returns the URL of the source.</remarks>
		public virtual string GetUrl()
		{
			return sourceInfo.Url();
		}

		/// <summary>Called when the text of the script has changed.</summary>
		/// <remarks>Called when the text of the script has changed.</remarks>
		public virtual void UpdateText(Dim.SourceInfo sourceInfo)
		{
			this.sourceInfo = sourceInfo;
			string newText = sourceInfo.Source();
			if (!textArea.GetText().Equals(newText))
			{
				textArea.SetText(newText);
				int pos = 0;
				if (currentPos != -1)
				{
					pos = currentPos;
				}
				textArea.Select(pos);
			}
			fileHeader.Update();
			fileHeader.Repaint();
		}

		/// <summary>Sets the cursor position.</summary>
		/// <remarks>Sets the cursor position.</remarks>
		public virtual void SetPosition(int pos)
		{
			textArea.Select(pos);
			currentPos = pos;
			fileHeader.Repaint();
		}

		/// <summary>Selects a range of characters.</summary>
		/// <remarks>Selects a range of characters.</remarks>
		public virtual void Select(int start, int end)
		{
			int docEnd = textArea.GetDocument().GetLength();
			textArea.Select(docEnd, docEnd);
			textArea.Select(start, end);
		}

		/// <summary>Disposes this FileWindow.</summary>
		/// <remarks>Disposes this FileWindow.</remarks>
		public override void Dispose()
		{
			debugGui.RemoveWindow(this);
			base.Dispose();
		}

		// ActionListener
		/// <summary>Performs an action.</summary>
		/// <remarks>Performs an action.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			if (cmd.Equals("Cut"))
			{
			}
			else
			{
				// textArea.cut();
				if (cmd.Equals("Copy"))
				{
					textArea.Copy();
				}
				else
				{
					if (cmd.Equals("Paste"))
					{
					}
				}
			}
		}
		// textArea.paste();
	}

	/// <summary>Table model class for watched expressions.</summary>
	/// <remarks>Table model class for watched expressions.</remarks>
	[System.Serializable]
	internal class MyTableModel : AbstractTableModel
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 2971618907207577000L;

		/// <summary>The debugger GUI.</summary>
		/// <remarks>The debugger GUI.</remarks>
		private SwingGui debugGui;

		/// <summary>List of watched expressions.</summary>
		/// <remarks>List of watched expressions.</remarks>
		private IList<string> expressions;

		/// <summary>
		/// List of values from evaluated from
		/// <see cref="expressions">expressions</see>
		/// .
		/// </summary>
		private IList<string> values;

		/// <summary>Creates a new MyTableModel.</summary>
		/// <remarks>Creates a new MyTableModel.</remarks>
		public MyTableModel(SwingGui debugGui)
		{
			this.debugGui = debugGui;
			expressions = Sharpen.Collections.SynchronizedList(new AList<string>());
			values = Sharpen.Collections.SynchronizedList(new AList<string>());
			expressions.AddItem(string.Empty);
			values.AddItem(string.Empty);
		}

		/// <summary>Returns the number of columns in the table (2).</summary>
		/// <remarks>Returns the number of columns in the table (2).</remarks>
		public override int GetColumnCount()
		{
			return 2;
		}

		/// <summary>Returns the number of rows in the table.</summary>
		/// <remarks>Returns the number of rows in the table.</remarks>
		public override int GetRowCount()
		{
			return expressions.Count;
		}

		/// <summary>Returns the name of the given column.</summary>
		/// <remarks>Returns the name of the given column.</remarks>
		public override string GetColumnName(int column)
		{
			switch (column)
			{
				case 0:
				{
					return "Expression";
				}

				case 1:
				{
					return "Value";
				}
			}
			return null;
		}

		/// <summary>Returns whether the given cell is editable.</summary>
		/// <remarks>Returns whether the given cell is editable.</remarks>
		public override bool IsCellEditable(int row, int column)
		{
			return true;
		}

		/// <summary>Returns the value in the given cell.</summary>
		/// <remarks>Returns the value in the given cell.</remarks>
		public override object GetValueAt(int row, int column)
		{
			switch (column)
			{
				case 0:
				{
					return expressions[row];
				}

				case 1:
				{
					return values[row];
				}
			}
			return string.Empty;
		}

		/// <summary>Sets the value in the given cell.</summary>
		/// <remarks>Sets the value in the given cell.</remarks>
		public override void SetValueAt(object value, int row, int column)
		{
			switch (column)
			{
				case 0:
				{
					string expr = value.ToString();
					expressions.Set(row, expr);
					string result = string.Empty;
					if (expr.Length > 0)
					{
						result = debugGui.dim.Eval(expr);
						if (result == null)
						{
							result = string.Empty;
						}
					}
					values.Set(row, result);
					UpdateModel();
					if (row + 1 == expressions.Count)
					{
						expressions.AddItem(string.Empty);
						values.AddItem(string.Empty);
						FireTableRowsInserted(row + 1, row + 1);
					}
					break;
				}

				case 1:
				{
					// just reset column 2; ignore edits
					FireTableDataChanged();
				}
			}
		}

		/// <summary>Re-evaluates the expressions in the table.</summary>
		/// <remarks>Re-evaluates the expressions in the table.</remarks>
		internal virtual void UpdateModel()
		{
			for (int i = 0; i < expressions.Count; ++i)
			{
				string expr = expressions[i];
				string result = string.Empty;
				if (expr.Length > 0)
				{
					result = debugGui.dim.Eval(expr);
					if (result == null)
					{
						result = string.Empty;
					}
				}
				else
				{
					result = string.Empty;
				}
				result = result.Replace('\n', ' ');
				values.Set(i, result);
			}
			FireTableDataChanged();
		}
	}

	/// <summary>A table for evaluated expressions.</summary>
	/// <remarks>A table for evaluated expressions.</remarks>
	[System.Serializable]
	internal class Evaluator : JTable
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 8133672432982594256L;

		/// <summary>
		/// The
		/// <see cref="Javax.Swing.Table.TableModel">Javax.Swing.Table.TableModel</see>
		/// for this table.
		/// </summary>
		internal MyTableModel tableModel;

		/// <summary>Creates a new Evaluator.</summary>
		/// <remarks>Creates a new Evaluator.</remarks>
		public Evaluator(SwingGui debugGui) : base(new MyTableModel(debugGui))
		{
			tableModel = (MyTableModel)GetModel();
		}
	}

	/// <summary>Tree model for script object inspection.</summary>
	/// <remarks>Tree model for script object inspection.</remarks>
	internal class VariableModel : TreeTableModel
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private static readonly string[] cNames = new string[] { " Name", " Value" };

		/// <summary>Tree column types.</summary>
		/// <remarks>Tree column types.</remarks>
		private static readonly Type[] cTypes = new Type[] { typeof(TreeTableModel), typeof(string) };

		/// <summary>
		/// Empty
		/// <see cref="VariableNode">VariableNode</see>
		/// array.
		/// </summary>
		private static readonly VariableModel.VariableNode[] CHILDLESS = new VariableModel.VariableNode[0];

		/// <summary>The debugger.</summary>
		/// <remarks>The debugger.</remarks>
		private Dim debugger;

		/// <summary>The root node.</summary>
		/// <remarks>The root node.</remarks>
		private VariableModel.VariableNode root;

		/// <summary>Creates a new VariableModel.</summary>
		/// <remarks>Creates a new VariableModel.</remarks>
		public VariableModel()
		{
		}

		/// <summary>Creates a new VariableModel.</summary>
		/// <remarks>Creates a new VariableModel.</remarks>
		public VariableModel(Dim debugger, object scope)
		{
			this.debugger = debugger;
			this.root = new VariableModel.VariableNode(scope, "this");
		}

		// TreeTableModel
		/// <summary>Returns the root node of the tree.</summary>
		/// <remarks>Returns the root node of the tree.</remarks>
		public virtual object GetRoot()
		{
			if (debugger == null)
			{
				return null;
			}
			return root;
		}

		/// <summary>Returns the number of children of the given node.</summary>
		/// <remarks>Returns the number of children of the given node.</remarks>
		public virtual int GetChildCount(object nodeObj)
		{
			if (debugger == null)
			{
				return 0;
			}
			VariableModel.VariableNode node = (VariableModel.VariableNode)nodeObj;
			return Children(node).Length;
		}

		/// <summary>Returns a child of the given node.</summary>
		/// <remarks>Returns a child of the given node.</remarks>
		public virtual object GetChild(object nodeObj, int i)
		{
			if (debugger == null)
			{
				return null;
			}
			VariableModel.VariableNode node = (VariableModel.VariableNode)nodeObj;
			return Children(node)[i];
		}

		/// <summary>Returns whether the given node is a leaf node.</summary>
		/// <remarks>Returns whether the given node is a leaf node.</remarks>
		public virtual bool IsLeaf(object nodeObj)
		{
			if (debugger == null)
			{
				return true;
			}
			VariableModel.VariableNode node = (VariableModel.VariableNode)nodeObj;
			return Children(node).Length == 0;
		}

		/// <summary>Returns the index of a node under its parent.</summary>
		/// <remarks>Returns the index of a node under its parent.</remarks>
		public virtual int GetIndexOfChild(object parentObj, object childObj)
		{
			if (debugger == null)
			{
				return -1;
			}
			VariableModel.VariableNode parent = (VariableModel.VariableNode)parentObj;
			VariableModel.VariableNode child = (VariableModel.VariableNode)childObj;
			VariableModel.VariableNode[] children = Children(parent);
			for (int i = 0; i != children.Length; i++)
			{
				if (children[i] == child)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>Returns whether the given cell is editable.</summary>
		/// <remarks>Returns whether the given cell is editable.</remarks>
		public virtual bool IsCellEditable(object node, int column)
		{
			return column == 0;
		}

		/// <summary>Sets the value at the given cell.</summary>
		/// <remarks>Sets the value at the given cell.</remarks>
		public virtual void SetValueAt(object value, object node, int column)
		{
		}

		/// <summary>Adds a TreeModelListener to this tree.</summary>
		/// <remarks>Adds a TreeModelListener to this tree.</remarks>
		public virtual void AddTreeModelListener(TreeModelListener l)
		{
		}

		/// <summary>Removes a TreeModelListener from this tree.</summary>
		/// <remarks>Removes a TreeModelListener from this tree.</remarks>
		public virtual void RemoveTreeModelListener(TreeModelListener l)
		{
		}

		public virtual void ValueForPathChanged(TreePath path, object newValue)
		{
		}

		// TreeTableNode
		/// <summary>Returns the number of columns.</summary>
		/// <remarks>Returns the number of columns.</remarks>
		public virtual int GetColumnCount()
		{
			return cNames.Length;
		}

		/// <summary>Returns the name of the given column.</summary>
		/// <remarks>Returns the name of the given column.</remarks>
		public virtual string GetColumnName(int column)
		{
			return cNames[column];
		}

		/// <summary>Returns the type of value stored in the given column.</summary>
		/// <remarks>Returns the type of value stored in the given column.</remarks>
		public virtual Type GetColumnClass(int column)
		{
			return cTypes[column];
		}

		/// <summary>Returns the value at the given cell.</summary>
		/// <remarks>Returns the value at the given cell.</remarks>
		public virtual object GetValueAt(object nodeObj, int column)
		{
			if (debugger == null)
			{
				return null;
			}
			VariableModel.VariableNode node = (VariableModel.VariableNode)nodeObj;
			switch (column)
			{
				case 0:
				{
					// Name
					return node.ToString();
				}

				case 1:
				{
					// Value
					string result;
					try
					{
						result = debugger.ObjectToString(GetValue(node));
					}
					catch (Exception exc)
					{
						result = exc.Message;
					}
					StringBuilder buf = new StringBuilder();
					int len = result.Length;
					for (int i = 0; i < len; i++)
					{
						char ch = result[i];
						if (char.IsISOControl(ch))
						{
							ch = ' ';
						}
						buf.Append(ch);
					}
					return buf.ToString();
				}
			}
			return null;
		}

		/// <summary>Returns an array of the children of the given node.</summary>
		/// <remarks>Returns an array of the children of the given node.</remarks>
		private VariableModel.VariableNode[] Children(VariableModel.VariableNode node)
		{
			if (node.children != null)
			{
				return node.children;
			}
			VariableModel.VariableNode[] children;
			object value = GetValue(node);
			object[] ids = debugger.GetObjectIds(value);
			if (ids == null || ids.Length == 0)
			{
				children = CHILDLESS;
			}
			else
			{
				Arrays.Sort(ids, new _IComparer_2628());
				children = new VariableModel.VariableNode[ids.Length];
				for (int i = 0; i != ids.Length; ++i)
				{
					children[i] = new VariableModel.VariableNode(value, ids[i]);
				}
			}
			node.children = children;
			return children;
		}

		private sealed class _IComparer_2628 : IComparer<object>
		{
			public _IComparer_2628()
			{
			}

			public int Compare(object l, object r)
			{
				if (l is string)
				{
					if (r is int)
					{
						return -1;
					}
					return ((string)l).CompareToIgnoreCase((string)r);
				}
				else
				{
					if (r is string)
					{
						return 1;
					}
					int lint = ((int)l);
					int rint = ((int)r);
					return lint - rint;
				}
			}
		}

		/// <summary>Returns the value of the given node.</summary>
		/// <remarks>Returns the value of the given node.</remarks>
		public virtual object GetValue(VariableModel.VariableNode node)
		{
			try
			{
				return debugger.GetObjectProperty(node.@object, node.id);
			}
			catch (Exception)
			{
				return "undefined";
			}
		}

		/// <summary>A variable node in the tree.</summary>
		/// <remarks>A variable node in the tree.</remarks>
		private class VariableNode
		{
			/// <summary>The script object.</summary>
			/// <remarks>The script object.</remarks>
			private object @object;

			/// <summary>The object name.</summary>
			/// <remarks>The object name.  Either a String or an Integer.</remarks>
			private object id;

			/// <summary>Array of child nodes.</summary>
			/// <remarks>
			/// Array of child nodes.  This is filled with the properties of
			/// the object.
			/// </remarks>
			private VariableModel.VariableNode[] children;

			/// <summary>Creates a new VariableNode.</summary>
			/// <remarks>Creates a new VariableNode.</remarks>
			public VariableNode(object @object, object id)
			{
				this.@object = @object;
				this.id = id;
			}

			/// <summary>Returns a string representation of this node.</summary>
			/// <remarks>Returns a string representation of this node.</remarks>
			public override string ToString()
			{
				return id is string ? (string)id : "[" + ((int)id) + "]";
			}
		}
	}

	/// <summary>A tree table for browsing script objects.</summary>
	/// <remarks>A tree table for browsing script objects.</remarks>
	[System.Serializable]
	internal class MyTreeTable : JTreeTable
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 3457265548184453049L;

		/// <summary>Creates a new MyTreeTable.</summary>
		/// <remarks>Creates a new MyTreeTable.</remarks>
		public MyTreeTable(VariableModel model) : base(model)
		{
		}

		/// <summary>Initializes a tree for this tree table.</summary>
		/// <remarks>Initializes a tree for this tree table.</remarks>
		public virtual JTree ResetTree(TreeTableModel treeTableModel)
		{
			tree = new JTreeTable.TreeTableCellRenderer(this, treeTableModel);
			// Install a tableModel representing the visible rows in the tree.
			base.SetModel(new TreeTableModelAdapter(treeTableModel, tree));
			// Force the JTable and JTree to share their row selection models.
			JTreeTable.ListToTreeSelectionModelWrapper selectionWrapper = new JTreeTable.ListToTreeSelectionModelWrapper(this);
			tree.SetSelectionModel(selectionWrapper);
			SetSelectionModel(selectionWrapper.GetListSelectionModel());
			// Make the tree and table row heights the same.
			if (tree.GetRowHeight() < 1)
			{
				// Metal looks better like this.
				SetRowHeight(18);
			}
			// Install the tree editor renderer and editor.
			SetDefaultRenderer(typeof(TreeTableModel), tree);
			SetDefaultEditor(typeof(TreeTableModel), new JTreeTable.TreeTableCellEditor(this));
			SetShowGrid(true);
			SetIntercellSpacing(new Dimension(1, 1));
			tree.SetRootVisible(false);
			tree.SetShowsRootHandles(true);
			DefaultTreeCellRenderer r = (DefaultTreeCellRenderer)tree.GetCellRenderer();
			r.SetOpenIcon(null);
			r.SetClosedIcon(null);
			r.SetLeafIcon(null);
			return tree;
		}

		/// <summary>
		/// Returns whether the cell under the coordinates of the mouse
		/// in the
		/// <see cref="Sharpen.EventObject">Sharpen.EventObject</see>
		/// is editable.
		/// </summary>
		public virtual bool IsCellEditable(EventObject e)
		{
			if (e is MouseEvent)
			{
				MouseEvent me = (MouseEvent)e;
				// If the modifiers are not 0 (or the left mouse button),
				// tree may try and toggle the selection, and table
				// will then try and toggle, resulting in the
				// selection remaining the same. To avoid this, we
				// only dispatch when the modifiers are 0 (or the left mouse
				// button).
				if (me.GetModifiers() == 0 || ((me.GetModifiers() & (InputEvent.BUTTON1_MASK | 1024)) != 0 && (me.GetModifiers() & (InputEvent.SHIFT_MASK | InputEvent.CTRL_MASK | InputEvent.ALT_MASK | InputEvent.BUTTON2_MASK | InputEvent.BUTTON3_MASK | 64 | 128 | 512 | 2048 | 4096)) == 0))
				{
					//SHIFT_DOWN_MASK
					//CTRL_DOWN_MASK
					// ALT_DOWN_MASK
					//BUTTON2_DOWN_MASK
					//BUTTON3_DOWN_MASK
					int row = RowAtPoint(me.GetPoint());
					for (int counter = GetColumnCount() - 1; counter >= 0; counter--)
					{
						if (typeof(TreeTableModel) == GetColumnClass(counter))
						{
							MouseEvent newME = new MouseEvent(this.tree, me.GetID(), me.GetWhen(), me.GetModifiers(), me.GetX() - GetCellRect(row, counter, true).x, me.GetY(), me.GetClickCount(), me.IsPopupTrigger());
							this.tree.DispatchEvent(newME);
							break;
						}
					}
				}
				if (me.GetClickCount() >= 3)
				{
					return true;
				}
				return false;
			}
			if (e == null)
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>Panel that shows information about the context.</summary>
	/// <remarks>Panel that shows information about the context.</remarks>
	[System.Serializable]
	internal class ContextWindow : JPanel, ActionListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 2306040975490228051L;

		/// <summary>The debugger GUI.</summary>
		/// <remarks>The debugger GUI.</remarks>
		private SwingGui debugGui;

		/// <summary>The combo box that holds the stack frames.</summary>
		/// <remarks>The combo box that holds the stack frames.</remarks>
		internal JComboBox context;

		/// <summary>Tool tips for the stack frames.</summary>
		/// <remarks>Tool tips for the stack frames.</remarks>
		internal IList<string> toolTips;

		/// <summary>Tabbed pane for "this" and "locals".</summary>
		/// <remarks>Tabbed pane for "this" and "locals".</remarks>
		private JTabbedPane tabs;

		/// <summary>Tabbed pane for "watch" and "evaluate".</summary>
		/// <remarks>Tabbed pane for "watch" and "evaluate".</remarks>
		private JTabbedPane tabs2;

		/// <summary>The table showing the "this" object.</summary>
		/// <remarks>The table showing the "this" object.</remarks>
		private MyTreeTable thisTable;

		/// <summary>The table showing the stack local variables.</summary>
		/// <remarks>The table showing the stack local variables.</remarks>
		private MyTreeTable localsTable;

		/// <summary>
		/// The
		/// <see cref="evaluator">evaluator</see>
		/// 's table model.
		/// </summary>
		private MyTableModel tableModel;

		/// <summary>The script evaluator table.</summary>
		/// <remarks>The script evaluator table.</remarks>
		private Evaluator evaluator;

		/// <summary>The script evaluation text area.</summary>
		/// <remarks>The script evaluation text area.</remarks>
		private EvalTextArea cmdLine;

		/// <summary>The split pane.</summary>
		/// <remarks>The split pane.</remarks>
		internal JSplitPane split;

		/// <summary>Whether the ContextWindow is enabled.</summary>
		/// <remarks>Whether the ContextWindow is enabled.</remarks>
		private bool enabled;

		/// <summary>Creates a new ContextWindow.</summary>
		/// <remarks>Creates a new ContextWindow.</remarks>
		public ContextWindow(SwingGui debugGui)
		{
			this.debugGui = debugGui;
			enabled = false;
			JPanel left = new JPanel();
			JToolBar t1 = new JToolBar();
			t1.SetName("Variables");
			t1.SetLayout(new GridLayout());
			t1.Add(left);
			JPanel p1 = new JPanel();
			p1.SetLayout(new GridLayout());
			JPanel p2 = new JPanel();
			p2.SetLayout(new GridLayout());
			p1.Add(t1);
			JLabel label = new JLabel("Context:");
			context = new JComboBox();
			context.SetLightWeightPopupEnabled(false);
			toolTips = Sharpen.Collections.SynchronizedList(new AList<string>());
			label.SetBorder(context.GetBorder());
			context.AddActionListener(this);
			context.SetActionCommand("ContextSwitch");
			GridBagLayout layout = new GridBagLayout();
			left.SetLayout(layout);
			GridBagConstraints lc = new GridBagConstraints();
			lc.insets.left = 5;
			lc.anchor = GridBagConstraints.WEST;
			lc.ipadx = 5;
			layout.SetConstraints(label, lc);
			left.Add(label);
			GridBagConstraints c = new GridBagConstraints();
			c.gridwidth = GridBagConstraints.REMAINDER;
			c.fill = GridBagConstraints.HORIZONTAL;
			c.anchor = GridBagConstraints.WEST;
			layout.SetConstraints(context, c);
			left.Add(context);
			tabs = new JTabbedPane(SwingConstantsConstants.BOTTOM);
			tabs.SetPreferredSize(new Dimension(500, 300));
			thisTable = new MyTreeTable(new VariableModel());
			JScrollPane jsp = new JScrollPane(thisTable);
			jsp.GetViewport().SetViewSize(new Dimension(5, 2));
			tabs.Add("this", jsp);
			localsTable = new MyTreeTable(new VariableModel());
			localsTable.SetAutoResizeMode(JTable.AUTO_RESIZE_ALL_COLUMNS);
			localsTable.SetPreferredSize(null);
			jsp = new JScrollPane(localsTable);
			tabs.Add("Locals", jsp);
			c.weightx = c.weighty = 1;
			c.gridheight = GridBagConstraints.REMAINDER;
			c.fill = GridBagConstraints.BOTH;
			c.anchor = GridBagConstraints.WEST;
			layout.SetConstraints(tabs, c);
			left.Add(tabs);
			evaluator = new Evaluator(debugGui);
			cmdLine = new EvalTextArea(debugGui);
			//cmdLine.requestFocus();
			tableModel = evaluator.tableModel;
			jsp = new JScrollPane(evaluator);
			JToolBar t2 = new JToolBar();
			t2.SetName("Evaluate");
			tabs2 = new JTabbedPane(SwingConstantsConstants.BOTTOM);
			tabs2.Add("Watch", jsp);
			tabs2.Add("Evaluate", new JScrollPane(cmdLine));
			tabs2.SetPreferredSize(new Dimension(500, 300));
			t2.SetLayout(new GridLayout());
			t2.Add(tabs2);
			p2.Add(t2);
			evaluator.SetAutoResizeMode(JTable.AUTO_RESIZE_ALL_COLUMNS);
			split = new JSplitPane(JSplitPane.HORIZONTAL_SPLIT, p1, p2);
			split.SetOneTouchExpandable(true);
			SwingGui.SetResizeWeight(split, 0.5);
			SetLayout(new BorderLayout());
			Add(split, BorderLayout.CENTER);
			JToolBar finalT1 = t1;
			JToolBar finalT2 = t2;
			JPanel finalP1 = p1;
			JPanel finalP2 = p2;
			JSplitPane finalSplit = split;
			JPanel finalThis = this;
			ComponentListener clistener = new _ComponentListener_2965(this, finalThis, finalT1, finalP1, debugGui, finalT2, finalP2, finalSplit);
			// We need the following hacks because:
			// - We want an undocked toolbar to be
			//   resizable.
			// - We are using JToolbar as a container of a
			//   JComboBox. Without this JComboBox's popup
			//   can get left floating when the toolbar is
			//   re-docked.
			//
			// We make the frame resizable and then
			// remove JToolbar's window listener
			// and insert one of our own that first ensures
			// the JComboBox's popup window is closed
			// and then calls JToolbar's window listener.
			//adjustVerticalSplit = true;
			// no change
			// both undocked
			p1.AddContainerListener(new _ContainerListener_3068(finalThis, finalT1, finalT2, finalP2, finalSplit));
			// both docked
			// left docked only
			// right docked only
			// both undocked
			t1.AddComponentListener(clistener);
			t2.AddComponentListener(clistener);
			SetEnabled(false);
		}

		private sealed class _ComponentListener_2965 : ComponentListener
		{
			public _ComponentListener_2965(ContextWindow _enclosing, JPanel finalThis, JToolBar finalT1, JPanel finalP1, SwingGui debugGui, JToolBar finalT2, JPanel finalP2, JSplitPane finalSplit)
			{
				this._enclosing = _enclosing;
				this.finalThis = finalThis;
				this.finalT1 = finalT1;
				this.finalP1 = finalP1;
				this.debugGui = debugGui;
				this.finalT2 = finalT2;
				this.finalP2 = finalP2;
				this.finalSplit = finalSplit;
				this.t2Docked = true;
			}

			internal bool t2Docked;

			internal void Check(Component comp)
			{
				Component thisParent = finalThis.GetParent();
				if (thisParent == null)
				{
					return;
				}
				Component parent = finalT1.GetParent();
				bool leftDocked = true;
				bool rightDocked = true;
				bool adjustVerticalSplit = false;
				if (parent != null)
				{
					if (parent != finalP1)
					{
						while (!(parent is JFrame))
						{
							parent = parent.GetParent();
						}
						JFrame frame = (JFrame)parent;
						debugGui.AddTopLevel("Variables", frame);
						if (!frame.IsResizable())
						{
							frame.SetResizable(true);
							frame.SetDefaultCloseOperation(WindowConstantsConstants.DO_NOTHING_ON_CLOSE);
							EventListener[] l = frame.GetListeners<WindowListener>();
							frame.RemoveWindowListener((WindowListener)l[0]);
							frame.AddWindowListener(new _WindowAdapter_3003(this, l));
						}
						leftDocked = false;
					}
					else
					{
						leftDocked = true;
					}
				}
				parent = finalT2.GetParent();
				if (parent != null)
				{
					if (parent != finalP2)
					{
						while (!(parent is JFrame))
						{
							parent = parent.GetParent();
						}
						JFrame frame = (JFrame)parent;
						debugGui.AddTopLevel("Evaluate", frame);
						frame.SetResizable(true);
						rightDocked = false;
					}
					else
					{
						rightDocked = true;
					}
				}
				if (leftDocked && this.t2Docked && rightDocked && this.t2Docked)
				{
					return;
				}
				this.t2Docked = rightDocked;
				JSplitPane split = (JSplitPane)thisParent;
				if (leftDocked)
				{
					if (rightDocked)
					{
						finalSplit.SetDividerLocation(0.5);
					}
					else
					{
						finalSplit.SetDividerLocation(1.0);
					}
					if (adjustVerticalSplit)
					{
						split.SetDividerLocation(0.66);
					}
				}
				else
				{
					if (rightDocked)
					{
						finalSplit.SetDividerLocation(0.0);
						split.SetDividerLocation(0.66);
					}
					else
					{
						split.SetDividerLocation(1.0);
					}
				}
			}

			private sealed class _WindowAdapter_3003 : WindowAdapter
			{
				public _WindowAdapter_3003(_ComponentListener_2965 _enclosing, EventListener[] l)
				{
					this._enclosing = _enclosing;
					this.l = l;
				}

				public override void WindowClosing(WindowEvent e)
				{
					this._enclosing._enclosing.context.HidePopup();
					((WindowListener)l[0]).WindowClosing(e);
				}

				private readonly _ComponentListener_2965 _enclosing;

				private readonly EventListener[] l;
			}

			public void ComponentHidden(ComponentEvent e)
			{
				this.Check(e.GetComponent());
			}

			public void ComponentMoved(ComponentEvent e)
			{
				this.Check(e.GetComponent());
			}

			public void ComponentResized(ComponentEvent e)
			{
				this.Check(e.GetComponent());
			}

			public void ComponentShown(ComponentEvent e)
			{
				this.Check(e.GetComponent());
			}

			private readonly ContextWindow _enclosing;

			private readonly JPanel finalThis;

			private readonly JToolBar finalT1;

			private readonly JPanel finalP1;

			private readonly SwingGui debugGui;

			private readonly JToolBar finalT2;

			private readonly JPanel finalP2;

			private readonly JSplitPane finalSplit;
		}

		private sealed class _ContainerListener_3068 : ContainerListener
		{
			public _ContainerListener_3068(JPanel finalThis, JToolBar finalT1, JToolBar finalT2, JPanel finalP2, JSplitPane finalSplit)
			{
				this.finalThis = finalThis;
				this.finalT1 = finalT1;
				this.finalT2 = finalT2;
				this.finalP2 = finalP2;
				this.finalSplit = finalSplit;
			}

			public void ComponentAdded(ContainerEvent e)
			{
				Component thisParent = finalThis.GetParent();
				JSplitPane split = (JSplitPane)thisParent;
				if (e.GetChild() == finalT1)
				{
					if (finalT2.GetParent() == finalP2)
					{
						finalSplit.SetDividerLocation(0.5);
					}
					else
					{
						finalSplit.SetDividerLocation(1.0);
					}
					split.SetDividerLocation(0.66);
				}
			}

			public void ComponentRemoved(ContainerEvent e)
			{
				Component thisParent = finalThis.GetParent();
				JSplitPane split = (JSplitPane)thisParent;
				if (e.GetChild() == finalT1)
				{
					if (finalT2.GetParent() == finalP2)
					{
						finalSplit.SetDividerLocation(0.0);
						split.SetDividerLocation(0.66);
					}
					else
					{
						split.SetDividerLocation(1.0);
					}
				}
			}

			private readonly JPanel finalThis;

			private readonly JToolBar finalT1;

			private readonly JToolBar finalT2;

			private readonly JPanel finalP2;

			private readonly JSplitPane finalSplit;
		}

		/// <summary>Enables or disables the component.</summary>
		/// <remarks>Enables or disables the component.</remarks>
		public override void SetEnabled(bool enabled)
		{
			context.SetEnabled(enabled);
			thisTable.SetEnabled(enabled);
			localsTable.SetEnabled(enabled);
			evaluator.SetEnabled(enabled);
			cmdLine.SetEnabled(enabled);
		}

		/// <summary>Disables updating of the component.</summary>
		/// <remarks>Disables updating of the component.</remarks>
		public virtual void DisableUpdate()
		{
			enabled = false;
		}

		/// <summary>Enables updating of the component.</summary>
		/// <remarks>Enables updating of the component.</remarks>
		public virtual void EnableUpdate()
		{
			enabled = true;
		}

		// ActionListener
		/// <summary>Performs an action.</summary>
		/// <remarks>Performs an action.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			if (!enabled)
			{
				return;
			}
			if (e.GetActionCommand().Equals("ContextSwitch"))
			{
				Dim.ContextData contextData = debugGui.dim.CurrentContextData();
				if (contextData == null)
				{
					return;
				}
				int frameIndex = context.GetSelectedIndex();
				context.SetToolTipText(toolTips[frameIndex]);
				int frameCount = contextData.FrameCount();
				if (frameIndex >= frameCount)
				{
					return;
				}
				Dim.StackFrame frame = contextData.GetFrame(frameIndex);
				object scope = frame.Scope();
				object thisObj = frame.ThisObj();
				thisTable.ResetTree(new VariableModel(debugGui.dim, thisObj));
				VariableModel scopeModel;
				if (scope != thisObj)
				{
					scopeModel = new VariableModel(debugGui.dim, scope);
				}
				else
				{
					scopeModel = new VariableModel();
				}
				localsTable.ResetTree(scopeModel);
				debugGui.dim.ContextSwitch(frameIndex);
				debugGui.ShowStopLine(frame);
				tableModel.UpdateModel();
			}
		}
	}

	/// <summary>The debugger frame menu bar.</summary>
	/// <remarks>The debugger frame menu bar.</remarks>
	[System.Serializable]
	internal class Menubar : JMenuBar, ActionListener
	{
		/// <summary>Serializable magic number.</summary>
		/// <remarks>Serializable magic number.</remarks>
		private const long serialVersionUID = 3217170497245911461L;

		/// <summary>Items that are enabled only when interrupted.</summary>
		/// <remarks>Items that are enabled only when interrupted.</remarks>
		private IList<JMenuItem> interruptOnlyItems = Sharpen.Collections.SynchronizedList(new AList<JMenuItem>());

		/// <summary>Items that are enabled only when running.</summary>
		/// <remarks>Items that are enabled only when running.</remarks>
		private IList<JMenuItem> runOnlyItems = Sharpen.Collections.SynchronizedList(new AList<JMenuItem>());

		/// <summary>The debugger GUI.</summary>
		/// <remarks>The debugger GUI.</remarks>
		private SwingGui debugGui;

		/// <summary>The menu listing the internal frames.</summary>
		/// <remarks>The menu listing the internal frames.</remarks>
		private JMenu windowMenu;

		/// <summary>The "Break on exceptions" menu item.</summary>
		/// <remarks>The "Break on exceptions" menu item.</remarks>
		private JCheckBoxMenuItem breakOnExceptions;

		/// <summary>The "Break on enter" menu item.</summary>
		/// <remarks>The "Break on enter" menu item.</remarks>
		private JCheckBoxMenuItem breakOnEnter;

		/// <summary>The "Break on return" menu item.</summary>
		/// <remarks>The "Break on return" menu item.</remarks>
		private JCheckBoxMenuItem breakOnReturn;

		/// <summary>Creates a new Menubar.</summary>
		/// <remarks>Creates a new Menubar.</remarks>
		internal Menubar(SwingGui debugGui) : base()
		{
			this.debugGui = debugGui;
			string[] fileItems = new string[] { "Open...", "Run...", string.Empty, "Exit" };
			string[] fileCmds = new string[] { "Open", "Load", string.Empty, "Exit" };
			char[] fileShortCuts = new char[] { '0', 'N', 0, 'X' };
			int[] fileAccelerators = new int[] { KeyEvent.VK_O, KeyEvent.VK_N, 0, KeyEvent.VK_Q };
			string[] editItems = new string[] { "Cut", "Copy", "Paste", "Go to function..." };
			char[] editShortCuts = new char[] { 'T', 'C', 'P', 'F' };
			string[] debugItems = new string[] { "Break", "Go", "Step Into", "Step Over", "Step Out" };
			char[] debugShortCuts = new char[] { 'B', 'G', 'I', 'O', 'T' };
			string[] plafItems = new string[] { "Metal", "Windows", "Motif" };
			char[] plafShortCuts = new char[] { 'M', 'W', 'F' };
			int[] debugAccelerators = new int[] { KeyEvent.VK_PAUSE, KeyEvent.VK_F5, KeyEvent.VK_F11, KeyEvent.VK_F7, KeyEvent.VK_F8, 0, 0 };
			JMenu fileMenu = new JMenu("File");
			fileMenu.SetMnemonic('F');
			JMenu editMenu = new JMenu("Edit");
			editMenu.SetMnemonic('E');
			JMenu plafMenu = new JMenu("Platform");
			plafMenu.SetMnemonic('P');
			JMenu debugMenu = new JMenu("Debug");
			debugMenu.SetMnemonic('D');
			windowMenu = new JMenu("Window");
			windowMenu.SetMnemonic('W');
			for (int i = 0; i < fileItems.Length; ++i)
			{
				if (fileItems[i].Length == 0)
				{
					fileMenu.AddSeparator();
				}
				else
				{
					JMenuItem item = new JMenuItem(fileItems[i], fileShortCuts[i]);
					item.SetActionCommand(fileCmds[i]);
					item.AddActionListener(this);
					fileMenu.Add(item);
					if (fileAccelerators[i] != 0)
					{
						KeyStroke k = KeyStroke.GetKeyStroke(fileAccelerators[i], Java.Awt.Event.CTRL_MASK);
						item.SetAccelerator(k);
					}
				}
			}
			for (int i_1 = 0; i_1 < editItems.Length; ++i_1)
			{
				JMenuItem item = new JMenuItem(editItems[i_1], editShortCuts[i_1]);
				item.AddActionListener(this);
				editMenu.Add(item);
			}
			for (int i_2 = 0; i_2 < plafItems.Length; ++i_2)
			{
				JMenuItem item = new JMenuItem(plafItems[i_2], plafShortCuts[i_2]);
				item.AddActionListener(this);
				plafMenu.Add(item);
			}
			for (int i_3 = 0; i_3 < debugItems.Length; ++i_3)
			{
				JMenuItem item = new JMenuItem(debugItems[i_3], debugShortCuts[i_3]);
				item.AddActionListener(this);
				if (debugAccelerators[i_3] != 0)
				{
					KeyStroke k = KeyStroke.GetKeyStroke(debugAccelerators[i_3], 0);
					item.SetAccelerator(k);
				}
				if (i_3 != 0)
				{
					interruptOnlyItems.AddItem(item);
				}
				else
				{
					runOnlyItems.AddItem(item);
				}
				debugMenu.Add(item);
			}
			breakOnExceptions = new JCheckBoxMenuItem("Break on Exceptions");
			breakOnExceptions.SetMnemonic('X');
			breakOnExceptions.AddActionListener(this);
			breakOnExceptions.SetSelected(false);
			debugMenu.Add(breakOnExceptions);
			breakOnEnter = new JCheckBoxMenuItem("Break on Function Enter");
			breakOnEnter.SetMnemonic('E');
			breakOnEnter.AddActionListener(this);
			breakOnEnter.SetSelected(false);
			debugMenu.Add(breakOnEnter);
			breakOnReturn = new JCheckBoxMenuItem("Break on Function Return");
			breakOnReturn.SetMnemonic('R');
			breakOnReturn.AddActionListener(this);
			breakOnReturn.SetSelected(false);
			debugMenu.Add(breakOnReturn);
			Add(fileMenu);
			Add(editMenu);
			//add(plafMenu);
			Add(debugMenu);
			JMenuItem item_1;
			windowMenu.Add(item_1 = new JMenuItem("Cascade", 'A'));
			item_1.AddActionListener(this);
			windowMenu.Add(item_1 = new JMenuItem("Tile", 'T'));
			item_1.AddActionListener(this);
			windowMenu.AddSeparator();
			windowMenu.Add(item_1 = new JMenuItem("Console", 'C'));
			item_1.AddActionListener(this);
			Add(windowMenu);
			UpdateEnabled(false);
		}

		/// <summary>Returns the "Break on exceptions" menu item.</summary>
		/// <remarks>Returns the "Break on exceptions" menu item.</remarks>
		public virtual JCheckBoxMenuItem GetBreakOnExceptions()
		{
			return breakOnExceptions;
		}

		/// <summary>Returns the "Break on enter" menu item.</summary>
		/// <remarks>Returns the "Break on enter" menu item.</remarks>
		public virtual JCheckBoxMenuItem GetBreakOnEnter()
		{
			return breakOnEnter;
		}

		/// <summary>Returns the "Break on return" menu item.</summary>
		/// <remarks>Returns the "Break on return" menu item.</remarks>
		public virtual JCheckBoxMenuItem GetBreakOnReturn()
		{
			return breakOnReturn;
		}

		/// <summary>Returns the "Debug" menu.</summary>
		/// <remarks>Returns the "Debug" menu.</remarks>
		public virtual JMenu GetDebugMenu()
		{
			return GetMenu(2);
		}

		// ActionListener
		/// <summary>Performs an action.</summary>
		/// <remarks>Performs an action.</remarks>
		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			string plaf_name = null;
			if (cmd.Equals("Metal"))
			{
				plaf_name = "javax.swing.plaf.metal.MetalLookAndFeel";
			}
			else
			{
				if (cmd.Equals("Windows"))
				{
					plaf_name = "com.sun.java.swing.plaf.windows.WindowsLookAndFeel";
				}
				else
				{
					if (cmd.Equals("Motif"))
					{
						plaf_name = "com.sun.java.swing.plaf.motif.MotifLookAndFeel";
					}
					else
					{
						object source = e.GetSource();
						if (source == breakOnExceptions)
						{
							debugGui.dim.SetBreakOnExceptions(breakOnExceptions.IsSelected());
						}
						else
						{
							if (source == breakOnEnter)
							{
								debugGui.dim.SetBreakOnEnter(breakOnEnter.IsSelected());
							}
							else
							{
								if (source == breakOnReturn)
								{
									debugGui.dim.SetBreakOnReturn(breakOnReturn.IsSelected());
								}
								else
								{
									debugGui.ActionPerformed(e);
								}
							}
						}
						return;
					}
				}
			}
			try
			{
				UIManager.SetLookAndFeel(plaf_name);
				SwingUtilities.UpdateComponentTreeUI(debugGui);
				SwingUtilities.UpdateComponentTreeUI(debugGui.dlg);
			}
			catch (Exception)
			{
			}
		}

		//ignored.printStackTrace();
		/// <summary>Adds a file to the window menu.</summary>
		/// <remarks>Adds a file to the window menu.</remarks>
		public virtual void AddFile(string url)
		{
			int count = windowMenu.GetItemCount();
			JMenuItem item;
			if (count == 4)
			{
				windowMenu.AddSeparator();
				count++;
			}
			JMenuItem lastItem = windowMenu.GetItem(count - 1);
			bool hasMoreWin = false;
			int maxWin = 5;
			if (lastItem != null && lastItem.GetText().Equals("More Windows..."))
			{
				hasMoreWin = true;
				maxWin++;
			}
			if (!hasMoreWin && count - 4 == 5)
			{
				windowMenu.Add(item = new JMenuItem("More Windows...", 'M'));
				item.SetActionCommand("More Windows...");
				item.AddActionListener(this);
				return;
			}
			else
			{
				if (count - 4 <= maxWin)
				{
					if (hasMoreWin)
					{
						count--;
						windowMenu.Remove(lastItem);
					}
					string shortName = SwingGui.GetShortName(url);
					windowMenu.Add(item = new JMenuItem((char)('0' + (count - 4)) + " " + shortName, '0' + (count - 4)));
					if (hasMoreWin)
					{
						windowMenu.Add(lastItem);
					}
				}
				else
				{
					return;
				}
			}
			item.SetActionCommand(url);
			item.AddActionListener(this);
		}

		/// <summary>Updates the enabledness of menu items.</summary>
		/// <remarks>Updates the enabledness of menu items.</remarks>
		public virtual void UpdateEnabled(bool interrupted)
		{
			for (int i = 0; i != interruptOnlyItems.Count; ++i)
			{
				JMenuItem item = interruptOnlyItems[i];
				item.SetEnabled(interrupted);
			}
			for (int i_1 = 0; i_1 != runOnlyItems.Count; ++i_1)
			{
				JMenuItem item = runOnlyItems[i_1];
				item.SetEnabled(!interrupted);
			}
		}
	}

	/// <summary>
	/// Class to consolidate all cases that require to implement Runnable
	/// to avoid class generation bloat.
	/// </summary>
	/// <remarks>
	/// Class to consolidate all cases that require to implement Runnable
	/// to avoid class generation bloat.
	/// </remarks>
	internal class RunProxy : Runnable
	{
		internal const int OPEN_FILE = 1;

		internal const int LOAD_FILE = 2;

		internal const int UPDATE_SOURCE_TEXT = 3;

		internal const int ENTER_INTERRUPT = 4;

		/// <summary>The debugger GUI.</summary>
		/// <remarks>The debugger GUI.</remarks>
		private SwingGui debugGui;

		/// <summary>The type of Runnable this object is.</summary>
		/// <remarks>
		/// The type of Runnable this object is.  Takes one of the constants
		/// defined in this class.
		/// </remarks>
		private int type;

		/// <summary>The name of the file to open or load.</summary>
		/// <remarks>The name of the file to open or load.</remarks>
		internal string fileName;

		/// <summary>The source text to update.</summary>
		/// <remarks>The source text to update.</remarks>
		internal string text;

		/// <summary>The source for which to update the text.</summary>
		/// <remarks>The source for which to update the text.</remarks>
		internal Dim.SourceInfo sourceInfo;

		/// <summary>The frame to interrupt in.</summary>
		/// <remarks>The frame to interrupt in.</remarks>
		internal Dim.StackFrame lastFrame;

		/// <summary>The name of the interrupted thread.</summary>
		/// <remarks>The name of the interrupted thread.</remarks>
		internal string threadTitle;

		/// <summary>
		/// The message of the exception thrown that caused the thread
		/// interruption, if any.
		/// </summary>
		/// <remarks>
		/// The message of the exception thrown that caused the thread
		/// interruption, if any.
		/// </remarks>
		internal string alertMessage;

		/// <summary>Creates a new RunProxy.</summary>
		/// <remarks>Creates a new RunProxy.</remarks>
		public RunProxy(SwingGui debugGui, int type)
		{
			// Constants for 'type'.
			this.debugGui = debugGui;
			this.type = type;
		}

		/// <summary>Runs this Runnable.</summary>
		/// <remarks>Runs this Runnable.</remarks>
		public virtual void Run()
		{
			switch (type)
			{
				case OPEN_FILE:
				{
					try
					{
						debugGui.dim.CompileScript(fileName, text);
					}
					catch (Exception ex)
					{
						MessageDialogWrapper.ShowMessageDialog(debugGui, ex.Message, "Error Compiling " + fileName, JOptionPane.ERROR_MESSAGE);
					}
					break;
				}

				case LOAD_FILE:
				{
					try
					{
						debugGui.dim.EvalScript(fileName, text);
					}
					catch (Exception ex)
					{
						MessageDialogWrapper.ShowMessageDialog(debugGui, ex.Message, "Run error for " + fileName, JOptionPane.ERROR_MESSAGE);
					}
					break;
				}

				case UPDATE_SOURCE_TEXT:
				{
					string fileName = sourceInfo.Url();
					if (!debugGui.UpdateFileWindow(sourceInfo) && !fileName.Equals("<stdin>"))
					{
						debugGui.CreateFileWindow(sourceInfo, -1);
					}
					break;
				}

				case ENTER_INTERRUPT:
				{
					debugGui.EnterInterruptImpl(lastFrame, threadTitle, alertMessage);
					break;
				}

				default:
				{
					throw new ArgumentException(type.ToString());
				}
			}
		}
	}
}
