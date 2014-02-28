/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Rhino.Xml;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// This class implements the global native object (function and value
	/// properties only).
	/// </summary>
	/// <remarks>
	/// This class implements the global native object (function and value
	/// properties only).
	/// See ECMA 15.1.[12].
	/// </remarks>
	/// <author>Mike Shaver</author>
	[System.Serializable]
	public class NativeGlobal : IdFunctionCall
	{
		public static void Init(Context cx, Scriptable scope, bool @sealed)
		{
			NativeGlobal obj = new NativeGlobal();
			for (int id = 1; id <= LAST_SCOPE_FUNCTION_ID; ++id)
			{
				string name;
				int arity = 1;
				switch (id)
				{
					case Id_decodeURI:
					{
						name = "decodeURI";
						break;
					}

					case Id_decodeURIComponent:
					{
						name = "decodeURIComponent";
						break;
					}

					case Id_encodeURI:
					{
						name = "encodeURI";
						break;
					}

					case Id_encodeURIComponent:
					{
						name = "encodeURIComponent";
						break;
					}

					case Id_escape:
					{
						name = "escape";
						break;
					}

					case Id_eval:
					{
						name = "eval";
						break;
					}

					case Id_isFinite:
					{
						name = "isFinite";
						break;
					}

					case Id_isNaN:
					{
						name = "isNaN";
						break;
					}

					case Id_isXMLName:
					{
						name = "isXMLName";
						break;
					}

					case Id_parseFloat:
					{
						name = "parseFloat";
						break;
					}

					case Id_parseInt:
					{
						name = "parseInt";
						arity = 2;
						break;
					}

					case Id_unescape:
					{
						name = "unescape";
						break;
					}

					case Id_uneval:
					{
						name = "uneval";
						break;
					}

					default:
					{
						throw Kit.CodeBug();
					}
				}
				IdFunctionObject f = new IdFunctionObject(obj, FTAG, id, name, arity, scope);
				if (@sealed)
				{
					f.SealObject();
				}
				f.ExportAsScopeProperty();
			}
			ScriptableObject.DefineProperty(scope, "NaN", ScriptRuntime.NaN, PropertyAttributes.READONLY | PropertyAttributes.DONTENUM | PropertyAttributes.PERMANENT);
			ScriptableObject.DefineProperty(scope, "Infinity", double.PositiveInfinity, PropertyAttributes.READONLY | PropertyAttributes.DONTENUM | PropertyAttributes.PERMANENT);
			ScriptableObject.DefineProperty(scope, "undefined", Undefined.instance, PropertyAttributes.READONLY | PropertyAttributes.DONTENUM | PropertyAttributes.PERMANENT);
			string[] errorMethods = new string[] { "ConversionError", "EvalError", "RangeError", "ReferenceError", "SyntaxError", "TypeError", "URIError", "InternalError", "JavaException" };
			for (int i = 0; i < errorMethods.Length; i++)
			{
				string name = errorMethods[i];
				ScriptableObject errorProto = (ScriptableObject)ScriptRuntime.NewObject(cx, scope, "Error", ScriptRuntime.emptyArgs);
				errorProto.Put("name", errorProto, name);
				errorProto.Put("message", errorProto, string.Empty);
				IdFunctionObject ctor = new IdFunctionObject(obj, FTAG, Id_new_CommonError, name, 1, scope);
				ctor.MarkAsConstructor(errorProto);
				errorProto.Put("constructor", errorProto, ctor);
				errorProto.SetAttributes("constructor", PropertyAttributes.DONTENUM);
				if (@sealed)
				{
					errorProto.SealObject();
					ctor.SealObject();
				}
				ctor.ExportAsScopeProperty();
			}
		}

		public virtual object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (f.HasTag(FTAG))
			{
				int methodId = f.MethodId();
				switch (methodId)
				{
					case Id_decodeURI:
					case Id_decodeURIComponent:
					{
						string str = ScriptRuntime.ToString(args, 0);
						return Decode(str, methodId == Id_decodeURI);
					}

					case Id_encodeURI:
					case Id_encodeURIComponent:
					{
						string str = ScriptRuntime.ToString(args, 0);
						return Encode(str, methodId == Id_encodeURI);
					}

					case Id_escape:
					{
						return Js_escape(args);
					}

					case Id_eval:
					{
						return Js_eval(cx, scope, args);
					}

					case Id_isFinite:
					{
						bool result;
						if (args.Length < 1)
						{
							result = false;
						}
						else
						{
							double d = ScriptRuntime.ToNumber(args[0]);
							result = (!Double.IsNaN(d) && d != double.PositiveInfinity && d != double.NegativeInfinity);
						}
						return result;
					}

					case Id_isNaN:
					{
						// The global method isNaN, as per ECMA-262 15.1.2.6.
						bool result;
						if (args.Length < 1)
						{
							result = true;
						}
						else
						{
							double d = ScriptRuntime.ToNumber(args[0]);
							result = Double.IsNaN(d);
						}
						return result;
					}

					case Id_isXMLName:
					{
						object name = (args.Length == 0) ? Undefined.instance : args[0];
						XMLLib xmlLib = XMLLib.ExtractFromScope(scope);
						return xmlLib.IsXMLName(cx, name);
					}

					case Id_parseFloat:
					{
						return Js_parseFloat(args);
					}

					case Id_parseInt:
					{
						return Js_parseInt(args);
					}

					case Id_unescape:
					{
						return Js_unescape(args);
					}

					case Id_uneval:
					{
						object value = (args.Length != 0) ? args[0] : Undefined.instance;
						return ScriptRuntime.Uneval(cx, scope, value);
					}

					case Id_new_CommonError:
					{
						// The implementation of all the ECMA error constructors
						// (SyntaxError, TypeError, etc.)
						return NativeError.Make(cx, scope, f, args);
					}
				}
			}
			throw f.Unknown();
		}

		/// <summary>The global method parseInt, as per ECMA-262 15.1.2.2.</summary>
		/// <remarks>The global method parseInt, as per ECMA-262 15.1.2.2.</remarks>
		private object Js_parseInt(object[] args)
		{
			string s = ScriptRuntime.ToString(args, 0);
			int radix = ScriptRuntime.ToInt32(args, 1);
			int len = s.Length;
			if (len == 0)
			{
				return ScriptRuntime.NaN;
			}
			bool negative = false;
			int start = 0;
			char c;
			do
			{
				c = s[start];
				if (!ScriptRuntime.IsStrWhiteSpaceChar(c))
				{
					break;
				}
				start++;
			}
			while (start < len);
			if (c == '+' || (negative = (c == '-')))
			{
				start++;
			}
			int NO_RADIX = -1;
			if (radix == 0)
			{
				radix = NO_RADIX;
			}
			else
			{
				if (radix < 2 || radix > 36)
				{
					return ScriptRuntime.NaN;
				}
				else
				{
					if (radix == 16 && len - start > 1 && s[start] == '0')
					{
						c = s[start + 1];
						if (c == 'x' || c == 'X')
						{
							start += 2;
						}
					}
				}
			}
			if (radix == NO_RADIX)
			{
				radix = 10;
				if (len - start > 1 && s[start] == '0')
				{
					c = s[start + 1];
					if (c == 'x' || c == 'X')
					{
						radix = 16;
						start += 2;
					}
					else
					{
						if ('0' <= c && c <= '9')
						{
							radix = 8;
							start++;
						}
					}
				}
			}
			double d = ScriptRuntime.StringToNumber(s, start, radix);
			return negative ? -d : d;
		}

		/// <summary>The global method parseFloat, as per ECMA-262 15.1.2.3.</summary>
		/// <remarks>The global method parseFloat, as per ECMA-262 15.1.2.3.</remarks>
		/// <param name="args">the arguments to parseFloat, ignoring args[&gt;=1]</param>
		private object Js_parseFloat(object[] args)
		{
			if (args.Length < 1)
			{
				return ScriptRuntime.NaN;
			}
			string s = ScriptRuntime.ToString(args[0]);
			int len = s.Length;
			int start = 0;
			// Scan forward to skip whitespace
			char c;
			for (; ; )
			{
				if (start == len)
				{
					return ScriptRuntime.NaN;
				}
				c = s[start];
				if (!ScriptRuntime.IsStrWhiteSpaceChar(c))
				{
					break;
				}
				++start;
			}
			int i = start;
			if (c == '+' || c == '-')
			{
				++i;
				if (i == len)
				{
					return ScriptRuntime.NaN;
				}
				c = s[i];
			}
			if (c == 'I')
			{
				// check for "Infinity"
				if (i + 8 <= len && s.RegionMatches(i, "Infinity", 0, 8))
				{
					double d;
					if (s[start] == '-')
					{
						d = double.NegativeInfinity;
					}
					else
					{
						d = double.PositiveInfinity;
					}
					return d;
				}
				return ScriptRuntime.NaN;
			}
			// Find the end of the legal bit
			int @decimal = -1;
			int exponent = -1;
			bool exponentValid = false;
			for (; i < len; i++)
			{
				switch (s[i])
				{
					case '.':
					{
						if (@decimal != -1)
						{
							// Only allow a single decimal point.
							break;
						}
						@decimal = i;
						continue;
					}

					case 'e':
					case 'E':
					{
						if (exponent != -1)
						{
							break;
						}
						else
						{
							if (i == len - 1)
							{
								break;
							}
						}
						exponent = i;
						continue;
					}

					case '+':
					case '-':
					{
						// Only allow '+' or '-' after 'e' or 'E'
						if (exponent != i - 1)
						{
							break;
						}
						else
						{
							if (i == len - 1)
							{
								--i;
								break;
							}
						}
						continue;
					}

					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					{
						if (exponent != -1)
						{
							exponentValid = true;
						}
						continue;
					}

					default:
					{
						break;
					}
				}
				break;
			}
			if (exponent != -1 && !exponentValid)
			{
				i = exponent;
			}
			s = s.Substring(start, i - start);
			try
			{
				return s;
			}
			catch (FormatException)
			{
				return ScriptRuntime.NaN;
			}
		}

		/// <summary>The global method escape, as per ECMA-262 15.1.2.4.</summary>
		/// <remarks>
		/// The global method escape, as per ECMA-262 15.1.2.4.
		/// Includes code for the 'mask' argument supported by the C escape
		/// method, which used to be part of the browser imbedding.  Blame
		/// for the strange constant names should be directed there.
		/// </remarks>
		private object Js_escape(object[] args)
		{
			int URL_XALPHAS = 1;
			int URL_XPALPHAS = 2;
			int URL_PATH = 4;
			string s = ScriptRuntime.ToString(args, 0);
			int mask = URL_XALPHAS | URL_XPALPHAS | URL_PATH;
			if (args.Length > 1)
			{
				// the 'mask' argument.  Non-ECMA.
				double d = ScriptRuntime.ToNumber(args[1]);
				if (Double.IsNaN(d) || ((mask = (int)d) != d) || 0 != (mask & ~(URL_XALPHAS | URL_XPALPHAS | URL_PATH)))
				{
					throw Context.ReportRuntimeError0("msg.bad.esc.mask");
				}
			}
			StringBuilder sb = null;
			for (int k = 0, L = s.Length; k != L; ++k)
			{
				int c = s[k];
				if (mask != 0 && ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '@' || c == '*' || c == '_' || c == '-' || c == '.' || (0 != (mask & URL_PATH) && (c == '/' || c == '+'))))
				{
					if (sb != null)
					{
						sb.Append((char)c);
					}
				}
				else
				{
					if (sb == null)
					{
						sb = new StringBuilder(L + 3);
						sb.Append(s);
						sb.Length = k;
					}
					int hexSize;
					if (c < 256)
					{
						if (c == ' ' && mask == URL_XPALPHAS)
						{
							sb.Append('+');
							continue;
						}
						sb.Append('%');
						hexSize = 2;
					}
					else
					{
						sb.Append('%');
						sb.Append('u');
						hexSize = 4;
					}
					// append hexadecimal form of c left-padded with 0
					for (int shift = (hexSize - 1) * 4; shift >= 0; shift -= 4)
					{
						int digit = unchecked((int)(0xf)) & (c >> shift);
						int hc = (digit < 10) ? '0' + digit : 'A' - 10 + digit;
						sb.Append((char)hc);
					}
				}
			}
			return (sb == null) ? s : sb.ToString();
		}

		/// <summary>The global unescape method, as per ECMA-262 15.1.2.5.</summary>
		/// <remarks>The global unescape method, as per ECMA-262 15.1.2.5.</remarks>
		private object Js_unescape(object[] args)
		{
			string s = ScriptRuntime.ToString(args, 0);
			int firstEscapePos = s.IndexOf('%');
			if (firstEscapePos >= 0)
			{
				int L = s.Length;
				char[] buf = s.ToCharArray();
				int destination = firstEscapePos;
				for (int k = firstEscapePos; k != L; )
				{
					char c = buf[k];
					++k;
					if (c == '%' && k != L)
					{
						int end;
						int start;
						if (buf[k] == 'u')
						{
							start = k + 1;
							end = k + 5;
						}
						else
						{
							start = k;
							end = k + 2;
						}
						if (end <= L)
						{
							int x = 0;
							for (int i = start; i != end; ++i)
							{
								x = Kit.XDigitToInt(buf[i], x);
							}
							if (x >= 0)
							{
								c = (char)x;
								k = end;
							}
						}
					}
					buf[destination] = c;
					++destination;
				}
				s = new string(buf, 0, destination);
			}
			return s;
		}

		/// <summary>This is an indirect call to eval, and thus uses the global environment.</summary>
		/// <remarks>
		/// This is an indirect call to eval, and thus uses the global environment.
		/// Direct calls are executed via ScriptRuntime.callSpecial().
		/// </remarks>
		private object Js_eval(Context cx, Scriptable scope, object[] args)
		{
			Scriptable global = ScriptableObject.GetTopLevelScope(scope);
			return ScriptRuntime.EvalSpecial(cx, global, global, args, "eval code", 1);
		}

		internal static bool IsEvalFunction(object functionObj)
		{
			var function = functionObj as IdFunctionObject;
			if (function != null)
			{
				if (function.HasTag(FTAG) && function.MethodId() == Id_eval)
				{
					return true;
				}
			}
			return false;
		}

		private static string Encode(string str, bool fullUri)
		{
			byte[] utf8buf = null;
			StringBuilder sb = null;
			for (int k = 0, length = str.Length; k != length; ++k)
			{
				char C = str[k];
				if (EncodeUnescaped(C, fullUri))
				{
					if (sb != null)
					{
						sb.Append(C);
					}
				}
				else
				{
					if (sb == null)
					{
						sb = new StringBuilder(length + 3);
						sb.Append(str);
						sb.Length = k;
						utf8buf = new byte[6];
					}
					if (unchecked((int)(0xDC00)) <= C && C <= unchecked((int)(0xDFFF)))
					{
						throw UriError();
					}
					int V;
					if (C < unchecked((int)(0xD800)) || unchecked((int)(0xDBFF)) < C)
					{
						V = C;
					}
					else
					{
						k++;
						if (k == length)
						{
							throw UriError();
						}
						char C2 = str[k];
						if (!(unchecked((int)(0xDC00)) <= C2 && C2 <= unchecked((int)(0xDFFF))))
						{
							throw UriError();
						}
						V = ((C - unchecked((int)(0xD800))) << 10) + (C2 - unchecked((int)(0xDC00))) + unchecked((int)(0x10000));
					}
					int L = OneUcs4ToUtf8Char(utf8buf, V);
					for (int j = 0; j < L; j++)
					{
						int d = unchecked((int)(0xff)) & utf8buf[j];
						sb.Append('%');
						sb.Append(ToHexChar((int)(((uint)d) >> 4)));
						sb.Append(ToHexChar(d & unchecked((int)(0xf))));
					}
				}
			}
			return (sb == null) ? str : sb.ToString();
		}

		private static char ToHexChar(int i)
		{
			if (i >> 4 != 0)
			{
				Kit.CodeBug();
			}
			return (char)((i < 10) ? i + '0' : i - 10 + 'A');
		}

		private static int UnHex(char c)
		{
			if ('A' <= c && c <= 'F')
			{
				return c - 'A' + 10;
			}
			else
			{
				if ('a' <= c && c <= 'f')
				{
					return c - 'a' + 10;
				}
				else
				{
					if ('0' <= c && c <= '9')
					{
						return c - '0';
					}
					else
					{
						return -1;
					}
				}
			}
		}

		private static int UnHex(char c1, char c2)
		{
			int i1 = UnHex(c1);
			int i2 = UnHex(c2);
			if (i1 >= 0 && i2 >= 0)
			{
				return (i1 << 4) | i2;
			}
			return -1;
		}

		private static string Decode(string str, bool fullUri)
		{
			char[] buf = null;
			int bufTop = 0;
			for (int k = 0, length = str.Length; k != length; )
			{
				char C = str[k];
				if (C != '%')
				{
					if (buf != null)
					{
						buf[bufTop++] = C;
					}
					++k;
				}
				else
				{
					if (buf == null)
					{
						// decode always compress so result can not be bigger then
						// str.length()
						buf = new char[length];
						str.CopyTo(0, buf, 0, k);
						bufTop = k;
					}
					int start = k;
					if (k + 3 > length)
					{
						throw UriError();
					}
					int B = UnHex(str[k + 1], str[k + 2]);
					if (B < 0)
					{
						throw UriError();
					}
					k += 3;
					if ((B & unchecked((int)(0x80))) == 0)
					{
						C = (char)B;
					}
					else
					{
						// Decode UTF-8 sequence into ucs4Char and encode it into
						// UTF-16
						int utf8Tail;
						int ucs4Char;
						int minUcs4Char;
						if ((B & unchecked((int)(0xC0))) == unchecked((int)(0x80)))
						{
							// First  UTF-8 should be ouside 0x80..0xBF
							throw UriError();
						}
						else
						{
							if ((B & unchecked((int)(0x20))) == 0)
							{
								utf8Tail = 1;
								ucs4Char = B & unchecked((int)(0x1F));
								minUcs4Char = unchecked((int)(0x80));
							}
							else
							{
								if ((B & unchecked((int)(0x10))) == 0)
								{
									utf8Tail = 2;
									ucs4Char = B & unchecked((int)(0x0F));
									minUcs4Char = unchecked((int)(0x800));
								}
								else
								{
									if ((B & unchecked((int)(0x08))) == 0)
									{
										utf8Tail = 3;
										ucs4Char = B & unchecked((int)(0x07));
										minUcs4Char = unchecked((int)(0x10000));
									}
									else
									{
										if ((B & unchecked((int)(0x04))) == 0)
										{
											utf8Tail = 4;
											ucs4Char = B & unchecked((int)(0x03));
											minUcs4Char = unchecked((int)(0x200000));
										}
										else
										{
											if ((B & unchecked((int)(0x02))) == 0)
											{
												utf8Tail = 5;
												ucs4Char = B & unchecked((int)(0x01));
												minUcs4Char = unchecked((int)(0x4000000));
											}
											else
											{
												// First UTF-8 can not be 0xFF or 0xFE
												throw UriError();
											}
										}
									}
								}
							}
						}
						if (k + 3 * utf8Tail > length)
						{
							throw UriError();
						}
						for (int j = 0; j != utf8Tail; j++)
						{
							if (str[k] != '%')
							{
								throw UriError();
							}
							B = UnHex(str[k + 1], str[k + 2]);
							if (B < 0 || (B & unchecked((int)(0xC0))) != unchecked((int)(0x80)))
							{
								throw UriError();
							}
							ucs4Char = (ucs4Char << 6) | (B & unchecked((int)(0x3F)));
							k += 3;
						}
						// Check for overlongs and other should-not-present codes
						if (ucs4Char < minUcs4Char || (ucs4Char >= unchecked((int)(0xD800)) && ucs4Char <= unchecked((int)(0xDFFF))))
						{
							ucs4Char = INVALID_UTF8;
						}
						else
						{
							if (ucs4Char == unchecked((int)(0xFFFE)) || ucs4Char == unchecked((int)(0xFFFF)))
							{
								ucs4Char = unchecked((int)(0xFFFD));
							}
						}
						if (ucs4Char >= unchecked((int)(0x10000)))
						{
							ucs4Char -= unchecked((int)(0x10000));
							if (ucs4Char > unchecked((int)(0xFFFFF)))
							{
								throw UriError();
							}
							char H = (char)(((int)(((uint)ucs4Char) >> 10)) + unchecked((int)(0xD800)));
							C = (char)((ucs4Char & unchecked((int)(0x3FF))) + unchecked((int)(0xDC00)));
							buf[bufTop++] = H;
						}
						else
						{
							C = (char)ucs4Char;
						}
					}
					if (fullUri && URI_DECODE_RESERVED.IndexOf(C) >= 0)
					{
						for (int x = start; x != k; x++)
						{
							buf[bufTop++] = str[x];
						}
					}
					else
					{
						buf[bufTop++] = C;
					}
				}
			}
			return (buf == null) ? str : new string(buf, 0, bufTop);
		}

		private static bool EncodeUnescaped(char c, bool fullUri)
		{
			if (('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z') || ('0' <= c && c <= '9'))
			{
				return true;
			}
			if ("-_.!~*'()".IndexOf(c) >= 0)
			{
				return true;
			}
			if (fullUri)
			{
				return URI_DECODE_RESERVED.IndexOf(c) >= 0;
			}
			return false;
		}

		private static EcmaError UriError()
		{
			return ScriptRuntime.ConstructError("URIError", ScriptRuntime.GetMessage0("msg.bad.uri"));
		}

		private const string URI_DECODE_RESERVED = ";/?:@&=+$,#";

		private const int INVALID_UTF8 = int.MaxValue;

		private static int OneUcs4ToUtf8Char(byte[] utf8Buffer, int ucs4Char)
		{
			int utf8Length = 1;
			//JS_ASSERT(ucs4Char <= 0x7FFFFFFF);
			if ((ucs4Char & ~unchecked((int)(0x7F))) == 0)
			{
				utf8Buffer[0] = unchecked((byte)ucs4Char);
			}
			else
			{
				int i;
				int a = (int)(((uint)ucs4Char) >> 11);
				utf8Length = 2;
				while (a != 0)
				{
					a = (int)(((uint)a) >> 5);
					utf8Length++;
				}
				i = utf8Length;
				while (--i > 0)
				{
					utf8Buffer[i] = unchecked((byte)((ucs4Char & unchecked((int)(0x3F))) | unchecked((int)(0x80))));
					ucs4Char = (int)(((uint)ucs4Char) >> 6);
				}
				utf8Buffer[0] = unchecked((byte)(unchecked((int)(0x100)) - (1 << (8 - utf8Length)) + ucs4Char));
			}
			return utf8Length;
		}

		private static readonly object FTAG = "Global";

		private const int Id_decodeURI = 1;

		private const int Id_decodeURIComponent = 2;

		private const int Id_encodeURI = 3;

		private const int Id_encodeURIComponent = 4;

		private const int Id_escape = 5;

		private const int Id_eval = 6;

		private const int Id_isFinite = 7;

		private const int Id_isNaN = 8;

		private const int Id_isXMLName = 9;

		private const int Id_parseFloat = 10;

		private const int Id_parseInt = 11;

		private const int Id_unescape = 12;

		private const int Id_uneval = 13;

		private const int LAST_SCOPE_FUNCTION_ID = 13;

		private const int Id_new_CommonError = 14;
	}
}
