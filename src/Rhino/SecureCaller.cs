/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <author>Attila Szegedi</author>
	public abstract class SecureCaller
	{
		private static readonly byte[] secureCallerImplBytecode = LoadBytecode();

		private static readonly IDictionary<CodeSource, IDictionary<ClassLoader, SoftReference<SecureCaller>>> callers = new WeakHashMap<CodeSource, IDictionary<ClassLoader, SoftReference<SecureCaller>>>();

		// We're storing a CodeSource -> (ClassLoader -> SecureRenderer), since we
		// need to have one renderer per class loader. We're using weak hash maps
		// and soft references all the way, since we don't want to interfere with
		// cleanup of either CodeSource or ClassLoader objects.
		public abstract object Call(Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args);

		/// <summary>
		/// Call the specified callable using a protection domain belonging to the
		/// specified code source.
		/// </summary>
		/// <remarks>
		/// Call the specified callable using a protection domain belonging to the
		/// specified code source.
		/// </remarks>
		internal static object CallSecurely(CodeSource codeSource, Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			Sharpen.Thread thread = Sharpen.Thread.CurrentThread();
			// Run in doPrivileged as we might be checked for "getClassLoader"
			// runtime permission
			ClassLoader classLoader = (ClassLoader)AccessController.DoPrivileged(new _PrivilegedAction_51(thread));
			IDictionary<ClassLoader, SoftReference<SecureCaller>> classLoaderMap;
			lock (callers)
			{
				classLoaderMap = callers.Get(codeSource);
				if (classLoaderMap == null)
				{
					classLoaderMap = new WeakHashMap<ClassLoader, SoftReference<SecureCaller>>();
					callers.Put(codeSource, classLoaderMap);
				}
			}
			SecureCaller caller;
			lock (classLoaderMap)
			{
				SoftReference<SecureCaller> @ref = classLoaderMap.Get(classLoader);
				if (@ref != null)
				{
					caller = @ref.Get();
				}
				else
				{
					caller = null;
				}
				if (caller == null)
				{
					try
					{
						// Run in doPrivileged as we'll be checked for
						// "createClassLoader" runtime permission
						caller = (SecureCaller)AccessController.DoPrivileged(new _PrivilegedExceptionAction_82(classLoader, codeSource));
						classLoaderMap.Put(classLoader, new SoftReference<SecureCaller>(caller));
					}
					catch (PrivilegedActionException ex)
					{
						throw new UndeclaredThrowableException(ex.InnerException);
					}
				}
			}
			return caller.Call(callable, cx, scope, thisObj, args);
		}

		private sealed class _PrivilegedAction_51 : PrivilegedAction<object>
		{
			public _PrivilegedAction_51(Sharpen.Thread thread)
			{
				this.thread = thread;
			}

			public object Run()
			{
				return thread.GetContextClassLoader();
			}

			private readonly Sharpen.Thread thread;
		}

		private sealed class _PrivilegedExceptionAction_82 : PrivilegedExceptionAction<object>
		{
			public _PrivilegedExceptionAction_82(ClassLoader classLoader, CodeSource codeSource)
			{
				this.classLoader = classLoader;
				this.codeSource = codeSource;
			}

			/// <exception cref="System.Exception"></exception>
			public object Run()
			{
				ClassLoader effectiveClassLoader;
				Type thisClass = this.GetType();
				if (classLoader.LoadClass(thisClass.FullName) != thisClass)
				{
					effectiveClassLoader = thisClass.GetClassLoader();
				}
				else
				{
					effectiveClassLoader = classLoader;
				}
				SecureCaller.SecureClassLoaderImpl secCl = new SecureCaller.SecureClassLoaderImpl(effectiveClassLoader);
				Type c = secCl.DefineAndLinkClass(typeof(SecureCaller).FullName + "Impl", SecureCaller.secureCallerImplBytecode, codeSource);
				return System.Activator.CreateInstance(c);
			}

			private readonly ClassLoader classLoader;

			private readonly CodeSource codeSource;
		}

		private class SecureClassLoaderImpl : SecureClassLoader
		{
			internal SecureClassLoaderImpl(ClassLoader parent) : base(parent)
			{
			}

			internal virtual Type DefineAndLinkClass(string name, byte[] bytes, CodeSource cs)
			{
				Type cl = DefineClass(name, bytes, 0, bytes.Length, cs);
				ResolveClass(cl);
				return cl;
			}
		}

		private static byte[] LoadBytecode()
		{
			return (byte[])AccessController.DoPrivileged(new _PrivilegedAction_129());
		}

		private sealed class _PrivilegedAction_129 : PrivilegedAction<object>
		{
			public _PrivilegedAction_129()
			{
			}

			public object Run()
			{
				return SecureCaller.LoadBytecodePrivileged();
			}
		}

		private static byte[] LoadBytecodePrivileged()
		{
			Uri url = typeof(SecureCaller).GetResource("SecureCallerImpl.clazz");
			try
			{
				InputStream @in = url.OpenStream();
				try
				{
					ByteArrayOutputStream bout = new ByteArrayOutputStream();
					for (; ; )
					{
						int r = @in.Read();
						if (r == -1)
						{
							return bout.ToByteArray();
						}
						bout.Write(r);
					}
				}
				finally
				{
					@in.Close();
				}
			}
			catch (IOException e)
			{
				throw new UndeclaredThrowableException(e);
			}
		}
	}
}