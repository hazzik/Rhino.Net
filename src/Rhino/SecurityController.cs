/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Security;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class describes the support needed to implement security.</summary>
	/// <remarks>
	/// This class describes the support needed to implement security.
	/// <p>
	/// Three main pieces of functionality are required to implement
	/// security for JavaScript. First, it must be possible to define
	/// classes with an associated security domain. (This security
	/// domain may be any object incorporating notion of access
	/// restrictions that has meaning to an embedding; for a client-side
	/// JavaScript embedding this would typically be
	/// java.security.ProtectionDomain or similar object depending on an
	/// origin URL and/or a digital certificate.)
	/// Next it must be possible to get a security domain object that
	/// allows a particular action only if all security domains
	/// associated with code on the current Java stack allows it. And
	/// finally, it must be possible to execute script code with
	/// associated security domain injected into Java stack.
	/// <p>
	/// These three pieces of functionality are encapsulated in the
	/// SecurityController class.
	/// </remarks>
	/// <seealso cref="Context.SetSecurityController(SecurityController)">Context.SetSecurityController(SecurityController)</seealso>
	/// <seealso cref="Sharpen.ClassLoader">Sharpen.ClassLoader</seealso>
	/// <since>1.5 Release 4</since>
	public abstract class SecurityController
	{
		private static SecurityController global;

		// API class
		// The method must NOT be public or protected
		internal static SecurityController Global()
		{
			return global;
		}

		/// <summary>
		/// Check if global
		/// <see cref="SecurityController">SecurityController</see>
		/// was already installed.
		/// </summary>
		/// <seealso cref="InitGlobal(SecurityController)">InitGlobal(SecurityController)</seealso>
		public static bool HasGlobal()
		{
			return global != null;
		}

		/// <summary>
		/// Initialize global controller that will be used for all
		/// security-related operations.
		/// </summary>
		/// <remarks>
		/// Initialize global controller that will be used for all
		/// security-related operations. The global controller takes precedence
		/// over already installed
		/// <see cref="Context">Context</see>
		/// -specific controllers and cause
		/// any subsequent call to
		/// <see cref="Context.SetSecurityController(SecurityController)">Context.SetSecurityController(SecurityController)</see>
		/// to throw an exception.
		/// <p>
		/// The method can only be called once.
		/// </remarks>
		/// <seealso cref="HasGlobal()">HasGlobal()</seealso>
		public static void InitGlobal(SecurityController controller)
		{
			if (controller == null)
			{
				throw new ArgumentException();
			}
			if (global != null)
			{
				throw new SecurityException("Cannot overwrite already installed global SecurityController");
			}
			global = controller;
		}

		/// <summary>
		/// Get class loader-like object that can be used
		/// to define classes with the given security context.
		/// </summary>
		/// <remarks>
		/// Get class loader-like object that can be used
		/// to define classes with the given security context.
		/// </remarks>
		/// <param name="parentLoader">
		/// parent class loader to delegate search for classes
		/// not defined by the class loader itself
		/// </param>
		/// <param name="securityDomain">
		/// some object specifying the security
		/// context of the code that is defined by the returned class loader.
		/// </param>
		public abstract GeneratedClassLoader CreateClassLoader(ClassLoader parentLoader, object securityDomain);

		/// <summary>
		/// Create
		/// <see cref="GeneratedClassLoader">GeneratedClassLoader</see>
		/// with restrictions imposed by
		/// staticDomain and all current stack frames.
		/// The method uses the SecurityController instance associated with the
		/// current
		/// <see cref="Context">Context</see>
		/// to construct proper dynamic domain and create
		/// corresponding class loader.
		/// <par>
		/// If no SecurityController is associated with the current
		/// <see cref="Context">Context</see>
		/// ,
		/// the method calls
		/// <see cref="Context.CreateClassLoader(Sharpen.ClassLoader)">Context.CreateClassLoader(Sharpen.ClassLoader)</see>
		/// .
		/// </summary>
		/// <param name="parent">
		/// parent class loader. If null,
		/// <see cref="Context.GetApplicationClassLoader()">Context.GetApplicationClassLoader()</see>
		/// will be used.
		/// </param>
		/// <param name="staticDomain">static security domain.</param>
		public static GeneratedClassLoader CreateLoader(ClassLoader parent, object staticDomain)
		{
			Context cx = Context.GetContext();
			if (parent == null)
			{
				parent = cx.GetApplicationClassLoader();
			}
			SecurityController sc = cx.GetSecurityController();
			GeneratedClassLoader loader;
			if (sc == null)
			{
				loader = cx.CreateClassLoader(parent);
			}
			else
			{
				object dynamicDomain = sc.GetDynamicSecurityDomain(staticDomain);
				loader = sc.CreateClassLoader(parent, dynamicDomain);
			}
			return loader;
		}

		public static Type GetStaticSecurityDomainClass()
		{
			SecurityController sc = Context.GetContext().GetSecurityController();
			return sc == null ? null : sc.GetStaticSecurityDomainClassInternal();
		}

		public virtual Type GetStaticSecurityDomainClassInternal()
		{
			return null;
		}

		/// <summary>
		/// Get dynamic security domain that allows an action only if it is allowed
		/// by the current Java stack and <i>securityDomain</i>.
		/// </summary>
		/// <remarks>
		/// Get dynamic security domain that allows an action only if it is allowed
		/// by the current Java stack and <i>securityDomain</i>. If
		/// <i>securityDomain</i> is null, return domain representing permissions
		/// allowed by the current stack.
		/// </remarks>
		public abstract object GetDynamicSecurityDomain(object securityDomain);

		/// <summary>
		/// Call
		/// <see cref="Callable.Call(Context, Scriptable, Scriptable, object[])">Callable.Call(Context, Scriptable, Scriptable, object[])</see>
		/// of <i>callable</i> under restricted security domain where an action is
		/// allowed only if it is allowed according to the Java stack on the
		/// moment of the <i>execWithDomain</i> call and <i>securityDomain</i>.
		/// Any call to
		/// <see cref="GetDynamicSecurityDomain(object)">GetDynamicSecurityDomain(object)</see>
		/// during
		/// execution of <tt>callable.call(cx, scope, thisObj, args)</tt>
		/// should return a domain incorporate restrictions imposed by
		/// <i>securityDomain</i> and Java stack on the moment of callWithDomain
		/// invocation.
		/// <p>
		/// The method should always be overridden, it is not declared abstract
		/// for compatibility reasons.
		/// </summary>
		public virtual object CallWithDomain(object securityDomain, Context cx, Callable callable, Scriptable scope, Scriptable thisObj, object[] args)
		{
			throw new InvalidOperationException("callWithDomain should be overridden");
		}
	}
}
