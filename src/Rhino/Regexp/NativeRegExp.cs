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
using Rhino.RegExp;
using Sharpen;

namespace Rhino.RegExp
{
	/// <summary>This class implements the RegExp native object.</summary>
	/// <remarks>
	/// This class implements the RegExp native object.
	/// Revision History:
	/// Implementation in C by Brendan Eich
	/// Initial port to Java by Norris Boyd from jsregexp.c version 1.36
	/// Merged up to version 1.38, which included Unicode support.
	/// Merged bug fixes in version 1.39.
	/// Merged JSFUN13_BRANCH changes up to 1.32.2.13
	/// </remarks>
	/// <author>Brendan Eich</author>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public class NativeRegExp : IdScriptableObject, Function
	{
		internal const long serialVersionUID = 4965263491464903264L;

		private static readonly object REGEXP_TAG = new object();

		public const int JSREG_GLOB = unchecked((int)(0x1));

		public const int JSREG_FOLD = unchecked((int)(0x2));

		public const int JSREG_MULTILINE = unchecked((int)(0x4));

		public const int TEST = 0;

		public const int MATCH = 1;

		public const int PREFIX = 2;

		private const bool debug = false;

		private const byte REOP_SIMPLE_START = 1;

		private const byte REOP_EMPTY = 1;

		private const byte REOP_BOL = 2;

		private const byte REOP_EOL = 3;

		private const byte REOP_WBDRY = 4;

		private const byte REOP_WNONBDRY = 5;

		private const byte REOP_DOT = 6;

		private const byte REOP_DIGIT = 7;

		private const byte REOP_NONDIGIT = 8;

		private const byte REOP_ALNUM = 9;

		private const byte REOP_NONALNUM = 10;

		private const byte REOP_SPACE = 11;

		private const byte REOP_NONSPACE = 12;

		private const byte REOP_BACKREF = 13;

		private const byte REOP_FLAT = 14;

		private const byte REOP_FLAT1 = 15;

		private const byte REOP_FLATi = 16;

		private const byte REOP_FLAT1i = 17;

		private const byte REOP_UCFLAT1 = 18;

		private const byte REOP_UCFLAT1i = 19;

		private const byte REOP_CLASS = 22;

		private const byte REOP_NCLASS = 23;

		private const byte REOP_SIMPLE_END = 23;

		private const byte REOP_QUANT = 25;

		private const byte REOP_STAR = 26;

		private const byte REOP_PLUS = 27;

		private const byte REOP_OPT = 28;

		private const byte REOP_LPAREN = 29;

		private const byte REOP_RPAREN = 30;

		private const byte REOP_ALT = 31;

		private const byte REOP_JUMP = 32;

		private const byte REOP_ASSERT = 41;

		private const byte REOP_ASSERT_NOT = 42;

		private const byte REOP_ASSERTTEST = 43;

		private const byte REOP_ASSERTNOTTEST = 44;

		private const byte REOP_MINIMALSTAR = 45;

		private const byte REOP_MINIMALPLUS = 46;

		private const byte REOP_MINIMALOPT = 47;

		private const byte REOP_MINIMALQUANT = 48;

		private const byte REOP_ENDCHILD = 49;

		private const byte REOP_REPEAT = 51;

		private const byte REOP_MINIMALREPEAT = 52;

		private const byte REOP_ALTPREREQ = 53;

		private const byte REOP_ALTPREREQi = 54;

		private const byte REOP_ALTPREREQ2 = 55;

		private const byte REOP_END = 57;

		private const int ANCHOR_BOL = -2;

		// 'g' flag: global
		// 'i' flag: fold
		// 'm' flag: multiline
		//type of match to perform
		//    private static final byte REOP_UCFLAT        = 20; /* flat Unicode string; len immediate counts chars */
		//    private static final byte REOP_UCFLATi       = 21; /* case-independent REOP_UCFLAT */
		//    private static final byte REOP_DOTSTAR       = 33; /* optimize .* to use a single opcode */
		//    private static final byte REOP_ANCHOR        = 34; /* like .* but skips left context to unanchored r.e. */
		//    private static final byte REOP_EOLONLY       = 35; /* $ not preceded by any pattern */
		//    private static final byte REOP_BACKREFi      = 37; /* case-independent REOP_BACKREF */
		//    private static final byte REOP_LPARENNON     = 40; /* non-capturing version of REOP_LPAREN */
		//    private static final byte REOP_ENDALT        = 56; /* end of final alternate */
		public static void Init(Context cx, Scriptable scope, bool @sealed)
		{
			NativeRegExp proto = new NativeRegExp();
			proto.re = CompileRE(cx, string.Empty, null, false);
			proto.ActivatePrototypeMap(MAX_PROTOTYPE_ID);
			proto.SetParentScope(scope);
			proto.SetPrototype(GetObjectPrototype(scope));
			NativeRegExpCtor ctor = new NativeRegExpCtor();
			// Bug #324006: ECMA-262 15.10.6.1 says "The initial value of
			// RegExp.prototype.constructor is the builtin RegExp constructor."
			proto.DefineProperty("constructor", ctor, ScriptableObject.DONTENUM);
			ScriptRuntime.SetFunctionProtoAndParent(ctor, scope);
			ctor.SetImmunePrototypeProperty(proto);
			if (@sealed)
			{
				proto.SealObject();
				ctor.SealObject();
			}
			DefineProperty(scope, "RegExp", ctor, ScriptableObject.DONTENUM);
		}

		internal NativeRegExp(Scriptable scope, RECompiled regexpCompiled)
		{
			this.re = regexpCompiled;
			this.lastIndex = 0;
			ScriptRuntime.SetBuiltinProtoAndParent(this, scope, TopLevel.Builtins.RegExp);
		}

		public override string GetClassName()
		{
			return "RegExp";
		}

		/// <summary>Gets the value to be returned by the typeof operator called on this object.</summary>
		/// <remarks>Gets the value to be returned by the typeof operator called on this object.</remarks>
		/// <seealso cref="Rhino.ScriptableObject.GetTypeOf()">Rhino.ScriptableObject.GetTypeOf()</seealso>
		/// <returns>"object"</returns>
		public override string GetTypeOf()
		{
			return "object";
		}

		public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			return ExecSub(cx, scope, args, MATCH);
		}

