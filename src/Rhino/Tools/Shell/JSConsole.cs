/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Java.Awt.Event;
using Javax.Swing;
using Javax.Swing.Filechooser;
using Rhino;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	[System.Serializable]
	public class JSConsole : JFrame, ActionListener
	{
		internal const long serialVersionUID = 2551225560631876300L;

		private FilePath CWD;

		private JFileChooser dlg;

		private ConsoleTextArea consoleTextArea;

		public virtual string ChooseFile()
		{
			if (CWD == null)
			{
				string dir = SecurityUtilities.GetSystemProperty("user.dir");
				if (dir != null)
				{
					CWD = new FilePath(dir);
				}
			}
			if (CWD != null)
			{
				dlg.SetCurrentDirectory(CWD);
			}
			dlg.SetDialogTitle("Select a file to load");
			int returnVal = dlg.ShowOpenDialog(this);
			if (returnVal == JFileChooser.APPROVE_OPTION)
			{
				string result = dlg.GetSelectedFile().GetPath();
				CWD = new FilePath(dlg.GetSelectedFile().GetParent());
				return result;
			}
			return null;
		}

		public static void Main(string[] args)
		{
			new Rhino.Tools.Shell.JSConsole(args);
		}

		public virtual void CreateFileChooser()
		{
			dlg = new JFileChooser();
			FileFilter filter = new _FileFilter_62();
			dlg.AddChoosableFileFilter(filter);
		}

		private sealed class _FileFilter_62 : FileFilter
		{
			public _FileFilter_62()
			{
			}

			public override bool Accept(FilePath f)
			{
				if (f.IsDirectory())
				{
					return true;
				}
				string name = f.GetName();
				int i = name.LastIndexOf('.');
				if (i > 0 && i < name.Length - 1)
				{
					string ext = Sharpen.Runtime.Substring(name, i + 1).ToLower();
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

		public JSConsole(string[] args) : base("Rhino JavaScript Console")
		{
			JMenuBar menubar = new JMenuBar();
			CreateFileChooser();
			string[] fileItems = new string[] { "Load...", "Exit" };
			string[] fileCmds = new string[] { "Load", "Exit" };
			char[] fileShortCuts = new char[] { 'L', 'X' };
			string[] editItems = new string[] { "Cut", "Copy", "Paste" };
			char[] editShortCuts = new char[] { 'T', 'C', 'P' };
			string[] plafItems = new string[] { "Metal", "Windows", "Motif" };
			bool[] plafState = new bool[] { true, false, false };
			JMenu fileMenu = new JMenu("File");
			fileMenu.SetMnemonic('F');
			JMenu editMenu = new JMenu("Edit");
			editMenu.SetMnemonic('E');
			JMenu plafMenu = new JMenu("Platform");
			plafMenu.SetMnemonic('P');
			for (int i = 0; i < fileItems.Length; ++i)
			{
				JMenuItem item = new JMenuItem(fileItems[i], fileShortCuts[i]);
				item.SetActionCommand(fileCmds[i]);
				item.AddActionListener(this);
				fileMenu.Add(item);
			}
			for (int i_1 = 0; i_1 < editItems.Length; ++i_1)
			{
				JMenuItem item = new JMenuItem(editItems[i_1], editShortCuts[i_1]);
				item.AddActionListener(this);
				editMenu.Add(item);
			}
			ButtonGroup group = new ButtonGroup();
			for (int i_2 = 0; i_2 < plafItems.Length; ++i_2)
			{
				JRadioButtonMenuItem item = new JRadioButtonMenuItem(plafItems[i_2], plafState[i_2]);
				group.Add(item);
				item.AddActionListener(this);
				plafMenu.Add(item);
			}
			menubar.Add(fileMenu);
			menubar.Add(editMenu);
			menubar.Add(plafMenu);
			SetJMenuBar(menubar);
			consoleTextArea = new ConsoleTextArea(args);
			JScrollPane scroller = new JScrollPane(consoleTextArea);
			SetContentPane(scroller);
			consoleTextArea.SetRows(24);
			consoleTextArea.SetColumns(80);
			AddWindowListener(new _WindowAdapter_135());
			Pack();
			SetVisible(true);
			// System.setIn(consoleTextArea.getIn());
			// System.setOut(consoleTextArea.getOut());
			// System.setErr(consoleTextArea.getErr());
			Rhino.Tools.Shell.Main.SetIn(consoleTextArea.GetIn());
			Rhino.Tools.Shell.Main.SetOut(consoleTextArea.GetOut());
			Rhino.Tools.Shell.Main.SetErr(consoleTextArea.GetErr());
			Rhino.Tools.Shell.Main.Main(args);
		}

		private sealed class _WindowAdapter_135 : WindowAdapter
		{
			public _WindowAdapter_135()
			{
			}

			public override void WindowClosing(WindowEvent e)
			{
				System.Environment.Exit(0);
			}
		}

		public virtual void ActionPerformed(ActionEvent e)
		{
			string cmd = e.GetActionCommand();
			string plaf_name = null;
			if (cmd.Equals("Load"))
			{
				string f = ChooseFile();
				if (f != null)
				{
					f = f.Replace('\\', '/');
					consoleTextArea.Eval("load(\"" + f + "\");");
				}
			}
			else
			{
				if (cmd.Equals("Exit"))
				{
					System.Environment.Exit(0);
				}
				else
				{
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
							else
							{
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
									}
								}
								if (plaf_name != null)
								{
									try
									{
										UIManager.SetLookAndFeel(plaf_name);
										SwingUtilities.UpdateComponentTreeUI(this);
										consoleTextArea.PostUpdateUI();
										// updateComponentTreeUI seems to mess up the file
										// chooser dialog, so just create a new one
										CreateFileChooser();
									}
									catch (Exception exc)
									{
										JOptionPane.ShowMessageDialog(this, exc.Message, "Platform", JOptionPane.ERROR_MESSAGE);
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
