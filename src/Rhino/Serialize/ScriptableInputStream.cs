/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if SERIALIZATION
using System;
using System.IO;
using Rhino;
using Rhino.Serialize;
using Sharpen;

namespace Rhino.Serialize
{
	/// <summary>
	/// Class ScriptableInputStream is used to read in a JavaScript
	/// object or function previously serialized with a ScriptableOutputStream.
	/// </summary>
	/// <remarks>
	/// Class ScriptableInputStream is used to read in a JavaScript
	/// object or function previously serialized with a ScriptableOutputStream.
	/// References to names in the exclusion list
	/// replaced with references to the top-level scope specified during
	/// creation of the ScriptableInputStream.
	/// </remarks>
	/// <author>Norris Boyd</author>
	public class ScriptableInputStream : ObjectInputStream
	{
		/// <summary>Create a ScriptableInputStream.</summary>
		/// <remarks>Create a ScriptableInputStream.</remarks>
		/// <param name="in">the InputStream to read from.</param>
		/// <param name="scope">the top-level scope to create the object in.</param>
		/// <exception cref="System.IO.IOException"></exception>
		public ScriptableInputStream(Stream @in, Scriptable scope) : base(@in)
		{
			// API class
			this.scope = scope;
			EnableResolveObject(true);
			Context cx = Context.GetCurrentContext();
			if (cx != null)
			{
				this.classLoader = cx.GetApplicationClassLoader();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		protected override Type ResolveClass(ObjectStreamClass desc)
		{
			string name = desc.GetName();
			if (classLoader != null)
			{
				try
				{
					return classLoader.LoadClass(name);
				}
				catch (TypeLoadException)
				{
				}
			}
			// fall through to default loading
			return base.ResolveClass(desc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override object ResolveObject(object obj)
		{
			if (obj is ScriptableOutputStream.PendingLookup)
			{
				string name = ((ScriptableOutputStream.PendingLookup)obj).GetName();
				obj = ScriptableOutputStream.LookupQualifiedName(scope, name);
				if (obj == ScriptableConstants.NOT_FOUND)
				{
					throw new IOException("Object " + name + " not found upon " + "deserialization.");
				}
			}
			else
			{
				if (obj is UniqueTag)
				{
					obj = ((UniqueTag)obj).ReadResolve();
				}
				else
				{
					if (obj is Undefined)
					{
						obj = ((Undefined)obj).ReadResolve();
					}
				}
			}
			return obj;
		}

		private Scriptable scope;

		private ClassLoader classLoader;
	}
}
#endif
