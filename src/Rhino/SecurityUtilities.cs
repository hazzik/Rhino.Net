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
	/// <author>Attila Szegedi</author>
	public class SecurityUtilities
	{
		/// <summary>Retrieves a system property within a privileged block.</summary>
		/// <remarks>
		/// Retrieves a system property within a privileged block. Use it only when
		/// the property is used from within Rhino code and is not passed out of it.
		/// </remarks>
		/// <param name="name">the name of the system property</param>
		/// <returns>the value of the system property</returns>
		public static string GetSystemProperty(string name)
		{
			return AccessController.DoPrivileged(new _PrivilegedAction_28(name));
		}

		private sealed class _PrivilegedAction_28 : PrivilegedAction<string>
		{
			public _PrivilegedAction_28(string name)
			{
				this.name = name;
			}

			public string Run()
			{
				return Runtime.GetProperty(name);
			}

			private readonly string name;
		}

		public static ProtectionDomain GetProtectionDomain(Type clazz)
		{
			return AccessController.DoPrivileged(new _PrivilegedAction_40(clazz));
		}

		private sealed class _PrivilegedAction_40 : PrivilegedAction<ProtectionDomain>
		{
			public _PrivilegedAction_40(Type clazz)
			{
				this.clazz = clazz;
			}

			public ProtectionDomain Run()
			{
				return clazz.GetProtectionDomain();
			}

			private readonly Type clazz;
		}

		/// <summary>
		/// Look up the top-most element in the current stack representing a
		/// script and return its protection domain.
		/// </summary>
		/// <remarks>
		/// Look up the top-most element in the current stack representing a
		/// script and return its protection domain. This relies on the system-wide
		/// SecurityManager being an instance of
		/// <see cref="RhinoSecurityManager">RhinoSecurityManager</see>
		/// ,
		/// otherwise it returns <code>null</code>.
		/// </remarks>
		/// <returns>The protection of the top-most script in the current stack, or null</returns>
		public static ProtectionDomain GetScriptProtectionDomain()
		{
			SecurityManager securityManager = Runtime.GetSecurityManager();
			if (securityManager is RhinoSecurityManager)
			{
				return AccessController.DoPrivileged(new _PrivilegedAction_59(securityManager));
			}
			return null;
		}

		private sealed class _PrivilegedAction_59 : PrivilegedAction<ProtectionDomain>
		{
			public _PrivilegedAction_59(SecurityManager securityManager)
			{
				this.securityManager = securityManager;
			}

			public ProtectionDomain Run()
			{
				Type c = ((RhinoSecurityManager)securityManager).GetCurrentScriptClass();
				return c == null ? null : c.GetProtectionDomain();
			}

			private readonly SecurityManager securityManager;
		}
	}
}
