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
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the String native object.</summary>
	/// <remarks>
	/// This class implements the String native object.
	/// See ECMA 15.5.
	/// String methods for dealing with regular expressions are
	/// ported directly from C. Latest port is from version 1.40.12.19
	/// in the JSFUN13_BRANCH.
	/// </remarks>
	/// <author>Mike McCabe</author>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	internal sealed class NativeString : IdScriptableObject
	{
		internal const long serialVersionUID = 920268368584188687L;

		private static readonly object STRING_TAG = "String";

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeString obj = new Rhino.NativeString(string.Empty);
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		internal NativeString(string s)
		{
			@string = s;
		}

		public override string GetClassName()
		{
			return "String";
		}

		private const int Id_length = 1;

		private const int MAX_INSTANCE_ID = 1;

		protected internal override int GetMaxInstanceId()
		{
			return MAX_INSTANCE_ID;
		}

		protected internal override int FindInstanceIdInfo(string s)
		{
			if (s.Equals("length"))
			{
				return InstanceIdInfo(DONTENUM | READONLY | PERMANENT, Id_length);
			}
			return base.FindInstanceIdInfo(s);
		}

		protected internal override string GetInstanceIdName(int id)
		{
			if (id == Id_length)
			{
				return "length";
			}
			return base.GetInstanceIdName(id);
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			if (id == Id_length)
			{
				return ScriptRuntime.WrapInt(@string.Length);
			}
			return base.GetInstanceIdValue(id);
		}

		protected internal override void FillConstructorProperties(IdFunctionObject ctor)
		{
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_fromCharCode, "fromCharCode", 1);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_charAt, "charAt", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_charCodeAt, "charCodeAt", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_indexOf, "indexOf", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_lastIndexOf, "lastIndexOf", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_split, "split", 3);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_substring, "substring", 3);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_toLowerCase, "toLowerCase", 1);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_toUpperCase, "toUpperCase", 1);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_substr, "substr", 3);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_concat, "concat", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_slice, "slice", 3);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_equalsIgnoreCase, "equalsIgnoreCase", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_match, "match", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_search, "search", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_replace, "replace", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_localeCompare, "localeCompare", 2);
			AddIdFunctionProperty(ctor, STRING_TAG, ConstructorId_toLocaleLowerCase, "toLocaleLowerCase", 1);
			base.FillConstructorProperties(ctor);
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 1;
					s = "constructor";
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

				case Id_valueOf:
				{
					arity = 0;
					s = "valueOf";
					break;
				}

				case Id_charAt:
				{
					arity = 1;
					s = "charAt";
					break;
				}

				case Id_charCodeAt:
				{
					arity = 1;
					s = "charCodeAt";
					break;
				}

				case Id_indexOf:
				{
					arity = 1;
					s = "indexOf";
					break;
				}

				case Id_lastIndexOf:
				{
					arity = 1;
					s = "lastIndexOf";
					break;
				}

				case Id_split:
				{
					arity = 2;
					s = "split";
					break;
				}

				case Id_substring:
				{
					arity = 2;
					s = "substring";
					break;
				}

				case Id_toLowerCase:
				{
					arity = 0;
					s = "toLowerCase";
					break;
				}

				case Id_toUpperCase:
				{
					arity = 0;
					s = "toUpperCase";
					break;
				}

				case Id_substr:
				{
					arity = 2;
					s = "substr";
					break;
				}

				case Id_concat:
				{
					arity = 1;
					s = "concat";
					break;
				}

				case Id_slice:
				{
					arity = 2;
					s = "slice";
					break;
				}

				case Id_bold:
				{
					arity = 0;
					s = "bold";
					break;
				}

				case Id_italics:
				{
					arity = 0;
					s = "italics";
					break;
				}

				case Id_fixed:
				{
					arity = 0;
					s = "fixed";
					break;
				}

				case Id_strike:
				{
					arity = 0;
					s = "strike";
					break;
				}

				case Id_small:
				{
					arity = 0;
					s = "small";
					break;
				}

				case Id_big:
				{
					arity = 0;
					s = "big";
					break;
				}

				case Id_blink:
				{
					arity = 0;
					s = "blink";
					break;
				}

				case Id_sup:
				{
					arity = 0;
					s = "sup";
					break;
				}

				case Id_sub:
				{
					arity = 0;
					s = "sub";
					break;
				}

				case Id_fontsize:
				{
					arity = 0;
					s = "fontsize";
					break;
				}

				case Id_fontcolor:
				{
					arity = 0;
					s = "fontcolor";
					break;
				}

				case Id_link:
				{
					arity = 0;
					s = "link";
					break;
				}

				case Id_anchor:
				{
					arity = 0;
					s = "anchor";
					break;
				}

				case Id_equals:
				{
					arity = 1;
					s = "equals";
					break;
				}

				case Id_equalsIgnoreCase:
				{
					arity = 1;
					s = "equalsIgnoreCase";
					break;
				}

				case Id_match:
				{
					arity = 1;
					s = "match";
					break;
				}

				case Id_search:
				{
					arity = 1;
					s = "search";
					break;
				}

				case Id_replace:
				{
					arity = 1;
					s = "replace";
					break;
				}

				case Id_localeCompare:
				{
					arity = 1;
					s = "localeCompare";
					break;
				}

				case Id_toLocaleLowerCase:
				{
					arity = 0;
					s = "toLocaleLowerCase";
					break;
				}

				case Id_toLocaleUpperCase:
				{
					arity = 0;
					s = "toLocaleUpperCase";
					break;
				}

				case Id_trim:
				{
					arity = 0;
					s = "trim";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(STRING_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(STRING_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			for (; ; )
			{
				switch (id)
				{
					case ConstructorId_charAt:
					case ConstructorId_charCodeAt:
					case ConstructorId_indexOf:
					case ConstructorId_lastIndexOf:
					case ConstructorId_split:
					case ConstructorId_substring:
					case ConstructorId_toLowerCase:
					case ConstructorId_toUpperCase:
					case ConstructorId_substr:
					case ConstructorId_concat:
					case ConstructorId_slice:
					case ConstructorId_equalsIgnoreCase:
					case ConstructorId_match:
					case ConstructorId_search:
					case ConstructorId_replace:
					case ConstructorId_localeCompare:
					case ConstructorId_toLocaleLowerCase:
					{
						if (args.Length > 0)
						{
							thisObj = ScriptRuntime.ToObject(scope, ScriptRuntime.ToCharSequence(args[0]));
							object[] newArgs = new object[args.Length - 1];
							for (int i = 0; i < newArgs.Length; i++)
							{
								newArgs[i] = args[i + 1];
							}
							args = newArgs;
						}
						else
						{
							thisObj = ScriptRuntime.ToObject(scope, ScriptRuntime.ToCharSequence(thisObj));
						}
						id = -id;
						goto again_continue;
					}

					case ConstructorId_fromCharCode:
					{
						int N = args.Length;
						if (N < 1)
						{
							return string.Empty;
						}
						StringBuilder sb = new StringBuilder(N);
						for (int i = 0; i != N; ++i)
						{
							sb.Append(ScriptRuntime.ToUint16(args[i]));
						}
						return sb.ToString();
					}

					case Id_constructor:
					{
						string s = (args.Length >= 1) ? ScriptRuntime.ToCharSequence(args[0]) : string.Empty;
						if (thisObj == null)
						{
							// new String(val) creates a new String object.
							return new Rhino.NativeString(s);
						}
						// String(val) converts val to a string value.
						return s is string ? s : s.ToString();
					}

					case Id_toString:
					case Id_valueOf:
					{
						// ECMA 15.5.4.2: 'the toString function is not generic.
						string cs = RealThis(thisObj, f).@string;
						return cs is string ? cs : cs.ToString();
					}

					case Id_toSource:
					{
						string s = RealThis(thisObj, f).@string;
						return "(new String(\"" + ScriptRuntime.EscapeString(s.ToString()) + "\"))";
					}

					case Id_charAt:
					case Id_charCodeAt:
					{
						// See ECMA 15.5.4.[4,5]
						string target = ScriptRuntime.ToCharSequence(thisObj);
						double pos = ScriptRuntime.ToInteger(args, 0);
						if (pos < 0 || pos >= target.Length)
						{
							if (id == Id_charAt)
							{
								return string.Empty;
							}
							else
							{
								return ScriptRuntime.NaN;
							}
						}
						char c = target[(int)pos];
						if (id == Id_charAt)
						{
							return c.ToString();
						}
						else
						{
							return ScriptRuntime.WrapInt(c);
						}
						goto case Id_indexOf;
					}

					case Id_indexOf:
					{
						return ScriptRuntime.WrapInt(Js_indexOf(ScriptRuntime.ToString(thisObj), args));
					}

					case Id_lastIndexOf:
					{
						return ScriptRuntime.WrapInt(Js_lastIndexOf(ScriptRuntime.ToString(thisObj), args));
					}

					case Id_split:
					{
						return ScriptRuntime.CheckRegExpProxy(cx).Js_split(cx, scope, ScriptRuntime.ToString(thisObj), args);
					}

					case Id_substring:
					{
						return Js_substring(cx, ScriptRuntime.ToCharSequence(thisObj), args);
					}

					case Id_toLowerCase:
					{
						// See ECMA 15.5.4.11
						return ScriptRuntime.ToString(thisObj).ToLower(ScriptRuntime.ROOT_LOCALE);
					}

					case Id_toUpperCase:
					{
						// See ECMA 15.5.4.12
						return ScriptRuntime.ToString(thisObj).ToUpper(ScriptRuntime.ROOT_LOCALE);
					}

					case Id_substr:
					{
						return Js_substr(ScriptRuntime.ToCharSequence(thisObj), args);
					}

					case Id_concat:
					{
						return Js_concat(ScriptRuntime.ToString(thisObj), args);
					}

					case Id_slice:
					{
						return Js_slice(ScriptRuntime.ToCharSequence(thisObj), args);
					}

					case Id_bold:
					{
						return Tagify(thisObj, "b", null, null);
					}

					case Id_italics:
					{
						return Tagify(thisObj, "i", null, null);
					}

					case Id_fixed:
					{
						return Tagify(thisObj, "tt", null, null);
					}

					case Id_strike:
					{
						return Tagify(thisObj, "strike", null, null);
					}

					case Id_small:
					{
						return Tagify(thisObj, "small", null, null);
					}

					case Id_big:
					{
						return Tagify(thisObj, "big", null, null);
					}

					case Id_blink:
					{
						return Tagify(thisObj, "blink", null, null);
					}

					case Id_sup:
					{
						return Tagify(thisObj, "sup", null, null);
					}

					case Id_sub:
					{
						return Tagify(thisObj, "sub", null, null);
					}

					case Id_fontsize:
					{
						return Tagify(thisObj, "font", "size", args);
					}

					case Id_fontcolor:
					{
						return Tagify(thisObj, "font", "color", args);
					}

					case Id_link:
					{
						return Tagify(thisObj, "a", "href", args);
					}

					case Id_anchor:
					{
						return Tagify(thisObj, "a", "name", args);
					}

					case Id_equals:
					case Id_equalsIgnoreCase:
					{
						string s1 = ScriptRuntime.ToString(thisObj);
						string s2 = ScriptRuntime.ToString(args, 0);
						return ScriptRuntime.WrapBoolean((id == Id_equals) ? s1.Equals(s2) : s1.Equals(s2, StringComparison.CurrentCultureIgnoreCase));
					}

					case Id_match:
					case Id_search:
					case Id_replace:
					{
						int actionType;
						if (id == Id_match)
						{
							actionType = RegExpProxyConstants.RA_MATCH;
						}
						else
						{
							if (id == Id_search)
							{
								actionType = RegExpProxyConstants.RA_SEARCH;
							}
							else
							{
								actionType = RegExpProxyConstants.RA_REPLACE;
							}
						}
						return ScriptRuntime.CheckRegExpProxy(cx).Action(cx, scope, thisObj, args, actionType);
					}

					case Id_localeCompare:
					{
						// ECMA-262 1 5.5.4.9
						return (double) string.Compare(ScriptRuntime.ToString(thisObj), ScriptRuntime.ToString(args, 0), StringComparison.CurrentCulture);
					}

					case Id_toLocaleLowerCase:
					{
						return ScriptRuntime.ToString(thisObj).ToLower(cx.GetLocale());
					}

					case Id_toLocaleUpperCase:
					{
						return ScriptRuntime.ToString(thisObj).ToUpper(cx.GetLocale());
					}

					case Id_trim:
					{
						string str = ScriptRuntime.ToString(thisObj);
						char[] chars = str.ToCharArray();
						int start = 0;
						while (start < chars.Length && ScriptRuntime.IsJSWhitespaceOrLineTerminator(chars[start]))
						{
							start++;
						}
						int end = chars.Length;
						while (end > start && ScriptRuntime.IsJSWhitespaceOrLineTerminator(chars[end - 1]))
						{
							end--;
						}
						return str.Substring(start, end - start);
					}
				}
				throw new ArgumentException(id.ToString());
again_continue: ;
			}
again_break: ;
		}

		private static Rhino.NativeString RealThis(Scriptable thisObj, IdFunctionObject f)
		{
			if (!(thisObj is Rhino.NativeString))
			{
				throw IncompatibleCallError(f);
			}
			return (Rhino.NativeString)thisObj;
		}

		private static string Tagify(object thisObj, string tag, string attribute, object[] args)
		{
			string str = ScriptRuntime.ToString(thisObj);
			StringBuilder result = new StringBuilder();
			result.Append('<');
			result.Append(tag);
			if (attribute != null)
			{
				result.Append(' ');
				result.Append(attribute);
				result.Append("=\"");
				result.Append(ScriptRuntime.ToString(args, 0));
				result.Append('"');
			}
			result.Append('>');
			result.Append(str);
			result.Append("</");
			result.Append(tag);
			result.Append('>');
			return result.ToString();
		}

		public string ToCharSequence()
		{
			return @string;
		}

		public override string ToString()
		{
			return @string is string ? (string)@string : @string.ToString();
		}

		public override object Get(int index, Scriptable start)
		{
			if (0 <= index && index < @string.Length)
			{
				return @string[index].ToString();
			}
			return base.Get(index, start);
		}

		public override void Put(int index, Scriptable start, object value)
		{
			if (0 <= index && index < @string.Length)
			{
				return;
			}
			base.Put(index, start, value);
		}

		private static int Js_indexOf(string target, object[] args)
		{
			string search = ScriptRuntime.ToString(args, 0);
			double begin = ScriptRuntime.ToInteger(args, 1);
			if (begin > target.Length)
			{
				return -1;
			}
			else
			{
				if (begin < 0)
				{
					begin = 0;
				}
				return target.IndexOf(search, (int)begin);
			}
		}

		private static int Js_lastIndexOf(string target, object[] args)
		{
			string search = ScriptRuntime.ToString(args, 0);
			double end = ScriptRuntime.ToNumber(args, 1);
			if (Double.IsNaN(end) || end > target.Length)
			{
				end = target.Length;
			}
			else
			{
				if (end < 0)
				{
					end = 0;
				}
			}
			return target.LastIndexOf(search, (int)end);
		}

		private static string Js_substring(Context cx, string target, object[] args)
		{
			int length = target.Length;
			double start = ScriptRuntime.ToInteger(args, 0);
			double end;
			if (start < 0)
			{
				start = 0;
			}
			else
			{
				if (start > length)
				{
					start = length;
				}
			}
			if (args.Length <= 1 || args[1] == Undefined.instance)
			{
				end = length;
			}
			else
			{
				end = ScriptRuntime.ToInteger(args[1]);
				if (end < 0)
				{
					end = 0;
				}
				else
				{
					if (end > length)
					{
						end = length;
					}
				}
				// swap if end < start
				if (end < start)
				{
					if (cx.GetLanguageVersion() != Context.VERSION_1_2)
					{
						double temp = start;
						start = end;
						end = temp;
					}
					else
					{
						// Emulate old JDK1.0 java.lang.String.substring()
						end = start;
					}
				}
			}
			return target.Substring((int) start, (int) end - (int) start);
		}

		internal int GetLength()
		{
			return @string.Length;
		}

		private static string Js_substr(string target, object[] args)
		{
			if (args.Length < 1)
			{
				return target;
			}
			double begin = ScriptRuntime.ToInteger(args[0]);
			double end;
			int length = target.Length;
			if (begin < 0)
			{
				begin += length;
				if (begin < 0)
				{
					begin = 0;
				}
			}
			else
			{
				if (begin > length)
				{
					begin = length;
				}
			}
			if (args.Length == 1)
			{
				end = length;
			}
			else
			{
				end = ScriptRuntime.ToInteger(args[1]);
				if (end < 0)
				{
					end = 0;
				}
				end += begin;
				if (end > length)
				{
					end = length;
				}
			}
			return target.Substring((int)begin, (int)end - (int)begin);
		}

		private static string Js_concat(string target, object[] args)
		{
			int N = args.Length;
			if (N == 0)
			{
				return target;
			}
			else
			{
				if (N == 1)
				{
					string arg = ScriptRuntime.ToString(args[0]);
					return System.String.Concat(target, arg);
				}
			}
			// Find total capacity for the final string to avoid unnecessary
			// re-allocations in StringBuffer
			int size = target.Length;
			string[] argsAsStrings = new string[N];
			for (int i = 0; i != N; ++i)
			{
				string s = ScriptRuntime.ToString(args[i]);
				argsAsStrings[i] = s;
				size += s.Length;
			}
			StringBuilder result = new StringBuilder(size);
			result.Append(target);
			for (int i_1 = 0; i_1 != N; ++i_1)
			{
				result.Append(argsAsStrings[i_1]);
			}
			return result.ToString();
		}

		private static string Js_slice(string target, object[] args)
		{
			if (args.Length != 0)
			{
				double begin = ScriptRuntime.ToInteger(args[0]);
				double end;
				int length = target.Length;
				if (begin < 0)
				{
					begin += length;
					if (begin < 0)
					{
						begin = 0;
					}
				}
				else
				{
					if (begin > length)
					{
						begin = length;
					}
				}
				if (args.Length == 1)
				{
					end = length;
				}
				else
				{
					end = ScriptRuntime.ToInteger(args[1]);
					if (end < 0)
					{
						end += length;
						if (end < 0)
						{
							end = 0;
						}
					}
					else
					{
						if (end > length)
						{
							end = length;
						}
					}
					if (end < begin)
					{
						end = begin;
					}
				}
				return target.Substring((int) begin, (int) end - (int) begin);
			}
			return target;
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2009-07-23 07:32:39 EST
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 3:
				{
					c = s[2];
					if (c == 'b')
					{
						if (s[0] == 's' && s[1] == 'u')
						{
							id = Id_sub;
							goto L0_break;
						}
					}
					else
					{
						if (c == 'g')
						{
							if (s[0] == 'b' && s[1] == 'i')
							{
								id = Id_big;
								goto L0_break;
							}
						}
						else
						{
							if (c == 'p')
							{
								if (s[0] == 's' && s[1] == 'u')
								{
									id = Id_sup;
									goto L0_break;
								}
							}
						}
					}
					goto L_break;
				}

				case 4:
				{
					c = s[0];
					if (c == 'b')
					{
						X = "bold";
						id = Id_bold;
					}
					else
					{
						if (c == 'l')
						{
							X = "link";
							id = Id_link;
						}
						else
						{
							if (c == 't')
							{
								X = "trim";
								id = Id_trim;
							}
						}
					}
					goto L_break;
				}

				case 5:
				{
					switch (s[4])
					{
						case 'd':
						{
							X = "fixed";
							id = Id_fixed;
							goto L_break;
						}

						case 'e':
						{
							X = "slice";
							id = Id_slice;
							goto L_break;
						}

						case 'h':
						{
							X = "match";
							id = Id_match;
							goto L_break;
						}

						case 'k':
						{
							X = "blink";
							id = Id_blink;
							goto L_break;
						}

						case 'l':
						{
							X = "small";
							id = Id_small;
							goto L_break;
						}

						case 't':
						{
							X = "split";
							id = Id_split;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 6:
				{
					switch (s[1])
					{
						case 'e':
						{
							X = "search";
							id = Id_search;
							goto L_break;
						}

						case 'h':
						{
							X = "charAt";
							id = Id_charAt;
							goto L_break;
						}

						case 'n':
						{
							X = "anchor";
							id = Id_anchor;
							goto L_break;
						}

						case 'o':
						{
							X = "concat";
							id = Id_concat;
							goto L_break;
						}

						case 'q':
						{
							X = "equals";
							id = Id_equals;
							goto L_break;
						}

						case 't':
						{
							X = "strike";
							id = Id_strike;
							goto L_break;
						}

						case 'u':
						{
							X = "substr";
							id = Id_substr;
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
							X = "valueOf";
							id = Id_valueOf;
							goto L_break;
						}

						case 'e':
						{
							X = "replace";
							id = Id_replace;
							goto L_break;
						}

						case 'n':
						{
							X = "indexOf";
							id = Id_indexOf;
							goto L_break;
						}

						case 't':
						{
							X = "italics";
							id = Id_italics;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 8:
				{
					c = s[4];
					if (c == 'r')
					{
						X = "toString";
						id = Id_toString;
					}
					else
					{
						if (c == 's')
						{
							X = "fontsize";
							id = Id_fontsize;
						}
						else
						{
							if (c == 'u')
							{
								X = "toSource";
								id = Id_toSource;
							}
						}
					}
					goto L_break;
				}

				case 9:
				{
					c = s[0];
					if (c == 'f')
					{
						X = "fontcolor";
						id = Id_fontcolor;
					}
					else
					{
						if (c == 's')
						{
							X = "substring";
							id = Id_substring;
						}
					}
					goto L_break;
				}

				case 10:
				{
					X = "charCodeAt";
					id = Id_charCodeAt;
					goto L_break;
				}

				case 11:
				{
					switch (s[2])
					{
						case 'L':
						{
							X = "toLowerCase";
							id = Id_toLowerCase;
							goto L_break;
						}

						case 'U':
						{
							X = "toUpperCase";
							id = Id_toUpperCase;
							goto L_break;
						}

						case 'n':
						{
							X = "constructor";
							id = Id_constructor;
							goto L_break;
						}

						case 's':
						{
							X = "lastIndexOf";
							id = Id_lastIndexOf;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 13:
				{
					X = "localeCompare";
					id = Id_localeCompare;
					goto L_break;
				}

				case 16:
				{
					X = "equalsIgnoreCase";
					id = Id_equalsIgnoreCase;
					goto L_break;
				}

				case 17:
				{
					c = s[8];
					if (c == 'L')
					{
						X = "toLocaleLowerCase";
						id = Id_toLocaleLowerCase;
					}
					else
					{
						if (c == 'U')
						{
							X = "toLocaleUpperCase";
							id = Id_toLocaleUpperCase;
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

		private const int ConstructorId_fromCharCode = -1;

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toSource = 3;

		private const int Id_valueOf = 4;

		private const int Id_charAt = 5;

		private const int Id_charCodeAt = 6;

		private const int Id_indexOf = 7;

		private const int Id_lastIndexOf = 8;

		private const int Id_split = 9;

		private const int Id_substring = 10;

		private const int Id_toLowerCase = 11;

		private const int Id_toUpperCase = 12;

		private const int Id_substr = 13;

		private const int Id_concat = 14;

		private const int Id_slice = 15;

		private const int Id_bold = 16;

		private const int Id_italics = 17;

		private const int Id_fixed = 18;

		private const int Id_strike = 19;

		private const int Id_small = 20;

		private const int Id_big = 21;

		private const int Id_blink = 22;

		private const int Id_sup = 23;

		private const int Id_sub = 24;

		private const int Id_fontsize = 25;

		private const int Id_fontcolor = 26;

		private const int Id_link = 27;

		private const int Id_anchor = 28;

		private const int Id_equals = 29;

		private const int Id_equalsIgnoreCase = 30;

		private const int Id_match = 31;

		private const int Id_search = 32;

		private const int Id_replace = 33;

		private const int Id_localeCompare = 34;

		private const int Id_toLocaleLowerCase = 35;

		private const int Id_toLocaleUpperCase = 36;

		private const int Id_trim = 37;

		private const int MAX_PROTOTYPE_ID = Id_trim;

		private const int ConstructorId_charAt = -Id_charAt;

		private const int ConstructorId_charCodeAt = -Id_charCodeAt;

		private const int ConstructorId_indexOf = -Id_indexOf;

		private const int ConstructorId_lastIndexOf = -Id_lastIndexOf;

		private const int ConstructorId_split = -Id_split;

		private const int ConstructorId_substring = -Id_substring;

		private const int ConstructorId_toLowerCase = -Id_toLowerCase;

		private const int ConstructorId_toUpperCase = -Id_toUpperCase;

		private const int ConstructorId_substr = -Id_substr;

		private const int ConstructorId_concat = -Id_concat;

		private const int ConstructorId_slice = -Id_slice;

		private const int ConstructorId_equalsIgnoreCase = -Id_equalsIgnoreCase;

		private const int ConstructorId_match = -Id_match;

		private const int ConstructorId_search = -Id_search;

		private const int ConstructorId_replace = -Id_replace;

		private const int ConstructorId_localeCompare = -Id_localeCompare;

		private const int ConstructorId_toLocaleLowerCase = -Id_toLocaleLowerCase;

		private string @string;
		// #/string_id_map#
	}
}
