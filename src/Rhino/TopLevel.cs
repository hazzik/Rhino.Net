/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// A top-level scope object that provides special means to cache and preserve
	/// the initial values of the built-in constructor properties for better
	/// ECMAScript compliance.
	/// </summary>
	/// <remarks>
	/// A top-level scope object that provides special means to cache and preserve
	/// the initial values of the built-in constructor properties for better
	/// ECMAScript compliance.
	/// <p>ECMA 262 requires that most constructors used internally construct
	/// objects with the original prototype object as value of their [[Prototype]]
	/// internal property. Since built-in global constructors are defined as
	/// writable and deletable, this means they should be cached to protect against
	/// redefinition at runtime.</p>
	/// <p>In order to implement this efficiently, this class provides a mechanism
	/// to access the original built-in global constructors and their prototypes
	/// via numeric class-ids. To make use of this, the new
	/// <see cref="ScriptRuntime.NewBuiltinObject(Context, Scriptable, Builtins, object[])">ScriptRuntime.newBuiltinObject</see>
	/// and
	/// <see cref="ScriptRuntime.SetBuiltinProtoAndParent(ScriptableObject, Scriptable, Builtins)">ScriptRuntime.setBuiltinProtoAndParent</see>
	/// methods should be used to create and initialize objects of built-in classes
	/// instead of their generic counterparts.</p>
	/// <p>Calling
	/// <see cref="Context.InitStandardObjects()">Context.InitStandardObjects()</see>
	/// with an instance of this class as argument will automatically cache
	/// built-in classes after initialization. For other setups involving
	/// top-level scopes that inherit global properties from their proptotypes
	/// (e.g. with dynamic scopes) embeddings should explicitly call
	/// <see cref="CacheBuiltins()">CacheBuiltins()</see>
	/// to initialize the class cache for each top-level
	/// scope.</p>
	/// </remarks>
	[System.Serializable]
	public class TopLevel : IdScriptableObject
	{
		internal const long serialVersionUID = -4648046356662472260L;

		/// <summary>An enumeration of built-in ECMAScript objects.</summary>
		/// <remarks>An enumeration of built-in ECMAScript objects.</remarks>
		public enum Builtins
		{
			Object,
			Array,
			Function,
			String,
			Number,
			Boolean,
			RegExp,
			Error
		}

		private EnumMap<TopLevel.Builtins, BaseFunction> ctors;

		public override string GetClassName()
		{
			return "global";
		}

		/// <summary>
		/// Cache the built-in ECMAScript objects to protect them against
		/// modifications by the script.
		/// </summary>
		/// <remarks>
		/// Cache the built-in ECMAScript objects to protect them against
		/// modifications by the script. This method is called automatically by
		/// <see cref="ScriptRuntime.InitStandardObjects(Context, ScriptableObject, bool)">ScriptRuntime.initStandardObjects</see>
		/// if the scope argument is an instance of this class. It only has to be
		/// called by the embedding if a top-level scope is not initialized through
		/// <code>initStandardObjects()</code>.
		/// </remarks>
		public virtual void CacheBuiltins()
		{
			ctors = new EnumMap<TopLevel.Builtins, BaseFunction>(typeof(TopLevel.Builtins));
			foreach (TopLevel.Builtins builtin in TopLevel.Builtins.Values())
			{
				object value = ScriptableObject.GetProperty(this, builtin.ToString());
				if (value is BaseFunction)
				{
					ctors.Put(builtin, (BaseFunction)value);
				}
			}
		}

		/// <summary>
		/// Static helper method to get a built-in object constructor with the given
		/// <code>type</code> from the given <code>scope</code>.
		/// </summary>
		/// <remarks>
		/// Static helper method to get a built-in object constructor with the given
		/// <code>type</code> from the given <code>scope</code>. If the scope is not
		/// an instance of this class or does have a cache of built-ins,
		/// the constructor is looked up via normal property lookup.
		/// </remarks>
		/// <param name="cx">the current Context</param>
		/// <param name="scope">the top-level scope</param>
		/// <param name="type">the built-in type</param>
		/// <returns>the built-in constructor</returns>
		public static Function GetBuiltinCtor(Context cx, Scriptable scope, TopLevel.Builtins type)
		{
			// must be called with top level scope
			System.Diagnostics.Debug.Assert(scope.GetParentScope() == null);
			if (scope is TopLevel)
			{
				Function result = ((TopLevel)scope).GetBuiltinCtor(type);
				if (result != null)
				{
					return result;
				}
			}
			// fall back to normal constructor lookup
			return ScriptRuntime.GetExistingCtor(cx, scope, type.ToString());
		}

		/// <summary>
		/// Static helper method to get a built-in object prototype with the given
		/// <code>type</code> from the given <code>scope</code>.
		/// </summary>
		/// <remarks>
		/// Static helper method to get a built-in object prototype with the given
		/// <code>type</code> from the given <code>scope</code>. If the scope is not
		/// an instance of this class or does have a cache of built-ins,
		/// the prototype is looked up via normal property lookup.
		/// </remarks>
		/// <param name="scope">the top-level scope</param>
		/// <param name="type">the built-in type</param>
		/// <returns>the built-in prototype</returns>
		public static Scriptable GetBuiltinPrototype(Scriptable scope, TopLevel.Builtins type)
		{
			// must be called with top level scope
			System.Diagnostics.Debug.Assert(scope.GetParentScope() == null);
			if (scope is TopLevel)
			{
				Scriptable result = ((TopLevel)scope).GetBuiltinPrototype(type);
				if (result != null)
				{
					return result;
				}
			}
			// fall back to normal prototype lookup
			return ScriptableObject.GetClassPrototype(scope, type.ToString());
		}

		/// <summary>
		/// Get the cached built-in object constructor from this scope with the
		/// given <code>type</code>.
		/// </summary>
		/// <remarks>
		/// Get the cached built-in object constructor from this scope with the
		/// given <code>type</code>. Returns null if
		/// <see cref="CacheBuiltins()">CacheBuiltins()</see>
		/// has not
		/// been called on this object.
		/// </remarks>
		/// <param name="type">the built-in type</param>
		/// <returns>the built-in constructor</returns>
		public virtual BaseFunction GetBuiltinCtor(TopLevel.Builtins type)
		{
			return ctors != null ? ctors.Get(type) : null;
		}

		/// <summary>
		/// Get the cached built-in object prototype from this scope with the
		/// given <code>type</code>.
		/// </summary>
		/// <remarks>
		/// Get the cached built-in object prototype from this scope with the
		/// given <code>type</code>. Returns null if
		/// <see cref="CacheBuiltins()">CacheBuiltins()</see>
		/// has not
		/// been called on this object.
		/// </remarks>
		/// <param name="type">the built-in type</param>
		/// <returns>the built-in prototype</returns>
		public virtual Scriptable GetBuiltinPrototype(TopLevel.Builtins type)
		{
			BaseFunction func = GetBuiltinCtor(type);
			object proto = func != null ? func.GetPrototypeProperty() : null;
			return proto is Scriptable ? (Scriptable)proto : null;
		}
	}
}
