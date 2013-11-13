/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tools.Idswitch;
using Sharpen;

namespace Rhino.Tools.Idswitch
{
	internal class CodePrinter
	{
		private const int LITERAL_CHAR_MAX_SIZE = 6;

		private string lineTerminator = "\n";

		private int indentStep = 4;

		private int indentTabSize = 8;

		private char[] buffer = new char[1 << 12];

		private int offset;

		// length of u-type escape like \u12AB
		// 4K
		public virtual string GetLineTerminator()
		{
			return lineTerminator;
		}

		public virtual void SetLineTerminator(string value)
		{
			lineTerminator = value;
		}

		public virtual int GetIndentStep()
		{
			return indentStep;
		}

		public virtual void SetIndentStep(int char_count)
		{
			indentStep = char_count;
		}

		public virtual int GetIndentTabSize()
		{
			return indentTabSize;
		}

		public virtual void SetIndentTabSize(int tab_size)
		{
			indentTabSize = tab_size;
		}

		public virtual void Clear()
		{
			offset = 0;
		}

		private int Ensure_area(int area_size)
		{
			int begin = offset;
			int end = begin + area_size;
			if (end > buffer.Length)
			{
				int new_capacity = buffer.Length * 2;
				if (end > new_capacity)
				{
					new_capacity = end;
				}
				char[] tmp = new char[new_capacity];
				System.Array.Copy(buffer, 0, tmp, 0, begin);
				buffer = tmp;
			}
			return begin;
		}

		private int Add_area(int area_size)
		{
			int pos = Ensure_area(area_size);
			offset = pos + area_size;
			return pos;
		}

		public virtual int GetOffset()
		{
			return offset;
		}

		public virtual int GetLastChar()
		{
			return offset == 0 ? -1 : buffer[offset - 1];
		}

		public virtual void P(char c)
		{
			int pos = Add_area(1);
			buffer[pos] = c;
		}

		public virtual void P(string s)
		{
			int l = s.Length;
			int pos = Add_area(l);
			Sharpen.Runtime.GetCharsForString(s, 0, l, buffer, pos);
		}

		public void P(char[] array)
		{
			P(array, 0, array.Length);
		}

		public virtual void P(char[] array, int begin, int end)
		{
			int l = end - begin;
			int pos = Add_area(l);
			System.Array.Copy(array, begin, buffer, pos, l);
		}

		public virtual void P(int i)
		{
			P(Sharpen.Extensions.ToString(i));
		}

		public virtual void Qchar(int c)
		{
			int pos = Ensure_area(2 + LITERAL_CHAR_MAX_SIZE);
			buffer[pos] = '\'';
			pos = Put_string_literal_char(pos + 1, c, false);
			buffer[pos] = '\'';
			offset = pos + 1;
		}

		public virtual void Qstring(string s)
		{
			int l = s.Length;
			int pos = Ensure_area(2 + LITERAL_CHAR_MAX_SIZE * l);
			buffer[pos] = '"';
			++pos;
			for (int i = 0; i != l; ++i)
			{
				pos = Put_string_literal_char(pos, s[i], true);
			}
			buffer[pos] = '"';
			offset = pos + 1;
		}

		private int Put_string_literal_char(int pos, int c, bool in_string)
		{
			bool backslash_symbol = true;
			switch (c)
			{
				case '\b':
				{
					c = 'b';
					break;
				}

				case '\t':
				{
					c = 't';
					break;
				}

				case '\n':
				{
					c = 'n';
					break;
				}

				case '\f':
				{
					c = 'f';
					break;
				}

				case '\r':
				{
					c = 'r';
					break;
				}

				case '\'':
				{
					backslash_symbol = !in_string;
					break;
				}

				case '"':
				{
					backslash_symbol = in_string;
					break;
				}

				default:
				{
					backslash_symbol = false;
					break;
				}
			}
			if (backslash_symbol)
			{
				buffer[pos] = '\\';
				buffer[pos + 1] = (char)c;
				pos += 2;
			}
			else
			{
				if (' ' <= c && c <= 126)
				{
					buffer[pos] = (char)c;
					++pos;
				}
				else
				{
					buffer[pos] = '\\';
					buffer[pos + 1] = 'u';
					buffer[pos + 2] = Digit_to_hex_letter(unchecked((int)(0xF)) & (c >> 12));
					buffer[pos + 3] = Digit_to_hex_letter(unchecked((int)(0xF)) & (c >> 8));
					buffer[pos + 4] = Digit_to_hex_letter(unchecked((int)(0xF)) & (c >> 4));
					buffer[pos + 5] = Digit_to_hex_letter(unchecked((int)(0xF)) & c);
					pos += 6;
				}
			}
			return pos;
		}

		private static char Digit_to_hex_letter(int d)
		{
			return (char)((d < 10) ? '0' + d : 'A' - 10 + d);
		}

		public virtual void Indent(int level)
		{
			int visible_size = indentStep * level;
			int indent_size;
			int tab_count;
			if (indentTabSize <= 0)
			{
				tab_count = 0;
				indent_size = visible_size;
			}
			else
			{
				tab_count = visible_size / indentTabSize;
				indent_size = tab_count + visible_size % indentTabSize;
			}
			int pos = Add_area(indent_size);
			int tab_end = pos + tab_count;
			int indent_end = pos + indent_size;
			for (; pos != tab_end; ++pos)
			{
				buffer[pos] = '\t';
			}
			for (; pos != indent_end; ++pos)
			{
				buffer[pos] = ' ';
			}
		}

		public virtual void Nl()
		{
			P('\n');
		}

		public virtual void Line(int indent_level, string s)
		{
			Indent(indent_level);
			P(s);
			Nl();
		}

		public virtual void Erase(int begin, int end)
		{
			System.Array.Copy(buffer, end, buffer, begin, offset - end);
			offset -= end - begin;
		}

		public override string ToString()
		{
			return new string(buffer, 0, offset);
		}
	}
}
