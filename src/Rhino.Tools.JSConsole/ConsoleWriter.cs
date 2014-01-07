using System.IO;
using System.Text;
using System.Windows;

namespace Rhino.Tools.JsConsole
{
	internal class ConsoleWriter : TextWriter
	{
		private readonly ConsoleTextArea textArea;

		private readonly StringBuilder buffer;

		public ConsoleWriter(ConsoleTextArea textArea)
		{
			this.textArea = textArea;
			buffer = new StringBuilder();
		}

		public override Encoding Encoding
		{
			get { return Encoding.UTF8; }
		}

		public override void Write(char[] data, int off, int len)
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
			Application.Current.Dispatcher.Invoke(() => textArea.Write(str));
		}
	}
}