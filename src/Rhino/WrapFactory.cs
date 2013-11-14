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
	/// <summary>
	/// Embeddings that wish to provide their own custom wrappings for Java
	/// objects may extend this class and call
	/// <see cref="Context.SetWrapFactory(WrapFactory)">Context.SetWrapFactory(WrapFactory)</see>
	/// Once an instance of this class or an extension of this class is enabled
	/// for a given context (by calling setWrapFactory on that context), Rhino
	/// will call the methods of this class whenever it needs to wrap a value
	/// resulting from a call to a Java method or an access to a Java field.
	/// </summary>
	/// <seealso cref="Context.SetWrapFactory(WrapFactory)">Context.SetWrapFactory(WrapFactory)</seealso>
	/// <since>1.5 Release 4</since>
	public class WrapFactory
	{
		// API class
		/// <summary>Wrap the object.</summary>
		/// <remarks>
		/// Wrap the object.
		/// <p>
		/// The value returned must be one of
		/// <UL>
		/// <LI>java.lang.Boolean</LI>
		/// <LI>java.lang.String</LI>
		/// <LI>java.lang.Number</LI>
		/// <LI>org.mozilla.javascript.Scriptable objects</LI>
		/// <LI>The value returned by Context.getUndefinedValue()</LI>
		/// <LI>null</LI>
		/// </UL>
		/// </remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">the scope of the executing script</param>
		/// <param name="obj">the object to be wrapped. Note it can be null.</param>
		/// <param name="staticType">
		/// type hint. If security restrictions prevent to wrap
		/// object based on its class, staticType will be used instead.
		/// </param>
		/// <returns>the wrapped value.</returns>
		public virtual object Wrap(Context cx, Scriptable scope, object obj, Type staticType)
		{
			if (obj == null || obj == Undefined.instance || obj is Scriptable)
			{
				return obj;
			}
			if (staticType != null && staticType.IsPrimitive)
			{
				if (staticType == typeof(void))
				{
					return Undefined.instance;
				}
				if (staticType == typeof(char))
				{
					return Sharpen.Extensions.ValueOf(((char)obj));
				}
				return obj;
			}
			if (!IsJavaPrimitiveWrap())
			{
				if (obj is string || obj.IsNumber()|| obj is bool)
				{
					return obj;
				}
				else
				{
					if (obj is char)
					{
						return ((char)obj).ToString();
					}
				}
			}
			Type cls = obj.GetType();
			if (cls.IsArray)
			{
				return NativeJavaArray.Wrap(scope, obj);
			}
			return WrapAsJavaObject(cx, scope, obj, staticType);
		}

		/// <summary>Wrap an object newly created by a constructor call.</summary>
		/// <remarks>Wrap an object newly created by a constructor call.</remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">the scope of the executing script</param>
		/// <param name="obj">the object to be wrapped</param>
		/// <returns>the wrapped value.</returns>
		public virtual Scriptable WrapNewObject(Context cx, Scriptable scope, object obj)
		{
			if (obj is Scriptable)
			{
				return (Scriptable)obj;
			}
			Type cls = obj.GetType();
			if (cls.IsArray)
			{
				return NativeJavaArray.Wrap(scope, obj);
			}
			return WrapAsJavaObject(cx, scope, obj, null);
		}

		/// <summary>
		/// Wrap Java object as Scriptable instance to allow full access to its
		/// methods and fields from JavaScript.
		/// </summary>
		/// <remarks>
		/// Wrap Java object as Scriptable instance to allow full access to its
		/// methods and fields from JavaScript.
		/// <p>
		/// <see cref="Wrap(Context, Scriptable, object, System.Type{T})">Wrap(Context, Scriptable, object, System.Type&lt;T&gt;)</see>
		/// and
		/// <see cref="WrapNewObject(Context, Scriptable, object)">WrapNewObject(Context, Scriptable, object)</see>
		/// call this method
		/// when they can not convert <tt>javaObject</tt> to JavaScript primitive
		/// value or JavaScript array.
		/// <p>
		/// Subclasses can override the method to provide custom wrappers
		/// for Java objects.
		/// </remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">the scope of the executing script</param>
		/// <param name="javaObject">the object to be wrapped</param>
		/// <param name="staticType">
		/// type hint. If security restrictions prevent to wrap
		/// object based on its class, staticType will be used instead.
		/// </param>
		/// <returns>the wrapped value which shall not be null</returns>
		public virtual Scriptable WrapAsJavaObject(Context cx, Scriptable scope, object javaObject, Type staticType)
		{
			return new NativeJavaObject(scope, javaObject, staticType);
		}

		/// <summary>
		/// Wrap a Java class as Scriptable instance to allow access to its static
		/// members and fields and use as constructor from JavaScript.
		/// </summary>
		/// <remarks>
		/// Wrap a Java class as Scriptable instance to allow access to its static
		/// members and fields and use as constructor from JavaScript.
		/// <p>
		/// Subclasses can override this method to provide custom wrappers for
		/// Java classes.
		/// </remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">the scope of the executing script</param>
		/// <param name="javaClass">the class to be wrapped</param>
		/// <returns>the wrapped value which shall not be null</returns>
		/// <since>1.7R3</since>
		public virtual Scriptable WrapJavaClass(Context cx, Scriptable scope, Type javaClass)
		{
			return new NativeJavaClass(scope, javaClass);
		}

		/// <summary>
		/// Return <code>false</code> if result of Java method, which is instance of
		/// <code>String</code>, <code>Number</code>, <code>Boolean</code> and
		/// <code>Character</code>, should be used directly as JavaScript primitive
		/// type.
		/// </summary>
		/// <remarks>
		/// Return <code>false</code> if result of Java method, which is instance of
		/// <code>String</code>, <code>Number</code>, <code>Boolean</code> and
		/// <code>Character</code>, should be used directly as JavaScript primitive
		/// type.
		/// By default the method returns true to indicate that instances of
		/// <code>String</code>, <code>Number</code>, <code>Boolean</code> and
		/// <code>Character</code> should be wrapped as any other Java object and
		/// scripts can access any Java method available in these objects.
		/// Use
		/// <see cref="SetJavaPrimitiveWrap(bool)">SetJavaPrimitiveWrap(bool)</see>
		/// to change this.
		/// </remarks>
		public bool IsJavaPrimitiveWrap()
		{
			return javaPrimitiveWrap;
		}

		/// <seealso cref="IsJavaPrimitiveWrap()">IsJavaPrimitiveWrap()</seealso>
		public void SetJavaPrimitiveWrap(bool value)
		{
			Context cx = Context.GetCurrentContext();
			if (cx != null && cx.IsSealed())
			{
				Context.OnSealedMutation();
			}
			javaPrimitiveWrap = value;
		}

		private bool javaPrimitiveWrap = true;
	}
}
