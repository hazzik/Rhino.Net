/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Rhino;
using Sharpen;

namespace Rhino
{
	public abstract class VMBridge
	{
		internal static readonly VMBridge instance = MakeInstance();

		// API class
		private static VMBridge MakeInstance()
		{
			string[] classNames = new string[] { "org.mozilla.javascript.VMBridge_custom", "org.mozilla.javascript.jdk15.VMBridge_jdk15", "org.mozilla.javascript.jdk13.VMBridge_jdk13", "org.mozilla.javascript.jdk11.VMBridge_jdk11" };
			for (int i = 0; i != classNames.Length; ++i)
			{
				string className = classNames[i];
				Type cl = Kit.ClassOrNull(className);
				if (cl != null)
				{
					VMBridge bridge = (VMBridge)Kit.NewInstanceOrNull(cl);
					if (bridge != null)
					{
						return bridge;
					}
				}
			}
			throw new InvalidOperationException("Failed to create VMBridge instance");
		}

		/// <summary>
		/// Return a helper object to optimize
		/// <see cref="Context">Context</see>
		/// access.
		/// <p>
		/// The runtime will pass the resulting helper object to the subsequent
		/// calls to
		/// <see cref="GetContext(object)">GetContext(object)</see>
		/// and
		/// <see cref="SetContext(object, Context)">SetContext(object, Context)</see>
		/// methods.
		/// In this way the implementation can use the helper to cache
		/// information about current thread to make
		/// <see cref="Context">Context</see>
		/// access faster.
		/// </summary>
		protected internal abstract object GetThreadContextHelper();

		/// <summary>
		/// Get
		/// <see cref="Context">Context</see>
		/// instance associated with the current thread
		/// or null if none.
		/// </summary>
		/// <param name="contextHelper">
		/// The result of
		/// <see cref="GetThreadContextHelper()">GetThreadContextHelper()</see>
		/// called from the current thread.
		/// </param>
		protected internal abstract Context GetContext(object contextHelper);

		/// <summary>
		/// Associate
		/// <see cref="Context">Context</see>
		/// instance with the current thread or remove
		/// the current association if <tt>cx</tt> is null.
		/// </summary>
		/// <param name="contextHelper">
		/// The result of
		/// <see cref="GetThreadContextHelper()">GetThreadContextHelper()</see>
		/// called from the current thread.
		/// </param>
		protected internal abstract void SetContext(object contextHelper, Context cx);

		/// <summary>Return the ClassLoader instance associated with the current thread.</summary>
		/// <remarks>Return the ClassLoader instance associated with the current thread.</remarks>
		protected internal abstract ClassLoader GetCurrentThreadClassLoader();

		/// <summary>
		/// In many JVMSs, public methods in private
		/// classes are not accessible by default (Sun Bug #4071593).
		/// </summary>
		/// <remarks>
		/// In many JVMSs, public methods in private
		/// classes are not accessible by default (Sun Bug #4071593).
		/// VMBridge instance should try to workaround that via, for example,
		/// calling method.setAccessible(true) when it is available.
		/// The implementation is responsible to catch all possible exceptions
		/// like SecurityException if the workaround is not available.
		/// </remarks>
		/// <returns>
		/// true if it was possible to make method accessible
		/// or false otherwise.
		/// </returns>
		protected internal abstract bool TryToMakeAccessible(object accessibleObject);

		/// <summary>
		/// Create helper object to create later proxies implementing the specified
		/// interfaces later.
		/// </summary>
		/// <remarks>
		/// Create helper object to create later proxies implementing the specified
		/// interfaces later. Under JDK 1.3 the implementation can look like:
		/// <pre>
		/// return java.lang.reflect.Proxy.getProxyClass(..., interfaces).
		/// getConstructor(new Class[] {
		/// java.lang.reflect.InvocationHandler.class });
		/// </pre>
		/// </remarks>
		/// <param name="interfaces">Array with one or more interface class objects.</param>
		protected internal virtual object GetInterfaceProxyHelper(ContextFactory cf, Type[] interfaces)
		{
			throw Context.ReportRuntimeError("VMBridge.getInterfaceProxyHelper is not supported");
		}

		/// <summary>
		/// Create proxy object for
		/// <see cref="InterfaceAdapter">InterfaceAdapter</see>
		/// . The proxy should call
		/// <see cref="InterfaceAdapter.Invoke(ContextFactory, object, Scriptable, object, System.Reflection.MethodInfo, object[])">InterfaceAdapter.Invoke(ContextFactory, object, Scriptable, object, System.Reflection.MethodInfo, object[])</see>
		/// as implementation of interface methods associated with
		/// <tt>proxyHelper</tt>.
		/// </summary>
		/// <param name="proxyHelper">
		/// The result of the previous call to
		/// <see cref="GetInterfaceProxyHelper(ContextFactory, System.Type{T}[])">GetInterfaceProxyHelper(ContextFactory, System.Type&lt;T&gt;[])</see>
		/// .
		/// </param>
		protected internal virtual object NewInterfaceProxy(object proxyHelper, ContextFactory cf, InterfaceAdapter adapter, object target, Scriptable topScope)
		{
			throw Context.ReportRuntimeError("VMBridge.newInterfaceProxy is not supported");
		}

		/// <summary>
		/// Returns whether or not a given member (method or constructor)
		/// has variable arguments.
		/// </summary>
		/// <remarks>
		/// Returns whether or not a given member (method or constructor)
		/// has variable arguments.
		/// Variable argument methods have only been supported in Java since
		/// JDK 1.5.
		/// </remarks>
		protected internal abstract bool IsVarArgs(MemberInfo member);

		/// <summary>
		/// If "obj" is a java.util.Iterator or a java.lang.Iterable, return a
		/// wrapping as a JavaScript Iterator.
		/// </summary>
		/// <remarks>
		/// If "obj" is a java.util.Iterator or a java.lang.Iterable, return a
		/// wrapping as a JavaScript Iterator. Otherwise, return null.
		/// This method is in VMBridge since Iterable is a JDK 1.5 addition.
		/// </remarks>
		public virtual IEnumerator<object> GetJavaIterator(Context cx, Scriptable scope, object obj)
		{
			if (obj is Wrapper)
			{
				object unwrapped = ((Wrapper)obj).Unwrap();
				IEnumerator<object> iterator = null;
				if (unwrapped is IEnumerator)
				{
					iterator = (IEnumerator<object>)unwrapped;
				}
				return iterator;
			}
			return null;
		}
	}
}
