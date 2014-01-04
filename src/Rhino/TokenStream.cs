/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the JavaScript scanner.</summary>
	/// <remarks>
	/// This class implements the JavaScript scanner.
	/// It is based on the C source files jsscan.c and jsscan.h
	/// in the jsref package.
	/// </remarks>
	/// <seealso cref="Parser">Parser</seealso>
	/// <author>Mike McCabe</author>
	/// <author>Brendan Eich</author>
	internal class TokenStream
	{
		private const int EOF_CHAR = -1;

		private const char BYTE_ORDER_MARK = '\uFEFF';

		internal TokenStream(Parser parser, TextReader sourceReader, string sourceString, int lineno)
		{
			this.parser = parser;
			this.lineno = lineno;
			if (sourceReader != null)
			{
				if (sourceString != null)
				{
					Kit.CodeBug();
				}
				this.sourceReader = sourceReader;
				this.sourceBuffer = new char[512];
				this.sourceEnd = 0;
			}
			else
			{
				if (sourceString == null)
				{
					Kit.CodeBug();
				}
				this.sourceString = sourceString;
				this.sourceEnd = sourceString.Length;
			}
			this.sourceCursor = this.cursor = 0;
		}

		internal virtual string TokenToString(int token)
		{
			return string.Empty;
		}

		internal static bool IsKeyword(string s)
		{
			return Token.EOF != StringToKeyword(s);
		}

		private static int StringToKeyword(string name)
		{
			// #string_id_map#
			// The following assumes that Token.EOF == 0
			int Id_break = Token.BREAK;
			int Id_case = Token.CASE;
			int Id_continue = Token.CONTINUE;
			int Id_default = Token.DEFAULT;
			int Id_delete = Token.DELPROP;
			int Id_do = Token.DO;
			int Id_else = Token.ELSE;
			int Id_export = Token.RESERVED;
			int Id_false = Token.FALSE;
			int Id_for = Token.FOR;
			int Id_function = Token.FUNCTION;
			int Id_if = Token.IF;
			int Id_in = Token.IN;
			int Id_let = Token.LET;
			int Id_new = Token.NEW;
			int Id_null = Token.NULL;
			int Id_return = Token.RETURN;
			int Id_switch = Token.SWITCH;
			int Id_this = Token.THIS;
			int Id_true = Token.TRUE;
			int Id_typeof = Token.TYPEOF;
			int Id_var = Token.VAR;
			int Id_void = Token.VOID;
			int Id_while = Token.WHILE;
			int Id_with = Token.WITH;
			int Id_yield = Token.YIELD;
			int Id_abstract = Token.RESERVED;
			int Id_boolean = Token.RESERVED;
			int Id_byte = Token.RESERVED;
			int Id_catch = Token.CATCH;
			int Id_char = Token.RESERVED;
			int Id_class = Token.RESERVED;
			int Id_const = Token.CONST;
			int Id_debugger = Token.DEBUGGER;
			int Id_double = Token.RESERVED;
			int Id_enum = Token.RESERVED;
			int Id_extends = Token.RESERVED;
			int Id_final = Token.RESERVED;
			int Id_finally = Token.FINALLY;
			int Id_float = Token.RESERVED;
			int Id_goto = Token.RESERVED;
			int Id_implements = Token.RESERVED;
			int Id_import = Token.RESERVED;
			int Id_instanceof = Token.INSTANCEOF;
			int Id_int = Token.RESERVED;
			int Id_interface = Token.RESERVED;
			int Id_long = Token.RESERVED;
			int Id_native = Token.RESERVED;
			int Id_package = Token.RESERVED;
			int Id_private = Token.RESERVED;
			int Id_protected = Token.RESERVED;
			int Id_public = Token.RESERVED;
			int Id_short = Token.RESERVED;
			int Id_static = Token.RESERVED;
			int Id_super = Token.RESERVED;
			int Id_synchronized = Token.RESERVED;
			int Id_throw = Token.THROW;
			int Id_throws = Token.RESERVED;
			int Id_transient = Token.RESERVED;
			int Id_try = Token.TRY;
			int Id_volatile = Token.RESERVED;
			// reserved ES5 strict
			// reserved ES5 strict
			// the following are #ifdef RESERVE_JAVA_KEYWORDS in jsscan.c
			// ES3 only
			// ES3 only
			// ES3 only
			// ES3 only
			// reserved
			// ES3 only
			// ES3 only
			// ES3 only
			// ES3 only
			// ES3, ES5 strict
			// ES3
			// ES3, ES5 strict
			// ES3 only
			// ES3 only
			// ES3, ES5 strict
			// ES3, ES5 strict
			// ES3, ES5 strict
			// ES3, ES5 strict
			// ES3 only
			// ES3, ES5 strict
			// ES3 only
			// ES3 only
			// ES3 only
			// ES3 only
			int id;
			string s = name;
			// #generated# Last update: 2007-04-18 13:53:30 PDT
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 2:
				{
					c = s[1];
					if (c == 'f')
					{
						if (s[0] == 'i')
						{
							id = Id_if;
							goto L0_break;
						}
					}
					else
					{
						if (c == 'n')
						{
							if (s[0] == 'i')
							{
								id = Id_in;
								goto L0_break;
							}
						}
						else
						{
							if (c == 'o')
							{
								if (s[0] == 'd')
								{
									id = Id_do;
									goto L0_break;
								}
							}
						}
					}
					goto L_break;
				}

				case 3:
				{
					switch (s[0])
					{
						case 'f':
						{
							if (s[2] == 'r' && s[1] == 'o')
							{
								id = Id_for;
								goto L0_break;
							}
							goto L_break;
						}

						case 'i':
						{
							if (s[2] == 't' && s[1] == 'n')
							{
								id = Id_int;
								goto L0_break;
							}
							goto L_break;
						}

						case 'l':
						{
							if (s[2] == 't' && s[1] == 'e')
							{
								id = Id_let;
								goto L0_break;
							}
							goto L_break;
						}

						case 'n':
						{
							if (s[2] == 'w' && s[1] == 'e')
							{
								id = Id_new;
								goto L0_break;
							}
							goto L_break;
						}

						case 't':
						{
							if (s[2] == 'y' && s[1] == 'r')
							{
								id = Id_try;
								goto L0_break;
							}
							goto L_break;
						}

						case 'v':
						{
							if (s[2] == 'r' && s[1] == 'a')
							{
								id = Id_var;
								goto L0_break;
							}
							goto L_break;
						}
					}
					goto L_break;
				}

				case 4:
				{
					switch (s[0])
					{
						case 'b':
						{
							X = "byte";
							id = Id_byte;
							goto L_break;
						}

						case 'c':
						{
							c = s[3];
							if (c == 'e')
							{
								if (s[2] == 's' && s[1] == 'a')
								{
									id = Id_case;
									goto L0_break;
								}
							}
							else
							{
								if (c == 'r')
								{
									if (s[2] == 'a' && s[1] == 'h')
									{
										id = Id_char;
										goto L0_break;
									}
								}
							}
							goto L_break;
						}

						case 'e':
						{
							c = s[3];
							if (c == 'e')
							{
								if (s[2] == 's' && s[1] == 'l')
								{
									id = Id_else;
									goto L0_break;
								}
							}
							else
							{
								if (c == 'm')
								{
									if (s[2] == 'u' && s[1] == 'n')
									{
										id = Id_enum;
										goto L0_break;
									}
								}
							}
							goto L_break;
						}

						case 'g':
						{
							X = "goto";
							id = Id_goto;
							goto L_break;
						}

						case 'l':
						{
							X = "long";
							id = Id_long;
							goto L_break;
						}

						case 'n':
						{
							X = "null";
							id = Id_null;
							goto L_break;
						}

						case 't':
						{
							c = s[3];
							if (c == 'e')
							{
								if (s[2] == 'u' && s[1] == 'r')
								{
									id = Id_true;
									goto L0_break;
								}
							}
							else
							{
								if (c == 's')
								{
									if (s[2] == 'i' && s[1] == 'h')
									{
										id = Id_this;
										goto L0_break;
									}
								}
							}
							goto L_break;
						}

						case 'v':
						{
							X = "void";
							id = Id_void;
							goto L_break;
						}

						case 'w':
						{
							X = "with";
							id = Id_with;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 5:
				{
					switch (s[2])
					{
						case 'a':
						{
							X = "class";
							id = Id_class;
							goto L_break;
						}

						case 'e':
						{
							c = s[0];
							if (c == 'b')
							{
								X = "break";
								id = Id_break;
							}
							else
							{
								if (c == 'y')
								{
									X = "yield";
									id = Id_yield;
								}
							}
							goto L_break;
						}

						case 'i':
						{
							X = "while";
							id = Id_while;
							goto L_break;
						}

						case 'l':
						{
							X = "false";
							id = Id_false;
							goto L_break;
						}

						case 'n':
						{
							c = s[0];
							if (c == 'c')
							{
								X = "const";
								id = Id_const;
							}
							else
							{
								if (c == 'f')
								{
									X = "final";
									id = Id_final;
								}
							}
							goto L_break;
						}

						case 'o':
						{
							c = s[0];
							if (c == 'f')
							{
								X = "float";
								id = Id_float;
							}
							else
							{
								if (c == 's')
								{
									X = "short";
									id = Id_short;
								}
							}
							goto L_break;
						}

						case 'p':
						{
							X = "super";
							id = Id_super;
							goto L_break;
						}

						case 'r':
						{
							X = "throw";
							id = Id_throw;
							goto L_break;
						}

						case 't':
						{
							X = "catch";
							id = Id_catch;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 6:
				{
					switch (s[1])
					{
						case 'a':
						{
							X = "native";
							id = Id_native;
							goto L_break;
						}

						case 'e':
						{
							c = s[0];
							if (c == 'd')
							{
								X = "delete";
								id = Id_delete;
							}
							else
							{
								if (c == 'r')
								{
									X = "return";
									id = Id_return;
								}
							}
							goto L_break;
						}

						case 'h':
						{
							X = "throws";
							id = Id_throws;
							goto L_break;
						}

						case 'm':
						{
							X = "import";
							id = Id_import;
							goto L_break;
						}

						case 'o':
						{
							X = "double";
							id = Id_double;
							goto L_break;
						}

						case 't':
						{
							X = "static";
							id = Id_static;
							goto L_break;
						}

						case 'u':
						{
							X = "public";
							id = Id_public;
							goto L_break;
						}

						case 'w':
						{
							X = "switch";
							id = Id_switch;
							goto L_break;
						}

						case 'x':
						{
							X = "export";
							id = Id_export;
							goto L_break;
						}

						case 'y':
						{
							X = "typeof";
							id = Id_typeof;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 7:
				{
					switch (s[1])
					{
						case 'a':
						{
							X = "package";
							id = Id_package;
							goto L_break;
						}

						case 'e':
						{
							X = "default";
							id = Id_default;
							goto L_break;
						}

						case 'i':
						{
							X = "finally";
							id = Id_finally;
							goto L_break;
						}

						case 'o':
						{
							X = "boolean";
							id = Id_boolean;
							goto L_break;
						}

						case 'r':
						{
							X = "private";
							id = Id_private;
							goto L_break;
						}

						case 'x':
						{
							X = "extends";
							id = Id_extends;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 8:
				{
					switch (s[0])
					{
						case 'a':
						{
							X = "abstract";
							id = Id_abstract;
							goto L_break;
						}

						case 'c':
						{
							X = "continue";
							id = Id_continue;
							goto L_break;
						}

						case 'd':
						{
							X = "debugger";
							id = Id_debugger;
							goto L_break;
						}

						case 'f':
						{
							X = "function";
							id = Id_function;
							goto L_break;
						}

						case 'v':
						{
							X = "volatile";
							id = Id_volatile;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 9:
				{
					c = s[0];
					if (c == 'i')
					{
						X = "interface";
						id = Id_interface;
					}
					else
					{
						if (c == 'p')
						{
							X = "protected";
							id = Id_protected;
						}
						else
						{
							if (c == 't')
							{
								X = "transient";
								id = Id_transient;
							}
						}
					}
					goto L_break;
				}

				case 10:
				{
					c = s[1];
					if (c == 'm')
					{
						X = "implements";
						id = Id_implements;
					}
					else
					{
						if (c == 'n')
						{
							X = "instanceof";
							id = Id_instanceof;
						}
					}
					goto L_break;
				}

				case 12:
				{
					X = "synchronized";
					id = Id_synchronized;
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
L0_break: ;
			// #/generated#
			// #/string_id_map#
			if (id == 0)
			{
				return Token.EOF;
			}
			return id & unchecked((int)(0xff));
		}

		internal string GetSourceString()
		{
			return sourceString;
		}

		internal int GetLineno()
		{
			return lineno;
		}

		internal string GetString()
		{
			return @string;
		}

		internal char GetQuoteChar()
		{
			return (char)quoteChar;
		}

		internal double GetNumber()
		{
			return number;
		}

		internal bool IsNumberOctal()
		{
			return isOctal;
		}

		internal bool Eof()
		{
			return hitEOF;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal int GetToken()
		{
			int c;
			for (; ; )
			{
				// Eat whitespace, possibly sensitive to newlines.
				for (; ; )
				{
					c = GetChar();
					if (c == EOF_CHAR)
					{
						tokenBeg = cursor - 1;
						tokenEnd = cursor;
						return Token.EOF;
					}
					else
					{
						if (c == '\n')
						{
							dirtyLine = false;
							tokenBeg = cursor - 1;
							tokenEnd = cursor;
							return Token.EOL;
						}
						else
						{
							if (!IsJSSpace(c))
							{
								if (c != '-')
								{
									dirtyLine = true;
								}
								break;
							}
						}
					}
				}
				// Assume the token will be 1 char - fixed up below.
				tokenBeg = cursor - 1;
				tokenEnd = cursor;
				if (c == '@')
				{
					return Token.XMLATTR;
				}
				// identifier/keyword/instanceof?
				// watch out for starting with a <backslash>
				bool identifierStart;
				bool isUnicodeEscapeStart = false;
				if (c == '\\')
				{
					c = GetChar();
					if (c == 'u')
					{
						identifierStart = true;
						isUnicodeEscapeStart = true;
						stringBufferTop = 0;
					}
					else
					{
						identifierStart = false;
						UngetChar(c);
						c = '\\';
					}
				}
				else
				{
					identifierStart = CharEx.IsJavaIdentifierStart((char)c);
					if (identifierStart)
					{
						stringBufferTop = 0;
						AddToString(c);
					}
				}
				if (identifierStart)
				{
					bool containsEscape = isUnicodeEscapeStart;
					for (; ; )
					{
						if (isUnicodeEscapeStart)
						{
							// strictly speaking we should probably push-back
							// all the bad characters if the <backslash>uXXXX
							// sequence is malformed. But since there isn't a
							// correct context(is there?) for a bad Unicode
							// escape sequence in an identifier, we can report
							// an error here.
							int escapeVal = 0;
							for (int i = 0; i != 4; ++i)
							{
								c = GetChar();
								escapeVal = Kit.XDigitToInt(c, escapeVal);
								// Next check takes care about c < 0 and bad escape
								if (escapeVal < 0)
								{
									break;
								}
							}
							if (escapeVal < 0)
							{
								parser.AddError("msg.invalid.escape");
								return Token.ERROR;
							}
							AddToString(escapeVal);
							isUnicodeEscapeStart = false;
						}
						else
						{
							c = GetChar();
							if (c == '\\')
							{
								c = GetChar();
								if (c == 'u')
								{
									isUnicodeEscapeStart = true;
									containsEscape = true;
								}
								else
								{
									parser.AddError("msg.illegal.character");
									return Token.ERROR;
								}
							}
							else
							{
								if (c == EOF_CHAR || c == BYTE_ORDER_MARK || !CharEx.IsJavaIdentifierPart((char)c))
								{
									break;
								}
								AddToString(c);
							}
						}
					}
					UngetChar(c);
					string str = GetStringFromBuffer();
					if (!containsEscape)
					{
						// OPT we shouldn't have to make a string (object!) to
						// check if it's a keyword.
						// Return the corresponding token if it's a keyword
						int result = StringToKeyword(str);
						if (result != Token.EOF)
						{
							if ((result == Token.LET || result == Token.YIELD) && parser.compilerEnv.GetLanguageVersion() < Context.VERSION_1_7)
							{
								// LET and YIELD are tokens only in 1.7 and later
								@string = result == Token.LET ? "let" : "yield";
								result = Token.NAME;
							}
							// Save the string in case we need to use in
							// object literal definitions.
							this.@string = (string)allStrings.Intern(str);
							if (result != Token.RESERVED)
							{
								return result;
							}
							else
							{
								if (!parser.compilerEnv.IsReservedKeywordAsIdentifier())
								{
									return result;
								}
							}
						}
					}
					else
					{
						if (IsKeyword(str))
						{
							// If a string contains unicodes, and converted to a keyword,
							// we convert the last character back to unicode
							str = ConvertLastCharToHex(str);
						}
					}
					this.@string = (string)allStrings.Intern(str);
					return Token.NAME;
				}
				// is it a number?
				if (IsDigit(c) || (c == '.' && IsDigit(PeekChar())))
				{
					isOctal = false;
					stringBufferTop = 0;
					int @base = 10;
					if (c == '0')
					{
						c = GetChar();
						if (c == 'x' || c == 'X')
						{
							@base = 16;
							c = GetChar();
						}
						else
						{
							if (IsDigit(c))
							{
								@base = 8;
								isOctal = true;
							}
							else
							{
								AddToString('0');
							}
						}
					}
					if (@base == 16)
					{
						while (0 <= Kit.XDigitToInt(c, 0))
						{
							AddToString(c);
							c = GetChar();
						}
					}
					else
					{
						while ('0' <= c && c <= '9')
						{
							if (@base == 8 && c >= '8')
							{
								parser.AddWarning("msg.bad.octal.literal", c == '8' ? "8" : "9");
								@base = 10;
							}
							AddToString(c);
							c = GetChar();
						}
					}
					bool isInteger = true;
					if (@base == 10 && (c == '.' || c == 'e' || c == 'E'))
					{
						isInteger = false;
						if (c == '.')
						{
							do
							{
								AddToString(c);
								c = GetChar();
							}
							while (IsDigit(c));
						}
						if (c == 'e' || c == 'E')
						{
							AddToString(c);
							c = GetChar();
							if (c == '+' || c == '-')
							{
								AddToString(c);
								c = GetChar();
							}
							if (!IsDigit(c))
							{
								parser.AddError("msg.missing.exponent");
								return Token.ERROR;
							}
							do
							{
								AddToString(c);
								c = GetChar();
							}
							while (IsDigit(c));
						}
					}
					UngetChar(c);
					string numString = GetStringFromBuffer();
					this.@string = numString;
					double dval;
					if (@base == 10 && !isInteger)
					{
						try
						{
							// Use Java conversion to number from string...
							dval = System.Double.Parse(numString, CultureInfo.InvariantCulture);
						}
						catch (FormatException)
						{
							parser.AddError("msg.caught.nfe");
							return Token.ERROR;
						}
						catch (OverflowException)
						{
							//Probably we have Infinity here.
							dval = numString.StartsWith("-") ? double.NegativeInfinity : double.PositiveInfinity;
						}
					}
					else
					{
						dval = ScriptRuntime.StringToNumber(numString, 0, @base);
					}
					this.number = dval;
					return Token.NUMBER;
				}
				// is it a string?
				if (c == '"' || c == '\'')
				{
					// We attempt to accumulate a string the fast way, by
					// building it directly out of the reader.  But if there
					// are any escaped characters in the string, we revert to
					// building it out of a StringBuffer.
					quoteChar = c;
					stringBufferTop = 0;
					c = GetChar(false);
					while (c != quoteChar)
					{
						if (c == '\n' || c == EOF_CHAR)
						{
							UngetChar(c);
							tokenEnd = cursor;
							parser.AddError("msg.unterminated.string.lit");
							return Token.ERROR;
						}
						if (c == '\\')
						{
							// We've hit an escaped character
							int escapeVal;
							c = GetChar();
							switch (c)
							{
								case 'b':
								{
									c = '\b';
									break;
								}

								case 'f':
								{
									c = '\f';
									break;
								}

								case 'n':
								{
									c = '\n';
									break;
								}

								case 'r':
								{
									c = '\r';
									break;
								}

								case 't':
								{
									c = '\t';
									break;
								}

								case 'v':
								{
									// \v a late addition to the ECMA spec,
									// it is not in Java, so use 0xb
									c = unchecked((int)(0xb));
									break;
								}

								case 'u':
								{
									// Get 4 hex digits; if the u escape is not
									// followed by 4 hex digits, use 'u' + the
									// literal character sequence that follows.
									int escapeStart = stringBufferTop;
									AddToString('u');
									escapeVal = 0;
									for (int i = 0; i != 4; ++i)
									{
										c = GetChar();
										escapeVal = Kit.XDigitToInt(c, escapeVal);
										if (escapeVal < 0)
										{
											goto strLoop_continue;
										}
										AddToString(c);
									}
									// prepare for replace of stored 'u' sequence
									// by escape value
									stringBufferTop = escapeStart;
									c = escapeVal;
									break;
								}

								case 'x':
								{
									// Get 2 hex digits, defaulting to 'x'+literal
									// sequence, as above.
									c = GetChar();
									escapeVal = Kit.XDigitToInt(c, 0);
									if (escapeVal < 0)
									{
										AddToString('x');
										goto strLoop_continue;
									}
									else
									{
										int c1 = c;
										c = GetChar();
										escapeVal = Kit.XDigitToInt(c, escapeVal);
										if (escapeVal < 0)
										{
											AddToString('x');
											AddToString(c1);
											goto strLoop_continue;
										}
										else
										{
											// got 2 hex digits
											c = escapeVal;
										}
									}
									break;
								}

								case '\n':
								{
									// Remove line terminator after escape to follow
									// SpiderMonkey and C/C++
									c = GetChar();
									goto strLoop_continue;
								}

								default:
								{
									if ('0' <= c && c < '8')
									{
										int val = c - '0';
										c = GetChar();
										if ('0' <= c && c < '8')
										{
											val = 8 * val + c - '0';
											c = GetChar();
											if ('0' <= c && c < '8' && val <= 0x1f)
											{
												// c is 3rd char of octal sequence only
												// if the resulting val <= 0377
												val = 8 * val + c - '0';
												c = GetChar();
											}
										}
										UngetChar(c);
										c = val;
									}
									break;
								}
							}
						}
						AddToString(c);
						c = GetChar(false);
strLoop_continue: ;
					}
strLoop_break: ;
					string str = GetStringFromBuffer();
					this.@string = (string)allStrings.Intern(str);
					return Token.STRING;
				}
				switch (c)
				{
					case ';':
					{
						return Token.SEMI;
					}

					case '[':
					{
						return Token.LB;
					}

					case ']':
					{
						return Token.RB;
					}

					case '{':
					{
						return Token.LC;
					}

					case '}':
					{
						return Token.RC;
					}

					case '(':
					{
						return Token.LP;
					}

					case ')':
					{
						return Token.RP;
					}

					case ',':
					{
						return Token.COMMA;
					}

					case '?':
					{
						return Token.HOOK;
					}

					case ':':
					{
						if (MatchChar(':'))
						{
							return Token.COLONCOLON;
						}
						else
						{
							return Token.COLON;
						}
						goto case '.';
					}

					case '.':
					{
						if (MatchChar('.'))
						{
							return Token.DOTDOT;
						}
						else
						{
							if (MatchChar('('))
							{
								return Token.DOTQUERY;
							}
							else
							{
								return Token.DOT;
							}
						}
						goto case '|';
					}

					case '|':
					{
						if (MatchChar('|'))
						{
							return Token.OR;
						}
						else
						{
							if (MatchChar('='))
							{
								return Token.ASSIGN_BITOR;
							}
							else
							{
								return Token.BITOR;
							}
						}
						goto case '^';
					}

					case '^':
					{
						if (MatchChar('='))
						{
							return Token.ASSIGN_BITXOR;
						}
						else
						{
							return Token.BITXOR;
						}
						goto case '&';
					}

					case '&':
					{
						if (MatchChar('&'))
						{
							return Token.AND;
						}
						else
						{
							if (MatchChar('='))
							{
								return Token.ASSIGN_BITAND;
							}
							else
							{
								return Token.BITAND;
							}
						}
						goto case '=';
					}

					case '=':
					{
						if (MatchChar('='))
						{
							if (MatchChar('='))
							{
								return Token.SHEQ;
							}
							else
							{
								return Token.EQ;
							}
						}
						else
						{
							return Token.ASSIGN;
						}
						goto case '!';
					}

					case '!':
					{
						if (MatchChar('='))
						{
							if (MatchChar('='))
							{
								return Token.SHNE;
							}
							else
							{
								return Token.NE;
							}
						}
						else
						{
							return Token.NOT;
						}
						goto case '<';
					}

					case '<':
					{
						if (MatchChar('!'))
						{
							if (MatchChar('-'))
							{
								if (MatchChar('-'))
								{
									tokenBeg = cursor - 4;
									SkipLine();
									commentType = Token.CommentType.HTML;
									return Token.COMMENT;
								}
								UngetCharIgnoreLineEnd('-');
							}
							UngetCharIgnoreLineEnd('!');
						}
						if (MatchChar('<'))
						{
							if (MatchChar('='))
							{
								return Token.ASSIGN_LSH;
							}
							else
							{
								return Token.LSH;
							}
						}
						else
						{
							if (MatchChar('='))
							{
								return Token.LE;
							}
							else
							{
								return Token.LT;
							}
						}
						goto case '>';
					}

					case '>':
					{
						if (MatchChar('>'))
						{
							if (MatchChar('>'))
							{
								if (MatchChar('='))
								{
									return Token.ASSIGN_URSH;
								}
								else
								{
									return Token.URSH;
								}
							}
							else
							{
								if (MatchChar('='))
								{
									return Token.ASSIGN_RSH;
								}
								else
								{
									return Token.RSH;
								}
							}
						}
						else
						{
							if (MatchChar('='))
							{
								return Token.GE;
							}
							else
							{
								return Token.GT;
							}
						}
						goto case '*';
					}

					case '*':
					{
						if (MatchChar('='))
						{
							return Token.ASSIGN_MUL;
						}
						else
						{
							return Token.MUL;
						}
						goto case '/';
					}

					case '/':
					{
						MarkCommentStart();
						// is it a // comment?
						if (MatchChar('/'))
						{
							tokenBeg = cursor - 2;
							SkipLine();
							commentType = Token.CommentType.LINE;
							return Token.COMMENT;
						}
						// is it a /* or /** comment?
						if (MatchChar('*'))
						{
							bool lookForSlash = false;
							tokenBeg = cursor - 2;
							if (MatchChar('*'))
							{
								lookForSlash = true;
								commentType = Token.CommentType.JSDOC;
							}
							else
							{
								commentType = Token.CommentType.BLOCK_COMMENT;
							}
							for (; ; )
							{
								c = GetChar();
								if (c == EOF_CHAR)
								{
									tokenEnd = cursor - 1;
									parser.AddError("msg.unterminated.comment");
									return Token.COMMENT;
								}
								else
								{
									if (c == '*')
									{
										lookForSlash = true;
									}
									else
									{
										if (c == '/')
										{
											if (lookForSlash)
											{
												tokenEnd = cursor;
												return Token.COMMENT;
											}
										}
										else
										{
											lookForSlash = false;
											tokenEnd = cursor;
										}
									}
								}
							}
						}
						if (MatchChar('='))
						{
							return Token.ASSIGN_DIV;
						}
						else
						{
							return Token.DIV;
						}
						goto case '%';
					}

					case '%':
					{
						if (MatchChar('='))
						{
							return Token.ASSIGN_MOD;
						}
						else
						{
							return Token.MOD;
						}
						goto case '~';
					}

					case '~':
					{
						return Token.BITNOT;
					}

					case '+':
					{
						if (MatchChar('='))
						{
							return Token.ASSIGN_ADD;
						}
						else
						{
							if (MatchChar('+'))
							{
								return Token.INC;
							}
							else
							{
								return Token.ADD;
							}
						}
						goto case '-';
					}

					case '-':
					{
						if (MatchChar('='))
						{
							c = Token.ASSIGN_SUB;
						}
						else
						{
							if (MatchChar('-'))
							{
								if (!dirtyLine)
								{
									// treat HTML end-comment after possible whitespace
									// after line start as comment-until-eol
									if (MatchChar('>'))
									{
										MarkCommentStart("--");
										SkipLine();
										commentType = Token.CommentType.HTML;
										return Token.COMMENT;
									}
								}
								c = Token.DEC;
							}
							else
							{
								c = Token.SUB;
							}
						}
						dirtyLine = true;
						return c;
					}

					default:
					{
						parser.AddError("msg.illegal.character");
						return Token.ERROR;
					}
				}
retry_continue: ;
			}
retry_break: ;
		}

		private static bool IsAlpha(int c)
		{
			// Use 'Z' < 'a'
			if (c <= 'Z')
			{
				return 'A' <= c;
			}
			else
			{
				return 'a' <= c && c <= 'z';
			}
		}

		internal static bool IsDigit(int c)
		{
			return '0' <= c && c <= '9';
		}

		internal static bool IsJSSpace(int c)
		{
			if (c <= 127)
			{
				return c == unchecked((int)(0x20)) || c == unchecked((int)(0x9)) || c == unchecked((int)(0xC)) || c == unchecked((int)(0xB));
			}
			else
			{
				return c == unchecked((int)(0xA0)) || c == BYTE_ORDER_MARK || char.GetUnicodeCategory((char)c) == UnicodeCategory.SpaceSeparator;
			}
		}

		private static bool IsJSFormatChar(int c)
		{
			return c > 127 && char.GetUnicodeCategory((char)c) == UnicodeCategory.Format;
		}

		/// <summary>Parser calls the method when it gets / or /= in literal context.</summary>
		/// <remarks>Parser calls the method when it gets / or /= in literal context.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void ReadRegExp(int startToken)
		{
			int start = tokenBeg;
			stringBufferTop = 0;
			if (startToken == Token.ASSIGN_DIV)
			{
				// Miss-scanned /=
				AddToString('=');
			}
			else
			{
				if (startToken != Token.DIV)
				{
					Kit.CodeBug();
				}
			}
			bool inCharSet = false;
			// true if inside a '['..']' pair
			int c;
			while ((c = GetChar()) != '/' || inCharSet)
			{
				if (c == '\n' || c == EOF_CHAR)
				{
					UngetChar(c);
					tokenEnd = cursor - 1;
					this.@string = new string(stringBuffer, 0, stringBufferTop);
					parser.ReportError("msg.unterminated.re.lit");
					return;
				}
				if (c == '\\')
				{
					AddToString(c);
					c = GetChar();
				}
				else
				{
					if (c == '[')
					{
						inCharSet = true;
					}
					else
					{
						if (c == ']')
						{
							inCharSet = false;
						}
					}
				}
				AddToString(c);
			}
			int reEnd = stringBufferTop;
			while (true)
			{
				if (MatchChar('g'))
				{
					AddToString('g');
				}
				else
				{
					if (MatchChar('i'))
					{
						AddToString('i');
					}
					else
					{
						if (MatchChar('m'))
						{
							AddToString('m');
						}
						else
						{
							if (MatchChar('y'))
							{
								// FireFox 3
								AddToString('y');
							}
							else
							{
								break;
							}
						}
					}
				}
			}
			tokenEnd = start + stringBufferTop + 2;
			// include slashes
			if (IsAlpha(PeekChar()))
			{
				parser.ReportError("msg.invalid.re.flag");
			}
			this.@string = new string(stringBuffer, 0, reEnd);
			this.regExpFlags = new string(stringBuffer, reEnd, stringBufferTop - reEnd);
		}

		internal virtual string ReadAndClearRegExpFlags()
		{
			string flags = this.regExpFlags;
			this.regExpFlags = null;
			return flags;
		}

		internal virtual bool IsXMLAttribute()
		{
			return xmlIsAttribute;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual int GetFirstXMLToken()
		{
			xmlOpenTagsCount = 0;
			xmlIsAttribute = false;
			xmlIsTagContent = false;
			if (!CanUngetChar())
			{
				return Token.ERROR;
			}
			UngetChar('<');
			return GetNextXMLToken();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual int GetNextXMLToken()
		{
			tokenBeg = cursor;
			stringBufferTop = 0;
			// remember the XML
			for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
			{
				if (xmlIsTagContent)
				{
					switch (c)
					{
						case '>':
						{
							AddToString(c);
							xmlIsTagContent = false;
							xmlIsAttribute = false;
							break;
						}

						case '/':
						{
							AddToString(c);
							if (PeekChar() == '>')
							{
								c = GetChar();
								AddToString(c);
								xmlIsTagContent = false;
								xmlOpenTagsCount--;
							}
							break;
						}

						case '{':
						{
							UngetChar(c);
							this.@string = GetStringFromBuffer();
							return Token.XML;
						}

						case '\'':
						case '"':
						{
							AddToString(c);
							if (!ReadQuotedString(c))
							{
								return Token.ERROR;
							}
							break;
						}

						case '=':
						{
							AddToString(c);
							xmlIsAttribute = true;
							break;
						}

						case ' ':
						case '\t':
						case '\r':
						case '\n':
						{
							AddToString(c);
							break;
						}

						default:
						{
							AddToString(c);
							xmlIsAttribute = false;
							break;
						}
					}
					if (!xmlIsTagContent && xmlOpenTagsCount == 0)
					{
						this.@string = GetStringFromBuffer();
						return Token.XMLEND;
					}
				}
				else
				{
					switch (c)
					{
						case '<':
						{
							AddToString(c);
							c = PeekChar();
							switch (c)
							{
								case '!':
								{
									c = GetChar();
									// Skip !
									AddToString(c);
									c = PeekChar();
									switch (c)
									{
										case '-':
										{
											c = GetChar();
											// Skip -
											AddToString(c);
											c = GetChar();
											if (c == '-')
											{
												AddToString(c);
												if (!ReadXmlComment())
												{
													return Token.ERROR;
												}
											}
											else
											{
												// throw away the string in progress
												stringBufferTop = 0;
												this.@string = null;
												parser.AddError("msg.XML.bad.form");
												return Token.ERROR;
											}
											break;
										}

										case '[':
										{
											c = GetChar();
											// Skip [
											AddToString(c);
											if (GetChar() == 'C' && GetChar() == 'D' && GetChar() == 'A' && GetChar() == 'T' && GetChar() == 'A' && GetChar() == '[')
											{
												AddToString('C');
												AddToString('D');
												AddToString('A');
												AddToString('T');
												AddToString('A');
												AddToString('[');
												if (!ReadCDATA())
												{
													return Token.ERROR;
												}
											}
											else
											{
												// throw away the string in progress
												stringBufferTop = 0;
												this.@string = null;
												parser.AddError("msg.XML.bad.form");
												return Token.ERROR;
											}
											break;
										}

										default:
										{
											if (!ReadEntity())
											{
												return Token.ERROR;
											}
											break;
										}
									}
									break;
								}

								case '?':
								{
									c = GetChar();
									// Skip ?
									AddToString(c);
									if (!ReadPI())
									{
										return Token.ERROR;
									}
									break;
								}

								case '/':
								{
									// End tag
									c = GetChar();
									// Skip /
									AddToString(c);
									if (xmlOpenTagsCount == 0)
									{
										// throw away the string in progress
										stringBufferTop = 0;
										this.@string = null;
										parser.AddError("msg.XML.bad.form");
										return Token.ERROR;
									}
									xmlIsTagContent = true;
									xmlOpenTagsCount--;
									break;
								}

								default:
								{
									// Start tag
									xmlIsTagContent = true;
									xmlOpenTagsCount++;
									break;
								}
							}
							break;
						}

						case '{':
						{
							UngetChar(c);
							this.@string = GetStringFromBuffer();
							return Token.XML;
						}

						default:
						{
							AddToString(c);
							break;
						}
					}
				}
			}
			tokenEnd = cursor;
			stringBufferTop = 0;
			// throw away the string in progress
			this.@string = null;
			parser.AddError("msg.XML.bad.form");
			return Token.ERROR;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ReadQuotedString(int quote)
		{
			for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
			{
				AddToString(c);
				if (c == quote)
				{
					return true;
				}
			}
			stringBufferTop = 0;
			// throw away the string in progress
			this.@string = null;
			parser.AddError("msg.XML.bad.form");
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ReadXmlComment()
		{
			for (int c = GetChar(); c != EOF_CHAR; )
			{
				AddToString(c);
				if (c == '-' && PeekChar() == '-')
				{
					c = GetChar();
					AddToString(c);
					if (PeekChar() == '>')
					{
						c = GetChar();
						// Skip >
						AddToString(c);
						return true;
					}
					else
					{
						continue;
					}
				}
				c = GetChar();
			}
			stringBufferTop = 0;
			// throw away the string in progress
			this.@string = null;
			parser.AddError("msg.XML.bad.form");
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ReadCDATA()
		{
			for (int c = GetChar(); c != EOF_CHAR; )
			{
				AddToString(c);
				if (c == ']' && PeekChar() == ']')
				{
					c = GetChar();
					AddToString(c);
					if (PeekChar() == '>')
					{
						c = GetChar();
						// Skip >
						AddToString(c);
						return true;
					}
					else
					{
						continue;
					}
				}
				c = GetChar();
			}
			stringBufferTop = 0;
			// throw away the string in progress
			this.@string = null;
			parser.AddError("msg.XML.bad.form");
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ReadEntity()
		{
			int declTags = 1;
			for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
			{
				AddToString(c);
				switch (c)
				{
					case '<':
					{
						declTags++;
						break;
					}

					case '>':
					{
						declTags--;
						if (declTags == 0)
						{
							return true;
						}
						break;
					}
				}
			}
			stringBufferTop = 0;
			// throw away the string in progress
			this.@string = null;
			parser.AddError("msg.XML.bad.form");
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ReadPI()
		{
			for (int c = GetChar(); c != EOF_CHAR; c = GetChar())
			{
				AddToString(c);
				if (c == '?' && PeekChar() == '>')
				{
					c = GetChar();
					// Skip >
					AddToString(c);
					return true;
				}
			}
			stringBufferTop = 0;
			// throw away the string in progress
			this.@string = null;
			parser.AddError("msg.XML.bad.form");
			return false;
		}

		private string GetStringFromBuffer()
		{
			tokenEnd = cursor;
			return new string(stringBuffer, 0, stringBufferTop);
		}

		private void AddToString(int c)
		{
			int N = stringBufferTop;
			if (N == stringBuffer.Length)
			{
				char[] tmp = new char[stringBuffer.Length * 2];
				System.Array.Copy(stringBuffer, 0, tmp, 0, N);
				stringBuffer = tmp;
			}
			stringBuffer[N] = (char)c;
			stringBufferTop = N + 1;
		}

		private bool CanUngetChar()
		{
			return ungetCursor == 0 || ungetBuffer[ungetCursor - 1] != '\n';
		}

		private void UngetChar(int c)
		{
			// can not unread past across line boundary
			if (ungetCursor != 0 && ungetBuffer[ungetCursor - 1] == '\n')
			{
				Kit.CodeBug();
			}
			ungetBuffer[ungetCursor++] = c;
			cursor--;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool MatchChar(int test)
		{
			int c = GetCharIgnoreLineEnd();
			if (c == test)
			{
				tokenEnd = cursor;
				return true;
			}
			else
			{
				UngetCharIgnoreLineEnd(c);
				return false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int PeekChar()
		{
			int c = GetChar();
			UngetChar(c);
			return c;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int GetChar()
		{
			return GetChar(true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int GetChar(bool skipFormattingChars)
		{
			if (ungetCursor != 0)
			{
				cursor++;
				return ungetBuffer[--ungetCursor];
			}
			for (; ; )
			{
				int c;
				if (sourceString != null)
				{
					if (sourceCursor == sourceEnd)
					{
						hitEOF = true;
						return EOF_CHAR;
					}
					cursor++;
					c = sourceString[sourceCursor++];
				}
				else
				{
					if (sourceCursor == sourceEnd)
					{
						if (!FillSourceBuffer())
						{
							hitEOF = true;
							return EOF_CHAR;
						}
					}
					cursor++;
					c = sourceBuffer[sourceCursor++];
				}
				if (lineEndChar >= 0)
				{
					if (lineEndChar == '\r' && c == '\n')
					{
						lineEndChar = '\n';
						continue;
					}
					lineEndChar = -1;
					lineStart = sourceCursor - 1;
					lineno++;
				}
				if (c <= 127)
				{
					if (c == '\n' || c == '\r')
					{
						lineEndChar = c;
						c = '\n';
					}
				}
				else
				{
					if (c == BYTE_ORDER_MARK)
					{
						return c;
					}
					// BOM is considered whitespace
					if (skipFormattingChars && IsJSFormatChar(c))
					{
						continue;
					}
					if (ScriptRuntime.IsJSLineTerminator(c))
					{
						lineEndChar = c;
						c = '\n';
					}
				}
				return c;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int GetCharIgnoreLineEnd()
		{
			if (ungetCursor != 0)
			{
				cursor++;
				return ungetBuffer[--ungetCursor];
			}
			for (; ; )
			{
				int c;
				if (sourceString != null)
				{
					if (sourceCursor == sourceEnd)
					{
						hitEOF = true;
						return EOF_CHAR;
					}
					cursor++;
					c = sourceString[sourceCursor++];
				}
				else
				{
					if (sourceCursor == sourceEnd)
					{
						if (!FillSourceBuffer())
						{
							hitEOF = true;
							return EOF_CHAR;
						}
					}
					cursor++;
					c = sourceBuffer[sourceCursor++];
				}
				if (c <= 127)
				{
					if (c == '\n' || c == '\r')
					{
						lineEndChar = c;
						c = '\n';
					}
				}
				else
				{
					if (c == BYTE_ORDER_MARK)
					{
						return c;
					}
					// BOM is considered whitespace
					if (IsJSFormatChar(c))
					{
						continue;
					}
					if (ScriptRuntime.IsJSLineTerminator(c))
					{
						lineEndChar = c;
						c = '\n';
					}
				}
				return c;
			}
		}

		private void UngetCharIgnoreLineEnd(int c)
		{
			ungetBuffer[ungetCursor++] = c;
			cursor--;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SkipLine()
		{
			// skip to end of line
			int c;
			while ((c = GetChar()) != EOF_CHAR && c != '\n')
			{
			}
			UngetChar(c);
			tokenEnd = cursor;
		}

		/// <summary>Returns the offset into the current line.</summary>
		/// <remarks>Returns the offset into the current line.</remarks>
		internal int GetOffset()
		{
			int n = sourceCursor - lineStart;
			if (lineEndChar >= 0)
			{
				--n;
			}
			return n;
		}

		internal string GetLine()
		{
			if (sourceString != null)
			{
				// String case
				int lineEnd = sourceCursor;
				if (lineEndChar >= 0)
				{
					--lineEnd;
				}
				else
				{
					for (; lineEnd != sourceEnd; ++lineEnd)
					{
						int c = sourceString[lineEnd];
						if (ScriptRuntime.IsJSLineTerminator(c))
						{
							break;
						}
					}
				}
				return sourceString.Substring(lineStart, lineEnd - lineStart);
			}
			else
			{
				// Reader case
				int lineLength = sourceCursor - lineStart;
				if (lineEndChar >= 0)
				{
					--lineLength;
				}
				else
				{
					// Read until the end of line
					for (; ; ++lineLength)
					{
						int i = lineStart + lineLength;
						if (i == sourceEnd)
						{
							try
							{
								if (!FillSourceBuffer())
								{
									break;
								}
							}
							catch (IOException)
							{
								// ignore it, we're already displaying an error...
								break;
							}
							// i recalculuation as fillSourceBuffer can move saved
							// line buffer and change lineStart
							i = lineStart + lineLength;
						}
						int c = sourceBuffer[i];
						if (ScriptRuntime.IsJSLineTerminator(c))
						{
							break;
						}
					}
				}
				return new string(sourceBuffer, lineStart, lineLength);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool FillSourceBuffer()
		{
			if (sourceString != null)
			{
				Kit.CodeBug();
			}
			if (sourceEnd == sourceBuffer.Length)
			{
				if (lineStart != 0 && !IsMarkingComment())
				{
					System.Array.Copy(sourceBuffer, lineStart, sourceBuffer, 0, sourceEnd - lineStart);
					sourceEnd -= lineStart;
					sourceCursor -= lineStart;
					lineStart = 0;
				}
				else
				{
					char[] tmp = new char[sourceBuffer.Length * 2];
					System.Array.Copy(sourceBuffer, 0, tmp, 0, sourceEnd);
					sourceBuffer = tmp;
				}
			}
			int n = sourceReader.Read(sourceBuffer, sourceEnd, sourceBuffer.Length - sourceEnd);
			if (n < 0)
			{
				return false;
			}
			sourceEnd += n;
			return true;
		}

		/// <summary>Return the current position of the scanner cursor.</summary>
		/// <remarks>Return the current position of the scanner cursor.</remarks>
		public virtual int GetCursor()
		{
			return cursor;
		}

		/// <summary>Return the absolute source offset of the last scanned token.</summary>
		/// <remarks>Return the absolute source offset of the last scanned token.</remarks>
		public virtual int GetTokenBeg()
		{
			return tokenBeg;
		}

		/// <summary>Return the absolute source end-offset of the last scanned token.</summary>
		/// <remarks>Return the absolute source end-offset of the last scanned token.</remarks>
		public virtual int GetTokenEnd()
		{
			return tokenEnd;
		}

		/// <summary>Return tokenEnd - tokenBeg</summary>
		public virtual int GetTokenLength()
		{
			return tokenEnd - tokenBeg;
		}

		/// <summary>Return the type of the last scanned comment.</summary>
		/// <remarks>Return the type of the last scanned comment.</remarks>
		/// <returns>type of last scanned comment, or 0 if none have been scanned.</returns>
		public virtual Token.CommentType GetCommentType()
		{
			return commentType;
		}

		private void MarkCommentStart()
		{
			MarkCommentStart(string.Empty);
		}

		private void MarkCommentStart(string prefix)
		{
			if (parser.compilerEnv.IsRecordingComments() && sourceReader != null)
			{
				commentPrefix = prefix;
				commentCursor = sourceCursor - 1;
			}
		}

		private bool IsMarkingComment()
		{
			return commentCursor != -1;
		}

		internal string GetAndResetCurrentComment()
		{
			if (sourceString != null)
			{
				if (IsMarkingComment())
				{
					Kit.CodeBug();
				}
				return sourceString.Substring(tokenBeg, tokenEnd - tokenBeg);
			}
			else
			{
				if (!IsMarkingComment())
				{
					Kit.CodeBug();
				}
				StringBuilder comment = new StringBuilder(commentPrefix);
				comment.Append(sourceBuffer, commentCursor, GetTokenLength() - commentPrefix.Length);
				commentCursor = -1;
				return comment.ToString();
			}
		}

		private string ConvertLastCharToHex(string str)
		{
			int lastIndex = str.Length - 1;
			StringBuilder buf = new StringBuilder(str.Substring(0, lastIndex));
			buf.Append("\\u");
			string hexCode = Sharpen.Extensions.ToHexString(str[lastIndex]);
			for (int i = 0; i < 4 - hexCode.Length; ++i)
			{
				buf.Append('0');
			}
			buf.Append(hexCode);
			return buf.ToString();
		}

		private bool dirtyLine;

		internal string regExpFlags;

		private string @string = string.Empty;

		private double number;

		private bool isOctal;

		private int quoteChar;

		private char[] stringBuffer = new char[128];

		private int stringBufferTop;

		private ObjToIntMap allStrings = new ObjToIntMap(50);

		private readonly int[] ungetBuffer = new int[3];

		private int ungetCursor;

		private bool hitEOF = false;

		private int lineStart = 0;

		private int lineEndChar = -1;

		internal int lineno;

		private string sourceString;

		private TextReader sourceReader;

		private char[] sourceBuffer;

		private int sourceEnd;

		internal int sourceCursor;

		internal int cursor;

		internal int tokenBeg;

		internal int tokenEnd;

		internal Token.CommentType commentType;

		private bool xmlIsAttribute;

		private bool xmlIsTagContent;

		private int xmlOpenTagsCount;

		private Parser parser;

		private string commentPrefix = string.Empty;

		private int commentCursor = -1;
		// stuff other than whitespace since start of line
		// Set this to an initial non-null value so that the Parser has
		// something to retrieve even if an error has occurred and no
		// string is found.  Fosters one class of error, but saves lots of
		// code.
		// delimiter for last string literal scanned
		// Room to backtrace from to < on failed match of the last - in <!--
		// sourceCursor is an index into a small buffer that keeps a
		// sliding window of the source stream.
		// cursor is a monotonically increasing index into the original
		// source stream, tracking exactly how far scanning has progressed.
		// Its value is the index of the next character to be scanned.
		// Record start and end positions of last scanned token.
		// Type of last comment scanned.
		// for xml tokenizer
	}
}
