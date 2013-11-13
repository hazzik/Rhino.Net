/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Rhino;
using Rhino.Regexp;
using Sharpen;

namespace Rhino.Regexp
{
	public class RegExpImpl : RegExpProxy
	{
		public virtual bool IsRegExp(Scriptable obj)
		{
			return obj is NativeRegExp;
		}

		public virtual object CompileRegExp(Context cx, string source, string flags)
		{
			return NativeRegExp.CompileRE(cx, source, flags, false);
		}

		public virtual Scriptable WrapRegExp(Context cx, Scriptable scope, object compiled)
		{
			return new NativeRegExp(scope, (RECompiled)compiled);
		}

		public virtual object Action(Context cx, Scriptable scope, Scriptable thisObj, object[] args, int actionType)
		{
			GlobData data = new GlobData();
			data.mode = actionType;
			switch (actionType)
			{
				case RegExpProxyConstants.RA_MATCH:
				{
					object rval;
					data.optarg = 1;
					rval = MatchOrReplace(cx, scope, thisObj, args, this, data, false);
					return data.arrayobj == null ? rval : data.arrayobj;
				}

				case RegExpProxyConstants.RA_SEARCH:
				{
					data.optarg = 1;
					return MatchOrReplace(cx, scope, thisObj, args, this, data, false);
				}

				case RegExpProxyConstants.RA_REPLACE:
				{
					object arg1 = args.Length < 2 ? Undefined.instance : args[1];
					string repstr = null;
					Function lambda = null;
					if (arg1 is Function)
					{
						lambda = (Function)arg1;
					}
					else
					{
						repstr = ScriptRuntime.ToString(arg1);
					}
					data.optarg = 2;
					data.lambda = lambda;
					data.repstr = repstr;
					data.dollar = repstr == null ? -1 : repstr.IndexOf('$');
					data.charBuf = null;
					data.leftIndex = 0;
					object val = MatchOrReplace(cx, scope, thisObj, args, this, data, true);
					if (data.charBuf == null)
					{
						if (data.global || val == null || !val.Equals(true))
						{
							return data.str;
						}
						SubString lc = this.leftContext;
						Replace_glob(data, cx, scope, this, lc.index, lc.length);
					}
					SubString rc = this.rightContext;
					data.charBuf.AppendRange(rc.str, rc.index, rc.index + rc.length);
					return data.charBuf.ToString();
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
		}

		/// <summary>Analog of C match_or_replace.</summary>
		/// <remarks>Analog of C match_or_replace.</remarks>
		private static object MatchOrReplace(Context cx, Scriptable scope, Scriptable thisObj, object[] args, RegExpImpl reImpl, GlobData data, bool forceFlat)
		{
			NativeRegExp re;
			string str = ScriptRuntime.ToString(thisObj);
			data.str = str;
			Scriptable topScope = ScriptableObject.GetTopLevelScope(scope);
			if (args.Length == 0)
			{
				RECompiled compiled = NativeRegExp.CompileRE(cx, string.Empty, string.Empty, false);
				re = new NativeRegExp(topScope, compiled);
			}
			else
			{
				if (args[0] is NativeRegExp)
				{
					re = (NativeRegExp)args[0];
				}
				else
				{
					string src = ScriptRuntime.ToString(args[0]);
					string opt;
					if (data.optarg < args.Length)
					{
						args[0] = src;
						opt = ScriptRuntime.ToString(args[data.optarg]);
					}
					else
					{
						opt = null;
					}
					RECompiled compiled = NativeRegExp.CompileRE(cx, src, opt, forceFlat);
					re = new NativeRegExp(topScope, compiled);
				}
			}
			data.global = (re.GetFlags() & NativeRegExp.JSREG_GLOB) != 0;
			int[] indexp = new int[] { 0 };
			object result = null;
			if (data.mode == RegExpProxyConstants.RA_SEARCH)
			{
				result = re.ExecuteRegExp(cx, scope, reImpl, str, indexp, NativeRegExp.TEST);
				if (result != null && result.Equals(true))
				{
					result = Sharpen.Extensions.ValueOf(reImpl.leftContext.length);
				}
				else
				{
					result = Sharpen.Extensions.ValueOf(-1);
				}
			}
			else
			{
				if (data.global)
				{
					re.lastIndex = 0;
					for (int count = 0; indexp[0] <= str.Length; count++)
					{
						result = re.ExecuteRegExp(cx, scope, reImpl, str, indexp, NativeRegExp.TEST);
						if (result == null || !result.Equals(true))
						{
							break;
						}
						if (data.mode == RegExpProxyConstants.RA_MATCH)
						{
							Match_glob(data, cx, scope, count, reImpl);
						}
						else
						{
							if (data.mode != RegExpProxyConstants.RA_REPLACE)
							{
								Kit.CodeBug();
							}
							SubString lastMatch = reImpl.lastMatch;
							int leftIndex = data.leftIndex;
							int leftlen = lastMatch.index - leftIndex;
							data.leftIndex = lastMatch.index + lastMatch.length;
							Replace_glob(data, cx, scope, reImpl, leftIndex, leftlen);
						}
						if (reImpl.lastMatch.length == 0)
						{
							if (indexp[0] == str.Length)
							{
								break;
							}
							indexp[0]++;
						}
					}
				}
				else
				{
					result = re.ExecuteRegExp(cx, scope, reImpl, str, indexp, ((data.mode == RegExpProxyConstants.RA_REPLACE) ? NativeRegExp.TEST : NativeRegExp.MATCH));
				}
			}
			return result;
		}

		public virtual int Find_split(Context cx, Scriptable scope, string target, string separator, Scriptable reObj, int[] ip, int[] matchlen, bool[] matched, string[][] parensp)
		{
			int i = ip[0];
			int length = target.Length;
			int result;
			int version = cx.GetLanguageVersion();
			NativeRegExp re = (NativeRegExp)reObj;
			while (true)
			{
				// imitating C label
				int ipsave = ip[0];
				// reuse ip to save object creation
				ip[0] = i;
				object ret = re.ExecuteRegExp(cx, scope, this, target, ip, NativeRegExp.TEST);
				if (ret != true)
				{
					// Mismatch: ensure our caller advances i past end of string.
					ip[0] = ipsave;
					matchlen[0] = 1;
					matched[0] = false;
					return length;
				}
				i = ip[0];
				ip[0] = ipsave;
				matched[0] = true;
				SubString sep = this.lastMatch;
				matchlen[0] = sep.length;
				if (matchlen[0] == 0)
				{
					if (i == ip[0])
					{
						if (i == length)
						{
							if (version == Context.VERSION_1_2)
							{
								matchlen[0] = 1;
								result = i;
							}
							else
							{
								result = -1;
							}
							break;
						}
						i++;
						goto again_continue;
					}
				}
				// imitating C goto
				// PR_ASSERT((size_t)i >= sep->length);
				result = i - matchlen[0];
				break;
again_continue: ;
			}
again_break: ;
			int size = (parens == null) ? 0 : parens.Length;
			parensp[0] = new string[size];
			for (int num = 0; num < size; num++)
			{
				SubString parsub = GetParenSubString(num);
				parensp[0][num] = parsub.ToString();
			}
			return result;
		}

		/// <summary>Analog of REGEXP_PAREN_SUBSTRING in C jsregexp.h.</summary>
		/// <remarks>
		/// Analog of REGEXP_PAREN_SUBSTRING in C jsregexp.h.
		/// Assumes zero-based; i.e., for $3, i==2
		/// </remarks>
		internal virtual SubString GetParenSubString(int i)
		{
			if (parens != null && i < parens.Length)
			{
				SubString parsub = parens[i];
				if (parsub != null)
				{
					return parsub;
				}
			}
			return SubString.emptySubString;
		}

		private static void Match_glob(GlobData mdata, Context cx, Scriptable scope, int count, RegExpImpl reImpl)
		{
			if (mdata.arrayobj == null)
			{
				mdata.arrayobj = cx.NewArray(scope, 0);
			}
			SubString matchsub = reImpl.lastMatch;
			string matchstr = matchsub.ToString();
			mdata.arrayobj.Put(count, mdata.arrayobj, matchstr);
		}

		private static void Replace_glob(GlobData rdata, Context cx, Scriptable scope, RegExpImpl reImpl, int leftIndex, int leftlen)
		{
			int replen;
			string lambdaStr;
			if (rdata.lambda != null)
			{
				// invoke lambda function with args lastMatch, $1, $2, ... $n,
				// leftContext.length, whole string.
				SubString[] parens = reImpl.parens;
				int parenCount = (parens == null) ? 0 : parens.Length;
				object[] args = new object[parenCount + 3];
				args[0] = reImpl.lastMatch.ToString();
				for (int i = 0; i < parenCount; i++)
				{
					SubString sub = parens[i];
					if (sub != null)
					{
						args[i + 1] = sub.ToString();
					}
					else
					{
						args[i + 1] = Undefined.instance;
					}
				}
				args[parenCount + 1] = Sharpen.Extensions.ValueOf(reImpl.leftContext.length);
				args[parenCount + 2] = rdata.str;
				// This is a hack to prevent expose of reImpl data to
				// JS function which can run new regexps modifing
				// regexp that are used later by the engine.
				// TODO: redesign is necessary
				if (reImpl != ScriptRuntime.GetRegExpProxy(cx))
				{
					Kit.CodeBug();
				}
				RegExpImpl re2 = new RegExpImpl();
				re2.multiline = reImpl.multiline;
				re2.input = reImpl.input;
				ScriptRuntime.SetRegExpProxy(cx, re2);
				try
				{
					Scriptable parent = ScriptableObject.GetTopLevelScope(scope);
					object result = rdata.lambda.Call(cx, parent, parent, args);
					lambdaStr = ScriptRuntime.ToString(result);
				}
				finally
				{
					ScriptRuntime.SetRegExpProxy(cx, reImpl);
				}
				replen = lambdaStr.Length;
			}
			else
			{
				lambdaStr = null;
				replen = rdata.repstr.Length;
				if (rdata.dollar >= 0)
				{
					int[] skip = new int[1];
					int dp = rdata.dollar;
					do
					{
						SubString sub = InterpretDollar(cx, reImpl, rdata.repstr, dp, skip);
						if (sub != null)
						{
							replen += sub.length - skip[0];
							dp += skip[0];
						}
						else
						{
							++dp;
						}
						dp = rdata.repstr.IndexOf('$', dp);
					}
					while (dp >= 0);
				}
			}
			int growth = leftlen + replen + reImpl.rightContext.length;
			StringBuilder charBuf = rdata.charBuf;
			if (charBuf == null)
			{
				charBuf = new StringBuilder(growth);
				rdata.charBuf = charBuf;
			}
			else
			{
				charBuf.EnsureCapacity(rdata.charBuf.Length + growth);
			}
			charBuf.AppendRange(reImpl.leftContext.str, leftIndex, leftIndex + leftlen);
			if (rdata.lambda != null)
			{
				charBuf.Append(lambdaStr);
			}
			else
			{
				Do_replace(rdata, cx, reImpl);
			}
		}

		private static SubString InterpretDollar(Context cx, RegExpImpl res, string da, int dp, int[] skip)
		{
			char dc;
			int num;
			int tmp;
			if (da[dp] != '$')
			{
				Kit.CodeBug();
			}
			int version = cx.GetLanguageVersion();
			if (version != Context.VERSION_DEFAULT && version <= Context.VERSION_1_4)
			{
				if (dp > 0 && da[dp - 1] == '\\')
				{
					return null;
				}
			}
			int daL = da.Length;
			if (dp + 1 >= daL)
			{
				return null;
			}
			dc = da[dp + 1];
			if (NativeRegExp.IsDigit(dc))
			{
				int cp;
				if (version != Context.VERSION_DEFAULT && version <= Context.VERSION_1_4)
				{
					if (dc == '0')
					{
						return null;
					}
					num = 0;
					cp = dp;
					while (++cp < daL && NativeRegExp.IsDigit(dc = da[cp]))
					{
						tmp = 10 * num + (dc - '0');
						if (tmp < num)
						{
							break;
						}
						num = tmp;
					}
				}
				else
				{
					int parenCount = (res.parens == null) ? 0 : res.parens.Length;
					num = dc - '0';
					if (num > parenCount)
					{
						return null;
					}
					cp = dp + 2;
					if ((dp + 2) < daL)
					{
						dc = da[dp + 2];
						if (NativeRegExp.IsDigit(dc))
						{
							tmp = 10 * num + (dc - '0');
							if (tmp <= parenCount)
							{
								cp++;
								num = tmp;
							}
						}
					}
					if (num == 0)
					{
						return null;
					}
				}
				num--;
				skip[0] = cp - dp;
				return res.GetParenSubString(num);
			}
			skip[0] = 2;
			switch (dc)
			{
				case '$':
				{
					return new SubString("$");
				}

				case '&':
				{
					return res.lastMatch;
				}

				case '+':
				{
					return res.lastParen;
				}

				case '`':
				{
					if (version == Context.VERSION_1_2)
					{
						res.leftContext.index = 0;
						res.leftContext.length = res.lastMatch.index;
					}
					return res.leftContext;
				}

				case '\'':
				{
					return res.rightContext;
				}
			}
			return null;
		}

		/// <summary>Analog of do_replace in jsstr.c</summary>
		private static void Do_replace(GlobData rdata, Context cx, RegExpImpl regExpImpl)
		{
			StringBuilder charBuf = rdata.charBuf;
			int cp = 0;
			string da = rdata.repstr;
			int dp = rdata.dollar;
			if (dp != -1)
			{
				int[] skip = new int[1];
				do
				{
					int len = dp - cp;
					charBuf.Append(Sharpen.Runtime.Substring(da, cp, dp));
					cp = dp;
					SubString sub = InterpretDollar(cx, regExpImpl, da, dp, skip);
					if (sub != null)
					{
						len = sub.length;
						if (len > 0)
						{
							charBuf.AppendRange(sub.str, sub.index, sub.index + len);
						}
						cp += skip[0];
						dp += skip[0];
					}
					else
					{
						++dp;
					}
					dp = da.IndexOf('$', dp);
				}
				while (dp >= 0);
			}
			int daL = da.Length;
			if (daL > cp)
			{
				charBuf.Append(Sharpen.Runtime.Substring(da, cp, daL));
			}
		}

		public virtual object Js_split(Context cx, Scriptable scope, string target, object[] args)
		{
			// create an empty Array to return;
			Scriptable result = cx.NewArray(scope, 0);
			// return an array consisting of the target if no separator given
			// don't check against undefined, because we want
			// 'fooundefinedbar'.split(void 0) to split to ['foo', 'bar']
			if (args.Length < 1)
			{
				result.Put(0, result, target);
				return result;
			}
			// Use the second argument as the split limit, if given.
			bool limited = (args.Length > 1) && (args[1] != Undefined.instance);
			long limit = 0;
			// Initialize to avoid warning.
			if (limited)
			{
				limit = ScriptRuntime.ToUint32(args[1]);
				if (limit > target.Length)
				{
					limit = 1 + target.Length;
				}
			}
			string separator = null;
			int[] matchlen = new int[1];
			Scriptable re = null;
			RegExpProxy reProxy = null;
			if (args[0] is Scriptable)
			{
				reProxy = ScriptRuntime.GetRegExpProxy(cx);
				if (reProxy != null)
				{
					Scriptable test = (Scriptable)args[0];
					if (reProxy.IsRegExp(test))
					{
						re = test;
					}
				}
			}
			if (re == null)
			{
				separator = ScriptRuntime.ToString(args[0]);
				matchlen[0] = separator.Length;
			}
			// split target with separator or re
			int[] ip = new int[] { 0 };
			int match;
			int len = 0;
			bool[] matched = new bool[] { false };
			string[][] parens = new string[][] { null };
			int version = cx.GetLanguageVersion();
			while ((match = Find_split(cx, scope, target, separator, version, reProxy, re, ip, matchlen, matched, parens)) >= 0)
			{
				if ((limited && len >= limit) || (match > target.Length))
				{
					break;
				}
				string substr;
				if (target.Length == 0)
				{
					substr = target;
				}
				else
				{
					substr = Sharpen.Runtime.Substring(target, ip[0], match);
				}
				result.Put(len, result, substr);
				len++;
				if (re != null && matched[0] == true)
				{
					int size = parens[0].Length;
					for (int num = 0; num < size; num++)
					{
						if (limited && len >= limit)
						{
							break;
						}
						result.Put(len, result, parens[0][num]);
						len++;
					}
					matched[0] = false;
				}
				ip[0] = match + matchlen[0];
				if (version < Context.VERSION_1_3 && version != Context.VERSION_DEFAULT)
				{
					if (!limited && ip[0] == target.Length)
					{
						break;
					}
				}
			}
			return result;
		}

		private static int Find_split(Context cx, Scriptable scope, string target, string separator, int version, RegExpProxy reProxy, Scriptable re, int[] ip, int[] matchlen, bool[] matched, string[][] parensp)
		{
			int i = ip[0];
			int length = target.Length;
			if (version == Context.VERSION_1_2 && re == null && separator.Length == 1 && separator[0] == ' ')
			{
				if (i == 0)
				{
					while (i < length && char.IsWhiteSpace(target[i]))
					{
						i++;
					}
					ip[0] = i;
				}
				if (i == length)
				{
					return -1;
				}
				while (i < length && !char.IsWhiteSpace(target[i]))
				{
					i++;
				}
				int j = i;
				while (j < length && char.IsWhiteSpace(target[j]))
				{
					j++;
				}
				matchlen[0] = j - i;
				return i;
			}
			if (i > length)
			{
				return -1;
			}
			if (re != null)
			{
				return reProxy.Find_split(cx, scope, target, separator, re, ip, matchlen, matched, parensp);
			}
			if (version != Context.VERSION_DEFAULT && version < Context.VERSION_1_3 && length == 0)
			{
				return -1;
			}
			if (separator.Length == 0)
			{
				if (version == Context.VERSION_1_2)
				{
					if (i == length)
					{
						matchlen[0] = 1;
						return i;
					}
					return i + 1;
				}
				return (i == length) ? -1 : i + 1;
			}
			if (ip[0] >= length)
			{
				return length;
			}
			i = target.IndexOf(separator, ip[0]);
			return (i != -1) ? i : length;
		}

		protected internal string input;

		protected internal bool multiline;

		protected internal SubString[] parens;

		protected internal SubString lastMatch;

		protected internal SubString lastParen;

		protected internal SubString leftContext;

		protected internal SubString rightContext;
	}

	internal sealed class GlobData
	{
		internal int mode;

		internal int optarg;

		internal bool global;

		internal string str;

		internal Scriptable arrayobj;

		internal Function lambda;

		internal string repstr;

		internal int dollar = -1;

		internal StringBuilder charBuf;

		internal int leftIndex;
		// match-specific data
		// replace-specific data
	}
}
