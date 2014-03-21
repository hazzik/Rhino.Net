using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Sharpen;

namespace Rhino.Tools.JsConsole
{
	/// <summary>
	///     Interaction logic for ConsoleTextArea.xaml
	/// </summary>
	public partial class ConsoleTextArea
	{
		public ConsoleTextArea()
		{
			InitializeComponent();

			history = new List<string>();
			@out = new ConsoleWriter(this);
			error = new ConsoleWriter(this);
			var outPipe = new PipedOutputStream();
			var inputStream = new PipedInputStream();
			outPipe.Attach(inputStream);
			inPipe = new StreamWriter(outPipe);
			@in = new StreamReader(inputStream);
		}

		private readonly TextWriter @out;

		private readonly TextWriter error;

		private readonly StreamWriter inPipe;

		private readonly StreamReader @in;

		private readonly List<string> history = new List<string>();

		private int historyIndex = -1;

		private int outputMark;

		private void ReturnPressed()
		{
		    var text = Text.Substring(outputMark);
		    if (string.IsNullOrWhiteSpace(text) == false)
		        history.Add(text);
		    historyIndex = history.Count;
		    inPipe.Write(text);
		    AppendText(Environment.NewLine);
		    outputMark = Text.Length;
		    inPipe.Write(Environment.NewLine);
		    inPipe.Flush();
		    @out.Flush();
		}

	    public void Eval(string str)
		{
			inPipe.Write(str);
			inPipe.Write("\n");
			inPipe.Flush();

			@out.Flush();
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
		    switch (e.Key)
			{
				case Key.Left:
				case Key.Back:
					if (CaretIndex == outputMark)
					{
						e.Handled = true;
					}
					break;
				case Key.Delete:
                    if (CaretIndex < outputMark)
					{
						e.Handled = true;
					}
					break;
				case Key.Home:
				{
                    int caretPos = CaretIndex;
					if (caretPos == outputMark)
					{
						e.Handled = true;
					}
					else
					{
						if (caretPos > outputMark)
						{
							if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
							{
                                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                                {
                                    Select(outputMark, CaretIndex);
                                }
                                else
                                {
                                    CaretIndex = outputMark;
                                }
                                e.Handled = true;
							}
						}
					}
					break;
				}
				case Key.Enter:
					ReturnPressed();
					e.Handled = true;
					break;
				case Key.Up:
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
							ReplaceInput(str);
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
					e.Handled = true;
					break;
				case Key.Down:
				{
					if (history.Count > 0)
					{
						historyIndex++;
						if (historyIndex < 0)
						{
							historyIndex = 0;
						}
						if (historyIndex < history.Count)
						{
							string str = history[historyIndex];
							ReplaceInput(str);
						}
						else
						{
							historyIndex = history.Count;
							ReplaceInput(string.Empty);
						}
					}
					e.Handled = true;
					break;
				}
			}
		}

		private void ReplaceInput(string str)
		{
			Select(outputMark, Text.Length - outputMark);
			SelectedText = str;
			Select(Text.Length, 0);
		}

		public void Write(string str)
		{
			lock (this)
			{
				ReplaceInput(str);
				outputMark = Text.Length;
				SelectionStart = outputMark;
				SelectionLength = 0;
			}
		}

		public TextReader In
		{
			get { return @in; }
		}

		public TextWriter Out
		{
			get { return @out; }
		}

		public TextWriter Error
		{
			get { return error; }
		}
	}
}