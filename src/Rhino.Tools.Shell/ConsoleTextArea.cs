/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using Java.Awt;
using Java.Awt.Event;
using Javax.Swing;
using Javax.Swing.Event;
using Javax.Swing.Text;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	internal class ConsoleWrite : Runnable
	{
		private ConsoleTextArea textArea;

		private string str;

		public ConsoleWrite(ConsoleTextArea textArea, string str)
		{
			this.textArea = textArea;
			this.str = str;
		}

		public virtual void Run()
		{
			textArea.Write(str);
		}
	}

	internal class ConsoleWriter : OutputStream
	{
		private ConsoleTextArea textArea;

		private StringBuilder buffer;

		public ConsoleWriter(ConsoleTextArea textArea)
		{
			this.textArea = textArea;
			buffer = new StringBuilder();
		}

		public override void Write(int ch)
		{
			lock (this)
			{
				buffer.Append((char)ch);
				if (ch == '\n')
				{
					FlushBuffer();
				}
			}
		}

		public virtual void Write(char[] data, int off, int len)
		{
			lock (this)
			{
				for (int i = off; i < len; i++)
				{
					buffer.Append(data[i]);
					if (data[i] == '\n')
					{
						FlushBuffer();
					}
				}
			}
		}

		public override void Flush()
		{
			lock (this)
			{
				if (buffer.Length > 0)
				{
					FlushBuffer();
				}
			}
		}

		public override void Close()
		{
			Flush();
		}

		private void FlushBuffer()
		{
			string str = buffer.ToString();
			buffer.Length = 0;
			SwingUtilities.InvokeLater(new ConsoleWrite(textArea, str));
		}
	}

	[System.Serializable]
	public class ConsoleTextArea : JTextArea, KeyListener, DocumentListener
	{
		internal const long serialVersionUID = 8557083244830872961L;

		private ConsoleWriter console1;

		private ConsoleWriter console2;

		private TextWriter @out;

		private TextWriter err;

		private PrintWriter inPipe;

		private PipedInputStream @in;

		private IList<string> history;

		private int historyIndex = -1;

		private int outputMark = 0;

		public override void Select(int start, int end)
		{
			RequestFocus();
			base.Select(start, end);
		}

		public ConsoleTextArea(string[] argv) : base()
		{
			history = new List<string>();
			console1 = new ConsoleWriter(this);
			console2 = new ConsoleWriter(this);
			@out = new TextWriter(console1, true);
			err = new TextWriter(console2, true);
			PipedOutputStream outPipe = new PipedOutputStream();
			inPipe = new PrintWriter(outPipe);
			@in = new PipedInputStream();
			try
			{
				outPipe.Connect(@in);
			}
			catch (IOException exc)
			{
				Sharpen.Runtime.PrintStackTrace(exc);
			}
			GetDocument().AddDocumentListener(this);
			AddKeyListener(this);
			SetLineWrap(true);
			SetFont(new Font("Monospaced", 0, 12));
		}

		internal virtual void ReturnPressed()
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
				if (segment.count > 0)
				{
					history.Add(segment.ToString());
				}
				historyIndex = history.Count;
				inPipe.Write(segment.array, segment.offset, segment.count);
				Append("\n");
				outputMark = doc.GetLength();
				inPipe.Write("\n");
				inPipe.Flush();
				console1.Flush();
			}
		}

		public virtual void Eval(string str)
		{
			inPipe.Write(str);
			inPipe.Write("\n");
			inPipe.Flush();
			console1.Flush();
		}

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

		public virtual void KeyReleased(KeyEvent e)
		{
			lock (this)
			{
			}
		}

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

		public virtual void PostUpdateUI()
		{
			lock (this)
			{
				// this attempts to cleanup the damage done by updateComponentTreeUI
				RequestFocus();
				SetCaret(GetCaret());
				Select(outputMark, outputMark);
			}
		}

		public virtual void ChangedUpdate(DocumentEvent e)
		{
			lock (this)
			{
			}
		}

		public virtual InputStream GetIn()
		{
			return @in;
		}

		public virtual TextWriter GetOut()
		{
			return @out;
		}

		public virtual TextWriter GetErr()
		{
			return err;
		}
	}
}
