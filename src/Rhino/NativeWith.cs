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
	/// This class implements the object lookup required for the
	/// <code>with</code> statement.
	/// </summary>
	/// <remarks>
	/// This class implements the object lookup required for the
	/// <code>with</code> statement.
	/// It simply delegates every action to its prototype except
	/// for operations on its parent.
	/// </remarks>
	[System.Serializable]
	public class NativeWith : Scriptable, IdFunctionCall
	{
		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeWith obj = new Rhino.NativeWith();
			obj.ParentScope = scope;
			obj.Prototype = ScriptableObject.GetObjectPrototype(scope);
			IdFunctionObject ctor = new IdFunctionObject(obj, FTAG, Id_constructor, "With", 0, scope);
			ctor.MarkAsConstructor(obj);
			if (@sealed)
			{
				ctor.SealObject();
			}
			ctor.ExportAsScopeProperty();
		}

		private NativeWith()
		{
		}

		protected internal NativeWith(Scriptable parent, Scriptable prototype)
		{
			this.parent = parent;
			this.prototype = prototype;
		}

		public virtual string GetClassName()
		{
			return "With";
		}

		public virtual bool Has(string id, Scriptable start)
		{
			return prototype.Has(id, prototype);
		}

		public virtual bool Has(int index, Scriptable start)
		{
			return prototype.Has(index, prototype);
		}

		public virtual object Get(string id, Scriptable start)
		{
			if (start == this)
			{
				start = prototype;
			}
			return prototype.Get(id, start);
		}

		public virtual object Get(int index, Scriptable start)
		{
			if (start == this)
			{
				start = prototype;
			}
			return prototype.Get(index, start);
		}

		public virtual void Put(string id, Scriptable start, object value)
		{
			if (start == this)
			{
				start = prototype;
			}
			prototype.Put(id, start, value);
		}

		public virtual void Put(int index, Scriptable start, object value)
		{
			if (start == this)
			{
				start = prototype;
			}
			prototype.Put(index, start, value);
		}

		public virtual void Delete(string id)
		{
			prototype.Delete(id);
		}

		public virtual void Delete(int index)
		{
			prototype.Delete(index);
		}

		public virtual Scriptable Prototype
		{
			set { this.prototype = value; }
			get { return prototype; }
		}

		public virtual Scriptable ParentScope
		{
			get { return parent; }
			set { this.parent = value; }
		}

		public virtual object[] GetIds()
		{
			return prototype.GetIds();
		}

		public virtual object GetDefaultValue(Type typeHint)
		{
			return prototype.GetDefaultValue(typeHint);
		}

		public virtual bool HasInstance(Scriptable value)
		{
			return prototype.HasInstance(value);
		}

		/// <summary>Must return null to continue looping or the final collection result.</summary>
		/// <remarks>Must return null to continue looping or the final collection result.</remarks>
		protected internal virtual object UpdateDotQuery(bool value)
		{
			// NativeWith itself does not support it
			throw new InvalidOperationException();
		}

		public virtual object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (f.HasTag(FTAG))
			{
				if (f.MethodId() == Id_constructor)
				{
					throw Context.ReportRuntimeError1("msg.cant.call.indirect", "With");
				}
			}
			throw f.Unknown();
		}

		internal static bool IsWithFunction(object functionObj)
		{
			if (functionObj is IdFunctionObject)
			{
				IdFunctionObject f = (IdFunctionObject)functionObj;
				return f.HasTag(FTAG) && f.MethodId() == Id_constructor;
			}
			return false;
		}

		internal static object NewWithSpecial(Context cx, Scriptable scope, object[] args)
		{
			ScriptRuntime.CheckDeprecated(cx, "With");
			scope = ScriptableObject.GetTopLevelScope(scope);
			Rhino.NativeWith thisObj = new Rhino.NativeWith();
			thisObj.Prototype = args.Length == 0 ? ScriptableObject.GetObjectPrototype(scope) : ScriptRuntime.ToObject(cx, scope, args[0]);
			thisObj.ParentScope = scope;
			return thisObj;
		}

		private static readonly object FTAG = "With";

		private const int Id_constructor = 1;

		protected internal Scriptable prototype;

		protected internal Scriptable parent;
	}
}
