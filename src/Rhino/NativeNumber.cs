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
	/// <summary>This class implements the Number native object.</summary>
	/// <remarks>
	/// This class implements the Number native object.
	/// See ECMA 15.7.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	internal sealed class NativeNumber : IdScriptableObject
	{
		private static readonly object NUMBER_TAG = "Number";

		private const int MAX_PRECISION = 100;

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeNumber obj = new Rhino.NativeNumber(0.0);
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		internal NativeNumber(double number)
		{
			doubleValue = number;
		}

		public override string GetClassName()
		{
			return "Number";
		}

		protected internal override void FillConstructorProperties(IdFunctionObject ctor)
		{
			PropertyAttributes attr = PropertyAttributes.DONTENUM | PropertyAttributes.PERMANENT | PropertyAttributes.READONLY;
			ctor.DefineProperty("NaN", ScriptRuntime.NaN, attr);
			ctor.DefineProperty("POSITIVE_INFINITY", ScriptRuntime.WrapNumber(double.PositiveInfinity), attr);
			ctor.DefineProperty("NEGATIVE_INFINITY", ScriptRuntime.WrapNumber(double.NegativeInfinity), attr);
			ctor.DefineProperty("MAX_VALUE", ScriptRuntime.WrapNumber(double.MaxValue), attr);
			ctor.DefineProperty("MIN_VALUE", ScriptRuntime.WrapNumber(double.MinValue), attr);
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
					arity = 1;
					s = "toString";
					break;
				}

				case Id_toLocaleString:
				{
					arity = 1;
					s = "toLocaleString";
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

				case Id_toFixed:
				{
					arity = 1;
					s = "toFixed";
					break;
				}

				case Id_toExponential:
				{
					arity = 1;
					s = "toExponential";
					break;
				}

				case Id_toPrecision:
				{
					arity = 1;
					s = "toPrecision";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(NUMBER_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(NUMBER_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			if (id == Id_constructor)
			{
				double val = (args.Length >= 1) ? ScriptRuntime.ToNumber(args[0]) : 0.0;
				if (thisObj == null)
				{
					// new Number(val) creates a new Number object.
					return new Rhino.NativeNumber(val);
				}
				// Number(val) converts val to a number value.
				return ScriptRuntime.WrapNumber(val);
			}
			// The rest of Number.prototype methods require thisObj to be Number
			if (!(thisObj is Rhino.NativeNumber))
			{
				throw IncompatibleCallError(f);
			}
			double value = ((Rhino.NativeNumber)thisObj).doubleValue;
			switch (id)
			{
				case Id_toString:
				case Id_toLocaleString:
				{
					// toLocaleString is just an alias for toString for now
					int @base = (args.Length == 0 || args[0] == Undefined.instance) ? 10 : ScriptRuntime.ToInt32(args[0]);
					return ScriptRuntime.NumberToString(value, @base);
				}

				case Id_toSource:
				{
					return "(new Number(" + ScriptRuntime.ToString(value) + "))";
				}

				case Id_valueOf:
				{
					return ScriptRuntime.WrapNumber(value);
				}

				case Id_toFixed:
				{
					return Num_to(value, args, DToA.DTOSTR_FIXED, DToA.DTOSTR_FIXED, -20, 0);
				}

				case Id_toExponential:
				{
					// Handle special values before range check
					if (double.IsNaN(value))
					{
						return "NaN";
					}
					if (System.Double.IsInfinity(value))
					{
						if (value >= 0)
						{
							return "Infinity";
						}
						else
						{
							return "-Infinity";
						}
					}
					// General case
					return Num_to(value, args, DToA.DTOSTR_STANDARD_EXPONENTIAL, DToA.DTOSTR_EXPONENTIAL, 0, 1);
				}

				case Id_toPrecision:
				{
					// Undefined precision, fall back to ToString()
					if (args.Length == 0 || args[0] == Undefined.instance)
					{
						return ScriptRuntime.NumberToString(value, 10);
					}
					// Handle special values before range check
					if (double.IsNaN(value))
					{
						return "NaN";
					}
					if (System.Double.IsInfinity(value))
					{
						if (value >= 0)
						{
							return "Infinity";
						}
						else
						{
							return "-Infinity";
						}
					}
					return Num_to(value, args, DToA.DTOSTR_STANDARD, DToA.DTOSTR_PRECISION, 1, 0);
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
		}

		public override string ToString()
		{
			return ScriptRuntime.NumberToString(doubleValue, 10);
		}

		private static string Num_to(double val, object[] args, int zeroArgMode, int oneArgMode, int precisionMin, int precisionOffset)
		{
			int precision;
			if (args.Length == 0)
			{
				precision = 0;
				oneArgMode = zeroArgMode;
			}
			else
			{
				precision = ScriptRuntime.ToInt32(args[0]);
				if (precision < precisionMin || precision > MAX_PRECISION)
				{
					string msg = ScriptRuntime.GetMessage1("msg.bad.precision", ScriptRuntime.ToString(args[0]));
					throw ScriptRuntime.ConstructError("RangeError", msg);
				}
			}
			StringBuilder sb = new StringBuilder();
			DToA.JS_dtostr(sb, oneArgMode, precision + precisionOffset, val);
			return sb.ToString();
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:15:50 EDT
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 7:
				{
					c = s[0];
					if (c == 't')
					{
						X = "toFixed";
						id = Id_toFixed;
					}
					else
					{
						if (c == 'v')
						{
							X = "valueOf";
							id = Id_valueOf;
						}
					}
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

				case 11:
				{
					c = s[0];
					if (c == 'c')
					{
						X = "constructor";
						id = Id_constructor;
					}
					else
					{
						if (c == 't')
						{
							X = "toPrecision";
							id = Id_toPrecision;
						}
					}
					goto L_break;
				}

				case 13:
				{
					X = "toExponential";
					id = Id_toExponential;
					goto L_break;
				}

				case 14:
				{
					X = "toLocaleString";
					id = Id_toLocaleString;
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

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toLocaleString = 3;

		private const int Id_toSource = 4;

		private const int Id_valueOf = 5;

		private const int Id_toFixed = 6;

		private const int Id_toExponential = 7;

		private const int Id_toPrecision = 8;

		private const int MAX_PROTOTYPE_ID = 8;

		private double doubleValue;
		// #/string_id_map#
	}
}
