/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Sharpen;

namespace Rhino
{
	[System.Serializable]
	public class IdFunctionObject : BaseFunction
	{
		public IdFunctionObject(IdFunctionCall idcall, object tag, int id, int arity)
		{
			// API class
			if (arity < 0)
			{
				throw new ArgumentException();
			}
			this.idcall = idcall;
			this.tag = tag;
			this.methodId = id;
			this.arity = arity;
			if (arity < 0)
			{
				throw new ArgumentException();
			}
		}

		public IdFunctionObject(IdFunctionCall idcall, object tag, int id, string name, int arity, Scriptable scope) : base(scope, null)
		{
			if (arity < 0)
			{
				throw new ArgumentException();
			}
			if (name == null)
			{
				throw new ArgumentException();
			}
			this.idcall = idcall;
			this.tag = tag;
			this.methodId = id;
			this.arity = arity;
			this.functionName = name;
		}

		public virtual void InitFunction(string name, Scriptable scope)
		{
			if (name == null)
			{
				throw new ArgumentException();
			}
			if (scope == null)
			{
				throw new ArgumentException();
			}
			this.functionName = name;
			ParentScope = scope;
		}

		public bool HasTag(object tag)
		{
			return tag == null ? this.tag == null : tag.Equals(this.tag);
		}

		public int MethodId()
		{
			return methodId;
		}

		public void MarkAsConstructor(Scriptable prototypeProperty)
		{
			useCallAsConstructor = true;
			SetImmunePrototypeProperty(prototypeProperty);
		}

		public void AddAsProperty(Scriptable target)
		{
			ScriptableObject.DefineProperty(target, functionName, this, PropertyAttributes.DONTENUM);
		}

		public virtual void ExportAsScopeProperty()
		{
			AddAsProperty(ParentScope);
		}

		public override Scriptable Prototype
		{
			get
			{
				// Lazy initialization of prototype: for native functions this
				// may not be called at all
				Scriptable proto = base.Prototype;
				if (proto == null)
				{
					proto = GetFunctionPrototype(ParentScope);
					Prototype = proto;
				}
				return proto;
			}
		}

		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			return idcall.ExecIdCall(this, cx, scope, thisObj, args);
		}

		public override Scriptable CreateObject(Context cx, Scriptable scope)
		{
			if (useCallAsConstructor)
			{
				return null;
			}
			// Throw error if not explicitly coded to be used as constructor,
			// to satisfy ECMAScript standard (see bugzilla 202019).
			// To follow current (2003-05-01) SpiderMonkey behavior, change it to:
			// return super.createObject(cx, scope);
			throw ScriptRuntime.TypeError1("msg.not.ctor", functionName);
		}

		internal override string Decompile(int indent, int flags)
		{
			StringBuilder sb = new StringBuilder();
			bool justbody = (0 != (flags & Decompiler.ONLY_BODY_FLAG));
			if (!justbody)
			{
				sb.Append("function ");
				sb.Append(FunctionName);
				sb.Append("() { ");
			}
			sb.Append("[native code for ");
			var sobj = idcall as Scriptable;
			if (sobj != null)
			{
				sb.Append(sobj.GetClassName());
				sb.Append('.');
			}
			sb.Append(FunctionName);
			sb.Append(", arity=");
			sb.Append(Arity);
			sb.Append(justbody ? "]\n" : "] }\n");
			return sb.ToString();
		}

		public override int Arity
		{
			get { return arity; }
		}

		public override int Length
		{
			get { return Arity; }
		}

		public override string FunctionName
		{
			get { return functionName ?? string.Empty; }
		}

		public Exception Unknown()
		{
			// It is program error to call id-like methods for unknown function
			return new ArgumentException("BAD FUNCTION ID=" + methodId + " MASTER=" + idcall);
		}

		private readonly IdFunctionCall idcall;

		private readonly object tag;

		private readonly int methodId;

		private int arity;

		private bool useCallAsConstructor;

		private string functionName;
	}
}
