using System;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using Rhino.Tools.Shell;

namespace Rhino.Tools.JsConsole
{
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void LoadClick(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog {Filter = "JavaScript Files (*.js)|*.js|All files (*.*)|*.*"};
			bool? result = dialog.ShowDialog();
			if (result.HasValue && result.Value)
			{
				string f = dialog.FileName.Replace('\\', '/');
				ConsoleTextArea.Eval("load(\"" + f + "\");");
			}
		}

		private void ExitClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void CutClick(object sender, RoutedEventArgs e)
		{
			ConsoleTextArea.Cut();
		}

		private void CopyClick(object sender, RoutedEventArgs e)
		{
			ConsoleTextArea.Copy();
		}

		private void PasteClick(object sender, RoutedEventArgs e)
		{
			ConsoleTextArea.Paste();
		}

		private void WindowLoaded(object sender, RoutedEventArgs e)
		{
			Console.SetIn(ConsoleTextArea.GetIn());
			Console.SetOut(ConsoleTextArea.GetOut());
			Console.SetError(ConsoleTextArea.GetError());
			Program.SetIn(ConsoleTextArea.GetIn());
			Program.SetOut(ConsoleTextArea.GetOut());
			Program.SetErr(ConsoleTextArea.GetError());
			var thread = new Thread(() => Program.Main(new string[0]))
			{
				IsBackground = true,
			};
			thread.Start();
		}
	}
}