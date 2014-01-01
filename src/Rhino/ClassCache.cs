/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// Cache of generated classes and data structures to access Java runtime
	/// from JavaScript.
	/// </summary>
	/// <remarks>
	/// Cache of generated classes and data structures to access Java runtime
	/// from JavaScript.
	/// </remarks>
	/// <author>Igor Bukanov</author>
	/// <since>Rhino 1.5 Release 5</since>
	[System.Serializable]
	public class ClassCache
	{
		private const long serialVersionUID = -8866246036237312215L;

		private static readonly object AKEY = "ClassCache";

		private volatile bool cachingIsEnabled = true;

		[System.NonSerialized]
		private IDictionary<Type, JavaMembers> classTable;

		[System.NonSerialized]
		private IDictionary<JavaAdapter.JavaAdapterSignature, Type> classAdapterCache;

		[System.NonSerialized]
		private IDictionary<Type, object> interfaceAdapterCache;

		private int generatedClassSerial;

		private Scriptable associatedScope;

		/// <summary>Search for ClassCache object in the given scope.</summary>
		/// <remarks>
		/// Search for ClassCache object in the given scope.
		/// The method first calls
		/// <see cref="ScriptableObject.GetTopLevelScope(Scriptable)">ScriptableObject.GetTopLevelScope(Scriptable)</see>
		/// to get the top most scope and then tries to locate associated
		/// ClassCache object in the prototype chain of the top scope.
		/// </remarks>
		/// <param name="scope">scope to search for ClassCache object.</param>
		/// <returns>
		/// previously associated ClassCache object or a new instance of
		/// ClassCache if no ClassCache object was found.
		/// </returns>
		/// <seealso cref="Associate(ScriptableObject)">Associate(ScriptableObject)</seealso>
		public static ClassCache Get(Scriptable scope)
		{
			ClassCache cache = (ClassCache)ScriptableObject.GetTopScopeValue(scope, AKEY);
			if (cache == null)
			{
				throw new Exception("Can't find top level scope for " + "ClassCache.get");
			}
			return cache;
		}

		/// <summary>Associate ClassCache object with the given top-level scope.</summary>
		/// <remarks>
		/// Associate ClassCache object with the given top-level scope.
		/// The ClassCache object can only be associated with the given scope once.
		/// </remarks>
		/// <param name="topScope">scope to associate this ClassCache object with.</param>
		/// <returns>
		/// true if no previous ClassCache objects were embedded into
		/// the scope and this ClassCache were successfully associated
		/// or false otherwise.
		/// </returns>
		/// <seealso cref="Get(Scriptable)">Get(Scriptable)</seealso>
		public virtual bool Associate(ScriptableObject topScope)
		{
			if (topScope.GetParentScope() != null)
			{
				// Can only associate cache with top level scope
				throw new ArgumentException();
			}
			if (this == topScope.AssociateValue(AKEY, this))
			{
				associatedScope = topScope;
				return true;
			}
			return false;
		}

		/// <summary>Empty caches of generated Java classes and Java reflection information.</summary>
		/// <remarks>Empty caches of generated Java classes and Java reflection information.</remarks>
		public virtual void ClearCaches()
		{
			lock (this)
			{
				classTable = null;
				classAdapterCache = null;
				interfaceAdapterCache = null;
			}
		}

		/// <summary>
		/// Check if generated Java classes and Java reflection information
		/// is cached.
		/// </summary>
		/// <remarks>
		/// Check if generated Java classes and Java reflection information
		/// is cached.
		/// </remarks>
		public bool IsCachingEnabled()
		{
			return cachingIsEnabled;
		}

		/// <summary>Set whether to cache some values.</summary>
		/// <remarks>
		/// Set whether to cache some values.
		/// <p>
		/// By default, the engine will cache the results of
		/// <tt>Class.getMethods()</tt> and similar calls.
		/// This can speed execution dramatically, but increases the memory
		/// footprint. Also, with caching enabled, references may be held to
		/// objects past the lifetime of any real usage.
		/// <p>
		/// If caching is enabled and this method is called with a
		/// <code>false</code> argument, the caches will be emptied.
		/// <p>
		/// Caching is enabled by default.
		/// </remarks>
		/// <param name="enabled">if true, caching is enabled</param>
		/// <seealso cref="ClearCaches()">ClearCaches()</seealso>
		public virtual void SetCachingEnabled(bool enabled)
		{
			lock (this)
			{
				if (enabled == cachingIsEnabled)
				{
					return;
				}
				if (!enabled)
				{
					ClearCaches();
				}
				cachingIsEnabled = enabled;
			}
		}

		/// <returns>a map from classes to associated JavaMembers objects</returns>
		internal virtual IDictionary<Type, JavaMembers> GetClassCacheMap()
		{
			if (classTable == null)
			{
				// Use 1 as concurrency level here and for other concurrent hash maps
				// as we don't expect high levels of sustained concurrent writes.
				classTable = new ConcurrentHashMap<Type, JavaMembers>(16, 0.75f, 1);
			}
			return classTable;
		}

		internal virtual IDictionary<JavaAdapter.JavaAdapterSignature, Type> GetInterfaceAdapterCacheMap()
		{
			if (classAdapterCache == null)
			{
				classAdapterCache = new ConcurrentHashMap<JavaAdapter.JavaAdapterSignature, Type>(16, 0.75f, 1);
			}
			return classAdapterCache;
		}

		/// <summary>
		/// Internal engine method to return serial number for generated classes
		/// to ensure name uniqueness.
		/// </summary>
		/// <remarks>
		/// Internal engine method to return serial number for generated classes
		/// to ensure name uniqueness.
		/// </remarks>
		public int NewClassSerialNumber()
		{
			lock (this)
			{
				return ++generatedClassSerial;
			}
		}

		internal virtual object GetInterfaceAdapter(Type cl)
		{
			return interfaceAdapterCache == null ? null : interfaceAdapterCache.Get(cl);
		}

		internal virtual void CacheInterfaceAdapter(Type cl, object iadapter)
		{
			lock (this)
			{
				if (cachingIsEnabled)
				{
					if (interfaceAdapterCache == null)
					{
						interfaceAdapterCache = new ConcurrentHashMap<Type, object>(16, 0.75f, 1);
					}
					interfaceAdapterCache[cl] = iadapter;
				}
			}
		}

		internal virtual Scriptable GetAssociatedScope()
		{
			return associatedScope;
		}
	}
}
