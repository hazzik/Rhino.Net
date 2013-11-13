/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// Adapter to use JS function as implementation of Java interfaces with
	/// single method or multiple methods with the same signature.
	/// </summary>
	/// <remarks>
	/// Adapter to use JS function as implementation of Java interfaces with
	/// single method or multiple methods with the same signature.
	/// </remarks>
	public class InterfaceAdapter
	{
		private readonly object proxyHelper;

		/// <summary>
		/// Make glue object implementing interface cl that will
		/// call the supplied JS function when called.
		/// </summary>
		/// <remarks>
		/// Make glue object implementing interface cl that will
		/// call the supplied JS function when called.
		/// Only interfaces were all methods have the same signature is supported.
		/// </remarks>
		/// <returns>
		/// The glue object or null if <tt>cl</tt> is not interface or
		/// has methods with different signatures.
		/// </returns>
		internal static object Create(Context cx, Type cl, ScriptableObject @object)
		{
			if (!cl.IsInterface)
			{
				throw new ArgumentException();
			}
			Scriptable topScope = ScriptRuntime.GetTopCallScope(cx);
			ClassCache cache = ClassCache.Get(topScope);
			Rhino.InterfaceAdapter adapter;
			adapter = (Rhino.InterfaceAdapter)cache.GetInterfaceAdapter(cl);
			ContextFactory cf = cx.GetFactory();
			if (adapter == null)
			{
				MethodInfo[] methods = cl.GetMethods();
				if (@object is Callable)
				{
					// Check if interface can be implemented by a single function.
					// We allow this if the interface has only one method or multiple 
					// methods with the same name (in which case they'd result in 
					// the same function to be invoked anyway).
					int length = methods.Length;
					if (length == 0)
					{
						throw Context.ReportRuntimeError1("msg.no.empty.interface.conversion", cl.FullName);
					}
					if (length > 1)
					{
						string methodName = methods[0].Name;
						for (int i = 1; i < length; i++)
						{
							if (!methodName.Equals(methods[i].Name))
							{
								throw Context.ReportRuntimeError1("msg.no.function.interface.conversion", cl.FullName);
							}
						}
					}
				}
				adapter = new Rhino.InterfaceAdapter(cf, cl);
				cache.CacheInterfaceAdapter(cl, adapter);
			}
			return VMBridge.instance.NewInterfaceProxy(adapter.proxyHelper, cf, adapter, @object, topScope);
		}

		private InterfaceAdapter(ContextFactory cf, Type cl)
		{
			this.proxyHelper = VMBridge.instance.GetInterfaceProxyHelper(cf, new Type[] { cl });
		}

		public virtual object Invoke(ContextFactory cf, object target, Scriptable topScope, object thisObject, MethodInfo method, object[] args)
		{
			ContextAction action = new _ContextAction_80(this, target, topScope, thisObject, method, args);
			return cf.Call(action);
		}

		private sealed class _ContextAction_80 : ContextAction
		{
			public _ContextAction_80(InterfaceAdapter _enclosing, object target, Scriptable topScope, object thisObject, MethodInfo method, object[] args)
			{
				this._enclosing = _enclosing;
				this.target = target;
				this.topScope = topScope;
				this.thisObject = thisObject;
				this.method = method;
				this.args = args;
			}

			public object Run(Context cx)
			{
				return this._enclosing.InvokeImpl(cx, target, topScope, thisObject, method, args);
			}

			private readonly InterfaceAdapter _enclosing;

			private readonly object target;

			private readonly Scriptable topScope;

			private readonly object thisObject;

			private readonly MethodInfo method;

			private readonly object[] args;
		}

		internal virtual object InvokeImpl(Context cx, object target, Scriptable topScope, object thisObject, MethodInfo method, object[] args)
		{
			Callable function;
			if (target is Callable)
			{
				function = (Callable)target;
			}
			else
			{
				Scriptable s = (Scriptable)target;
				string methodName = method.Name;
				object value = ScriptableObject.GetProperty(s, methodName);
				if (value == ScriptableObject.NOT_FOUND)
				{
					// We really should throw an error here, but for the sake of
					// compatibility with JavaAdapter we silently ignore undefined
					// methods.
					Context.ReportWarning(ScriptRuntime.GetMessage1("msg.undefined.function.interface", methodName));
					Type resultType = method.ReturnType;
					if (resultType == typeof(void))
					{
						return null;
					}
					else
					{
						return Context.JsToJava(null, resultType);
					}
				}
				if (!(value is Callable))
				{
					throw Context.ReportRuntimeError1("msg.not.function.interface", methodName);
				}
				function = (Callable)value;
			}
			WrapFactory wf = cx.GetWrapFactory();
			if (args == null)
			{
				args = ScriptRuntime.emptyArgs;
			}
			else
			{
				for (int i = 0, N = args.Length; i != N; ++i)
				{
					object arg = args[i];
					// neutralize wrap factory java primitive wrap feature
					if (!(arg is string || arg is Number || arg is bool))
					{
						args[i] = wf.Wrap(cx, topScope, arg, null);
					}
				}
			}
			Scriptable thisObj = wf.WrapAsJavaObject(cx, topScope, thisObject, null);
			object result = function.Call(cx, topScope, thisObj, args);
			Type javaResultType = method.ReturnType;
			if (javaResultType == typeof(void))
			{
				result = null;
			}
			else
			{
				result = Context.JsToJava(result, javaResultType);
			}
			return result;
		}
	}
}
