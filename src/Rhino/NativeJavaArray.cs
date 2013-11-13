/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class reflects Java arrays into the JavaScript environment.</summary>
	/// <remarks>This class reflects Java arrays into the JavaScript environment.</remarks>
	/// <author>Mike Shaver</author>
	/// <seealso cref="NativeJavaClass">NativeJavaClass</seealso>
	/// <seealso cref="NativeJavaObject">NativeJavaObject</seealso>
	/// <seealso cref="NativeJavaPackage">NativeJavaPackage</seealso>
	[System.Serializable]
	public class NativeJavaArray : NativeJavaObject
	{
		internal const long serialVersionUID = -924022554283675333L;

		public override string GetClassName()
		{
			return "JavaArray";
		}

		public static Rhino.NativeJavaArray Wrap(Scriptable scope, object array)
		{
			return new Rhino.NativeJavaArray(scope, array);
		}

		public override object Unwrap()
		{
			return array;
		}

		public NativeJavaArray(Scriptable scope, object array) : base(scope, null, ScriptRuntime.ObjectClass)
		{
			Type cl = array.GetType();
			if (!cl.IsArray)
			{
				throw new Exception("Array expected");
			}
			this.array = array;
			this.length = Sharpen.Runtime.GetArrayLength(array);
			this.cls = cl.GetElementType();
		}

		public override bool Has(string id, Scriptable start)
		{
			return id.Equals("length") || base.Has(id, start);
		}

		public override bool Has(int index, Scriptable start)
		{
			return 0 <= index && index < length;
		}

		public override object Get(string id, Scriptable start)
		{
			if (id.Equals("length"))
			{
				return Sharpen.Extensions.ValueOf(length);
			}
			object result = base.Get(id, start);
			if (result == ScriptableConstants.NOT_FOUND && !ScriptableObject.HasProperty(GetPrototype(), id))
			{
				throw Context.ReportRuntimeError2("msg.java.member.not.found", array.GetType().FullName, id);
			}
			return result;
		}

		public override object Get(int index, Scriptable start)
		{
			if (0 <= index && index < length)
			{
				Context cx = Context.GetContext();
				object obj = Sharpen.Runtime.GetArrayValue(array, index);
				return cx.GetWrapFactory().Wrap(cx, this, obj, cls);
			}
			return Undefined.instance;
		}

		public override void Put(string id, Scriptable start, object value)
		{
			// Ignore assignments to "length"--it's readonly.
			if (!id.Equals("length"))
			{
				throw Context.ReportRuntimeError1("msg.java.array.member.not.found", id);
			}
		}

		public override void Put(int index, Scriptable start, object value)
		{
			if (0 <= index && index < length)
			{
				Sharpen.Runtime.SetArrayValue(array, index, Context.JsToJava(value, cls));
			}
			else
			{
				throw Context.ReportRuntimeError2("msg.java.array.index.out.of.bounds", index.ToString(), (length - 1).ToString());
			}
		}

		public override object GetDefaultValue(Type hint)
		{
			if (hint == null || hint == ScriptRuntime.StringClass)
			{
				return array.ToString();
			}
			if (hint == ScriptRuntime.BooleanClass)
			{
				return true;
			}
			if (hint == ScriptRuntime.NumberClass)
			{
				return ScriptRuntime.NaNobj;
			}
			return this;
		}

		public override object[] GetIds()
		{
			object[] result = new object[length];
			int i = length;
			while (--i >= 0)
			{
				result[i] = Sharpen.Extensions.ValueOf(i);
			}
			return result;
		}

		public override bool HasInstance(Scriptable value)
		{
			if (!(value is Wrapper))
			{
				return false;
			}
			object instance = ((Wrapper)value).Unwrap();
			return cls.IsInstanceOfType(instance);
		}

		public override Scriptable GetPrototype()
		{
			if (prototype == null)
			{
				prototype = ScriptableObject.GetArrayPrototype(this.GetParentScope());
			}
			return prototype;
		}

		internal object array;

		internal int length;

		internal Type cls;
	}
}