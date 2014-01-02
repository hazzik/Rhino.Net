/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Optimizer;
using Sharpen;

namespace Rhino.Optimizer
{
	public sealed class OptRuntime : ScriptRuntime
	{
		public static readonly double zeroObj = 0.0;

		public static readonly double oneObj = 1.0;

		public static readonly double minusOneObj = -1.0;

		/// <summary>Implement ....() call shrinking optimizer code.</summary>
		/// <remarks>Implement ....() call shrinking optimizer code.</remarks>
		public static object Call0(Callable fun, Scriptable thisObj, Context cx, Scriptable scope)
		{
			return fun.Call(cx, scope, thisObj, ScriptRuntime.emptyArgs);
		}

		/// <summary>Implement ....(arg) call shrinking optimizer code.</summary>
		/// <remarks>Implement ....(arg) call shrinking optimizer code.</remarks>
		public static object Call1(Callable fun, Scriptable thisObj, object arg0, Context cx, Scriptable scope)
		{
			return fun.Call(cx, scope, thisObj, new object[] { arg0 });
		}

		/// <summary>Implement ....(arg0, arg1) call shrinking optimizer code.</summary>
		/// <remarks>Implement ....(arg0, arg1) call shrinking optimizer code.</remarks>
		public static object Call2(Callable fun, Scriptable thisObj, object arg0, object arg1, Context cx, Scriptable scope)
		{
			return fun.Call(cx, scope, thisObj, new object[] { arg0, arg1 });
		}

		/// <summary>Implement ....(arg0, arg1, ...) call shrinking optimizer code.</summary>
		/// <remarks>Implement ....(arg0, arg1, ...) call shrinking optimizer code.</remarks>
		public static object CallN(Callable fun, Scriptable thisObj, object[] args, Context cx, Scriptable scope)
		{
			return fun.Call(cx, scope, thisObj, args);
		}

		/// <summary>Implement name(args) call shrinking optimizer code.</summary>
		/// <remarks>Implement name(args) call shrinking optimizer code.</remarks>
		public static object CallName(object[] args, string name, Context cx, Scriptable scope)
		{
			Callable f = GetNameFunctionAndThis(name, cx, scope);
			Scriptable thisObj = LastStoredScriptable(cx);
			return f.Call(cx, scope, thisObj, args);
		}

		/// <summary>Implement name() call shrinking optimizer code.</summary>
		/// <remarks>Implement name() call shrinking optimizer code.</remarks>
		public static object CallName0(string name, Context cx, Scriptable scope)
		{
			Callable f = GetNameFunctionAndThis(name, cx, scope);
			Scriptable thisObj = LastStoredScriptable(cx);
			return f.Call(cx, scope, thisObj, ScriptRuntime.emptyArgs);
		}

		/// <summary>Implement x.property() call shrinking optimizer code.</summary>
		/// <remarks>Implement x.property() call shrinking optimizer code.</remarks>
		public static object CallProp0(object value, string property, Context cx, Scriptable scope)
		{
			Callable f = GetPropFunctionAndThis(value, property, cx, scope);
			Scriptable thisObj = LastStoredScriptable(cx);
			return f.Call(cx, scope, thisObj, ScriptRuntime.emptyArgs);
		}

		public static object Add(object val1, double val2)
		{
			if (val1 is Scriptable)
			{
				val1 = ((Scriptable)val1).GetDefaultValue(null);
			}
			if (!(val1 is string))
			{
				return WrapDouble(ToNumber(val1) + val2);
			}
			return (string) val1 + ToString(val2);
		}

		public static object Add(double val1, object val2)
		{
			if (val2 is Scriptable)
			{
				val2 = ((Scriptable)val2).GetDefaultValue(null);
			}
			if (!(val2 is string))
			{
				return WrapDouble(ToNumber(val2) + val1);
			}
			return ToString(val1) + (string) val2;
		}

		public static object ElemIncrDecr(object obj, double index, Context cx, int incrDecrMask)
		{
			return ScriptRuntime.ElemIncrDecr(obj, index, cx, incrDecrMask);
		}

		public static object[] PadStart(object[] currentArgs, int count)
		{
			object[] result = new object[currentArgs.Length + count];
			System.Array.Copy(currentArgs, 0, result, count, currentArgs.Length);
			return result;
		}

		public static void InitFunction(NativeFunction fn, int functionType, Scriptable scope, Context cx)
		{
			ScriptRuntime.InitFunction(cx, scope, fn, functionType, false);
		}

		public static object CallSpecial(Context cx, Callable fun, Scriptable thisObj, object[] args, Scriptable scope, Scriptable callerThis, int callType, string fileName, int lineNumber)
		{
			return ScriptRuntime.CallSpecial(cx, fun, thisObj, args, scope, callerThis, callType, fileName, lineNumber);
		}

