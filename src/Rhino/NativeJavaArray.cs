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
	[Serializable]
	public class NativeJavaArray : NativeJavaObject
	{
		public override string GetClassName()
		{
			return "JavaArray";
		}

		public static NativeJavaArray Wrap(Scriptable scope, Array array)
		{
			return new NativeJavaArray(scope, array);
		}

		public override object Unwrap()
		{
			return array;
		}

		public NativeJavaArray(Scriptable scope, Array array) : base(scope, null, ScriptRuntime.ObjectClass)
		{
			Type cl = array.GetType();
			if (!cl.IsArray)
			{
				throw new Exception("Array expected");
			}
			this.array = array;
			length = array.Length;
			cls = cl.GetElementType();
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
				return length;
			}
			object result = base.Get(id, start);
			if (result == ScriptableConstants.NOT_FOUND && !ScriptableObject.HasProperty(Prototype, id))
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
				object obj = array.GetValue(index);
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
				array.SetValue(Context.JsToJava(value, cls), index);
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
				return ScriptRuntime.NaN;
			}
			return this;
		}

		public override object[] GetIds()
		{
			object[] result = new object[length];
			int i = length;
			while (--i >= 0)
			{
				result[i] = i;
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

		public override Scriptable Prototype
		{
			get
			{
				if (prototype == null)
				{
					prototype = ScriptableObject.GetArrayPrototype(ParentScope);
				}
				return prototype;
			}
		}

		internal Array array;

		internal int length;

		internal Type cls;
	}
}
