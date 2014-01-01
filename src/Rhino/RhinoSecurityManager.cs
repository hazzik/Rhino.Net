/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if ENCHANCED_SECURITY
using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// A <code>java.lang.SecurityManager</code> subclass that provides access to
	/// the current top-most script class on the execution stack.
	/// </summary>
	/// <remarks>
	/// A <code>java.lang.SecurityManager</code> subclass that provides access to
	/// the current top-most script class on the execution stack. This can be used
	/// to get the class loader or protection domain of the script that triggered
	/// the current action. It is required for JavaAdapters to have the same
	/// <code>ProtectionDomain</code> as the script code that created them.
	/// Embeddings that implement their own SecurityManager can use this as base class.
	/// </remarks>
	public class RhinoSecurityManager : SecurityManager
	{
		/// <summary>Get the class of the top-most stack element representing a script.</summary>
		/// <remarks>Get the class of the top-most stack element representing a script.</remarks>
		/// <returns>
		/// The class of the top-most script in the current stack,
		/// or null if no script is currently running
		/// </returns>
		protected internal virtual Type GetCurrentScriptClass()
		{
			Type[] context = GetClassContext();
			foreach (Type c in context)
			{
				if (c != typeof(InterpretedFunction) && typeof(NativeFunction).IsAssignableFrom(c) || typeof(PolicySecurityController.SecureCaller).IsAssignableFrom(c))
				{
					return c;
				}
			}
			return null;
		}
	}
}
#endif
