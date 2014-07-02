using System;
using System.Collections.Generic;
using System.Windows.Input;
using Sharpen;

namespace Rhino.Tools.Debugger
{
    /// <summary>Extension of TextBox for script evaluation input.</summary>
    public partial class EvalTextArea
    {
        /// <summary>History of expressions that have been evaluated</summary>
        private readonly IList<string> history = new Sharpen.SynchronizedList<string> (new List<string>());

        /// <summary>Index of the selected history item.</summary>
        private int historyIndex = -1;

        /// <summary>Position in the display where output should go.</summary>
        private int outputMark;

        public EvalTextArea()
        {
            InitializeComponent();
            AppendText("% ");
            CaretIndex = outputMark = Text.Length;
        }

        public Dim Debugger { get; set; }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.Back:
                    if (outputMark == CaretIndex)
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
                    var caretPos = CaretIndex;
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
                            var str = history[historyIndex];
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
                            ReplaceInput(history[historyIndex]);
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
            Select(outputMark, Text.Length);
            SelectedText = str;
            Select(outputMark + str.Length, 0);
        }

        /// <summary>Called when Enter is pressed.</summary>
        private void ReturnPressed()
        {
            var text = Text.Substring(outputMark);
            if (!Debugger.StringIsCompilableUnit(text))
            {
                AppendText(Environment.NewLine);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    history.Add(text);
                    historyIndex = history.Count;
                }
                AppendText(Environment.NewLine);
                var result = Debugger.Eval(text);
                if (!string.IsNullOrEmpty(result))
                {
                    AppendText(result);
                    AppendText(Environment.NewLine);
                }
                AppendText("% ");
                CaretIndex = outputMark = Text.Length;

            }
        }
    }
}
