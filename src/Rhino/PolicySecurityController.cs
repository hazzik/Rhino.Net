/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Utils;
#if ENHANCED_SECURITY
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Org.Mozilla.Classfile;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// A security controller relying on Java
	/// <see cref="Policy">Sharpen.Policy</see>
	/// in effect. When you use
	/// this security controller, your securityDomain objects must be instances of
	/// <see cref="CodeSource">Sharpen.CodeSource</see>
	/// representing the location from where you load your
	/// scripts. Any Java policy "grant" statements matching the URL and certificate
	/// in code sources will apply to the scripts. If you specify any certificates
	/// within your
	/// <see cref="CodeSource">Sharpen.CodeSource</see>
	/// objects, it is your responsibility to verify
	/// (or not) that the script source files are signed in whatever
	/// implementation-specific way you're using.
	/// </summary>
	/// <author>Attila Szegedi</author>
	public class PolicySecurityController : SecurityController
	{
		private static readonly byte[] secureCallerImplBytecode = LoadByteCode();

		private static readonly IDictionary<CodeSource, IDictionary<ClassLoader, SoftReference<SecureCaller>>> callers = new WeakHashMap<CodeSource, IDictionary<ClassLoader, SoftReference<SecureCaller>>>();

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
			return (Loader)AccessController.DoPrivileged(new _PrivilegedAction_78(parent, securityDomain));
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
				return new Loader(parent, (CodeSource)securityDomain);
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
			IDictionary<ClassLoader, SoftReference<SecureCaller>> classLoaderMap;
			lock (callers)
			{
				classLoaderMap = callers.GetValueOrDefault(codeSource);
				if (classLoaderMap == null)
				{
					classLoaderMap = new WeakHashMap<ClassLoader, SoftReference<SecureCaller>>();
					callers[codeSource] = classLoaderMap;
				}
			}
			SecureCaller caller;
			lock (classLoaderMap)
			{
				SoftReference<SecureCaller> @ref = classLoaderMap.GetValueOrDefault(classLoader);
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
						caller = (SecureCaller)AccessController.DoPrivileged(new _PrivilegedExceptionAction_132(classLoader, codeSource));
						classLoaderMap[classLoader] = new SoftReference<SecureCaller>(caller);
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
				Loader loader = new Loader(classLoader, codeSource);
				Type c = loader.DefineClass(typeof(SecureCaller).FullName + "Impl", secureCallerImplBytecode);
				return Activator.CreateInstance(c);
			}

			private readonly ClassLoader classLoader;

			private readonly CodeSource codeSource;
		}

		public abstract class SecureCaller
		{
			public abstract object Call(Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args);
		}

		private static ModuleBuilder module;

		private static byte[] LoadByteCode()
		{
			var baseType = typeof (SecureCaller);
			var type = module.DefineType(baseType.Name + "Impl", TypeAttributes.Public, baseType);
			GenerateConstructor(type, baseType);
			GenerateCall(type);

			return cfw.ToByteArray();
		}

		private static void GenerateCall(TypeBuilder type)
		{
			var method = type.DefineMethod("Call", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual, typeof (object), new[] {typeof (Context), typeof (Scriptable), typeof (Scriptable), typeof (object[])});
			var il = method.GetILGenerator();
			il.Emit(OpCodes.Ldarg_1); // callable
			il.Emit(OpCodes.Ldarg_2); // cx
			il.Emit(OpCodes.Ldarg_3); // scope
			il.Emit(OpCodes.Ldarg_S, (byte) 4); // thisObj
			il.Emit(OpCodes.Ldarg_S, (byte) 5); // args
			il.Emit(OpCodes.Callvirt, typeof (Callable).GetMethod("Call", new[] {typeof (Context), typeof (Scriptable), typeof (Scriptable), typeof (object[])}));
			il.Emit(OpCodes.Ret);
		}

		private static void GenerateConstructor(TypeBuilder type, Type baseType)
		{
			var constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
			var il = constructor.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Call, baseType.GetConstructor(Type.EmptyTypes));
			il.Emit(OpCodes.Ret);
		}
	}
}
#endif