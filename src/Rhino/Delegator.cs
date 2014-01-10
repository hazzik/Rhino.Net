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
	/// This is a helper class for implementing wrappers around Scriptable
	/// objects.
	/// </summary>
	/// <remarks>
	/// This is a helper class for implementing wrappers around Scriptable
	/// objects. It implements the Function interface and delegates all
	/// invocations to a delegee Scriptable object. The normal use of this
	/// class involves creating a sub-class and overriding one or more of
	/// the methods.
	/// A useful application is the implementation of interceptors,
	/// pre/post conditions, debugging.
	/// </remarks>
	/// <seealso cref="Function">Function</seealso>
	/// <seealso cref="Scriptable">Scriptable</seealso>
	/// <author>Matthias Radestock</author>
	public class Delegator : Function
	{
		protected internal Scriptable obj = null;

		/// <summary>Create a Delegator prototype.</summary>
		/// <remarks>
		/// Create a Delegator prototype.
		/// This constructor should only be used for creating prototype
		/// objects of Delegator.
		/// </remarks>
		/// <seealso cref="Construct(Context, Scriptable, object[])">Construct(Context, Scriptable, object[])</seealso>
		public Delegator()
		{
		}

		/// <summary>
		/// Create a new Delegator that forwards requests to a delegee
		/// Scriptable object.
		/// </summary>
		/// <remarks>
		/// Create a new Delegator that forwards requests to a delegee
		/// Scriptable object.
		/// </remarks>
		/// <param name="obj">the delegee</param>
		/// <seealso cref="Scriptable">Scriptable</seealso>
		public Delegator(Scriptable obj)
		{
			// API class
			this.obj = obj;
		}

		/// <summary>Crete new Delegator instance.</summary>
		/// <remarks>
		/// Crete new Delegator instance.
		/// The default implementation calls this.getClass().newInstance().
		/// </remarks>
		/// <seealso cref="Construct(Context, Scriptable, object[])">Construct(Context, Scriptable, object[])</seealso>
		protected internal virtual Rhino.Delegator NewInstance()
		{
			try
			{
				return (Delegator) System.Activator.CreateInstance(this.GetType());
			}
			catch (Exception ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
		}

		/// <summary>Retrieve the delegee.</summary>
		/// <remarks>Retrieve the delegee.</remarks>
		/// <returns>the delegee</returns>
		public virtual Scriptable GetDelegee()
		{
			return obj;
		}

		/// <summary>Set the delegee.</summary>
		/// <remarks>Set the delegee.</remarks>
		/// <param name="obj">the delegee</param>
		/// <seealso cref="Scriptable">Scriptable</seealso>
		public virtual void SetDelegee(Scriptable obj)
		{
			this.obj = obj;
		}

		/// <seealso cref="Scriptable.GetClassName()">Scriptable.GetClassName()</seealso>
		public virtual string GetClassName()
		{
			return obj.GetClassName();
		}

		/// <seealso cref="Scriptable.Get(string, SIScriptable">Scriptable.Get(string, IScriptable)</seealso>
		public virtual object Get(string name, Scriptable start)
		{
			return obj.Get(name, start);
		}

		/// <seealso cref="Scriptable.Get(int, SIScriptable">Scriptable.Get(int, Scriptable)</seealso>
		public virtual object Get(int index, Scriptable start)
		{
			return obj.Get(index, start);
		}

		/// <seealso cref="Scriptable.Has(string, SIScriptable">Scriptable.Has(string, Scriptable)</seealso>
		public virtual bool Has(string name, Scriptable start)
		{
			return obj.Has(name, start);
		}

		/// <seealso cref="Scriptable.Has(int, SIScriptable">Scriptable.Has(int, Scriptable)</seealso>
		public virtual bool Has(int index, Scriptable start)
		{
			return obj.Has(index, start);
		}

		/// <seealso cref="Scriptable.Put(string, SIScriptable object)">Scriptable.Put(string, Scriptable, object)</seealso>
		public virtual void Put(string name, Scriptable start, object value)
		{
			obj.Put(name, start, value);
		}

		/// <seealso cref="Scriptable.Put(int,Scriptablee, object)">Scriptable.Put(int, Scriptable, object)</seealso>
		public virtual void Put(int index, Scriptable start, object value)
		{
			obj.Put(index, start, value);
		}

		/// <seealso cref="Scriptable.Delete(string)">Scriptable.Delete(string)</seealso>
		public virtual void Delete(string name)
		{
			obj.Delete(name);
		}

		/// <seealso cref="Scriptable.Delete(int)">Scriptable.Delete(int)</seealso>
		public virtual void Delete(int index)
		{
			obj.Delete(index);
		}

		/// <seealso cref="Scriptable.GetPrototype()">Scriptable.GetPrototype()</seealso>
		public virtual Scriptable GetPrototype()
		{
			return obj.GetPrototype();
		}

		/// <seealso cref="Scriptable.SetPrototypeScriptablee)">Scriptable.SetPrototype(Scriptable)</seealso>
		public virtual void SetPrototype(Scriptable prototype)
		{
			obj.SetPrototype(prototype);
		}

		/// <seealso cref="Scriptable.GetParentScope()">Scriptable.GetParentScope()</seealso>
		public virtual Scriptable ParentScope
		{
			get { return obj.ParentScope; }
			set { obj.ParentScope = value; }
		}

		/// <seealso cref="Scriptable.GetIds()">Scriptable.GetIds()</seealso>
		public virtual object[] GetIds()
		{
			return obj.GetIds();
		}

		/// <summary>
		/// Note that this method does not get forwarded to the delegee if
		/// the <code>hint</code> parameter is null,
		/// <code>ScriptRuntime.ScriptableClass</code> or
		/// <code>ScriptRuntime.FunctionClass</code>.
		/// </summary>
		/// <remarks>
		/// Note that this method does not get forwarded to the delegee if
		/// the <code>hint</code> parameter is null,
		/// <code>ScriptRuntime.ScriptableClass</code> or
		/// <code>ScriptRuntime.FunctionClass</code>. Instead the object
		/// itself is returned.
		/// </remarks>
		/// <param name="hint">the type hint</param>
		/// <returns>the default value</returns>
		/// <seealso cref="Scriptable.GetDefaultValue(System.Type{T})">Scriptable.GetDefaultValue(System.Type&lt;T&gt;)</seealso>
		public virtual object GetDefaultValue(Type hint)
		{
			return (hint == null || hint == ScriptRuntime.ScriptableClass || hint == ScriptRuntime.FunctionClass) ? this : obj.GetDefaultValue(hint);
		}

		/// <seealso cref="Scriptable.HasInstanceScriptablee)">Scriptable.HasInstance(Scriptable)</seealso>
		public virtual bool HasInstance(Scriptable instance)
		{
			return obj.HasInstance(instance);
		}

		/// <seealso cref="Function.Call(Context, Scriptable, Scriptable, object[])">Function.Call(Context, Scriptable, Scriptable, object[])</seealso>
		public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			return ((Function)obj).Call(cx, scope, thisObj, args);
		}

		/// <summary>
		/// Note that if the <code>delegee</code> is <code>null</code>,
		/// this method creates a new instance of the Delegator itself
		/// rathert than forwarding the call to the
		/// <code>delegee</code>.
		/// </summary>
		/// <remarks>
		/// Note that if the <code>delegee</code> is <code>null</code>,
		/// this method creates a new instance of the Delegator itself
		/// rathert than forwarding the call to the
		/// <code>delegee</code>. This permits the use of Delegator
		/// prototypes.
		/// </remarks>
		/// <param name="cx">the current Context for this thread</param>
		/// <param name="scope">
		/// an enclosing scope of the caller except
		/// when the function is called from a closure.
		/// </param>
		/// <param name="args">the array of arguments</param>
		/// <returns>the allocated object</returns>
		/// <seealso cref="Function.Construct(Context, Scriptable, object[])">Function.Construct(Context, Scriptable, object[])</seealso>
		public virtual Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			if (obj == null)
			{
				//this little trick allows us to declare prototype objects for
				//Delegators
				Rhino.Delegator n = NewInstance();
				Scriptable delegee;
				if (args.Length == 0)
				{
					delegee = new NativeObject();
				}
				else
				{
					delegee = ScriptRuntime.ToObject(cx, scope, args[0]);
				}
				n.SetDelegee(delegee);
				return n;
			}
			else
			{
				return ((Function)obj).Construct(cx, scope, args);
			}
		}
	}
}
