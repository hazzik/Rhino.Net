/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using Sharpen;
using Thread = System.Threading.Thread;

namespace Rhino
{
	public class VMBridge
	{
		private static readonly ThreadLocal<object[]> context = new ThreadLocal<object[]>(() => new object[1]);

		internal static Context Context
		{
			get { return (Context) context.Value [0]; }
			set { context.Value [0] = value; }
		}

#if ENCHANCED_SECURITY
		internal static ClassLoader GetCurrentThreadClassLoader()
		{
			return Thread.CurrentThread.GetContextClassLoader();
		}
#endif

#if INTERFACE_ADAPTER
		private sealed class _InvocationHandler_107 : InvocationHandler
		{
			public _InvocationHandler_107(object target, InterfaceAdapter adapter, ContextFactory cf, Scriptable topScope)
			{
				this.target = target;
				this.adapter = adapter;
				this.cf = cf;
				this.topScope = topScope;
			}

			public object Invoke(object proxy, MethodInfo method, object[] args)
			{
				if (method.DeclaringType == typeof(object))
				{
					string methodName = method.Name;
					if (methodName.Equals("equals"))
					{
						object other = args[0];
						return proxy == other;
					}
					if (methodName.Equals("hashCode"))
					{
						return target.GetHashCode();
					}
					if (methodName.Equals("toString"))
					{
						return "Proxy[" + target + "]";
					}
				}
				return adapter.Invoke(cf, target, topScope, proxy, method, args);
			}

			private readonly object target;

			private readonly InterfaceAdapter adapter;

			private readonly ContextFactory cf;

			private readonly Scriptable topScope;
		}

		internal static object GetInterfaceProxyHelper(ContextFactory cf, Type[] interfaces)
		{
			// XXX: How to handle interfaces array withclasses from different
			// class loaders? Using cf.getApplicationClassLoader() ?
			ClassLoader loader = interfaces[0].GetClassLoader();
			Type cl = Proxy.GetProxyClass(loader, interfaces);
			ConstructorInfo c;
			try
			{
				c = cl.GetConstructor(new Type[] { typeof(InvocationHandler) });
			}
			catch (MissingMethodException ex)
			{
				// Should not happen
				throw new InvalidOperationException("Invalid operation", ex);
			}
			return c;
		}

		internal static object NewInterfaceProxy(object proxyHelper, ContextFactory cf, InterfaceAdapter adapter, object target, Scriptable topScope)
		{
			ConstructorInfo c = (ConstructorInfo)proxyHelper;
			InvocationHandler handler = new _InvocationHandler_107(target, adapter, cf, topScope);
			// In addition to methods declared in the interface, proxies
			// also route some java.lang.Object methods through the
			// invocation handler.
			// Note: we could compare a proxy and its wrapped function
			// as equal here but that would break symmetry of equal().
			// The reason == suffices here is that proxies are cached
			// in ScriptableObject (see NativeJavaObject.coerceType())
			object proxy;
			try
			{
				proxy = c.NewInstance(handler);
			}
			catch (TargetInvocationException ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
			catch (MemberAccessException ex)
			{
				// Should not happen
				throw new InvalidOperationException("Invalid operation", ex);
			}
			catch (InstantiationException ex)
			{
				// Should not happen
				throw new InvalidOperationException("Invalid operation", ex);
			}
			return proxy;
		}
#endif
	}
}
