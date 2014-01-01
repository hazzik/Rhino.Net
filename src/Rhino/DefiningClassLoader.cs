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
	/// <summary>Load generated classes.</summary>
	/// <remarks>Load generated classes.</remarks>
	/// <author>Norris Boyd</author>
	public class DefiningClassLoader : ClassLoader, GeneratedClassLoader
	{
		public DefiningClassLoader()
		{
			this.parentLoader = GetType().GetClassLoader();
		}

		public DefiningClassLoader(ClassLoader parentLoader)
		{
			this.parentLoader = parentLoader;
		}

		public virtual Type DefineClass(string name, byte[] data)
		{
			// Use our own protection domain for the generated classes.
			// TODO: we might want to use a separate protection domain for classes
			// compiled from scripts, based on where the script was loaded from.
			return base.DefineClass(name, data, 0, data.Length, SecurityUtilities.GetProtectionDomain(GetType()));
		}

		public virtual void LinkClass(Type cl)
		{
			ResolveClass(cl);
		}

		/// <exception cref="System.TypeLoadException"></exception>
		protected virtual Type LoadClass(string name, bool resolve)
		{
			Type cl = FindLoadedClass(name);
			if (cl == null)
			{
				if (parentLoader != null)
				{
					cl = parentLoader.LoadClass(name);
				}
				else
				{
					cl = FindSystemClass(name);
				}
			}
			if (resolve)
			{
				ResolveClass(cl);
			}
			return cl;
		}

		private readonly ClassLoader parentLoader;
	}
}

#endif