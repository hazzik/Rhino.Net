/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Sharpen;

namespace Rhino.Xml
{
	/// <summary>This Interface describes what all XML objects (XML, XMLList) should have in common.</summary>
	/// <remarks>This Interface describes what all XML objects (XML, XMLList) should have in common.</remarks>
	[System.Serializable]
	public abstract class XMLObject : IdScriptableObject
	{
		public XMLObject()
		{
		}

		public XMLObject(Scriptable scope, Scriptable prototype) : base(scope, prototype)
		{
		}

		/// <summary>Implementation of ECMAScript [[Has]].</summary>
		/// <remarks>Implementation of ECMAScript [[Has]].</remarks>
		public abstract bool Has(Context cx, object id);

		/// <summary>Implementation of ECMAScript [[Get]].</summary>
		/// <remarks>Implementation of ECMAScript [[Get]].</remarks>
		public abstract object Get(Context cx, object id);

		/// <summary>Implementation of ECMAScript [[Put]].</summary>
		/// <remarks>Implementation of ECMAScript [[Put]].</remarks>
		public abstract void Put(Context cx, object id, object value);

		/// <summary>Implementation of ECMAScript [[Delete]].</summary>
		/// <remarks>Implementation of ECMAScript [[Delete]].</remarks>
		public abstract bool Delete(Context cx, object id);

		public abstract object GetFunctionProperty(Context cx, string name);

		public abstract object GetFunctionProperty(Context cx, int id);

		/// <summary>
		/// Return an additional object to look for methods that runtime should
		/// consider during method search.
		/// </summary>
		/// <remarks>
		/// Return an additional object to look for methods that runtime should
		/// consider during method search. Return null if no such object available.
		/// </remarks>
		public abstract Scriptable GetExtraMethodSource(Context cx);

		/// <summary>Generic reference to implement x.@y, x..y etc.</summary>
		/// <remarks>Generic reference to implement x.@y, x..y etc.</remarks>
		public abstract Ref MemberRef(Context cx, object elem, int memberTypeFlags);

		/// <summary>Generic reference to implement x::ns, x.@ns::y, x..@ns::y etc.</summary>
		/// <remarks>Generic reference to implement x::ns, x.@ns::y, x..@ns::y etc.</remarks>
		public abstract Ref MemberRef(Context cx, object @namespace, object elem, int memberTypeFlags);

		/// <summary>Wrap this object into NativeWith to implement the with statement.</summary>
		/// <remarks>Wrap this object into NativeWith to implement the with statement.</remarks>
		public abstract NativeWith EnterWith(Scriptable scope);

		/// <summary>Wrap this object into NativeWith to implement the .() query.</summary>
		/// <remarks>Wrap this object into NativeWith to implement the .() query.</remarks>
		public abstract NativeWith EnterDotQuery(Scriptable scope);

		/// <summary>Custom <tt>+</tt> operator.</summary>
		/// <remarks>
		/// Custom <tt>+</tt> operator.
		/// Should return
		/// <see cref="Rhino.ScriptableConstants.NOT_FOUND">Rhino.ScriptableConstants.NOT_FOUND</see>
		/// if this object does not have
		/// custom addition operator for the given value,
		/// or the result of the addition operation.
		/// <p>
		/// The default implementation returns
		/// <see cref="Rhino.ScriptableConstants.NOT_FOUND">Rhino.ScriptableConstants.NOT_FOUND</see>
		/// to indicate no custom addition operation.
		/// </remarks>
		/// <param name="cx">the Context object associated with the current thread.</param>
		/// <param name="thisIsLeft">
		/// if true, the object should calculate this + value
		/// if false, the object should calculate value + this.
		/// </param>
		/// <param name="value">the second argument for addition operation.</param>
		public virtual object AddValues(Context cx, bool thisIsLeft, object value)
		{
			return ScriptableConstants.NOT_FOUND;
		}

		/// <summary>Gets the value returned by calling the typeof operator on this object.</summary>
		/// <remarks>Gets the value returned by calling the typeof operator on this object.</remarks>
		/// <seealso cref="Rhino.ScriptableObject.GetTypeOf()">Rhino.ScriptableObject.GetTypeOf()</seealso>
		/// <returns>
		/// "xml" or "undefined" if
		/// <see cref="Rhino.ScriptableObject.AvoidObjectDetection()">Rhino.ScriptableObject.AvoidObjectDetection()</see>
		/// returns <code>true</code>
		/// </returns>
		public override string GetTypeOf()
		{
			return AvoidObjectDetection() ? "undefined" : "xml";
		}
	}
}
