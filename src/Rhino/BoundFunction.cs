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
	/// <summary>
	/// The class for results of the Function.bind operation
	/// EcmaScript 5 spec, 15.3.4.5
	/// </summary>
	/// <author>Raphael Speyer</author>
	[System.Serializable]
	public class BoundFunction : BaseFunction
	{
		private readonly Callable targetFunction;

		private readonly Scriptable boundThis;

		private readonly object[] boundArgs;

		private readonly int length;

		public BoundFunction(Context cx, Scriptable scope, Callable targetFunction, Scriptable boundThis, object[] boundArgs)
		{
			this.targetFunction = targetFunction;
			this.boundThis = boundThis;
			this.boundArgs = boundArgs;
			if (targetFunction is BaseFunction)
			{
				length = Math.Max(0, ((BaseFunction)targetFunction).Length - boundArgs.Length);
			}
			else
			{
				length = 0;
			}
			ScriptRuntime.SetFunctionProtoAndParent(this, scope);
			Function thrower = ScriptRuntime.TypeErrorThrower();
			NativeObject throwing = new NativeObject();
			throwing.Put("get", throwing, thrower);
			throwing.Put("set", throwing, thrower);
			throwing.Put("enumerable", throwing, false);
			throwing.Put("configurable", throwing, false);
			throwing.PreventExtensions();
			this.DefineOwnProperty(cx, "caller", throwing, false);
			this.DefineOwnProperty(cx, "arguments", throwing, false);
		}

		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] extraArgs)
		{
			Scriptable callThis = boundThis != null ? boundThis : ScriptRuntime.GetTopCallScope(cx);
			return targetFunction.Call(cx, scope, callThis, Concat(boundArgs, extraArgs));
		}

		public override Scriptable Construct(Context cx, Scriptable scope, object[] extraArgs)
		{
			if (targetFunction is Function)
			{
				return ((Function)targetFunction).Construct(cx, scope, Concat(boundArgs, extraArgs));
			}
			throw ScriptRuntime.TypeError0("msg.not.ctor");
		}

		public override bool HasInstance(Scriptable instance)
		{
			if (targetFunction is Function)
			{
				return ((Function)targetFunction).HasInstance(instance);
			}
			throw ScriptRuntime.TypeError0("msg.not.ctor");
		}

		public override int Length
		{
			get { return length; }
		}

		private object[] Concat(object[] first, object[] second)
		{
			object[] args = new object[first.Length + second.Length];
			System.Array.Copy(first, 0, args, 0, first.Length);
			System.Array.Copy(second, 0, args, first.Length, second.Length);
			return args;
		}
	}
}
