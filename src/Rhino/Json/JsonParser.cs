/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Json;
using Sharpen;

namespace Rhino.Json
{
	/// <summary>This class converts a stream of JSON tokens into a JSON value.</summary>
	/// <remarks>
	/// This class converts a stream of JSON tokens into a JSON value.
	/// See ECMA 15.12.
	/// </remarks>
	/// <author>Raphael Speyer</author>
	/// <author>Hannes Wallnoefer</author>
	public class JsonParser
	{
		private Context cx;

		private Scriptable scope;

		private int pos;

		private int length;

		private string src;

		public JsonParser(Context cx, Scriptable scope)
		{
			this.cx = cx;
			this.scope = scope;
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		public virtual object ParseValue(string json)
		{
			lock (this)
			{
				if (json == null)
				{
					throw new JsonParser.ParseException("Input string may not be null");
				}
				pos = 0;
				length = json.Length;
				src = json;
				object value = ReadValue();
				ConsumeWhitespace();
				if (pos < length)
				{
					throw new JsonParser.ParseException("Expected end of stream at char " + pos);
				}
				return value;
			}
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private object ReadValue()
		{
			ConsumeWhitespace();
			while (pos < length)
			{
				char c = src[pos++];
				switch (c)
				{
					case '{':
					{
						return ReadObject();
					}

					case '[':
					{
						return ReadArray();
					}

					case 't':
					{
						return ReadTrue();
					}

					case 'f':
					{
						return ReadFalse();
					}

					case '"':
					{
						return ReadString();
					}

					case 'n':
					{
						return ReadNull();
					}

					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case '0':
					case '-':
					{
						return ReadNumber(c);
					}

					default:
					{
						throw new JsonParser.ParseException("Unexpected token: " + c);
					}
				}
			}
			throw new JsonParser.ParseException("Empty JSON string");
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private object ReadObject()
		{
			ConsumeWhitespace();
			Scriptable @object = cx.NewObject(scope);
			// handle empty object literal case early
			if (pos < length && src[pos] == '}')
			{
				pos += 1;
				return @object;
			}
			string id;
			object value;
			bool needsComma = false;
			while (pos < length)
			{
				char c = src[pos++];
				switch (c)
				{
					case '}':
					{
						if (!needsComma)
						{
							throw new JsonParser.ParseException("Unexpected comma in object literal");
						}
						return @object;
					}

					case ',':
					{
						if (!needsComma)
						{
							throw new JsonParser.ParseException("Unexpected comma in object literal");
						}
						needsComma = false;
						break;
					}

					case '"':
					{
						if (needsComma)
						{
							throw new JsonParser.ParseException("Missing comma in object literal");
						}
						id = ReadString();
						Consume(':');
						value = ReadValue();
						long index = ScriptRuntime.IndexFromString(id);
						if (index < 0)
						{
							@object.Put(id, @object, value);
						}
						else
						{
							@object.Put((int)index, @object, value);
						}
						needsComma = true;
						break;
					}

					default:
					{
						throw new JsonParser.ParseException("Unexpected token in object literal");
					}
				}
				ConsumeWhitespace();
			}
			throw new JsonParser.ParseException("Unterminated object literal");
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private object ReadArray()
		{
			ConsumeWhitespace();
			// handle empty array literal case early
			if (pos < length && src[pos] == ']')
			{
				pos += 1;
				return cx.NewArray(scope, 0);
			}
			IList<object> list = new List<object>();
			bool needsComma = false;
			while (pos < length)
			{
				char c = src[pos];
				switch (c)
				{
					case ']':
					{
						if (!needsComma)
						{
							throw new JsonParser.ParseException("Unexpected comma in array literal");
						}
						pos += 1;
						return cx.NewArray(scope, Sharpen.Collections.ToArray(list));
					}

					case ',':
					{
						if (!needsComma)
						{
							throw new JsonParser.ParseException("Unexpected comma in array literal");
						}
						needsComma = false;
						pos += 1;
						break;
					}

					default:
					{
						if (needsComma)
						{
							throw new JsonParser.ParseException("Missing comma in array literal");
						}
						list.Add(ReadValue());
						needsComma = true;
						break;
					}
				}
				ConsumeWhitespace();
			}
			throw new JsonParser.ParseException("Unterminated array literal");
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private string ReadString()
		{
			int stringStart = pos;
			while (pos < length)
			{
				char c = src[pos++];
				if (c <= '\u001F')
				{
					throw new JsonParser.ParseException("String contains control character");
				}
				else
				{
					if (c == '\\')
					{
						break;
					}
					else
					{
						if (c == '"')
						{
							return Sharpen.Runtime.Substring(src, stringStart, pos - 1);
						}
					}
				}
			}
			StringBuilder b = new StringBuilder();
			while (pos < length)
			{
				System.Diagnostics.Debug.Assert(src[pos - 1] == '\\');
				b.AppendRange(src, stringStart, pos - 1);
				if (pos >= length)
				{
					throw new JsonParser.ParseException("Unterminated string");
				}
				char c = src[pos++];
				switch (c)
				{
					case '"':
					{
						b.Append('"');
						break;
					}

					case '\\':
					{
						b.Append('\\');
						break;
					}

					case '/':
					{
						b.Append('/');
						break;
					}

					case 'b':
					{
						b.Append('\b');
						break;
					}

					case 'f':
					{
						b.Append('\f');
						break;
					}

					case 'n':
					{
						b.Append('\n');
						break;
					}

					case 'r':
					{
						b.Append('\r');
						break;
					}

					case 't':
					{
						b.Append('\t');
						break;
					}

					case 'u':
					{
						if (length - pos < 5)
						{
							throw new JsonParser.ParseException("Invalid character code: \\u" + Sharpen.Runtime.Substring(src, pos));
						}
						int code = FromHex(src[pos + 0]) << 12 | FromHex(src[pos + 1]) << 8 | FromHex(src[pos + 2]) << 4 | FromHex(src[pos + 3]);
						if (code < 0)
						{
							throw new JsonParser.ParseException("Invalid character code: " + Sharpen.Runtime.Substring(src, pos, pos + 4));
						}
						pos += 4;
						b.Append((char)code);
						break;
					}

					default:
					{
						throw new JsonParser.ParseException("Unexpected character in string: '\\" + c + "'");
					}
				}
				stringStart = pos;
				while (pos < length)
				{
					c = src[pos++];
					if (c <= '\u001F')
					{
						throw new JsonParser.ParseException("String contains control character");
					}
					else
					{
						if (c == '\\')
						{
							break;
						}
						else
						{
							if (c == '"')
							{
								b.AppendRange(src, stringStart, pos - 1);
								return b.ToString();
							}
						}
					}
				}
			}
			throw new JsonParser.ParseException("Unterminated string literal");
		}

		private int FromHex(char c)
		{
			return c >= '0' && c <= '9' ? c - '0' : c >= 'A' && c <= 'F' ? c - 'A' + 10 : c >= 'a' && c <= 'f' ? c - 'a' + 10 : -1;
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private object ReadNumber(char c)
		{
			System.Diagnostics.Debug.Assert(c == '-' || (c >= '0' && c <= '9'));
			int numberStart = pos - 1;
			if (c == '-')
			{
				c = NextOrNumberError(numberStart);
				if (!(c >= '0' && c <= '9'))
				{
					throw NumberError(numberStart, pos);
				}
			}
			if (c != '0')
			{
				ReadDigits();
			}
			// read optional fraction part
			if (pos < length)
			{
				c = src[pos];
				if (c == '.')
				{
					pos += 1;
					c = NextOrNumberError(numberStart);
					if (!(c >= '0' && c <= '9'))
					{
						throw NumberError(numberStart, pos);
					}
					ReadDigits();
				}
			}
			// read optional exponent part
			if (pos < length)
			{
				c = src[pos];
				if (c == 'e' || c == 'E')
				{
					pos += 1;
					c = NextOrNumberError(numberStart);
					if (c == '-' || c == '+')
					{
						c = NextOrNumberError(numberStart);
					}
					if (!(c >= '0' && c <= '9'))
					{
						throw NumberError(numberStart, pos);
					}
					ReadDigits();
				}
			}
			string num = Sharpen.Runtime.Substring(src, numberStart, pos);
			double dval = System.Double.Parse(num);
			int ival = (int)dval;
			if (ival == dval)
			{
				return Sharpen.Extensions.ValueOf(ival);
			}
			else
			{
				return Sharpen.Extensions.ValueOf(dval);
			}
		}

		private JsonParser.ParseException NumberError(int start, int end)
		{
			return new JsonParser.ParseException("Unsupported number format: " + Sharpen.Runtime.Substring(src, start, end));
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private char NextOrNumberError(int numberStart)
		{
			if (pos >= length)
			{
				throw NumberError(numberStart, length);
			}
			return src[pos++];
		}

		private void ReadDigits()
		{
			for (; pos < length; ++pos)
			{
				char c = src[pos];
				if (!(c >= '0' && c <= '9'))
				{
					break;
				}
			}
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private bool ReadTrue()
		{
			if (length - pos < 3 || src[pos] != 'r' || src[pos + 1] != 'u' || src[pos + 2] != 'e')
			{
				throw new JsonParser.ParseException("Unexpected token: t");
			}
			pos += 3;
			return true;
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private bool ReadFalse()
		{
			if (length - pos < 4 || src[pos] != 'a' || src[pos + 1] != 'l' || src[pos + 2] != 's' || src[pos + 3] != 'e')
			{
				throw new JsonParser.ParseException("Unexpected token: f");
			}
			pos += 4;
			return false;
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private object ReadNull()
		{
			if (length - pos < 3 || src[pos] != 'u' || src[pos + 1] != 'l' || src[pos + 2] != 'l')
			{
				throw new JsonParser.ParseException("Unexpected token: n");
			}
			pos += 3;
			return null;
		}

		private void ConsumeWhitespace()
		{
			while (pos < length)
			{
				char c = src[pos];
				switch (c)
				{
					case ' ':
					case '\t':
					case '\r':
					case '\n':
					{
						pos += 1;
						break;
					}

					default:
					{
						return;
					}
				}
			}
		}

		/// <exception cref="Rhino.Json.JsonParser.ParseException"></exception>
		private void Consume(char token)
		{
			ConsumeWhitespace();
			if (pos >= length)
			{
				throw new JsonParser.ParseException("Expected " + token + " but reached end of stream");
			}
			char c = src[pos++];
			if (c == token)
			{
				return;
			}
			else
			{
				throw new JsonParser.ParseException("Expected " + token + " found " + c);
			}
		}

		[System.Serializable]
		public class ParseException : Exception
		{
			internal const long serialVersionUID = 4804542791749920772L;

			internal ParseException(string message) : base(message)
			{
			}

			internal ParseException(Exception cause) : base(cause)
			{
			}
		}
	}
}
