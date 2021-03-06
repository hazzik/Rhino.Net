/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if INTERFACE_ADAPTER
using System;
using System.Reflection;
using Rhino;
using Rhino.Utils;
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
			InterfaceAdapter adapter = (Rhino.InterfaceAdapter) cache.GetInterfaceAdapter(cl);
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
							if (methodName != methods[i].Name)
							{
								throw Context.ReportRuntimeError1("msg.no.function.interface.conversion", cl.FullName);
							}
						}
					}
				}
				adapter = new Rhino.InterfaceAdapter(cf, cl);
				cache.CacheInterfaceAdapter(cl, adapter);
			}
			return VMBridge.NewInterfaceProxy(adapter.proxyHelper, cf, adapter, @object, topScope);
		}

		private InterfaceAdapter(ContextFactory cf, Type cl)
		{
			this.proxyHelper = VMBridge.GetInterfaceProxyHelper(cf, new Type[] { cl });
		}

		public virtual object Invoke(ContextFactory cf, object target, Scriptable topScope, object thisObject, MethodInfo method, object[] args)
		{
			return cf.Call(cx => InvokeImpl(cx, target, topScope, thisObject, method, args));
		}

		internal virtual object InvokeImpl(Context cx, object target, Scriptable topScope, object thisObject, MethodInfo method, object[] args)
		{
			Callable function;
			var callable = target as Callable;
			if (callable != null)
			{
				function = callable;
			}
			else
			{
				Scriptable s = (Scriptable)target;
				string methodName = method.Name;
				object value = ScriptableObject.GetProperty(s, methodName);
				if (value == ScriptableConstants.NOT_FOUND)
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
					if (!(arg is string || arg.IsNumber() || arg is bool))
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
#endif
