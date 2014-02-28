/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.RegExp;
using Sharpen;

namespace Rhino.RegExp
{
	/// <summary>This class implements the RegExp constructor native object.</summary>
	/// <remarks>
	/// This class implements the RegExp constructor native object.
	/// Revision History:
	/// Implementation in C by Brendan Eich
	/// Initial port to Java by Norris Boyd from jsregexp.c version 1.36
	/// Merged up to version 1.38, which included Unicode support.
	/// Merged bug fixes in version 1.39.
	/// Merged JSFUN13_BRANCH changes up to 1.32.2.11
	/// </remarks>
	/// <author>Brendan Eich</author>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	internal class NativeRegExpCtor : BaseFunction
	{
		internal NativeRegExpCtor()
		{
		}

		public override string GetFunctionName()
		{
			return "RegExp";
		}

		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (args.Length > 0 && args[0] is NativeRegExp && (args.Length == 1 || args[1] == Undefined.instance))
			{
				return args[0];
			}
			return Construct(cx, scope, args);
		}

		public override Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			NativeRegExp re = new NativeRegExp();
			re.Compile(cx, scope, args);
			ScriptRuntime.SetBuiltinProtoAndParent(re, scope, TopLevel.Builtins.RegExp);
			return re;
		}

		private static RegExpImpl GetImpl()
		{
			Context cx = Context.GetCurrentContext();
			return (RegExpImpl)ScriptRuntime.GetRegExpProxy(cx);
		}

		private const int Id_multiline = 1;

		private const int Id_STAR = 2;

		private const int Id_input = 3;

		private const int Id_UNDERSCORE = 4;

		private const int Id_lastMatch = 5;

		private const int Id_AMPERSAND = 6;

		private const int Id_lastParen = 7;

		private const int Id_PLUS = 8;

		private const int Id_leftContext = 9;

		private const int Id_BACK_QUOTE = 10;

		private const int Id_rightContext = 11;

		private const int Id_QUOTE = 12;

		private const int DOLLAR_ID_BASE = 12;

		private const int Id_DOLLAR_1 = DOLLAR_ID_BASE + 1;

		private const int Id_DOLLAR_2 = DOLLAR_ID_BASE + 2;

		private const int Id_DOLLAR_3 = DOLLAR_ID_BASE + 3;

		private const int Id_DOLLAR_4 = DOLLAR_ID_BASE + 4;

		private const int Id_DOLLAR_5 = DOLLAR_ID_BASE + 5;

		private const int Id_DOLLAR_6 = DOLLAR_ID_BASE + 6;

		private const int Id_DOLLAR_7 = DOLLAR_ID_BASE + 7;

		private const int Id_DOLLAR_8 = DOLLAR_ID_BASE + 8;

		private const int Id_DOLLAR_9 = DOLLAR_ID_BASE + 9;

		private const int MAX_INSTANCE_ID = DOLLAR_ID_BASE + 9;

		// #string_id_map#
		// #string=$*#
		// #string=$_#
		// #string=$&#
		// #string=$+#
		// #string=$`#
		// #string=$'#
		// #string=$1#
		// #string=$2#
		// #string=$3#
		// #string=$4#
		// #string=$5#
		// #string=$6#
		// #string=$7#
		// #string=$8#
		// #string=$9#
		protected internal override int GetMaxInstanceId()
		{
			return base.GetMaxInstanceId() + MAX_INSTANCE_ID;
		}

		protected internal override InstanceIdInfo FindInstanceIdInfo(string s)
		{
			int id;
			// #generated# Last update: 2001-05-24 16:09:31 GMT+02:00
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 2:
				{
					switch (s[1])
					{
						case '&':
						{
							if (s[0] == '$')
							{
								id = Id_AMPERSAND;
								goto L0_break;
							}
							goto L_break;
						}

						case '\'':
						{
							if (s[0] == '$')
							{
								id = Id_QUOTE;
								goto L0_break;
							}
							goto L_break;
						}

						case '*':
						{
							if (s[0] == '$')
							{
								id = Id_STAR;
								goto L0_break;
							}
							goto L_break;
						}

						case '+':
						{
							if (s[0] == '$')
							{
								id = Id_PLUS;
								goto L0_break;
							}
							goto L_break;
						}

						case '1':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_1;
								goto L0_break;
							}
							goto L_break;
						}

						case '2':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_2;
								goto L0_break;
							}
							goto L_break;
						}

						case '3':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_3;
								goto L0_break;
							}
							goto L_break;
						}

						case '4':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_4;
								goto L0_break;
							}
							goto L_break;
						}

						case '5':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_5;
								goto L0_break;
							}
							goto L_break;
						}

						case '6':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_6;
								goto L0_break;
							}
							goto L_break;
						}

						case '7':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_7;
								goto L0_break;
							}
							goto L_break;
						}

						case '8':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_8;
								goto L0_break;
							}
							goto L_break;
						}

						case '9':
						{
							if (s[0] == '$')
							{
								id = Id_DOLLAR_9;
								goto L0_break;
							}
							goto L_break;
						}

						case '_':
						{
							if (s[0] == '$')
							{
								id = Id_UNDERSCORE;
								goto L0_break;
							}
							goto L_break;
						}

						case '`':
						{
							if (s[0] == '$')
							{
								id = Id_BACK_QUOTE;
								goto L0_break;
							}
							goto L_break;
						}
					}
					goto L_break;
				}

				case 5:
				{
					X = "input";
					id = Id_input;
					goto L_break;
				}

				case 9:
				{
					c = s[4];
					if (c == 'M')
					{
						X = "lastMatch";
						id = Id_lastMatch;
					}
					else
					{
						if (c == 'P')
						{
							X = "lastParen";
							id = Id_lastParen;
						}
						else
						{
							if (c == 'i')
							{
								X = "multiline";
								id = Id_multiline;
							}
						}
					}
					goto L_break;
				}

				case 11:
				{
					X = "leftContext";
					id = Id_leftContext;
					goto L_break;
				}

				case 12:
				{
					X = "rightContext";
					id = Id_rightContext;
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
			if (id == 0)
			{
				return base.FindInstanceIdInfo(s);
			}
			PropertyAttributes attr;
			switch (id)
			{
				case Id_multiline:
				case Id_STAR:
				case Id_input:
				case Id_UNDERSCORE:
				{
					attr = PropertyAttributes.PERMANENT;
					break;
				}

				default:
				{
					attr = PropertyAttributes.PERMANENT | PropertyAttributes.READONLY;
					break;
				}
			}
			return InstanceIdInfo(attr, base.GetMaxInstanceId() + id);
		}

		// #/string_id_map#
		protected internal override string GetInstanceIdName(int id)
		{
			int shifted = id - base.GetMaxInstanceId();
			if (1 <= shifted && shifted <= MAX_INSTANCE_ID)
			{
				switch (shifted)
				{
					case Id_multiline:
					{
						return "multiline";
					}

					case Id_STAR:
					{
						return "$*";
					}

					case Id_input:
					{
						return "input";
					}

					case Id_UNDERSCORE:
					{
						return "$_";
					}

					case Id_lastMatch:
					{
						return "lastMatch";
					}

					case Id_AMPERSAND:
					{
						return "$&";
					}

					case Id_lastParen:
					{
						return "lastParen";
					}

					case Id_PLUS:
					{
						return "$+";
					}

					case Id_leftContext:
					{
						return "leftContext";
					}

					case Id_BACK_QUOTE:
					{
						return "$`";
					}

					case Id_rightContext:
					{
						return "rightContext";
					}

					case Id_QUOTE:
					{
						return "$'";
					}
				}
				// Must be one of $1..$9, convert to 0..8
				int substring_number = shifted - DOLLAR_ID_BASE - 1;
				char[] buf = new char[] { '$', (char)('1' + substring_number) };
				return new string(buf);
			}
			return base.GetInstanceIdName(id);
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			int shifted = id - base.GetMaxInstanceId();
			if (1 <= shifted && shifted <= MAX_INSTANCE_ID)
			{
				RegExpImpl impl = GetImpl();
				object stringResult;
				switch (shifted)
				{
					case Id_multiline:
					case Id_STAR:
					{
						return impl.multiline;
					}

					case Id_input:
					case Id_UNDERSCORE:
					{
						stringResult = impl.input;
						break;
					}

					case Id_lastMatch:
					case Id_AMPERSAND:
					{
						stringResult = impl.lastMatch;
						break;
					}

					case Id_lastParen:
					case Id_PLUS:
					{
						stringResult = impl.lastParen;
						break;
					}

					case Id_leftContext:
					case Id_BACK_QUOTE:
					{
						stringResult = impl.leftContext;
						break;
					}

					case Id_rightContext:
					case Id_QUOTE:
					{
						stringResult = impl.rightContext;
						break;
					}

					default:
					{
						// Must be one of $1..$9, convert to 0..8
						int substring_number = shifted - DOLLAR_ID_BASE - 1;
						stringResult = impl.GetParenSubString(substring_number);
						break;
					}
				}
				return (stringResult == null) ? string.Empty : stringResult.ToString();
			}
			return base.GetInstanceIdValue(id);
		}

		protected internal override void SetInstanceIdValue(int id, object value)
		{
			int shifted = id - base.GetMaxInstanceId();
			switch (shifted)
			{
				case Id_multiline:
				case Id_STAR:
				{
					GetImpl().multiline = ScriptRuntime.ToBoolean(value);
					return;
				}

				case Id_input:
				case Id_UNDERSCORE:
				{
					GetImpl().input = ScriptRuntime.ToString(value);
					return;
				}

				case Id_lastMatch:
				case Id_AMPERSAND:
				case Id_lastParen:
				case Id_PLUS:
				case Id_leftContext:
				case Id_BACK_QUOTE:
				case Id_rightContext:
				case Id_QUOTE:
				{
					return;
				}

				default:
				{
					int substring_number = shifted - DOLLAR_ID_BASE - 1;
					if (0 <= substring_number && substring_number <= 8)
					{
						return;
					}
					break;
				}
			}
			base.SetInstanceIdValue(id, value);
		}
	}
}
