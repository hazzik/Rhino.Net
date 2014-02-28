/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the Math native object.</summary>
	/// <remarks>
	/// This class implements the Math native object.
	/// See ECMA 15.8.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	internal sealed class NativeMath : IdScriptableObject
	{
		private static readonly object MATH_TAG = "Math";

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeMath obj = new Rhino.NativeMath();
			obj.ActivatePrototypeMap(MAX_ID);
			obj.Prototype = GetObjectPrototype(scope);
			obj.ParentScope = scope;
			if (@sealed)
			{
				obj.SealObject();
			}
			ScriptableObject.DefineProperty(scope, "Math", obj, PropertyAttributes.DONTENUM);
		}

		private NativeMath()
		{
		}

		public override string GetClassName()
		{
			return "Math";
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

					case Id_abs:
					{
						arity = 1;
						name = "abs";
						break;
					}

					case Id_acos:
					{
						arity = 1;
						name = "acos";
						break;
					}

					case Id_asin:
					{
						arity = 1;
						name = "asin";
						break;
					}

					case Id_atan:
					{
						arity = 1;
						name = "atan";
						break;
					}

					case Id_atan2:
					{
						arity = 2;
						name = "atan2";
						break;
					}

					case Id_ceil:
					{
						arity = 1;
						name = "ceil";
						break;
					}

					case Id_cos:
					{
						arity = 1;
						name = "cos";
						break;
					}

					case Id_exp:
					{
						arity = 1;
						name = "exp";
						break;
					}

					case Id_floor:
					{
						arity = 1;
						name = "floor";
						break;
					}

					case Id_log:
					{
						arity = 1;
						name = "log";
						break;
					}

					case Id_max:
					{
						arity = 2;
						name = "max";
						break;
					}

					case Id_min:
					{
						arity = 2;
						name = "min";
						break;
					}

					case Id_pow:
					{
						arity = 2;
						name = "pow";
						break;
					}

					case Id_random:
					{
						arity = 0;
						name = "random";
						break;
					}

					case Id_round:
					{
						arity = 1;
						name = "round";
						break;
					}

					case Id_sin:
					{
						arity = 1;
						name = "sin";
						break;
					}

					case Id_sqrt:
					{
						arity = 1;
						name = "sqrt";
						break;
					}

					case Id_tan:
					{
						arity = 1;
						name = "tan";
						break;
					}

					default:
					{
						throw new InvalidOperationException(id.ToString());
					}
				}
				InitPrototypeMethod(MATH_TAG, id, name, arity);
			}
			else
			{
				string name;
				double x;
				switch (id)
				{
					case Id_E:
					{
						x = Math.E;
						name = "E";
						break;
					}

					case Id_PI:
					{
						x = Math.PI;
						name = "PI";
						break;
					}

					case Id_LN10:
					{
						x = 2.302585092994046;
						name = "LN10";
						break;
					}

					case Id_LN2:
					{
						x = 0.6931471805599453;
						name = "LN2";
						break;
					}

					case Id_LOG2E:
					{
						x = 1.4426950408889634;
						name = "LOG2E";
						break;
					}

					case Id_LOG10E:
					{
						x = 0.4342944819032518;
						name = "LOG10E";
						break;
					}

					case Id_SQRT1_2:
					{
						x = 0.7071067811865476;
						name = "SQRT1_2";
						break;
					}

					case Id_SQRT2:
					{
						x = 1.4142135623730951;
						name = "SQRT2";
						break;
					}

					default:
					{
						throw new InvalidOperationException(id.ToString());
					}
				}
				InitPrototypeValue(id, name, x, PropertyAttributes.DONTENUM | PropertyAttributes.READONLY | PropertyAttributes.PERMANENT);
			}
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(MATH_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			double x;
			int methodId = f.MethodId();
			switch (methodId)
			{
				case Id_toSource:
				{
					return "Math";
				}

				case Id_abs:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					// abs(-0.0) should be 0.0, but -0.0 < 0.0 == false
					x = (x == 0.0) ? 0.0 : (x < 0.0) ? -x : x;
					break;
				}

				case Id_acos:
				case Id_asin:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					if (!Double.IsNaN(x) && -1.0 <= x && x <= 1.0)
					{
						x = (methodId == Id_acos) ? Math.Acos(x) : Math.Asin(x);
					}
					else
					{
						x = double.NaN;
					}
					break;
				}

				case Id_atan:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = Math.Atan(x);
					break;
				}

				case Id_atan2:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = Math.Atan2(x, ScriptRuntime.ToNumber(args, 1));
					break;
				}

				case Id_ceil:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = System.Math.Ceiling(x);
					break;
				}

				case Id_cos:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = (x == double.PositiveInfinity || x == double.NegativeInfinity) ? double.NaN : Math.Cos(x);
					break;
				}

				case Id_exp:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = (x == double.PositiveInfinity) ? x : (x == double.NegativeInfinity) ? 0.0 : Math.Exp(x);
					break;
				}

				case Id_floor:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = Math.Floor(x);
					break;
				}

				case Id_log:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					// Java's log(<0) = -Infinity; we need NaN
					x = (x < 0) ? double.NaN : Math.Log(x);
					break;
				}

				case Id_max:
				case Id_min:
				{
					x = (methodId == Id_max) ? double.NegativeInfinity : double.PositiveInfinity;
					for (int i = 0; i != args.Length; ++i)
					{
						double d = ScriptRuntime.ToNumber(args[i]);
						if (Double.IsNaN(d))
						{
							x = d;
							// NaN
							break;
						}
						if (methodId == Id_max)
						{
							// if (x < d) x = d; does not work due to -0.0 >= +0.0
							x = Math.Max(x, d);
						}
						else
						{
							x = Math.Min(x, d);
						}
					}
					break;
				}

				case Id_pow:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = Js_pow(x, ScriptRuntime.ToNumber(args, 1));
					break;
				}

				case Id_random:
				{
					x = new Random().Next();
					break;
				}

				case Id_round:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					if (!Double.IsNaN(x) && x != double.PositiveInfinity && x != double.NegativeInfinity)
					{
						// Round only finite x
						long l = (long) Math.Round(x);
						if (l != 0)
						{
							x = l;
						}
						else
						{
							// We must propagate the sign of d into the result
							if (x < 0.0)
							{
								x = ScriptRuntime.negativeZero;
							}
							else
							{
								if (x != 0.0)
								{
									x = 0.0;
								}
							}
						}
					}
					break;
				}

				case Id_sin:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = (x == double.PositiveInfinity || x == double.NegativeInfinity) ? double.NaN : Math.Sin(x);
					break;
				}

				case Id_sqrt:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = Math.Sqrt(x);
					break;
				}

				case Id_tan:
				{
					x = ScriptRuntime.ToNumber(args, 0);
					x = Math.Tan(x);
					break;
				}

				default:
				{
					throw new InvalidOperationException(methodId.ToString());
				}
			}
			return x;
		}

		// See Ecma 15.8.2.13
		private double Js_pow(double x, double y)
		{
			double result;
			if (Double.IsNaN(y))
			{
				// y is NaN, result is always NaN
				result = y;
			}
			else
			{
				if (y == 0)
				{
					// Java's pow(NaN, 0) = NaN; we need 1
					result = 1.0;
				}
				else
				{
					if (x == 0)
					{
						// Many differences from Java's Math.pow
						if (1 / x > 0)
						{
							result = (y > 0) ? 0 : double.PositiveInfinity;
						}
						else
						{
							// x is -0, need to check if y is an odd integer
							long y_long = (long)y;
							if (y_long == y && (y_long & unchecked((int)(0x1))) != 0)
							{
								result = (y > 0) ? -0.0 : double.NegativeInfinity;
							}
							else
							{
								result = (y > 0) ? 0.0 : double.PositiveInfinity;
							}
						}
					}
					else
					{
						result = Math.Pow(x, y);
						if (Double.IsNaN(result))
						{
							// Check for broken Java implementations that gives NaN
							// when they should return something else
							if (y == double.PositiveInfinity)
							{
								if (x < -1.0 || 1.0 < x)
								{
									result = double.PositiveInfinity;
								}
								else
								{
									if (-1.0 < x && x < 1.0)
									{
										result = 0;
									}
								}
							}
							else
							{
								if (y == double.NegativeInfinity)
								{
									if (x < -1.0 || 1.0 < x)
									{
										result = 0;
									}
									else
									{
										if (-1.0 < x && x < 1.0)
										{
											result = double.PositiveInfinity;
										}
									}
								}
								else
								{
									if (x == double.PositiveInfinity)
									{
										result = (y > 0) ? double.PositiveInfinity : 0.0;
									}
									else
									{
										if (x == double.NegativeInfinity)
										{
											long y_long = (long)y;
											if (y_long == y && (y_long & unchecked((int)(0x1))) != 0)
											{
												// y is odd integer
												result = (y > 0) ? double.NegativeInfinity : -0.0;
											}
											else
											{
												result = (y > 0) ? double.PositiveInfinity : 0.0;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2004-03-17 13:51:32 CET
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 1:
				{
					if (s[0] == 'E')
					{
						id = Id_E;
						goto L0_break;
					}
					goto L_break;
				}

				case 2:
				{
					if (s[0] == 'P' && s[1] == 'I')
					{
						id = Id_PI;
						goto L0_break;
					}
					goto L_break;
				}

				case 3:
				{
					switch (s[0])
					{
						case 'L':
						{
							if (s[2] == '2' && s[1] == 'N')
							{
								id = Id_LN2;
								goto L0_break;
							}
							goto L_break;
						}

						case 'a':
						{
							if (s[2] == 's' && s[1] == 'b')
							{
								id = Id_abs;
								goto L0_break;
							}
							goto L_break;
						}

						case 'c':
						{
							if (s[2] == 's' && s[1] == 'o')
							{
								id = Id_cos;
								goto L0_break;
							}
							goto L_break;
						}

						case 'e':
						{
							if (s[2] == 'p' && s[1] == 'x')
							{
								id = Id_exp;
								goto L0_break;
							}
							goto L_break;
						}

						case 'l':
						{
							if (s[2] == 'g' && s[1] == 'o')
							{
								id = Id_log;
								goto L0_break;
							}
							goto L_break;
						}

						case 'm':
						{
							c = s[2];
							if (c == 'n')
							{
								if (s[1] == 'i')
								{
									id = Id_min;
									goto L0_break;
								}
							}
							else
							{
								if (c == 'x')
								{
									if (s[1] == 'a')
									{
										id = Id_max;
										goto L0_break;
									}
								}
							}
							goto L_break;
						}

						case 'p':
						{
							if (s[2] == 'w' && s[1] == 'o')
							{
								id = Id_pow;
								goto L0_break;
							}
							goto L_break;
						}

						case 's':
						{
							if (s[2] == 'n' && s[1] == 'i')
							{
								id = Id_sin;
								goto L0_break;
							}
							goto L_break;
						}

						case 't':
						{
							if (s[2] == 'n' && s[1] == 'a')
							{
								id = Id_tan;
								goto L0_break;
							}
							goto L_break;
						}
					}
					goto L_break;
				}

				case 4:
				{
					switch (s[1])
					{
						case 'N':
						{
							X = "LN10";
							id = Id_LN10;
							goto L_break;
						}

						case 'c':
						{
							X = "acos";
							id = Id_acos;
							goto L_break;
						}

						case 'e':
						{
							X = "ceil";
							id = Id_ceil;
							goto L_break;
						}

						case 'q':
						{
							X = "sqrt";
							id = Id_sqrt;
							goto L_break;
						}

						case 's':
						{
							X = "asin";
							id = Id_asin;
							goto L_break;
						}

						case 't':
						{
							X = "atan";
							id = Id_atan;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 5:
				{
					switch (s[0])
					{
						case 'L':
						{
							X = "LOG2E";
							id = Id_LOG2E;
							goto L_break;
						}

						case 'S':
						{
							X = "SQRT2";
							id = Id_SQRT2;
							goto L_break;
						}

						case 'a':
						{
							X = "atan2";
							id = Id_atan2;
							goto L_break;
						}

						case 'f':
						{
							X = "floor";
							id = Id_floor;
							goto L_break;
						}

						case 'r':
						{
							X = "round";
							id = Id_round;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 6:
				{
					c = s[0];
					if (c == 'L')
					{
						X = "LOG10E";
						id = Id_LOG10E;
					}
					else
					{
						if (c == 'r')
						{
							X = "random";
							id = Id_random;
						}
					}
					goto L_break;
				}

				case 7:
				{
					X = "SQRT1_2";
					id = Id_SQRT1_2;
					goto L_break;
				}

				case 8:
				{
					X = "toSource";
					id = Id_toSource;
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
			return id;
		}

		private const int Id_toSource = 1;

		private const int Id_abs = 2;

		private const int Id_acos = 3;

		private const int Id_asin = 4;

		private const int Id_atan = 5;

		private const int Id_atan2 = 6;

		private const int Id_ceil = 7;

		private const int Id_cos = 8;

		private const int Id_exp = 9;

		private const int Id_floor = 10;

		private const int Id_log = 11;

		private const int Id_max = 12;

		private const int Id_min = 13;

		private const int Id_pow = 14;

		private const int Id_random = 15;

		private const int Id_round = 16;

		private const int Id_sin = 17;

		private const int Id_sqrt = 18;

		private const int Id_tan = 19;

		private const int LAST_METHOD_ID = 19;

		private const int Id_E = LAST_METHOD_ID + 1;

		private const int Id_PI = LAST_METHOD_ID + 2;

		private const int Id_LN10 = LAST_METHOD_ID + 3;

		private const int Id_LN2 = LAST_METHOD_ID + 4;

		private const int Id_LOG2E = LAST_METHOD_ID + 5;

		private const int Id_LOG10E = LAST_METHOD_ID + 6;

		private const int Id_SQRT1_2 = LAST_METHOD_ID + 7;

		private const int Id_SQRT2 = LAST_METHOD_ID + 8;

		private const int MAX_ID = LAST_METHOD_ID + 8;
		// #/string_id_map#
	}
}