		public static object NewObjectSpecial(Context cx, object fun, object[] args, Scriptable scope, Scriptable callerThis, int callType)
		{
			return ScriptRuntime.NewSpecial(cx, fun, args, scope, callType);
		}

		public static double WrapDouble(double num)
		{
			if (num == 0.0)
			{
				if (1 / num > 0)
				{
					// +0.0
					return zeroObj;
				}
			}
			else
			{
				if (num == 1.0)
				{
					return oneObj;
				}
				else
				{
					if (num == -1.0)
					{
						return minusOneObj;
					}
					else
					{
						if (num != num)
						{
							return NaNobj;
						}
					}
				}
			}
			return num;
		}

		internal static string EncodeIntArray(int[] array)
		{
			// XXX: this extremely inefficient for small integers
			if (array == null)
			{
				return null;
			}
			int n = array.Length;
			char[] buffer = new char[1 + n * 2];
			buffer[0] = (char)1;
			for (int i = 0; i != n; ++i)
			{
				int value = array[i];
				int shift = 1 + i * 2;
				buffer[shift] = (char)((int)(((uint)value) >> 16));
				buffer[shift + 1] = (char)value;
			}
			return new string(buffer);
		}

		private static int[] DecodeIntArray(string str, int arraySize)
		{
			// XXX: this extremely inefficient for small integers
			if (arraySize == 0)
			{
				if (str != null)
				{
					throw new ArgumentException();
				}
				return null;
			}
			if (str.Length != 1 + arraySize * 2 && str[0] != 1)
			{
				throw new ArgumentException();
			}
			int[] array = new int[arraySize];
			for (int i = 0; i != arraySize; ++i)
			{
				int shift = 1 + i * 2;
				array[i] = (str[shift] << 16) | str[shift + 1];
			}
			return array;
		}

		public static Scriptable NewArrayLiteral(object[] objects, string encodedInts, int skipCount, Context cx, Scriptable scope)
		{
			int[] skipIndexces = DecodeIntArray(encodedInts, skipCount);
			return NewArrayLiteral(objects, skipIndexces, cx, scope);
		}

		public static void Main(Script script, string[] args)
		{
			ContextFactory.GetGlobal().Call(cx =>
			{
				ScriptableObject global = ScriptRuntime.GetGlobal(cx);
				// get the command line arguments and define "arguments"
				// array in the top-level object
				object[] argsCopy = new object[args.Length];
				System.Array.Copy(args, 0, argsCopy, 0, args.Length);
				Scriptable argsObj = cx.NewArray(global, argsCopy);
				global.DefineProperty("arguments", argsObj, ScriptableObject.DONTENUM);
				script.Exec(cx, global);
				return null;
			});
		}

		public static void ThrowStopIteration(object obj)
		{
			throw new JavaScriptException(NativeIterator.GetStopIterationObject((Scriptable)obj), string.Empty, 0);
		}

		public static Scriptable CreateNativeGenerator(NativeFunction funObj, Scriptable scope, Scriptable thisObj, int maxLocals, int maxStack)
		{
			return new NativeGenerator(scope, funObj, new OptRuntime.GeneratorState(thisObj, maxLocals, maxStack));
		}

		public static object[] GetGeneratorStackState(object obj)
		{
			OptRuntime.GeneratorState rgs = (OptRuntime.GeneratorState)obj;
			if (rgs.stackState == null)
			{
				rgs.stackState = new object[rgs.maxStack];
			}
			return rgs.stackState;
		}

		public static object[] GetGeneratorLocalsState(object obj)
		{
			OptRuntime.GeneratorState rgs = (OptRuntime.GeneratorState)obj;
			if (rgs.localsState == null)
			{
				rgs.localsState = new object[rgs.maxLocals];
			}
			return rgs.localsState;
		}

		public class GeneratorState
		{
			internal const string CLASS_NAME = "org/mozilla/javascript/optimizer/OptRuntime$GeneratorState";

			public int resumptionPoint;

			internal const string resumptionPoint_NAME = "resumptionPoint";

			internal const string resumptionPoint_TYPE = "I";

			public Scriptable thisObj;

			internal const string thisObj_NAME = "thisObj";

			internal const string thisObj_TYPE = "Lorg/mozilla/javascript/Scriptable;";

			internal object[] stackState;

			internal object[] localsState;

			internal int maxLocals;

			internal int maxStack;

			internal GeneratorState(Scriptable thisObj, int maxLocals, int maxStack)
			{
				this.thisObj = thisObj;
				this.maxLocals = maxLocals;
				this.maxStack = maxStack;
			}
		}
	}
}
