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
using Rhino.Jdk13;
using Sharpen;

namespace Rhino.Jdk13
{
	public class VMBridge_jdk13 : VMBridge
	{
		private ThreadLocal<object[]> contextLocal = new ThreadLocal<object[]>();

		protected internal override object GetThreadContextHelper()
		{
			// To make subsequent batch calls to getContext/setContext faster
			// associate permanently one element array with contextLocal
			// so getContext/setContext would need just to read/write the first
			// array element.
			// Note that it is necessary to use Object[], not Context[] to allow
			// garbage collection of Rhino classes. For details see comments
			// by Attila Szegedi in
			// https://bugzilla.mozilla.org/show_bug.cgi?id=281067#c5
			object[] storage = contextLocal.Get();
			if (storage == null)
			{
				storage = new object[1];
				contextLocal.Set(storage);
			}
			return storage;
		}

		protected internal override Context GetContext(object contextHelper)
		{
			object[] storage = (object[])contextHelper;
			return (Context)storage[0];
		}

		protected internal override void SetContext(object contextHelper, Context cx)
		{
			object[] storage = (object[])contextHelper;
			storage[0] = cx;
		}

		protected internal override ClassLoader GetCurrentThreadClassLoader()
		{
			return Sharpen.Thread.CurrentThread().GetContextClassLoader();
		}

		protected internal override bool TryToMakeAccessible(object accessibleObject)
		{
			if (!(accessibleObject is AccessibleObject))
			{
				return false;
			}
			AccessibleObject accessible = (AccessibleObject)accessibleObject;
			if (accessible.IsAccessible())
			{
				return true;
			}
			try
			{
			}
			catch (Exception)
			{
			}
			return accessible.IsAccessible();
		}

		protected internal override object GetInterfaceProxyHelper(ContextFactory cf, Type[] interfaces)
		{
			// XXX: How to handle interfaces array withclasses from different
			// class loaders? Using cf.getApplicationClassLoader() ?
			ClassLoader loader = interfaces[0].GetClassLoader();
			Type cl = Proxy.GetProxyClass(loader, interfaces);
			ConstructorInfo<object> c;
			try
			{
				c = cl.GetConstructor(new Type[] { typeof(InvocationHandler) });
			}
			catch (MissingMethodException ex)
			{
				// Should not happen
				throw Kit.InitCause(new InvalidOperationException(), ex);
			}
			return c;
		}

		protected internal override object NewInterfaceProxy(object proxyHelper, ContextFactory cf, InterfaceAdapter adapter, object target, Scriptable topScope)
		{
			ConstructorInfo<object> c = (ConstructorInfo<object>)proxyHelper;
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
				throw Kit.InitCause(new InvalidOperationException(), ex);
			}
			catch (InstantiationException ex)
			{
				// Should not happen
				throw Kit.InitCause(new InvalidOperationException(), ex);
			}
			return proxy;
		}

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
						return Sharpen.Extensions.ValueOf(proxy == other);
					}
					if (methodName.Equals("hashCode"))
					{
						return Sharpen.Extensions.ValueOf(target.GetHashCode());
					}
					if (methodName.Equals("toString"))
					{
						return "Proxy[" + target.ToString() + "]";
					}
				}
				return adapter.Invoke(cf, target, topScope, proxy, method, args);
			}

			private readonly object target;

			private readonly InterfaceAdapter adapter;

			private readonly ContextFactory cf;

			private readonly Scriptable topScope;
		}

		protected internal override bool IsVarArgs(MemberInfo member)
		{
			return false;
		}
	}
}
