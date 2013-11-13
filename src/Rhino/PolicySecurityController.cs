/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Org.Mozilla.Classfile;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// A security controller relying on Java
	/// <see cref="Sharpen.Policy">Sharpen.Policy</see>
	/// in effect. When you use
	/// this security controller, your securityDomain objects must be instances of
	/// <see cref="Sharpen.CodeSource">Sharpen.CodeSource</see>
	/// representing the location from where you load your
	/// scripts. Any Java policy "grant" statements matching the URL and certificate
	/// in code sources will apply to the scripts. If you specify any certificates
	/// within your
	/// <see cref="Sharpen.CodeSource">Sharpen.CodeSource</see>
	/// objects, it is your responsibility to verify
	/// (or not) that the script source files are signed in whatever
	/// implementation-specific way you're using.
	/// </summary>
	/// <author>Attila Szegedi</author>
	public class PolicySecurityController : SecurityController
	{
		private static readonly byte[] secureCallerImplBytecode = LoadBytecode();

		private static readonly IDictionary<CodeSource, IDictionary<ClassLoader, SoftReference<PolicySecurityController.SecureCaller>>> callers = new WeakHashMap<CodeSource, IDictionary<ClassLoader, SoftReference<PolicySecurityController.SecureCaller>>>();

		// We're storing a CodeSource -> (ClassLoader -> SecureRenderer), since we
		// need to have one renderer per class loader. We're using weak hash maps
		// and soft references all the way, since we don't want to interfere with
		// cleanup of either CodeSource or ClassLoader objects.
		public override Type GetStaticSecurityDomainClassInternal()
		{
			return typeof(CodeSource);
		}

		private class Loader : SecureClassLoader, GeneratedClassLoader
		{
			private readonly CodeSource codeSource;

			internal Loader(ClassLoader parent, CodeSource codeSource) : base(parent)
			{
				this.codeSource = codeSource;
			}

			public virtual Type DefineClass(string name, byte[] data)
			{
				return DefineClass(name, data, 0, data.Length, codeSource);
			}

			public virtual void LinkClass(Type cl)
			{
				ResolveClass(cl);
			}
		}

		public override GeneratedClassLoader CreateClassLoader(ClassLoader parent, object securityDomain)
		{
			return (PolicySecurityController.Loader)AccessController.DoPrivileged(new _PrivilegedAction_78(parent, securityDomain));
		}

		private sealed class _PrivilegedAction_78 : PrivilegedAction<object>
		{
			public _PrivilegedAction_78(ClassLoader parent, object securityDomain)
			{
				this.parent = parent;
				this.securityDomain = securityDomain;
			}

			public object Run()
			{
				return new PolicySecurityController.Loader(parent, (CodeSource)securityDomain);
			}

			private readonly ClassLoader parent;

			private readonly object securityDomain;
		}

		public override object GetDynamicSecurityDomain(object securityDomain)
		{
			// No separate notion of dynamic security domain - just return what was
			// passed in.
			return securityDomain;
		}

		public override object CallWithDomain(object securityDomain, Context cx, Callable callable, Scriptable scope, Scriptable thisObj, object[] args)
		{
			// Run in doPrivileged as we might be checked for "getClassLoader"
			// runtime permission
			ClassLoader classLoader = (ClassLoader)AccessController.DoPrivileged(new _PrivilegedAction_102(cx));
			CodeSource codeSource = (CodeSource)securityDomain;
			IDictionary<ClassLoader, SoftReference<PolicySecurityController.SecureCaller>> classLoaderMap;
			lock (callers)
			{
				classLoaderMap = callers.Get(codeSource);
				if (classLoaderMap == null)
				{
					classLoaderMap = new WeakHashMap<ClassLoader, SoftReference<PolicySecurityController.SecureCaller>>();
					callers.Put(codeSource, classLoaderMap);
				}
			}
			PolicySecurityController.SecureCaller caller;
			lock (classLoaderMap)
			{
				SoftReference<PolicySecurityController.SecureCaller> @ref = classLoaderMap.Get(classLoader);
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
						caller = (PolicySecurityController.SecureCaller)AccessController.DoPrivileged(new _PrivilegedExceptionAction_132(classLoader, codeSource));
						classLoaderMap.Put(classLoader, new SoftReference<PolicySecurityController.SecureCaller>(caller));
					}
					catch (PrivilegedActionException ex)
					{
						throw new UndeclaredThrowableException(ex.InnerException);
					}
				}
			}
			return caller.Call(callable, cx, scope, thisObj, args);
		}

		private sealed class _PrivilegedAction_102 : PrivilegedAction<object>
		{
			public _PrivilegedAction_102(Context cx)
			{
				this.cx = cx;
			}

			public object Run()
			{
				return cx.GetApplicationClassLoader();
			}

			private readonly Context cx;
		}

		private sealed class _PrivilegedExceptionAction_132 : PrivilegedExceptionAction<object>
		{
			public _PrivilegedExceptionAction_132(ClassLoader classLoader, CodeSource codeSource)
			{
				this.classLoader = classLoader;
				this.codeSource = codeSource;
			}

			/// <exception cref="System.Exception"></exception>
			public object Run()
			{
				PolicySecurityController.Loader loader = new PolicySecurityController.Loader(classLoader, codeSource);
				Type c = loader.DefineClass(typeof(PolicySecurityController.SecureCaller).FullName + "Impl", PolicySecurityController.secureCallerImplBytecode);
				return System.Activator.CreateInstance(c);
			}

			private readonly ClassLoader classLoader;

			private readonly CodeSource codeSource;
		}

		public abstract class SecureCaller
		{
			public abstract object Call(Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args);
		}

		private static byte[] LoadBytecode()
		{
			string secureCallerClassName = typeof(PolicySecurityController.SecureCaller).FullName;
			ClassFileWriter cfw = new ClassFileWriter(secureCallerClassName + "Impl", secureCallerClassName, "<generated>");
			cfw.StartMethod("<init>", "()V", ClassFileWriter.ACC_PUBLIC);
			cfw.AddALoad(0);
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, secureCallerClassName, "<init>", "()V");
			cfw.Add(ByteCode.RETURN);
			cfw.StopMethod((short)1);
			string callableCallSig = "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;)Ljava/lang/Object;";
			cfw.StartMethod("call", "(Lorg/mozilla/javascript/Callable;" + callableCallSig, (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_FINAL));
			for (int i = 1; i < 6; ++i)
			{
				cfw.AddALoad(i);
			}
			cfw.AddInvoke(ByteCode.INVOKEINTERFACE, "org/mozilla/javascript/Callable", "call", "(" + callableCallSig);
			cfw.Add(ByteCode.ARETURN);
			cfw.StopMethod((short)6);
			return cfw.ToByteArray();
		}
	}
}
