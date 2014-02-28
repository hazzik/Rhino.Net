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
	/// This class provides support for implementing Java-style synchronized
	/// methods in Javascript.
	/// </summary>
	/// <remarks>
	/// This class provides support for implementing Java-style synchronized
	/// methods in Javascript.
	/// Synchronized functions are created from ordinary Javascript
	/// functions by the <code>Synchronizer</code> constructor, e.g.
	/// <code>new Packages.Rhino.Net.Synchronizer(fun)</code>.
	/// The resulting object is a function that establishes an exclusive
	/// lock on the <code>this</code> object of its invocation.
	/// The Rhino shell provides a short-cut for the creation of
	/// synchronized methods: <code>sync(fun)</code> has the same effect as
	/// calling the above constructor.
	/// </remarks>
	/// <seealso cref="Delegator">Delegator</seealso>
	/// <author>Matthias Radestock</author>
	public class Synchronizer : Delegator
	{
		private object syncObject;

		/// <summary>Create a new synchronized function from an existing one.</summary>
		/// <remarks>Create a new synchronized function from an existing one.</remarks>
		/// <param name="obj">the existing function</param>
		public Synchronizer(Scriptable obj) : base(obj)
		{
		}

		/// <summary>
		/// Create a new synchronized function from an existing one using
		/// an explicit object as synchronization object.
		/// </summary>
		/// <remarks>
		/// Create a new synchronized function from an existing one using
		/// an explicit object as synchronization object.
		/// </remarks>
		/// <param name="obj">the existing function</param>
		/// <param name="syncObject">the object to synchronized on</param>
		public Synchronizer(Scriptable obj, object syncObject) : base(obj)
		{
			// API class
			this.syncObject = syncObject;
		}

		/// <seealso cref="Function.Call(Context, Scriptable, Scriptable, object[])">Function.Call(Context, Scriptable, Scriptable, object[])</seealso>
		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			object sync = syncObject ?? thisObj;
			var wrapper = sync as Wrapper;
			object o = wrapper != null ? wrapper.Unwrap() : sync;
			lock (o)
			{
				return ((Function)obj).Call(cx, scope, thisObj, args);
			}
		}
	}
}
