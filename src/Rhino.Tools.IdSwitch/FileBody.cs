/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using Rhino.Tools.Idswitch;
using Sharpen;

namespace Rhino.Tools.Idswitch
{
	public class FileBody
	{
		private class ReplaceItem
		{
			internal FileBody.ReplaceItem next;

			internal int begin;

			internal int end;

			internal string replacement;

			internal ReplaceItem(int begin, int end, string text)
			{
				this.begin = begin;
				this.end = end;
				this.replacement = text;
			}
		}

		private char[] buffer = new char[1 << 14];

		private int bufferEnd;

		private int lineBegin;

		private int lineEnd;

		private int nextLineStart;

		private int lineNumber;

		internal FileBody.ReplaceItem firstReplace;

		internal FileBody.ReplaceItem lastReplace;

		// 16K
		public virtual char[] GetBuffer()
		{
			return buffer;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ReadData(TextReader r)
		{
			int capacity = buffer.Length;
			int offset = 0;
			for (; ; )
			{
				int n_read = r.Read(buffer, offset, capacity - offset);
				if (n_read < 0)
				{
					break;
				}
				offset += n_read;
				if (capacity == offset)
				{
					capacity *= 2;
					char[] tmp = new char[capacity];
					System.Array.Copy(buffer, 0, tmp, 0, offset);
					buffer = tmp;
				}
			}
			bufferEnd = offset;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WriteInitialData(TextWriter w)
		{
			w.Write(buffer, 0, bufferEnd);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WriteData(TextWriter w)
		{
			int offset = 0;
			for (FileBody.ReplaceItem x = firstReplace; x != null; x = x.next)
			{
				int before_replace = x.begin - offset;
				if (before_replace > 0)
				{
					w.Write(buffer, offset, before_replace);
				}
				w.Write(x.replacement);
				offset = x.end;
			}
			int tail = bufferEnd - offset;
			if (tail != 0)
			{
				w.Write(buffer, offset, tail);
			}
		}

		public virtual bool WasModified()
		{
			return firstReplace != null;
		}

		public virtual bool SetReplacement(int begin, int end, string text)
		{
			if (Equals(text, buffer, begin, end))
			{
				return false;
			}
			FileBody.ReplaceItem item = new FileBody.ReplaceItem(begin, end, text);
			if (firstReplace == null)
			{
				firstReplace = lastReplace = item;
			}
			else
			{
				if (begin < firstReplace.begin)
				{
					item.next = firstReplace;
					firstReplace = item;
				}
				else
				{
					FileBody.ReplaceItem cursor = firstReplace;
					FileBody.ReplaceItem next = cursor.next;
					while (next != null)
					{
						if (begin < next.begin)
						{
							item.next = next;
							cursor.next = item;
							break;
						}
						cursor = next;
						next = next.next;
					}
					if (next == null)
					{
						lastReplace.next = item;
					}
				}
			}
			return true;
		}

		public virtual int GetLineNumber()
		{
			return lineNumber;
		}

		public virtual int GetLineBegin()
		{
			return lineBegin;
		}

		public virtual int GetLineEnd()
		{
			return lineEnd;
		}

		public virtual void StartLineLoop()
		{
			lineNumber = 0;
			lineBegin = lineEnd = nextLineStart = 0;
		}

		public virtual bool NextLine()
		{
			if (nextLineStart == bufferEnd)
			{
				lineNumber = 0;
				return false;
			}
			int i;
			int c = 0;
			for (i = nextLineStart; i != bufferEnd; ++i)
			{
				c = buffer[i];
				if (c == '\n' || c == '\r')
				{
					break;
				}
			}
			lineBegin = nextLineStart;
			lineEnd = i;
			if (i == bufferEnd)
			{
				nextLineStart = i;
			}
			else
			{
				if (c == '\r' && i + 1 != bufferEnd && buffer[i + 1] == '\n')
				{
					nextLineStart = i + 2;
				}
				else
				{
					nextLineStart = i + 1;
				}
			}
			++lineNumber;
			return true;
		}

		private static bool Equals(string str, char[] array, int begin, int end)
		{
			if (str.Length == end - begin)
			{
				for (int i = begin, j = 0; i != end; ++i, ++j)
				{
					if (array[i] != str[j])
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
	}
}
