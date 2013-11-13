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
using Rhino;
using Rhino.Serialize;
using Sharpen;

namespace Rhino.Serialize
{
	/// <summary>
	/// Class ScriptableOutputStream is an ObjectOutputStream used
	/// to serialize JavaScript objects and functions.
	/// </summary>
	/// <remarks>
	/// Class ScriptableOutputStream is an ObjectOutputStream used
	/// to serialize JavaScript objects and functions. Note that
	/// compiled functions currently cannot be serialized, only
	/// interpreted functions. The top-level scope containing the
	/// object is not written out, but is instead replaced with
	/// another top-level object when the ScriptableInputStream
	/// reads in this object. Also, object corresponding to names
	/// added to the exclude list are not written out but instead
	/// are looked up during deserialization. This approach avoids
	/// the creation of duplicate copies of standard objects
	/// during deserialization.
	/// </remarks>
	/// <author>Norris Boyd</author>
	public class ScriptableOutputStream : ObjectOutputStream
	{
		/// <summary>ScriptableOutputStream constructor.</summary>
		/// <remarks>
		/// ScriptableOutputStream constructor.
		/// Creates a ScriptableOutputStream for use in serializing
		/// JavaScript objects. Calls excludeStandardObjectNames.
		/// </remarks>
		/// <param name="out">the OutputStream to write to.</param>
		/// <param name="scope">the scope containing the object.</param>
		/// <exception cref="System.IO.IOException"></exception>
		public ScriptableOutputStream(OutputStream @out, Scriptable scope) : base(@out)
		{
			// API class
			this.scope = scope;
			table = new Dictionary<object, string>();
			table.Put(scope, string.Empty);
			EnableReplaceObject(true);
			ExcludeStandardObjectNames();
		}

		// XXX
		public virtual void ExcludeAllIds(object[] ids)
		{
			foreach (object id in ids)
			{
				if (id is string && (scope.Get((string)id, scope) is Scriptable))
				{
					this.AddExcludedName((string)id);
				}
			}
		}

		/// <summary>
		/// Adds a qualified name to the list of object to be excluded from
		/// serialization.
		/// </summary>
		/// <remarks>
		/// Adds a qualified name to the list of object to be excluded from
		/// serialization. Names excluded from serialization are looked up
		/// in the new scope and replaced upon deserialization.
		/// </remarks>
		/// <param name="name">
		/// a fully qualified name (of the form "a.b.c", where
		/// "a" must be a property of the top-level object). The object
		/// need not exist, in which case the name is ignored.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if the object is not a
		/// <see cref="Rhino.Scriptable">Rhino.Scriptable</see>
		/// .
		/// </exception>
		public virtual void AddOptionalExcludedName(string name)
		{
			object obj = LookupQualifiedName(scope, name);
			if (obj != null && obj != UniqueTag.NOT_FOUND)
			{
				if (!(obj is Scriptable))
				{
					throw new ArgumentException("Object for excluded name " + name + " is not a Scriptable, it is " + obj.GetType().FullName);
				}
				table.Put(obj, name);
			}
		}

		/// <summary>
		/// Adds a qualified name to the list of objects to be excluded from
		/// serialization.
		/// </summary>
		/// <remarks>
		/// Adds a qualified name to the list of objects to be excluded from
		/// serialization. Names excluded from serialization are looked up
		/// in the new scope and replaced upon deserialization.
		/// </remarks>
		/// <param name="name">
		/// a fully qualified name (of the form "a.b.c", where
		/// "a" must be a property of the top-level object)
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if the object is not found or is not
		/// a
		/// <see cref="Rhino.Scriptable">Rhino.Scriptable</see>
		/// .
		/// </exception>
		public virtual void AddExcludedName(string name)
		{
			object obj = LookupQualifiedName(scope, name);
			if (!(obj is Scriptable))
			{
				throw new ArgumentException("Object for excluded name " + name + " not found.");
			}
			table.Put(obj, name);
		}

		/// <summary>Returns true if the name is excluded from serialization.</summary>
		/// <remarks>Returns true if the name is excluded from serialization.</remarks>
		public virtual bool HasExcludedName(string name)
		{
			return table.Get(name) != null;
		}

		/// <summary>Removes a name from the list of names to exclude.</summary>
		/// <remarks>Removes a name from the list of names to exclude.</remarks>
		public virtual void RemoveExcludedName(string name)
		{
			Sharpen.Collections.Remove(table, name);
		}

		/// <summary>
		/// Adds the names of the standard objects and their
		/// prototypes to the list of excluded names.
		/// </summary>
		/// <remarks>
		/// Adds the names of the standard objects and their
		/// prototypes to the list of excluded names.
		/// </remarks>
		public virtual void ExcludeStandardObjectNames()
		{
			string[] names = new string[] { "Object", "Object.prototype", "Function", "Function.prototype", "String", "String.prototype", "Math", "Array", "Array.prototype", "Error", "Error.prototype", "Number", "Number.prototype", "Date", "Date.prototype", "RegExp", "RegExp.prototype", "Script", "Script.prototype", "Continuation", "Continuation.prototype" };
			// no Math.prototype
			for (int i = 0; i < names.Length; i++)
			{
				AddExcludedName(names[i]);
			}
			string[] optionalNames = new string[] { "XML", "XML.prototype", "XMLList", "XMLList.prototype" };
			for (int i_1 = 0; i_1 < optionalNames.Length; i_1++)
			{
				AddOptionalExcludedName(optionalNames[i_1]);
			}
		}

		internal static object LookupQualifiedName(Scriptable scope, string qualifiedName)
		{
			StringTokenizer st = new StringTokenizer(qualifiedName, ".");
			object result = scope;
			while (st.HasMoreTokens())
			{
				string s = st.NextToken();
				result = ScriptableObject.GetProperty((Scriptable)result, s);
				if (result == null || !(result is Scriptable))
				{
					break;
				}
			}
			return result;
		}

		[System.Serializable]
		internal class PendingLookup
		{
			internal const long serialVersionUID = -2692990309789917727L;

			internal PendingLookup(string name)
			{
				this.name = name;
			}

			internal virtual string GetName()
			{
				return name;
			}

			private string name;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override object ReplaceObject(object obj)
		{
			if (false)
			{
				throw new IOException();
			}
			// suppress warning
			string name = table.Get(obj);
			if (name == null)
			{
				return obj;
			}
			return new ScriptableOutputStream.PendingLookup(name);
		}

		private Scriptable scope;

		private IDictionary<object, string> table;
	}
}