		public virtual Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			return (Scriptable)ExecSub(cx, scope, args, MATCH);
		}

		internal virtual Scriptable Compile(Context cx, Scriptable scope, object[] args)
		{
			if (args.Length > 0 && args[0] is NativeRegExp)
			{
				if (args.Length > 1 && args[1] != Undefined.instance)
				{
					// report error
					throw ScriptRuntime.TypeError0("msg.bad.regexp.compile");
				}
				NativeRegExp thatObj = (NativeRegExp)args[0];
				this.re = thatObj.re;
				this.lastIndex = thatObj.lastIndex;
				return this;
			}
			string s = args.Length == 0 ? string.Empty : EscapeRegExp(args[0]);
			string global = args.Length > 1 && args[1] != Undefined.instance ? ScriptRuntime.ToString(args[1]) : null;
			this.re = CompileRE(cx, s, global, false);
			this.lastIndex = 0;
			return this;
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append('/');
			if (re.source.Length != 0)
			{
				buf.Append(re.source);
			}
			else
			{
				// See bugzilla 226045
				buf.Append("(?:)");
			}
			buf.Append('/');
			if ((re.flags & JSREG_GLOB) != 0)
			{
				buf.Append('g');
			}
			if ((re.flags & JSREG_FOLD) != 0)
			{
				buf.Append('i');
			}
			if ((re.flags & JSREG_MULTILINE) != 0)
			{
				buf.Append('m');
			}
			return buf.ToString();
		}

		internal NativeRegExp()
		{
		}

		private static RegExpImpl GetImpl(Context cx)
		{
			return (RegExpImpl)ScriptRuntime.GetRegExpProxy(cx);
		}

		private static string EscapeRegExp(object src)
		{
			string s = ScriptRuntime.ToString(src);
			// Escape any naked slashes in regexp source, see bug #510265
			StringBuilder sb = null;
			// instantiated only if necessary
			int start = 0;
			int slash = s.IndexOf('/');
			while (slash > -1)
			{
				if (slash == start || s[slash - 1] != '\\')
				{
					if (sb == null)
					{
						sb = new StringBuilder();
					}
					sb.AppendRange(s, start, slash);
					sb.Append("\\/");
					start = slash + 1;
				}
				slash = s.IndexOf('/', slash + 1);
			}
			if (sb != null)
			{
				sb.AppendRange(s, start, s.Length);
				s = sb.ToString();
			}
			return s;
		}

		private object ExecSub(Context cx, Scriptable scopeObj, object[] args, int matchType)
		{
			RegExpImpl reImpl = GetImpl(cx);
			string str;
			if (args.Length == 0)
			{
				str = reImpl.input;
				if (str == null)
				{
					ReportError("msg.no.re.input.for", ToString());
				}
			}
			else
			{
				str = ScriptRuntime.ToString(args[0]);
			}
			double d = ((re.flags & JSREG_GLOB) != 0) ? lastIndex : 0;
			object rval;
			if (d < 0 || str.Length < d)
			{
				lastIndex = 0;
				rval = null;
			}
			else
			{
				int[] indexp = new int[] { (int)d };
				rval = ExecuteRegExp(cx, scopeObj, reImpl, str, indexp, matchType);
				if ((re.flags & JSREG_GLOB) != 0)
				{
					lastIndex = (rval == null || rval == Undefined.instance) ? 0 : indexp[0];
				}
			}
			return rval;
		}

		internal static RECompiled CompileRE(Context cx, string str, string global, bool flat)
		{
			RECompiled regexp = new RECompiled(str);
			int length = str.Length;
			int flags = 0;
			if (global != null)
			{
				for (int i = 0; i < global.Length; i++)
				{
					char c = global[i];
					if (c == 'g')
					{
						flags |= JSREG_GLOB;
					}
					else
					{
						if (c == 'i')
						{
							flags |= JSREG_FOLD;
						}
						else
						{
							if (c == 'm')
							{
								flags |= JSREG_MULTILINE;
							}
							else
							{
								ReportError("msg.invalid.re.flag", c.ToString());
							}
						}
					}
				}
			}
			regexp.flags = flags;
			CompilerState state = new CompilerState(cx, regexp.source, length, flags);
			if (flat && length > 0)
			{
				state.result = new RENode(REOP_FLAT);
				state.result.chr = state.cpbegin[0];
				state.result.length = length;
				state.result.flatIndex = 0;
				state.progLength += 5;
			}
			else
			{
				if (!ParseDisjunction(state))
				{
					return null;
				}
			}
			regexp.program = new byte[state.progLength + 1];
			if (state.classCount != 0)
			{
				regexp.classList = new RECharSet[state.classCount];
				regexp.classCount = state.classCount;
			}
			int endPC = EmitREBytecode(state, regexp, 0, state.result);
			regexp.program[endPC++] = REOP_END;
			regexp.parenCount = state.parenCount;
			switch (regexp.program[0])
			{
				case REOP_UCFLAT1:
				case REOP_UCFLAT1i:
				{
					// If re starts with literal, init anchorCh accordingly
					regexp.anchorCh = (char)GetIndex(regexp.program, 1);
					break;
				}

				case REOP_FLAT1:
				case REOP_FLAT1i:
				{
					regexp.anchorCh = (char)(regexp.program[1] & unchecked((int)(0xFF)));
					break;
				}

				case REOP_FLAT:
				case REOP_FLATi:
				{
					int k = GetIndex(regexp.program, 1);
					regexp.anchorCh = regexp.source[k];
					break;
				}

				case REOP_BOL:
				{
					regexp.anchorCh = ANCHOR_BOL;
					break;
				}

				case REOP_ALT:
				{
					RENode n = state.result;
					if (n.kid.op == REOP_BOL && n.kid2.op == REOP_BOL)
					{
						regexp.anchorCh = ANCHOR_BOL;
					}
					break;
				}
			}
			return regexp;
		}

		internal static bool IsDigit(char c)
		{
			return '0' <= c && c <= '9';
		}

		private static bool IsWord(char c)
		{
			return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || IsDigit(c) || c == '_';
		}

		private static bool IsControlLetter(char c)
		{
			return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z');
		}

		private static bool IsLineTerm(char c)
		{
			return ScriptRuntime.IsJSLineTerminator(c);
		}

		private static bool IsREWhiteSpace(int c)
		{
			return ScriptRuntime.IsJSWhitespaceOrLineTerminator(c);
		}

		private static char Upcase(char ch)
		{
			if (ch < 128)
			{
				if ('a' <= ch && ch <= 'z')
				{
					return (char)(ch + ('A' - 'a'));
				}
				return ch;
			}
			char cu = System.Char.ToUpper(ch);
			return (cu < 128) ? ch : cu;
		}

		private static char Downcase(char ch)
		{
			if (ch < 128)
			{
				if ('A' <= ch && ch <= 'Z')
				{
					return (char)(ch + ('a' - 'A'));
				}
				return ch;
			}
			char cl = System.Char.ToLower(ch);
			return (cl < 128) ? ch : cl;
		}

		private static int ToASCIIHexDigit(int c)
		{
			if (c < '0')
			{
				return -1;
			}
			if (c <= '9')
			{
				return c - '0';
			}
			c |= unchecked((int)(0x20));
			if ('a' <= c && c <= 'f')
			{
				return c - 'a' + 10;
			}
			return -1;
		}

		private static bool ParseDisjunction(CompilerState state)
		{
			if (!ParseAlternative(state))
			{
				return false;
			}
			char[] source = state.cpbegin;
			int index = state.cp;
			if (index != source.Length && source[index] == '|')
			{
				RENode result;
				++state.cp;
				result = new RENode(REOP_ALT);
				result.kid = state.result;
				if (!ParseDisjunction(state))
				{
					return false;
				}
				result.kid2 = state.result;
				state.result = result;
				if (result.kid.op == REOP_FLAT && result.kid2.op == REOP_FLAT)
				{
					result.op = (state.flags & JSREG_FOLD) == 0 ? REOP_ALTPREREQ : REOP_ALTPREREQi;
					result.chr = result.kid.chr;
					result.index = result.kid2.chr;
					state.progLength += 13;
				}
				else
				{
					if (result.kid.op == REOP_CLASS && result.kid.index < 256 && result.kid2.op == REOP_FLAT && (state.flags & JSREG_FOLD) == 0)
					{
						result.op = REOP_ALTPREREQ2;
						result.chr = result.kid2.chr;
						result.index = result.kid.index;
						state.progLength += 13;
					}
					else
					{
						if (result.kid.op == REOP_FLAT && result.kid2.op == REOP_CLASS && result.kid2.index < 256 && (state.flags & JSREG_FOLD) == 0)
						{
							result.op = REOP_ALTPREREQ2;
							result.chr = result.kid.chr;
							result.index = result.kid2.index;
							state.progLength += 13;
						}
						else
						{
							state.progLength += 9;
						}
					}
				}
			}
			return true;
		}

		private static bool ParseAlternative(CompilerState state)
		{
			RENode headTerm = null;
			RENode tailTerm = null;
			char[] source = state.cpbegin;
			while (true)
			{
				if (state.cp == state.cpend || source[state.cp] == '|' || (state.parenNesting != 0 && source[state.cp] == ')'))
				{
					if (headTerm == null)
					{
						state.result = new RENode(REOP_EMPTY);
					}
					else
					{
						state.result = headTerm;
					}
					return true;
				}
				if (!ParseTerm(state))
				{
					return false;
				}
				if (headTerm == null)
				{
					headTerm = state.result;
					tailTerm = headTerm;
				}
				else
				{
					tailTerm.next = state.result;
				}
				while (tailTerm.next != null)
				{
					tailTerm = tailTerm.next;
				}
			}
		}

		private static bool CalculateBitmapSize(CompilerState state, RENode target, char[] src, int index, int end)
		{
			char rangeStart = 0;
			char c;
			int n;
			int nDigits;
			int i;
			int max = 0;
			bool inRange = false;
			target.bmsize = 0;
			target.sense = true;
			if (index == end)
			{
				return true;
			}
			if (src[index] == '^')
			{
				++index;
				target.sense = false;
			}
			while (index != end)
			{
				int localMax = 0;
				nDigits = 2;
				switch (src[index])
				{
					case '\\':
					{
						++index;
						c = src[index++];
						switch (c)
						{
							case 'b':
							{
								localMax = unchecked((int)(0x8));
								break;
							}

							case 'f':
							{
								localMax = unchecked((int)(0xC));
								break;
							}

							case 'n':
							{
								localMax = unchecked((int)(0xA));
								break;
							}

							case 'r':
							{
								localMax = unchecked((int)(0xD));
								break;
							}

							case 't':
							{
								localMax = unchecked((int)(0x9));
								break;
							}

							case 'v':
							{
								localMax = unchecked((int)(0xB));
								break;
							}

							case 'c':
							{
								if ((index < end) && IsControlLetter(src[index]))
								{
									localMax = (char)(src[index++] & unchecked((int)(0x1F)));
								}
								else
								{
									--index;
								}
								localMax = '\\';
								break;
							}

							case 'u':
							{
								nDigits += 2;
								goto case 'x';
							}

							case 'x':
							{
								// fall thru...
								n = 0;
								for (i = 0; (i < nDigits) && (index < end); i++)
								{
									c = src[index++];
									n = Kit.XDigitToInt(c, n);
									if (n < 0)
									{
										// Back off to accepting the original
										// '\' as a literal
										index -= (i + 1);
										n = '\\';
										break;
									}
								}
								localMax = n;
								break;
							}

							case 'd':
							{
								if (inRange)
								{
									ReportError("msg.bad.range", string.Empty);
									return false;
								}
								localMax = '9';
								break;
							}

							case 'D':
							case 's':
							case 'S':
							case 'w':
							case 'W':
							{
								if (inRange)
								{
									ReportError("msg.bad.range", string.Empty);
									return false;
								}
								target.bmsize = 65536;
								return true;
							}

							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							{
								n = (c - '0');
								c = src[index];
								if ('0' <= c && c <= '7')
								{
									index++;
									n = 8 * n + (c - '0');
									c = src[index];
									if ('0' <= c && c <= '7')
									{
										index++;
										i = 8 * n + (c - '0');
										if (i <= 0xff)
										{
											n = i;
										}
										else
										{
											index--;
										}
									}
								}
								localMax = n;
								break;
							}

							default:
							{
								localMax = c;
								break;
							}
						}
						break;
					}

					default:
					{
						localMax = src[index++];
						break;
					}
				}
				if (inRange)
				{
					if (rangeStart > localMax)
					{
						ReportError("msg.bad.range", string.Empty);
						return false;
					}
					inRange = false;
				}
				else
				{
					if (index < (end - 1))
					{
						if (src[index] == '-')
						{
							++index;
							inRange = true;
							rangeStart = (char)localMax;
							continue;
						}
					}
				}
				if ((state.flags & JSREG_FOLD) != 0)
				{
					char cu = Upcase((char)localMax);
					char cd = Downcase((char)localMax);
					localMax = (cu >= cd) ? cu : cd;
				}
				if (localMax > max)
				{
					max = localMax;
				}
			}
			target.bmsize = max + 1;
			return true;
		}

		private static void DoFlat(CompilerState state, char c)
		{
			state.result = new RENode(REOP_FLAT);
			state.result.chr = c;
			state.result.length = 1;
			state.result.flatIndex = -1;
			state.progLength += 3;
		}

		private static int GetDecimalValue(char c, CompilerState state, int maxValue, string overflowMessageId)
		{
			bool overflow = false;
			int start = state.cp;
			char[] src = state.cpbegin;
			int value = c - '0';
			for (; state.cp != state.cpend; ++state.cp)
			{
				c = src[state.cp];
				if (!IsDigit(c))
				{
					break;
				}
				if (!overflow)
				{
					int digit = c - '0';
					if (value < (maxValue - digit) / 10)
					{
						value = value * 10 + digit;
					}
					else
					{
						overflow = true;
						value = maxValue;
					}
				}
			}
			if (overflow)
			{
				ReportError(overflowMessageId, src.ToString(start, state.cp - start));
			}
			return value;
		}

		private static bool ParseTerm(CompilerState state)
		{
			char[] src = state.cpbegin;
			char c = src[state.cp++];
			int nDigits = 2;
			int parenBaseCount = state.parenCount;
			int num;
			int tmp;
			RENode term;
			int termStart;
			switch (c)
			{
				case '^':
				{
					state.result = new RENode(REOP_BOL);
					state.progLength++;
					return true;
				}

				case '$':
				{
					state.result = new RENode(REOP_EOL);
					state.progLength++;
					return true;
				}

				case '\\':
				{
					if (state.cp < state.cpend)
					{
						c = src[state.cp++];
						switch (c)
						{
							case 'b':
							{
								state.result = new RENode(REOP_WBDRY);
								state.progLength++;
								return true;
							}

							case 'B':
							{
								state.result = new RENode(REOP_WNONBDRY);
								state.progLength++;
								return true;
							}

							case '0':
							{
								ReportWarning(state.cx, "msg.bad.backref", string.Empty);
								num = 0;
								while (state.cp < state.cpend)
								{
									c = src[state.cp];
									if ((c >= '0') && (c <= '7'))
									{
										state.cp++;
										tmp = 8 * num + (c - '0');
										if (tmp > 0xff)
										{
											break;
										}
										num = tmp;
									}
									else
									{
										break;
									}
								}
								c = (char)(num);
								DoFlat(state, c);
								break;
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
							{
								termStart = state.cp - 1;
								num = GetDecimalValue(c, state, unchecked((int)(0xFFFF)), "msg.overlarge.backref");
								if (num > state.parenCount)
								{
									ReportWarning(state.cx, "msg.bad.backref", string.Empty);
								}
								if ((num > 9) && (num > state.parenCount))
								{
									state.cp = termStart;
									num = 0;
									while (state.cp < state.cpend)
									{
										c = src[state.cp];
										if ((c >= '0') && (c <= '7'))
										{
											state.cp++;
											tmp = 8 * num + (c - '0');
											if (tmp > 0xff)
											{
												break;
											}
											num = tmp;
										}
										else
										{
											break;
										}
									}
									c = (char)(num);
									DoFlat(state, c);
									break;
								}
								state.result = new RENode(REOP_BACKREF);
								state.result.parenIndex = num - 1;
								state.progLength += 3;
								break;
							}

							case 'f':
							{
								c = (char)unchecked((int)(0xC));
								DoFlat(state, c);
								break;
							}

							case 'n':
							{
								c = (char)unchecked((int)(0xA));
								DoFlat(state, c);
								break;
							}

							case 'r':
							{
								c = (char)unchecked((int)(0xD));
								DoFlat(state, c);
								break;
							}

							case 't':
							{
								c = (char)unchecked((int)(0x9));
								DoFlat(state, c);
								break;
							}

							case 'v':
							{
								c = (char)unchecked((int)(0xB));
								DoFlat(state, c);
								break;
							}

							case 'c':
							{
								if ((state.cp < state.cpend) && IsControlLetter(src[state.cp]))
								{
									c = (char)(src[state.cp++] & unchecked((int)(0x1F)));
								}
								else
								{
									--state.cp;
									c = '\\';
								}
								DoFlat(state, c);
								break;
							}

							case 'u':
							{
								nDigits += 2;
								goto case 'x';
							}

							case 'x':
							{
								// fall thru...
								int n = 0;
								int i;
								for (i = 0; (i < nDigits) && (state.cp < state.cpend); i++)
								{
									c = src[state.cp++];
									n = Kit.XDigitToInt(c, n);
									if (n < 0)
									{
										// Back off to accepting the original
										// 'u' or 'x' as a literal
										state.cp -= (i + 2);
										n = src[state.cp++];
										break;
									}
								}
								c = (char)(n);
								DoFlat(state, c);
								break;
							}

							case 'd':
							{
								state.result = new RENode(REOP_DIGIT);
								state.progLength++;
								break;
							}

							case 'D':
							{
								state.result = new RENode(REOP_NONDIGIT);
								state.progLength++;
								break;
							}

							case 's':
							{
								state.result = new RENode(REOP_SPACE);
								state.progLength++;
								break;
							}

							case 'S':
							{
								state.result = new RENode(REOP_NONSPACE);
								state.progLength++;
								break;
							}

							case 'w':
							{
								state.result = new RENode(REOP_ALNUM);
								state.progLength++;
								break;
							}

							case 'W':
							{
								state.result = new RENode(REOP_NONALNUM);
								state.progLength++;
								break;
							}

							default:
							{
								state.result = new RENode(REOP_FLAT);
								state.result.chr = c;
								state.result.length = 1;
								state.result.flatIndex = state.cp - 1;
								state.progLength += 3;
								break;
							}
						}
						break;
					}
					else
					{
						ReportError("msg.trail.backslash", string.Empty);
						return false;
					}
					goto case '(';
				}

				case '(':
				{
					RENode result = null;
					termStart = state.cp;
					if (state.cp + 1 < state.cpend && src[state.cp] == '?' && ((c = src[state.cp + 1]) == '=' || c == '!' || c == ':'))
					{
						state.cp += 2;
						if (c == '=')
						{
							result = new RENode(REOP_ASSERT);
							state.progLength += 4;
						}
						else
						{
							if (c == '!')
							{
								result = new RENode(REOP_ASSERT_NOT);
								state.progLength += 4;
							}
						}
					}
					else
					{
						result = new RENode(REOP_LPAREN);
						state.progLength += 6;
						result.parenIndex = state.parenCount++;
					}
					++state.parenNesting;
					if (!ParseDisjunction(state))
					{
						return false;
					}
					if (state.cp == state.cpend || src[state.cp] != ')')
					{
						ReportError("msg.unterm.paren", string.Empty);
						return false;
					}
					++state.cp;
					--state.parenNesting;
					if (result != null)
					{
						result.kid = state.result;
						state.result = result;
					}
					break;
				}

				case ')':
				{
					ReportError("msg.re.unmatched.right.paren", string.Empty);
					return false;
				}

				case '[':
				{
					state.result = new RENode(REOP_CLASS);
					termStart = state.cp;
					state.result.startIndex = termStart;
					while (true)
					{
						if (state.cp == state.cpend)
						{
							ReportError("msg.unterm.class", string.Empty);
							return false;
						}
						if (src[state.cp] == '\\')
						{
							state.cp++;
						}
						else
						{
							if (src[state.cp] == ']')
							{
								state.result.kidlen = state.cp - termStart;
								break;
							}
						}
						state.cp++;
					}
					state.result.index = state.classCount++;
					if (!CalculateBitmapSize(state, state.result, src, termStart, state.cp++))
					{
						return false;
					}
					state.progLength += 3;
					break;
				}

				case '.':
				{
					state.result = new RENode(REOP_DOT);
					state.progLength++;
					break;
				}

				case '*':
				case '+':
				case '?':
				{
					ReportError("msg.bad.quant", src[state.cp - 1].ToString());
					return false;
				}

				default:
				{
					state.result = new RENode(REOP_FLAT);
					state.result.chr = c;
					state.result.length = 1;
					state.result.flatIndex = state.cp - 1;
					state.progLength += 3;
					break;
				}
			}
			term = state.result;
			if (state.cp == state.cpend)
			{
				return true;
			}
			bool hasQ = false;
			switch (src[state.cp])
			{
				case '+':
				{
					state.result = new RENode(REOP_QUANT);
					state.result.min = 1;
					state.result.max = -1;
					state.progLength += 8;
					hasQ = true;
					break;
				}

				case '*':
				{
					state.result = new RENode(REOP_QUANT);
					state.result.min = 0;
					state.result.max = -1;
					state.progLength += 8;
					hasQ = true;
					break;
				}

				case '?':
				{
					state.result = new RENode(REOP_QUANT);
					state.result.min = 0;
					state.result.max = 1;
					state.progLength += 8;
					hasQ = true;
					break;
				}

				case '{':
				{
					int min = 0;
					int max = -1;
					int leftCurl = state.cp;
					if (++state.cp < src.Length && IsDigit(c = src[state.cp]))
					{
						++state.cp;
						min = GetDecimalValue(c, state, unchecked((int)(0xFFFF)), "msg.overlarge.min");
						c = src[state.cp];
						if (c == ',')
						{
							c = src[++state.cp];
							if (IsDigit(c))
							{
								++state.cp;
								max = GetDecimalValue(c, state, unchecked((int)(0xFFFF)), "msg.overlarge.max");
								c = src[state.cp];
								if (min > max)
								{
									ReportError("msg.max.lt.min", src[state.cp].ToString());
									return false;
								}
							}
						}
						else
						{
							max = min;
						}
						if (c == '}')
						{
							state.result = new RENode(REOP_QUANT);
							state.result.min = min;
							state.result.max = max;
							// QUANT, <min>, <max>, <parencount>,
							// <parenindex>, <next> ... <ENDCHILD>
							state.progLength += 12;
							hasQ = true;
						}
					}
					if (!hasQ)
					{
						state.cp = leftCurl;
					}
					break;
				}
			}
			if (!hasQ)
			{
				return true;
			}
			++state.cp;
			state.result.kid = term;
			state.result.parenIndex = parenBaseCount;
			state.result.parenCount = state.parenCount - parenBaseCount;
			if ((state.cp < state.cpend) && (src[state.cp] == '?'))
			{
				++state.cp;
				state.result.greedy = false;
			}
			else
			{
				state.result.greedy = true;
			}
			return true;
		}

		private static void ResolveForwardJump(byte[] array, int from, int pc)
		{
			if (from > pc)
			{
				throw Kit.CodeBug();
			}
			AddIndex(array, from, pc - from);
		}

		private static int GetOffset(byte[] array, int pc)
		{
			return GetIndex(array, pc);
		}

		private static int AddIndex(byte[] array, int pc, int index)
		{
			if (index < 0)
			{
				throw Kit.CodeBug();
			}
			if (index > unchecked((int)(0xFFFF)))
			{
				throw Context.ReportRuntimeError("Too complex regexp");
			}
			array[pc] = unchecked((byte)(index >> 8));
			array[pc + 1] = unchecked((byte)(index));
			return pc + 2;
		}

		private static int GetIndex(byte[] array, int pc)
		{
			return ((array[pc] & unchecked((int)(0xFF))) << 8) | (array[pc + 1] & unchecked((int)(0xFF)));
		}

		private const int INDEX_LEN = 2;

		private static int EmitREBytecode(CompilerState state, RECompiled re, int pc, RENode t)
		{
			RENode nextAlt;
			int nextAltFixup;
			int nextTermFixup;
			byte[] program = re.program;
			while (t != null)
			{
				program[pc++] = t.op;
				switch (t.op)
				{
					case REOP_EMPTY:
					{
						--pc;
						break;
					}

					case REOP_ALTPREREQ:
					case REOP_ALTPREREQi:
					case REOP_ALTPREREQ2:
					{
						bool ignoreCase = t.op == REOP_ALTPREREQi;
						AddIndex(program, pc, ignoreCase ? Upcase(t.chr) : t.chr);
						pc += INDEX_LEN;
						AddIndex(program, pc, ignoreCase ? Upcase((char)t.index) : t.index);
						pc += INDEX_LEN;
						goto case REOP_ALT;
					}

					case REOP_ALT:
					{
						// fall through to REOP_ALT
						nextAlt = t.kid2;
						nextAltFixup = pc;
						pc += INDEX_LEN;
						pc = EmitREBytecode(state, re, pc, t.kid);
						program[pc++] = REOP_JUMP;
						nextTermFixup = pc;
						pc += INDEX_LEN;
						ResolveForwardJump(program, nextAltFixup, pc);
						pc = EmitREBytecode(state, re, pc, nextAlt);
						program[pc++] = REOP_JUMP;
						nextAltFixup = pc;
						pc += INDEX_LEN;
						ResolveForwardJump(program, nextTermFixup, pc);
						ResolveForwardJump(program, nextAltFixup, pc);
						break;
					}

					case REOP_FLAT:
					{
						if (t.flatIndex != -1)
						{
							while ((t.next != null) && (t.next.op == REOP_FLAT) && ((t.flatIndex + t.length) == t.next.flatIndex))
							{
								t.length += t.next.length;
								t.next = t.next.next;
							}
						}
						if ((t.flatIndex != -1) && (t.length > 1))
						{
							if ((state.flags & JSREG_FOLD) != 0)
							{
								program[pc - 1] = REOP_FLATi;
							}
							else
							{
								program[pc - 1] = REOP_FLAT;
							}
							pc = AddIndex(program, pc, t.flatIndex);
							pc = AddIndex(program, pc, t.length);
						}
						else
						{
							if (t.chr < 256)
							{
								if ((state.flags & JSREG_FOLD) != 0)
								{
									program[pc - 1] = REOP_FLAT1i;
								}
								else
								{
									program[pc - 1] = REOP_FLAT1;
								}
								program[pc++] = unchecked((byte)(t.chr));
							}
							else
							{
								if ((state.flags & JSREG_FOLD) != 0)
								{
									program[pc - 1] = REOP_UCFLAT1i;
								}
								else
								{
									program[pc - 1] = REOP_UCFLAT1;
								}
								pc = AddIndex(program, pc, t.chr);
							}
						}
						break;
					}

					case REOP_LPAREN:
					{
						pc = AddIndex(program, pc, t.parenIndex);
						pc = EmitREBytecode(state, re, pc, t.kid);
						program[pc++] = REOP_RPAREN;
						pc = AddIndex(program, pc, t.parenIndex);
						break;
					}

					case REOP_BACKREF:
					{
						pc = AddIndex(program, pc, t.parenIndex);
						break;
					}

					case REOP_ASSERT:
					{
						nextTermFixup = pc;
						pc += INDEX_LEN;
						pc = EmitREBytecode(state, re, pc, t.kid);
						program[pc++] = REOP_ASSERTTEST;
						ResolveForwardJump(program, nextTermFixup, pc);
						break;
					}

					case REOP_ASSERT_NOT:
					{
						nextTermFixup = pc;
						pc += INDEX_LEN;
						pc = EmitREBytecode(state, re, pc, t.kid);
						program[pc++] = REOP_ASSERTNOTTEST;
						ResolveForwardJump(program, nextTermFixup, pc);
						break;
					}

					case REOP_QUANT:
					{
						if ((t.min == 0) && (t.max == -1))
						{
							program[pc - 1] = (t.greedy) ? REOP_STAR : REOP_MINIMALSTAR;
						}
						else
						{
							if ((t.min == 0) && (t.max == 1))
							{
								program[pc - 1] = (t.greedy) ? REOP_OPT : REOP_MINIMALOPT;
							}
							else
							{
								if ((t.min == 1) && (t.max == -1))
								{
									program[pc - 1] = (t.greedy) ? REOP_PLUS : REOP_MINIMALPLUS;
								}
								else
								{
									if (!t.greedy)
									{
										program[pc - 1] = REOP_MINIMALQUANT;
									}
									pc = AddIndex(program, pc, t.min);
									// max can be -1 which addIndex does not accept
									pc = AddIndex(program, pc, t.max + 1);
								}
							}
						}
						pc = AddIndex(program, pc, t.parenCount);
						pc = AddIndex(program, pc, t.parenIndex);
						nextTermFixup = pc;
						pc += INDEX_LEN;
						pc = EmitREBytecode(state, re, pc, t.kid);
						program[pc++] = REOP_ENDCHILD;
						ResolveForwardJump(program, nextTermFixup, pc);
						break;
					}

					case REOP_CLASS:
					{
						if (!t.sense)
						{
							program[pc - 1] = REOP_NCLASS;
						}
						pc = AddIndex(program, pc, t.index);
						re.classList[t.index] = new RECharSet(t.bmsize, t.startIndex, t.kidlen, t.sense);
						break;
					}

					default:
					{
						break;
					}
				}
				t = t.next;
			}
			return pc;
		}

		private static void PushProgState(REGlobalData gData, int min, int max, int cp, REBackTrackData backTrackLastToSave, int continuationOp, int continuationPc)
		{
			gData.stateStackTop = new REProgState(gData.stateStackTop, min, max, cp, backTrackLastToSave, continuationOp, continuationPc);
		}

		private static REProgState PopProgState(REGlobalData gData)
		{
			REProgState state = gData.stateStackTop;
			gData.stateStackTop = state.previous;
			return state;
		}

		private static void PushBackTrackState(REGlobalData gData, byte op, int pc)
		{
			REProgState state = gData.stateStackTop;
			gData.backTrackStackTop = new REBackTrackData(gData, op, pc, gData.cp, state.continuationOp, state.continuationPc);
		}

		private static void PushBackTrackState(REGlobalData gData, byte op, int pc, int cp, int continuationOp, int continuationPc)
		{
			gData.backTrackStackTop = new REBackTrackData(gData, op, pc, cp, continuationOp, continuationPc);
		}

		private static bool FlatNMatcher(REGlobalData gData, int matchChars, int length, string input, int end)
		{
			if ((gData.cp + length) > end)
			{
				return false;
			}
			for (int i = 0; i < length; i++)
			{
				if (gData.regexp.source[matchChars + i] != input[gData.cp + i])
				{
					return false;
				}
			}
			gData.cp += length;
			return true;
		}

		private static bool FlatNIMatcher(REGlobalData gData, int matchChars, int length, string input, int end)
		{
			if ((gData.cp + length) > end)
			{
				return false;
			}
			char[] source = gData.regexp.source;
			for (int i = 0; i < length; i++)
			{
				char c1 = source[matchChars + i];
				char c2 = input[gData.cp + i];
				if (c1 != c2 && Upcase(c1) != Upcase(c2))
				{
					return false;
				}
			}
			gData.cp += length;
			return true;
		}

		private static bool BackrefMatcher(REGlobalData gData, int parenIndex, string input, int end)
		{
			int len;
			int i;
			if (gData.parens == null || parenIndex >= gData.parens.Length)
			{
				return false;
			}
			int parenContent = gData.ParensIndex(parenIndex);
			if (parenContent == -1)
			{
				return true;
			}
			len = gData.ParensLength(parenIndex);
			if ((gData.cp + len) > end)
			{
				return false;
			}
			if ((gData.regexp.flags & JSREG_FOLD) != 0)
			{
				for (i = 0; i < len; i++)
				{
					char c1 = input[parenContent + i];
					char c2 = input[gData.cp + i];
					if (c1 != c2 && Upcase(c1) != Upcase(c2))
					{
						return false;
					}
				}
			}
			else
			{
				if (!input.RegionMatches(parenContent, input, gData.cp, len))
				{
					return false;
				}
			}
			gData.cp += len;
			return true;
		}

		private static void AddCharacterToCharSet(RECharSet cs, char c)
		{
			int byteIndex = (c / 8);
			if (c >= cs.length)
			{
				throw ScriptRuntime.ConstructError("SyntaxError", "invalid range in character class");
			}
			cs.bits[byteIndex] |= 1 << (c & unchecked((int)(0x7)));
		}

		private static void AddCharacterRangeToCharSet(RECharSet cs, char c1, char c2)
		{
			int i;
			int byteIndex1 = (c1 / 8);
			int byteIndex2 = (c2 / 8);
			if ((c2 >= cs.length) || (c1 > c2))
			{
				throw ScriptRuntime.ConstructError("SyntaxError", "invalid range in character class");
			}
			c1 &= (char)unchecked((int)(0x7));
			c2 &= (char)unchecked((int)(0x7));
			if (byteIndex1 == byteIndex2)
			{
				cs.bits[byteIndex1] |= ((unchecked((int)(0xFF))) >> (7 - (c2 - c1))) << c1;
			}
			else
			{
				cs.bits[byteIndex1] |= unchecked((int)(0xFF)) << c1;
				for (i = byteIndex1 + 1; i < byteIndex2; i++)
				{
					cs.bits[i] = unchecked((byte)unchecked((int)(0xFF)));
				}
				cs.bits[byteIndex2] |= (unchecked((int)(0xFF))) >> (7 - c2);
			}
		}

		private static void ProcessCharSet(REGlobalData gData, RECharSet charSet)
		{
			lock (charSet)
			{
				if (!charSet.converted)
				{
					ProcessCharSetImpl(gData, charSet);
					charSet.converted = true;
				}
			}
		}

		private static void ProcessCharSetImpl(REGlobalData gData, RECharSet charSet)
		{
			int src = charSet.startIndex;
			int end = src + charSet.strlength;
			char rangeStart = 0;
			char thisCh;
			int byteLength;
			char c;
			int n;
			int nDigits;
			int i;
			bool inRange = false;
			byteLength = (charSet.length + 7) / 8;
			charSet.bits = new byte[byteLength];
			if (src == end)
			{
				return;
			}
			if (gData.regexp.source[src] == '^')
			{
				System.Diagnostics.Debug.Assert((!charSet.sense));
				++src;
			}
			else
			{
				System.Diagnostics.Debug.Assert((charSet.sense));
			}
			while (src != end)
			{
				nDigits = 2;
				switch (gData.regexp.source[src])
				{
					case '\\':
					{
						++src;
						c = gData.regexp.source[src++];
						switch (c)
						{
							case 'b':
							{
								thisCh = (char)unchecked((int)(0x8));
								break;
							}

							case 'f':
							{
								thisCh = (char)unchecked((int)(0xC));
								break;
							}

							case 'n':
							{
								thisCh = (char)unchecked((int)(0xA));
								break;
							}

							case 'r':
							{
								thisCh = (char)unchecked((int)(0xD));
								break;
							}

							case 't':
							{
								thisCh = (char)unchecked((int)(0x9));
								break;
							}

							case 'v':
							{
								thisCh = (char)unchecked((int)(0xB));
								break;
							}

							case 'c':
							{
								if ((src < end) && IsControlLetter(gData.regexp.source[src]))
								{
									thisCh = (char)(gData.regexp.source[src++] & unchecked((int)(0x1F)));
								}
								else
								{
									--src;
									thisCh = '\\';
								}
								break;
							}

							case 'u':
							{
								nDigits += 2;
								goto case 'x';
							}

							case 'x':
							{
								// fall thru
								n = 0;
								for (i = 0; (i < nDigits) && (src < end); i++)
								{
									c = gData.regexp.source[src++];
									int digit = ToASCIIHexDigit(c);
									if (digit < 0)
									{
										src -= (i + 1);
										n = '\\';
										break;
									}
									n = (n << 4) | digit;
								}
								thisCh = (char)(n);
								break;
							}

							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							{
								n = (c - '0');
								c = gData.regexp.source[src];
								if ('0' <= c && c <= '7')
								{
									src++;
									n = 8 * n + (c - '0');
									c = gData.regexp.source[src];
									if ('0' <= c && c <= '7')
									{
										src++;
										i = 8 * n + (c - '0');
										if (i <= 0xff)
										{
											n = i;
										}
										else
										{
											src--;
										}
									}
								}
								thisCh = (char)(n);
								break;
							}

							case 'd':
							{
								AddCharacterRangeToCharSet(charSet, '0', '9');
								continue;
							}

							case 'D':
							{
								AddCharacterRangeToCharSet(charSet, (char)0, (char)('0' - 1));
								AddCharacterRangeToCharSet(charSet, (char)('9' + 1), (char)(charSet.length - 1));
								continue;
							}

							case 's':
							{
								for (i = (charSet.length - 1); i >= 0; i--)
								{
									if (IsREWhiteSpace(i))
									{
										AddCharacterToCharSet(charSet, (char)(i));
									}
								}
								continue;
							}

							case 'S':
							{
								for (i = (charSet.length - 1); i >= 0; i--)
								{
									if (!IsREWhiteSpace(i))
									{
										AddCharacterToCharSet(charSet, (char)(i));
									}
								}
								continue;
							}

							case 'w':
							{
								for (i = (charSet.length - 1); i >= 0; i--)
								{
									if (IsWord((char)i))
									{
										AddCharacterToCharSet(charSet, (char)(i));
									}
								}
								continue;
							}

							case 'W':
							{
								for (i = (charSet.length - 1); i >= 0; i--)
								{
									if (!IsWord((char)i))
									{
										AddCharacterToCharSet(charSet, (char)(i));
									}
								}
								continue;
							}

							default:
							{
								thisCh = c;
								break;
							}
						}
						break;
					}

					default:
					{
						thisCh = gData.regexp.source[src++];
						break;
					}
				}
				if (inRange)
				{
					if ((gData.regexp.flags & JSREG_FOLD) != 0)
					{
						System.Diagnostics.Debug.Assert((rangeStart <= thisCh));
						for (c = rangeStart; c <= thisCh; )
						{
							AddCharacterToCharSet(charSet, c);
							char uch = Upcase(c);
							char dch = Downcase(c);
							if (c != uch)
							{
								AddCharacterToCharSet(charSet, uch);
							}
							if (c != dch)
							{
								AddCharacterToCharSet(charSet, dch);
							}
							if (++c == 0)
							{
								break;
							}
						}
					}
					else
					{
						// overflow
						AddCharacterRangeToCharSet(charSet, rangeStart, thisCh);
					}
					inRange = false;
				}
				else
				{
					if ((gData.regexp.flags & JSREG_FOLD) != 0)
					{
						AddCharacterToCharSet(charSet, Upcase(thisCh));
						AddCharacterToCharSet(charSet, Downcase(thisCh));
					}
					else
					{
						AddCharacterToCharSet(charSet, thisCh);
					}
					if (src < (end - 1))
					{
						if (gData.regexp.source[src] == '-')
						{
							++src;
							inRange = true;
							rangeStart = thisCh;
						}
					}
				}
			}
		}

		private static bool ClassMatcher(REGlobalData gData, RECharSet charSet, char ch)
		{
			if (!charSet.converted)
			{
				ProcessCharSet(gData, charSet);
			}
			int byteIndex = ch >> 3;
			return (charSet.length == 0 || ch >= charSet.length || (charSet.bits[byteIndex] & (1 << (ch & unchecked((int)(0x7))))) == 0) ^ charSet.sense;
		}

		private static bool ReopIsSimple(int op)
		{
			return op >= REOP_SIMPLE_START && op <= REOP_SIMPLE_END;
		}

		private static int SimpleMatch(REGlobalData gData, string input, int op, byte[] program, int pc, int end, bool updatecp)
		{
			bool result = false;
			char matchCh;
			int parenIndex;
			int offset;
			int length;
			int index;
			int startcp = gData.cp;
			switch (op)
			{
				case REOP_EMPTY:
				{
					result = true;
					break;
				}

				case REOP_BOL:
				{
					if (gData.cp != 0)
					{
						if (!gData.multiline || !IsLineTerm(input[gData.cp - 1]))
						{
							break;
						}
					}
					result = true;
					break;
				}

				case REOP_EOL:
				{
					if (gData.cp != end)
					{
						if (!gData.multiline || !IsLineTerm(input[gData.cp]))
						{
							break;
						}
					}
					result = true;
					break;
				}

				case REOP_WBDRY:
				{
					result = ((gData.cp == 0 || !IsWord(input[gData.cp - 1])) ^ !((gData.cp < end) && IsWord(input[gData.cp])));
					break;
				}

				case REOP_WNONBDRY:
				{
					result = ((gData.cp == 0 || !IsWord(input[gData.cp - 1])) ^ ((gData.cp < end) && IsWord(input[gData.cp])));
					break;
				}

				case REOP_DOT:
				{
					if (gData.cp != end && !IsLineTerm(input[gData.cp]))
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_DIGIT:
				{
					if (gData.cp != end && IsDigit(input[gData.cp]))
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_NONDIGIT:
				{
					if (gData.cp != end && !IsDigit(input[gData.cp]))
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_ALNUM:
				{
					if (gData.cp != end && IsWord(input[gData.cp]))
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_NONALNUM:
				{
					if (gData.cp != end && !IsWord(input[gData.cp]))
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_SPACE:
				{
					if (gData.cp != end && IsREWhiteSpace(input[gData.cp]))
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_NONSPACE:
				{
					if (gData.cp != end && !IsREWhiteSpace(input[gData.cp]))
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_BACKREF:
				{
					parenIndex = GetIndex(program, pc);
					pc += INDEX_LEN;
					result = BackrefMatcher(gData, parenIndex, input, end);
					break;
				}

				case REOP_FLAT:
				{
					offset = GetIndex(program, pc);
					pc += INDEX_LEN;
					length = GetIndex(program, pc);
					pc += INDEX_LEN;
					result = FlatNMatcher(gData, offset, length, input, end);
					break;
				}

				case REOP_FLAT1:
				{
					matchCh = (char)(program[pc++] & unchecked((int)(0xFF)));
					if (gData.cp != end && input[gData.cp] == matchCh)
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_FLATi:
				{
					offset = GetIndex(program, pc);
					pc += INDEX_LEN;
					length = GetIndex(program, pc);
					pc += INDEX_LEN;
					result = FlatNIMatcher(gData, offset, length, input, end);
					break;
				}

				case REOP_FLAT1i:
				{
					matchCh = (char)(program[pc++] & unchecked((int)(0xFF)));
					if (gData.cp != end)
					{
						char c = input[gData.cp];
						if (matchCh == c || Upcase(matchCh) == Upcase(c))
						{
							result = true;
							gData.cp++;
						}
					}
					break;
				}

				case REOP_UCFLAT1:
				{
					matchCh = (char)GetIndex(program, pc);
					pc += INDEX_LEN;
					if (gData.cp != end && input[gData.cp] == matchCh)
					{
						result = true;
						gData.cp++;
					}
					break;
				}

				case REOP_UCFLAT1i:
				{
					matchCh = (char)GetIndex(program, pc);
					pc += INDEX_LEN;
					if (gData.cp != end)
					{
						char c = input[gData.cp];
						if (matchCh == c || Upcase(matchCh) == Upcase(c))
						{
							result = true;
							gData.cp++;
						}
					}
					break;
				}

				case REOP_CLASS:
				case REOP_NCLASS:
				{
					index = GetIndex(program, pc);
					pc += INDEX_LEN;
					if (gData.cp != end)
					{
						if (ClassMatcher(gData, gData.regexp.classList[index], input[gData.cp]))
						{
							gData.cp++;
							result = true;
							break;
						}
					}
					break;
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
			if (result)
			{
				if (!updatecp)
				{
					gData.cp = startcp;
				}
				return pc;
			}
			gData.cp = startcp;
			return -1;
		}

		private static bool ExecuteREBytecode(REGlobalData gData, string input, int end)
		{
			int pc = 0;
			byte[] program = gData.regexp.program;
			int continuationOp = REOP_END;
			int continuationPc = 0;
			bool result = false;
			int op = program[pc++];
			if (gData.regexp.anchorCh < 0 && ReopIsSimple(op))
			{
				bool anchor = false;
				while (gData.cp <= end)
				{
					int match = SimpleMatch(gData, input, op, program, pc, end, true);
					if (match >= 0)
					{
						anchor = true;
						pc = match;
						op = program[pc++];
						break;
					}
					gData.skipped++;
					gData.cp++;
				}
				if (!anchor)
				{
					return false;
				}
			}
			for (; ; )
			{
				if (ReopIsSimple(op))
				{
					int match = SimpleMatch(gData, input, op, program, pc, end, true);
					result = match >= 0;
					if (result)
					{
						pc = match;
					}
				}
				else
				{
					switch (op)
					{
						case REOP_ALTPREREQ:
						case REOP_ALTPREREQi:
						case REOP_ALTPREREQ2:
						{
							char matchCh1 = (char)GetIndex(program, pc);
							pc += INDEX_LEN;
							char matchCh2 = (char)GetIndex(program, pc);
							pc += INDEX_LEN;
							if (gData.cp == end)
							{
								result = false;
								break;
							}
							char c = input[gData.cp];
							if (op == REOP_ALTPREREQ2)
							{
								if (c != matchCh1 && !ClassMatcher(gData, gData.regexp.classList[matchCh2], c))
								{
									result = false;
									break;
								}
							}
							else
							{
								if (op == REOP_ALTPREREQi)
								{
									c = Upcase(c);
								}
								if (c != matchCh1 && c != matchCh2)
								{
									result = false;
									break;
								}
							}
							goto case REOP_ALT;
						}

						case REOP_ALT:
						{
							int nextpc = pc + GetOffset(program, pc);
							pc += INDEX_LEN;
							op = program[pc++];
							int startcp = gData.cp;
							if (ReopIsSimple(op))
							{
								int match = SimpleMatch(gData, input, op, program, pc, end, true);
								if (match < 0)
								{
									op = program[nextpc++];
									pc = nextpc;
									continue;
								}
								result = true;
								pc = match;
								op = program[pc++];
							}
							byte nextop = program[nextpc++];
							PushBackTrackState(gData, nextop, nextpc, startcp, continuationOp, continuationPc);
							continue;
						}

						case REOP_JUMP:
						{
							int offset = GetOffset(program, pc);
							pc += offset;
							op = program[pc++];
							continue;
						}

						case REOP_LPAREN:
						{
							int parenIndex = GetIndex(program, pc);
							pc += INDEX_LEN;
							gData.SetParens(parenIndex, gData.cp, 0);
							op = program[pc++];
							continue;
						}

						case REOP_RPAREN:
						{
							int parenIndex = GetIndex(program, pc);
							pc += INDEX_LEN;
							int cap_index = gData.ParensIndex(parenIndex);
							gData.SetParens(parenIndex, cap_index, gData.cp - cap_index);
							op = program[pc++];
							continue;
						}

						case REOP_ASSERT:
						{
							int nextpc = pc + GetIndex(program, pc);
							pc += INDEX_LEN;
							op = program[pc++];
							if (ReopIsSimple(op) && SimpleMatch(gData, input, op, program, pc, end, false) < 0)
							{
								result = false;
								break;
							}
							PushProgState(gData, 0, 0, gData.cp, gData.backTrackStackTop, continuationOp, continuationPc);
							PushBackTrackState(gData, REOP_ASSERTTEST, nextpc);
							continue;
						}

						case REOP_ASSERT_NOT:
						{
							int nextpc = pc + GetIndex(program, pc);
							pc += INDEX_LEN;
							op = program[pc++];
							if (ReopIsSimple(op))
							{
								int match = SimpleMatch(gData, input, op, program, pc, end, false);
								if (match >= 0 && program[match] == REOP_ASSERTNOTTEST)
								{
									result = false;
									break;
								}
							}
							PushProgState(gData, 0, 0, gData.cp, gData.backTrackStackTop, continuationOp, continuationPc);
							PushBackTrackState(gData, REOP_ASSERTNOTTEST, nextpc);
							continue;
						}

						case REOP_ASSERTTEST:
						case REOP_ASSERTNOTTEST:
						{
							REProgState state = PopProgState(gData);
							gData.cp = state.index;
							gData.backTrackStackTop = state.backTrack;
							continuationPc = state.continuationPc;
							continuationOp = state.continuationOp;
							if (op == REOP_ASSERTNOTTEST)
							{
								result = !result;
							}
							break;
						}

						case REOP_STAR:
						case REOP_PLUS:
						case REOP_OPT:
						case REOP_QUANT:
						case REOP_MINIMALSTAR:
						case REOP_MINIMALPLUS:
						case REOP_MINIMALOPT:
						case REOP_MINIMALQUANT:
						{
							int min;
							int max;
							bool greedy = false;
							switch (op)
							{
								case REOP_STAR:
								{
									greedy = true;
									goto case REOP_MINIMALSTAR;
								}

								case REOP_MINIMALSTAR:
								{
									// fallthrough
									min = 0;
									max = -1;
									break;
								}

								case REOP_PLUS:
								{
									greedy = true;
									goto case REOP_MINIMALPLUS;
								}

								case REOP_MINIMALPLUS:
								{
									// fallthrough
									min = 1;
									max = -1;
									break;
								}

								case REOP_OPT:
								{
									greedy = true;
									goto case REOP_MINIMALOPT;
								}

								case REOP_MINIMALOPT:
								{
									// fallthrough
									min = 0;
									max = 1;
									break;
								}

								case REOP_QUANT:
								{
									greedy = true;
									goto case REOP_MINIMALQUANT;
								}

								case REOP_MINIMALQUANT:
								{
									// fallthrough
									min = GetOffset(program, pc);
									pc += INDEX_LEN;
									// See comments in emitREBytecode for " - 1" reason
									max = GetOffset(program, pc) - 1;
									pc += INDEX_LEN;
									break;
								}

								default:
								{
									throw Kit.CodeBug();
								}
							}
							PushProgState(gData, min, max, gData.cp, null, continuationOp, continuationPc);
							if (greedy)
							{
								PushBackTrackState(gData, REOP_REPEAT, pc);
								continuationOp = REOP_REPEAT;
								continuationPc = pc;
								pc += 3 * INDEX_LEN;
								op = program[pc++];
							}
							else
							{
								if (min != 0)
								{
									continuationOp = REOP_MINIMALREPEAT;
									continuationPc = pc;
									pc += 3 * INDEX_LEN;
									op = program[pc++];
								}
								else
								{
									PushBackTrackState(gData, REOP_MINIMALREPEAT, pc);
									PopProgState(gData);
									pc += 2 * INDEX_LEN;
									// <parencount> & <parenindex>
									pc = pc + GetOffset(program, pc);
									op = program[pc++];
								}
							}
							continue;
						}

						case REOP_ENDCHILD:
						{
							// If we have not gotten a result here, it is because of an
							// empty match.  Do the same thing REOP_EMPTY would do.
							result = true;
							// Use the current continuation.
							pc = continuationPc;
							op = continuationOp;
							continue;
						}

						case REOP_REPEAT:
						{
							int nextpc;
							int nextop;
							do
							{
								REProgState state = PopProgState(gData);
								if (!result)
								{
									// Failed, see if we have enough children.
									if (state.min == 0)
									{
										result = true;
									}
									continuationPc = state.continuationPc;
									continuationOp = state.continuationOp;
									pc += 2 * INDEX_LEN;
									pc += GetOffset(program, pc);
									goto switchStatement_break;
								}
								if (state.min == 0 && gData.cp == state.index)
								{
									// matched an empty string, that'll get us nowhere
									result = false;
									continuationPc = state.continuationPc;
									continuationOp = state.continuationOp;
									pc += 2 * INDEX_LEN;
									pc += GetOffset(program, pc);
									goto switchStatement_break;
								}
								int new_min = state.min;
								int new_max = state.max;
								if (new_min != 0)
								{
									new_min--;
								}
								if (new_max != -1)
								{
									new_max--;
								}
								if (new_max == 0)
								{
									result = true;
									continuationPc = state.continuationPc;
									continuationOp = state.continuationOp;
									pc += 2 * INDEX_LEN;
									pc += GetOffset(program, pc);
									goto switchStatement_break;
								}
								nextpc = pc + 3 * INDEX_LEN;
								nextop = program[nextpc];
								int startcp = gData.cp;
								if (ReopIsSimple(nextop))
								{
									nextpc++;
									int match = SimpleMatch(gData, input, nextop, program, nextpc, end, true);
									if (match < 0)
									{
										result = (new_min == 0);
										continuationPc = state.continuationPc;
										continuationOp = state.continuationOp;
										pc += 2 * INDEX_LEN;
										pc += GetOffset(program, pc);
										goto switchStatement_break;
									}
									result = true;
									nextpc = match;
								}
								continuationOp = REOP_REPEAT;
								continuationPc = pc;
								PushProgState(gData, new_min, new_max, startcp, null, state.continuationOp, state.continuationPc);
								if (new_min == 0)
								{
									PushBackTrackState(gData, REOP_REPEAT, pc, startcp, state.continuationOp, state.continuationPc);
									int parenCount = GetIndex(program, pc);
									int parenIndex = GetIndex(program, pc + INDEX_LEN);
									for (int k = 0; k < parenCount; k++)
									{
										gData.SetParens(parenIndex + k, -1, 0);
									}
								}
							}
							while (program[nextpc] == REOP_ENDCHILD);
							pc = nextpc;
							op = program[pc++];
							continue;
						}

						case REOP_MINIMALREPEAT:
						{
							REProgState state = PopProgState(gData);
							if (!result)
							{
								//
								// Non-greedy failure - try to consume another child.
								//
								if (state.max == -1 || state.max > 0)
								{
									PushProgState(gData, state.min, state.max, gData.cp, null, state.continuationOp, state.continuationPc);
									continuationOp = REOP_MINIMALREPEAT;
									continuationPc = pc;
									int parenCount = GetIndex(program, pc);
									pc += INDEX_LEN;
									int parenIndex = GetIndex(program, pc);
									pc += 2 * INDEX_LEN;
									for (int k = 0; k < parenCount; k++)
									{
										gData.SetParens(parenIndex + k, -1, 0);
									}
									op = program[pc++];
									continue;
								}
								else
								{
									// Don't need to adjust pc since we're going to pop.
									continuationPc = state.continuationPc;
									continuationOp = state.continuationOp;
									break;
								}
							}
							else
							{
								if (state.min == 0 && gData.cp == state.index)
								{
									// Matched an empty string, that'll get us nowhere.
									result = false;
									continuationPc = state.continuationPc;
									continuationOp = state.continuationOp;
									break;
								}
								int new_min = state.min;
								int new_max = state.max;
								if (new_min != 0)
								{
									new_min--;
								}
								if (new_max != -1)
								{
									new_max--;
								}
								PushProgState(gData, new_min, new_max, gData.cp, null, state.continuationOp, state.continuationPc);
								if (new_min != 0)
								{
									continuationOp = REOP_MINIMALREPEAT;
									continuationPc = pc;
									int parenCount = GetIndex(program, pc);
									pc += INDEX_LEN;
									int parenIndex = GetIndex(program, pc);
									pc += 2 * INDEX_LEN;
									for (int k = 0; k < parenCount; k++)
									{
										gData.SetParens(parenIndex + k, -1, 0);
									}
									op = program[pc++];
								}
								else
								{
									continuationPc = state.continuationPc;
									continuationOp = state.continuationOp;
									PushBackTrackState(gData, REOP_MINIMALREPEAT, pc);
									PopProgState(gData);
									pc += 2 * INDEX_LEN;
									pc = pc + GetOffset(program, pc);
									op = program[pc++];
								}
								continue;
							}
							goto case REOP_END;
						}

						case REOP_END:
						{
							return true;
						}

						default:
						{
							throw Kit.CodeBug("invalid bytecode");
						}
					}
switchStatement_break: ;
				}
				if (!result)
				{
					REBackTrackData backTrackData = gData.backTrackStackTop;
					if (backTrackData != null)
					{
						gData.backTrackStackTop = backTrackData.previous;
						gData.parens = backTrackData.parens;
						gData.cp = backTrackData.cp;
						gData.stateStackTop = backTrackData.stateStackTop;
						continuationOp = backTrackData.continuationOp;
						continuationPc = backTrackData.continuationPc;
						pc = backTrackData.pc;
						op = backTrackData.op;
						continue;
					}
					else
					{
						return false;
					}
				}
				op = program[pc++];
			}
		}

		private static bool MatchRegExp(REGlobalData gData, RECompiled re, string input, int start, int end, bool multiline)
		{
			if (re.parenCount != 0)
			{
				gData.parens = new long[re.parenCount];
			}
			else
			{
				gData.parens = null;
			}
			gData.backTrackStackTop = null;
			gData.stateStackTop = null;
			gData.multiline = multiline || (re.flags & JSREG_MULTILINE) != 0;
			gData.regexp = re;
			int anchorCh = gData.regexp.anchorCh;
			//
			// have to include the position beyond the last character
			//  in order to detect end-of-input/line condition
			//
			for (int i = start; i <= end; ++i)
			{
				//
				// If the first node is a literal match, step the index into
				// the string until that match is made, or fail if it can't be
				// found at all.
				//
				if (anchorCh >= 0)
				{
					for (; ; )
					{
						if (i == end)
						{
							return false;
						}
						char matchCh = input[i];
						if (matchCh == anchorCh || ((gData.regexp.flags & JSREG_FOLD) != 0 && Upcase(matchCh) == Upcase((char)anchorCh)))
						{
							break;
						}
						++i;
					}
				}
				gData.cp = i;
				gData.skipped = i - start;
				for (int j = 0; j < re.parenCount; j++)
				{
					gData.parens[j] = -1l;
				}
				bool result = ExecuteREBytecode(gData, input, end);
				gData.backTrackStackTop = null;
				gData.stateStackTop = null;
				if (result)
				{
					return true;
				}
				if (anchorCh == ANCHOR_BOL && !gData.multiline)
				{
					gData.skipped = end;
					return false;
				}
				i = start + gData.skipped;
			}
			return false;
		}

		internal virtual object ExecuteRegExp(Context cx, Scriptable scope, RegExpImpl res, string str, int[] indexp, int matchType)
		{
			REGlobalData gData = new REGlobalData();
			int start = indexp[0];
			int end = str.Length;
			if (start > end)
			{
				start = end;
			}
			//
			// Call the recursive matcher to do the real work.
			//
			bool matches = MatchRegExp(gData, re, str, start, end, res.multiline);
			if (!matches)
			{
				if (matchType != PREFIX)
				{
					return null;
				}
				return Undefined.instance;
			}
			int index = gData.cp;
			int ep = indexp[0] = index;
			int matchlen = ep - (start + gData.skipped);
			index -= matchlen;
			object result;
			Scriptable obj;
			if (matchType == TEST)
			{
				result = true;
				obj = null;
			}
			else
			{
				result = cx.NewArray(scope, 0);
				obj = (Scriptable)result;
				string matchstr = Sharpen.Runtime.Substring(str, index, index + matchlen);
				obj.Put(0, obj, matchstr);
			}
			if (re.parenCount == 0)
			{
				res.parens = null;
				res.lastParen = SubString.emptySubString;
			}
			else
			{
				SubString parsub = null;
				int num;
				res.parens = new SubString[re.parenCount];
				for (num = 0; num < re.parenCount; num++)
				{
					int cap_index = gData.ParensIndex(num);
					string parstr;
					if (cap_index != -1)
					{
						int cap_length = gData.ParensLength(num);
						parsub = new SubString(str, cap_index, cap_length);
						res.parens[num] = parsub;
						if (matchType != TEST)
						{
							obj.Put(num + 1, obj, parsub.ToString());
						}
					}
					else
					{
						if (matchType != TEST)
						{
							obj.Put(num + 1, obj, Undefined.instance);
						}
					}
				}
				res.lastParen = parsub;
			}
			if (!(matchType == TEST))
			{
				obj.Put("index", obj, Sharpen.Extensions.ValueOf(start + gData.skipped));
				obj.Put("input", obj, str);
			}
			if (res.lastMatch == null)
			{
				res.lastMatch = new SubString();
				res.leftContext = new SubString();
				res.rightContext = new SubString();
			}
			res.lastMatch.str = str;
			res.lastMatch.index = index;
			res.lastMatch.length = matchlen;
			res.leftContext.str = str;
			if (cx.GetLanguageVersion() == Context.VERSION_1_2)
			{
				res.leftContext.index = start;
				res.leftContext.length = gData.skipped;
			}
			else
			{
				res.leftContext.index = 0;
				res.leftContext.length = start + gData.skipped;
			}
			res.rightContext.str = str;
			res.rightContext.index = ep;
			res.rightContext.length = end - ep;
			return result;
		}

		internal virtual int GetFlags()
		{
			return re.flags;
		}

		private static void ReportWarning(Context cx, string messageId, string arg)
		{
			if (cx.HasFeature(Context.FEATURE_STRICT_MODE))
			{
				string msg = ScriptRuntime.GetMessage1(messageId, arg);
				Context.ReportWarning(msg);
			}
		}

		private static void ReportError(string messageId, string arg)
		{
			string msg = ScriptRuntime.GetMessage1(messageId, arg);
			throw ScriptRuntime.ConstructError("SyntaxError", msg);
		}

		private const int Id_lastIndex = 1;

		private const int Id_source = 2;

		private const int Id_global = 3;

		private const int Id_ignoreCase = 4;

		private const int Id_multiline = 5;

		private const int MAX_INSTANCE_ID = 5;

		// #string_id_map#
		protected internal override int GetMaxInstanceId()
		{
			return MAX_INSTANCE_ID;
		}

		protected internal override int FindInstanceIdInfo(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:16:24 EDT
			id = 0;
			string X = null;
			int c;
			int s_length = s.Length;
			if (s_length == 6)
			{
				c = s[0];
				if (c == 'g')
				{
					X = "global";
					id = Id_global;
				}
				else
				{
					if (c == 's')
					{
						X = "source";
						id = Id_source;
					}
				}
			}
			else
			{
				if (s_length == 9)
				{
					c = s[0];
					if (c == 'l')
					{
						X = "lastIndex";
						id = Id_lastIndex;
					}
					else
					{
						if (c == 'm')
						{
							X = "multiline";
							id = Id_multiline;
						}
					}
				}
				else
				{
					if (s_length == 10)
					{
						X = "ignoreCase";
						id = Id_ignoreCase;
					}
				}
			}
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			// #/string_id_map#
			if (id == 0)
			{
				return base.FindInstanceIdInfo(s);
			}
			int attr;
			switch (id)
			{
				case Id_lastIndex:
				{
					attr = PERMANENT | DONTENUM;
					break;
				}

				case Id_source:
				case Id_global:
				case Id_ignoreCase:
				case Id_multiline:
				{
					attr = PERMANENT | READONLY | DONTENUM;
					break;
				}

				default:
				{
					throw new InvalidOperationException();
				}
			}
			return InstanceIdInfo(attr, id);
		}

		protected internal override string GetInstanceIdName(int id)
		{
			switch (id)
			{
				case Id_lastIndex:
				{
					return "lastIndex";
				}

				case Id_source:
				{
					return "source";
				}

				case Id_global:
				{
					return "global";
				}

				case Id_ignoreCase:
				{
					return "ignoreCase";
				}

				case Id_multiline:
				{
					return "multiline";
				}
			}
			return base.GetInstanceIdName(id);
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			switch (id)
			{
				case Id_lastIndex:
				{
					return ScriptRuntime.WrapNumber(lastIndex);
				}

				case Id_source:
				{
					return new string(re.source);
				}

				case Id_global:
				{
					return ScriptRuntime.WrapBoolean((re.flags & JSREG_GLOB) != 0);
				}

				case Id_ignoreCase:
				{
					return ScriptRuntime.WrapBoolean((re.flags & JSREG_FOLD) != 0);
				}

				case Id_multiline:
				{
					return ScriptRuntime.WrapBoolean((re.flags & JSREG_MULTILINE) != 0);
				}
			}
			return base.GetInstanceIdValue(id);
		}

		protected internal override void SetInstanceIdValue(int id, object value)
		{
			switch (id)
			{
				case Id_lastIndex:
				{
					lastIndex = ScriptRuntime.ToNumber(value);
					return;
				}

				case Id_source:
				case Id_global:
				case Id_ignoreCase:
				case Id_multiline:
				{
					return;
				}
			}
			base.SetInstanceIdValue(id, value);
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_compile:
				{
					arity = 1;
					s = "compile";
					break;
				}

				case Id_toString:
				{
					arity = 0;
					s = "toString";
					break;
				}

				case Id_toSource:
				{
					arity = 0;
					s = "toSource";
					break;
				}

				case Id_exec:
				{
					arity = 1;
					s = "exec";
					break;
				}

				case Id_test:
				{
					arity = 1;
					s = "test";
					break;
				}

				case Id_prefix:
				{
					arity = 1;
					s = "prefix";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(REGEXP_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(REGEXP_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_compile:
				{
					return RealThis(thisObj, f).Compile(cx, scope, args);
				}

				case Id_toString:
				case Id_toSource:
				{
					return RealThis(thisObj, f).ToString();
				}

				case Id_exec:
				{
					return RealThis(thisObj, f).ExecSub(cx, scope, args, MATCH);
				}

				case Id_test:
				{
					object x = RealThis(thisObj, f).ExecSub(cx, scope, args, TEST);
					return true.Equals(x) ? true : false;
				}

				case Id_prefix:
				{
					return RealThis(thisObj, f).ExecSub(cx, scope, args, PREFIX);
				}
			}
			throw new ArgumentException(id.ToString());
		}

		private static NativeRegExp RealThis(Scriptable thisObj, IdFunctionObject f)
		{
			if (!(thisObj is NativeRegExp))
			{
				throw IncompatibleCallError(f);
			}
			return (NativeRegExp)thisObj;
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:16:24 EDT
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 4:
				{
					c = s[0];
					if (c == 'e')
					{
						X = "exec";
						id = Id_exec;
					}
					else
					{
						if (c == 't')
						{
							X = "test";
							id = Id_test;
						}
					}
					goto L_break;
				}

				case 6:
				{
					X = "prefix";
					id = Id_prefix;
					goto L_break;
				}

				case 7:
				{
					X = "compile";
					id = Id_compile;
					goto L_break;
				}

				case 8:
				{
					c = s[3];
					if (c == 'o')
					{
						X = "toSource";
						id = Id_toSource;
					}
					else
					{
						if (c == 't')
						{
							X = "toString";
							id = Id_toString;
						}
					}
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		private const int Id_compile = 1;

		private const int Id_toString = 2;

		private const int Id_toSource = 3;

		private const int Id_exec = 4;

		private const int Id_test = 5;

		private const int Id_prefix = 6;

		private const int MAX_PROTOTYPE_ID = 6;

		private RECompiled re;

		internal double lastIndex;
		// #/string_id_map#
	}

	[System.Serializable]
	internal class RECompiled
	{
		internal const long serialVersionUID = -6144956577595844213L;

		internal readonly char[] source;

		internal int parenCount;

		internal int flags;

		internal byte[] program;

		internal int classCount;

		internal RECharSet[] classList;

		internal int anchorCh = -1;

		internal RECompiled(string str)
		{
			// class NativeRegExp
			this.source = str.ToCharArray();
		}
	}

	internal class RENode
	{
		internal RENode(byte op)
		{
			this.op = op;
		}

		internal byte op;

		internal RENode next;

		internal RENode kid;

		internal RENode kid2;

		internal int parenIndex;

		internal int min;

		internal int max;

		internal int parenCount;

		internal bool greedy;

		internal int startIndex;

		internal int kidlen;

		internal int bmsize;

		internal int index;

		internal bool sense;

		internal char chr;

		internal int length;

		internal int flatIndex;
	}

	internal class CompilerState
	{
		internal CompilerState(Context cx, char[] source, int length, int flags)
		{
			this.cx = cx;
			this.cpbegin = source;
			this.cp = 0;
			this.cpend = length;
			this.flags = flags;
			this.parenCount = 0;
			this.classCount = 0;
			this.progLength = 0;
		}

		internal Context cx;

		internal char[] cpbegin;

		internal int cpend;

		internal int cp;

		internal int flags;

		internal int parenCount;

		internal int parenNesting;

		internal int classCount;

		internal int progLength;

		internal RENode result;
	}

	internal class REProgState
	{
		internal REProgState(REProgState previous, int min, int max, int index, REBackTrackData backTrack, int continuationOp, int continuationPc)
		{
			this.previous = previous;
			this.min = min;
			this.max = max;
			this.index = index;
			this.continuationOp = continuationOp;
			this.continuationPc = continuationPc;
			this.backTrack = backTrack;
		}

		internal readonly REProgState previous;

		internal readonly int min;

		internal readonly int max;

		internal readonly int index;

		internal readonly int continuationOp;

		internal readonly int continuationPc;

		internal readonly REBackTrackData backTrack;
		// previous state in stack
		// used by ASSERT_  to recover state
	}

	internal class REBackTrackData
	{
		internal REBackTrackData(REGlobalData gData, int op, int pc, int cp, int continuationOp, int continuationPc)
		{
			previous = gData.backTrackStackTop;
			this.op = op;
			this.pc = pc;
			this.cp = cp;
			this.continuationOp = continuationOp;
			this.continuationPc = continuationPc;
			parens = gData.parens;
			stateStackTop = gData.stateStackTop;
		}

		internal readonly REBackTrackData previous;

		internal readonly int op;

		internal readonly int pc;

		internal readonly int cp;

		internal readonly int continuationOp;

		internal readonly int continuationPc;

		internal readonly long[] parens;

		internal readonly REProgState stateStackTop;
	}

	internal class REGlobalData
	{
		internal bool multiline;

		internal RECompiled regexp;

		internal int skipped;

		internal int cp;

		internal long[] parens;

		internal REProgState stateStackTop;

		internal REBackTrackData backTrackStackTop;

		/// <summary>Get start of parenthesis capture contents, -1 for empty.</summary>
		/// <remarks>Get start of parenthesis capture contents, -1 for empty.</remarks>
		internal virtual int ParensIndex(int i)
		{
			return (int)(parens[i]);
		}

		/// <summary>Get length of parenthesis capture contents.</summary>
		/// <remarks>Get length of parenthesis capture contents.</remarks>
		internal virtual int ParensLength(int i)
		{
			return (int)((long)(((ulong)parens[i]) >> 32));
		}

		internal virtual void SetParens(int i, int index, int length)
		{
			// clone parens array if it is shared with backtrack state
			if (backTrackStackTop != null && backTrackStackTop.parens == parens)
			{
				parens = parens.Clone();
			}
			parens[i] = (index & unchecked((long)(0xffffffffL))) | ((long)length << 32);
		}
	}

	[System.Serializable]
	internal sealed class RECharSet
	{
		internal const long serialVersionUID = 7931787979395898394L;

		internal RECharSet(int length, int startIndex, int strlength, bool sense)
		{
			this.length = length;
			this.startIndex = startIndex;
			this.strlength = strlength;
			this.sense = sense;
		}

		internal readonly int length;

		internal readonly int startIndex;

		internal readonly int strlength;

		internal readonly bool sense;

		[System.NonSerialized]
		internal volatile bool converted;

		[System.NonSerialized]
		internal volatile byte[] bits;
	}
}
