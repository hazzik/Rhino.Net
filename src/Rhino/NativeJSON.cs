/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Json;
using Rhino.Utils;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the JSON native object.</summary>
	/// <remarks>
	/// This class implements the JSON native object.
	/// See ECMA 15.12.
	/// </remarks>
	/// <author>Matthew Crumley, Raphael Speyer</author>
	[System.Serializable]
	public sealed class NativeJSON : IdScriptableObject
	{
		internal const long serialVersionUID = -4567599697595654984L;

		private static readonly object JSON_TAG = "JSON";

		private const int MAX_STRINGIFY_GAP_LENGTH = 10;

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeJSON obj = new Rhino.NativeJSON();
			obj.ActivatePrototypeMap(MAX_ID);
			obj.SetPrototype(GetObjectPrototype(scope));
			obj.SetParentScope(scope);
			if (@sealed)
			{
				obj.SealObject();
			}
			ScriptableObject.DefineProperty(scope, "JSON", obj, PropertyAttributes.DONTENUM);
		}

		private NativeJSON()
		{
		}

		public override string GetClassName()
		{
			return "JSON";
		}

		protected internal override void InitPrototypeId(int id)
		{
			if (id <= LAST_METHOD_ID)
			{
				string name;
				int arity;
				switch (id)
				{
					case Id_toSource:
					{
						arity = 0;
						name = "toSource";
						break;
					}

					case Id_parse:
					{
						arity = 2;
						name = "parse";
						break;
					}

					case Id_stringify:
					{
						arity = 3;
						name = "stringify";
						break;
					}

					default:
					{
						throw new InvalidOperationException(id.ToString());
					}
				}
				InitPrototypeMethod(JSON_TAG, id, name, arity);
			}
			else
			{
				throw new InvalidOperationException(id.ToString());
			}
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(JSON_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int methodId = f.MethodId();
			switch (methodId)
			{
				case Id_toSource:
				{
					return "JSON";
				}

				case Id_parse:
				{
					string jtext = ScriptRuntime.ToString(args, 0);
					object reviver = null;
					if (args.Length > 1)
					{
						reviver = args[1];
					}
					if (reviver is Callable)
					{
						return Parse(cx, scope, jtext, (Callable)reviver);
					}
					else
					{
						return Parse(cx, scope, jtext);
					}
					goto case Id_stringify;
				}

				case Id_stringify:
				{
					object value = null;
					object replacer = null;
					object space = null;
					switch (args.Length)
					{
						case 3:
						default:
						{
							space = args[2];
							goto case 2;
						}

						case 2:
						{
							replacer = args[1];
							goto case 1;
						}

						case 1:
						{
							value = args[0];
							goto case 0;
						}

						case 0:
						{
							break;
						}
					}
					return Stringify(cx, scope, value, replacer, space);
				}

				default:
				{
					throw new InvalidOperationException(methodId.ToString());
				}
			}
		}

		private static object Parse(Context cx, Scriptable scope, string jtext)
		{
			try
			{
				return new JsonParser(cx, scope).ParseValue(jtext);
			}
			catch (JsonParser.ParseException ex)
			{
				throw ScriptRuntime.ConstructError("SyntaxError", ex.Message);
			}
		}

		public static object Parse(Context cx, Scriptable scope, string jtext, Callable reviver)
		{
			object unfiltered = Parse(cx, scope, jtext);
			Scriptable root = cx.NewObject(scope);
			root.Put(string.Empty, root, unfiltered);
			return Walk(cx, scope, reviver, root, string.Empty);
		}

		private static object Walk(Context cx, Scriptable scope, Callable reviver, Scriptable holder, object name)
		{
			object property;
			if (name.IsNumber())
			{
				property = holder.Get(System.Convert.ToInt32(name), holder);
			}
			else
			{
				property = holder.Get(((string)name), holder);
			}
			if (property is Scriptable)
			{
				Scriptable val = ((Scriptable)property);
				if (val is NativeArray)
				{
					long len = ((NativeArray)val).GetLength();
					for (long i = 0; i < len; i++)
					{
						// indices greater than MAX_INT are represented as strings
						if (i > int.MaxValue)
						{
							string id = System.Convert.ToString(i);
							object newElement = Walk(cx, scope, reviver, val, id);
							if (newElement == Undefined.instance)
							{
								val.Delete(id);
							}
							else
							{
								val.Put(id, val, newElement);
							}
						}
						else
						{
							int idx = (int)i;
							object newElement = Walk(cx, scope, reviver, val, idx);
							if (newElement == Undefined.instance)
							{
								val.Delete(idx);
							}
							else
							{
								val.Put(idx, val, newElement);
							}
						}
					}
				}
				else
				{
					object[] keys = val.GetIds();
					foreach (object p in keys)
					{
						object newElement = Walk(cx, scope, reviver, val, p);
						if (newElement == Undefined.instance)
						{
							if (p.IsNumber())
							{
								val.Delete(System.Convert.ToInt32(p));
							}
							else
							{
								val.Delete((string)p);
							}
						}
						else
						{
							if (p.IsNumber())
							{
								val.Put(System.Convert.ToInt32(p), val, newElement);
							}
							else
							{
								val.Put((string)p, val, newElement);
							}
						}
					}
				}
			}
			return reviver.Call(cx, scope, holder, new object[] { name, property });
		}

		private static string Repeat(char c, int count)
		{
			return new string(c, count);
		}

		private class StringifyState
		{
			internal StringifyState(Context cx, Scriptable scope, string indent, string gap, Callable replacer, IList<object> propertyList, object space)
			{
				this.cx = cx;
				this.scope = scope;
				this.indent = indent;
				this.gap = gap;
				this.replacer = replacer;
				this.propertyList = propertyList;
				this.space = space;
			}

			internal Stack<Scriptable> stack = new Stack<Scriptable>();

			internal string indent;

			internal string gap;

			internal Callable replacer;

			internal IList<object> propertyList;

			internal object space;

			internal Context cx;

			internal Scriptable scope;
		}

		public static object Stringify(Context cx, Scriptable scope, object value, object replacer, object space)
		{
			string indent = string.Empty;
			string gap = string.Empty;
			IList<object> propertyList = null;
			Callable replacerFunction = null;
			if (replacer is Callable)
			{
				replacerFunction = (Callable)replacer;
			}
			else
			{
				if (replacer is NativeArray)
				{
					propertyList = new List<object>();
					NativeArray replacerArray = (NativeArray)replacer;
					foreach (int i in replacerArray.GetIndexIds())
					{
						object v = replacerArray.Get(i, replacerArray);
						if (v is string || v.IsNumber())
						{
							propertyList.Add(v);
						}
						else
						{
							if (v is NativeString || v is NativeNumber)
							{
								propertyList.Add(ScriptRuntime.ToString(v));
							}
						}
					}
				}
			}
			if (space is NativeNumber)
			{
				space = ScriptRuntime.ToNumber(space);
			}
			else
			{
				if (space is NativeString)
				{
					space = ScriptRuntime.ToString(space);
				}
			}
			if (space.IsNumber())
			{
				int gapLength = (int)ScriptRuntime.ToInteger(space);
				gapLength = Math.Min(MAX_STRINGIFY_GAP_LENGTH, gapLength);
				gap = (gapLength > 0) ? Repeat(' ', gapLength) : string.Empty;
				space = gapLength;
			}
			else
			{
				if (space is string)
				{
					gap = (string)space;
					if (gap.Length > MAX_STRINGIFY_GAP_LENGTH)
					{
						gap = gap.Substring(0, MAX_STRINGIFY_GAP_LENGTH);
					}
				}
			}
			NativeJSON.StringifyState state = new NativeJSON.StringifyState(cx, scope, indent, gap, replacerFunction, propertyList, space);
			ScriptableObject wrapper = new NativeObject();
			wrapper.SetParentScope(scope);
			wrapper.SetPrototype(ScriptableObject.GetObjectPrototype(scope));
			wrapper.DefineProperty(string.Empty, value, 0);
			return Str(string.Empty, wrapper, state);
		}

		private static object Str(object key, Scriptable holder, NativeJSON.StringifyState state)
		{
			object value = null;
			if (key is string)
			{
				value = GetProperty(holder, (string)key);
			}
			else
			{
				value = GetProperty(holder, System.Convert.ToInt32(key));
			}
			if (value is Scriptable)
			{
				object toJSON = GetProperty((Scriptable)value, "toJSON");
				if (toJSON is Callable)
				{
					value = CallMethod(state.cx, (Scriptable)value, "toJSON", new object[] { key });
				}
			}
			if (state.replacer != null)
			{
				value = state.replacer.Call(state.cx, state.scope, holder, new object[] { key, value });
			}
			if (value is NativeNumber)
			{
				value = ScriptRuntime.ToNumber(value);
			}
			else
			{
				if (value is NativeString)
				{
					value = ScriptRuntime.ToString(value);
				}
				else
				{
					if (value is NativeBoolean)
					{
						value = ((NativeBoolean)value).GetDefaultValue(ScriptRuntime.BooleanClass);
					}
				}
			}
			if (value == null)
			{
				return "null";
			}
			if (value.Equals(true))
			{
				return "true";
			}
			if (value.Equals(false))
			{
				return "false";
			}
			if (value is string)
			{
				return Quote(value.ToString());
			}
			if (value.IsNumber())
			{
				double d = System.Convert.ToDouble(value);
				if (!Double.IsNaN(d) && d != double.PositiveInfinity && d != double.NegativeInfinity)
				{
					return ScriptRuntime.ToString(value);
				}
				else
				{
					return "null";
				}
			}
			if (value is Scriptable && !(value is Callable))
			{
				if (value is NativeArray)
				{
					return Ja((NativeArray)value, state);
				}
				return Jo((Scriptable)value, state);
			}
			return Undefined.instance;
		}

		private static string Join(ICollection<object> objs, string delimiter)
		{
			return string.Join(delimiter, objs);
		}

		private static string Jo(Scriptable value, NativeJSON.StringifyState state)
		{
			if (state.stack.Contains(value))
			{
				throw ScriptRuntime.TypeError0("msg.cyclic.value");
			}
			state.stack.Push(value);
			string stepback = state.indent;
			state.indent = state.indent + state.gap;
			object[] k = null;
			if (state.propertyList != null)
			{
				k = state.propertyList.ToArray();
			}
			else
			{
				k = value.GetIds();
			}
			IList<object> partial = new List<object>();
			foreach (object p in k)
			{
				object strP = Str(p, value, state);
				if (strP != Undefined.instance)
				{
					string member = Quote(p.ToString()) + ":";
					if (state.gap.Length > 0)
					{
						member = member + " ";
					}
					member = member + strP;
					partial.Add(member);
				}
			}
			string finalValue;
			if (partial.IsEmpty())
			{
				finalValue = "{}";
			}
			else
			{
				if (state.gap.Length == 0)
				{
					finalValue = '{' + Join(partial, ",") + '}';
				}
				else
				{
					string separator = ",\n" + state.indent;
					string properties = Join(partial, separator);
					finalValue = "{\n" + state.indent + properties + '\n' + stepback + '}';
				}
			}
			state.stack.Pop();
			state.indent = stepback;
			return finalValue;
		}

		private static string Ja(NativeArray value, NativeJSON.StringifyState state)
		{
			if (state.stack.Contains(value))
			{
				throw ScriptRuntime.TypeError0("msg.cyclic.value");
			}
			state.stack.Push(value);
			string stepback = state.indent;
			state.indent = state.indent + state.gap;
			IList<object> partial = new List<object>();
			long len = value.GetLength();
			for (long index = 0; index < len; index++)
			{
				object strP;
				if (index > int.MaxValue)
				{
					strP = Str(System.Convert.ToString(index), value, state);
				}
				else
				{
					strP = Str((int)index, value, state);
				}
				if (strP == Undefined.instance)
				{
					partial.Add("null");
				}
				else
				{
					partial.Add(strP);
				}
			}
			string finalValue;
			if (partial.IsEmpty())
			{
				finalValue = "[]";
			}
			else
			{
				if (state.gap.Length == 0)
				{
					finalValue = '[' + Join(partial, ",") + ']';
				}
				else
				{
					string separator = ",\n" + state.indent;
					string properties = Join(partial, separator);
					finalValue = "[\n" + state.indent + properties + '\n' + stepback + ']';
				}
			}
			state.stack.Pop();
			state.indent = stepback;
			return finalValue;
		}

		private static string Quote(string @string)
		{
			StringBuilder product = new StringBuilder(@string.Length + 2);
			// two extra chars for " on either side
			product.Append('"');
			int length = @string.Length;
			for (int i = 0; i < length; i++)
			{
				char c = @string[i];
				switch (c)
				{
					case '"':
					{
						product.Append("\\\"");
						break;
					}

					case '\\':
					{
						product.Append("\\\\");
						break;
					}

					case '\b':
					{
						product.Append("\\b");
						break;
					}

					case '\f':
					{
						product.Append("\\f");
						break;
					}

					case '\n':
					{
						product.Append("\\n");
						break;
					}

					case '\r':
					{
						product.Append("\\r");
						break;
					}

					case '\t':
					{
						product.Append("\\t");
						break;
					}

					default:
					{
						if (c < ' ')
						{
							product.Append("\\u");
							string hex = string.Format("%04x", (int)c);
							product.Append(hex);
						}
						else
						{
							product.Append(c);
						}
						break;
					}
				}
			}
			product.Append('"');
			return product.ToString();
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			{
				// #generated# Last update: 2009-05-25 16:01:00 EDT
				id = 0;
				string X = null;
				switch (s.Length)
				{
					case 5:
					{
						X = "parse";
						id = Id_parse;
						goto L_break;
					}

					case 8:
					{
						X = "toSource";
						id = Id_toSource;
						goto L_break;
					}

					case 9:
					{
						X = "stringify";
						id = Id_stringify;
						goto L_break;
					}
				}
L_break: ;
				if (X != null && X != s && !X.Equals(s))
				{
					id = 0;
				}
			}
			// #/generated#
			return id;
		}

		private const int Id_toSource = 1;

		private const int Id_parse = 2;

		private const int Id_stringify = 3;

		private const int LAST_METHOD_ID = 3;

		private const int MAX_ID = 3;
		// #/string_id_map#
	}
}
