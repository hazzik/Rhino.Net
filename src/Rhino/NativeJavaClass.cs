/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// This class reflects Java classes into the JavaScript environment, mainly
	/// for constructors and static members.
	/// </summary>
	/// <remarks>
	/// This class reflects Java classes into the JavaScript environment, mainly
	/// for constructors and static members.  We lazily reflect properties,
	/// and currently do not guarantee that a single j.l.Class is only
	/// reflected once into the JS environment, although we should.
	/// The only known case where multiple reflections
	/// are possible occurs when a j.l.Class is wrapped as part of a
	/// method return or property access, rather than by walking the
	/// Packages/java tree.
	/// </remarks>
	/// <author>Mike Shaver</author>
	/// <seealso cref="NativeJavaArray">NativeJavaArray</seealso>
	/// <seealso cref="NativeJavaObject">NativeJavaObject</seealso>
	/// <seealso cref="NativeJavaPackage">NativeJavaPackage</seealso>
	[System.Serializable]
	public class NativeJavaClass : NativeJavaObject, Function
	{
		internal const long serialVersionUID = -6460763940409461664L;

		internal const string javaClassPropertyName = "__javaObject__";

		public NativeJavaClass()
		{
		}

		public NativeJavaClass(Scriptable scope, Type cl) : this(scope, cl, false)
		{
		}

		public NativeJavaClass(Scriptable scope, Type cl, bool isAdapter) : base(scope, cl, null, isAdapter)
		{
		}

		// Special property for getting the underlying Java class object.
		protected internal override void InitMembers()
		{
			Type cl = (Type)javaObject;
			members = JavaMembers.LookupClass(parent, cl, cl, isAdapter);
			staticFieldAndMethods = members.GetFieldAndMethodsObjects(this, cl, true);
		}

		public override string GetClassName()
		{
			return "JavaClass";
		}

		public override bool Has(string name, Scriptable start)
		{
			return members.Has(name, true) || javaClassPropertyName.Equals(name);
		}

		public override object Get(string name, Scriptable start)
		{
			// When used as a constructor, ScriptRuntime.newObject() asks
			// for our prototype to create an object of the correct type.
			// We don't really care what the object is, since we're returning
			// one constructed out of whole cloth, so we return null.
			if (name.Equals("prototype"))
			{
				return null;
			}
			if (staticFieldAndMethods != null)
			{
				object result = staticFieldAndMethods.Get(name);
				if (result != null)
				{
					return result;
				}
			}
			if (members.Has(name, true))
			{
				return members.Get(this, name, javaObject, true);
			}
			Context cx = Context.GetContext();
			Scriptable scope = ScriptableObject.GetTopLevelScope(start);
			WrapFactory wrapFactory = cx.GetWrapFactory();
			if (javaClassPropertyName.Equals(name))
			{
				return wrapFactory.Wrap(cx, scope, javaObject, ScriptRuntime.ClassClass);
			}
			// experimental:  look for nested classes by appending $name to
			// current class' name.
			Type nestedClass = FindNestedClass(GetClassObject(), name);
			if (nestedClass != null)
			{
				Scriptable nestedValue = wrapFactory.WrapJavaClass(cx, scope, nestedClass);
				nestedValue.SetParentScope(this);
				return nestedValue;
			}
			throw members.ReportMemberNotFound(name);
		}

		public override void Put(string name, Scriptable start, object value)
		{
			members.Put(this, name, javaObject, value, true);
		}

		public override object[] GetIds()
		{
			return members.GetIds(true);
		}

		public virtual Type GetClassObject()
		{
			return (Type)base.Unwrap();
		}

		public override object GetDefaultValue(Type hint)
		{
			if (hint == null || hint == ScriptRuntime.StringClass)
			{
				return this.ToString();
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

		public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			// If it looks like a "cast" of an object to this class type,
			// walk the prototype chain to see if there's a wrapper of a
			// object that's an instanceof this class.
			if (args.Length == 1 && args[0] is Scriptable)
			{
				Type c = GetClassObject();
				Scriptable p = (Scriptable)args[0];
				do
				{
					if (p is Wrapper)
					{
						object o = ((Wrapper)p).Unwrap();
						if (c.IsInstanceOfType(o))
						{
							return p;
						}
					}
					p = p.GetPrototype();
				}
				while (p != null);
			}
			return Construct(cx, scope, args);
		}

		public virtual Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			Type classObject = GetClassObject();
			if (!(classObject.IsInterface || classObject.IsAbstract))
			{
				NativeJavaMethod ctors = members.ctors;
				int index = ctors.FindCachedFunction(cx, args);
				if (index < 0)
				{
					string sig = NativeJavaMethod.ScriptSignature(args);
					throw Context.ReportRuntimeError2("msg.no.java.ctor", classObject.FullName, sig);
				}
				// Found the constructor, so try invoking it.
				return ConstructSpecific(cx, scope, args, ctors.methods[index]);
			}
			else
			{
				if (args.Length == 0)
				{
					throw Context.ReportRuntimeError0("msg.adapter.zero.args");
				}
				Scriptable topLevel = ScriptableObject.GetTopLevelScope(this);
				string msg = string.Empty;
				try
				{
					// When running on Android create an InterfaceAdapter since our
					// bytecode generation won't work on Dalvik VM.
					if ("Dalvik".Equals(Runtime.GetProperty("java.vm.name")) && classObject.IsInterface)
					{
						object obj = CreateInterfaceAdapter(classObject, ScriptableObject.EnsureScriptableObject(args[0]));
						return cx.GetWrapFactory().WrapAsJavaObject(cx, scope, obj, null);
					}
					// use JavaAdapter to construct a new class on the fly that
					// implements/extends this interface/abstract class.
					object v = topLevel.Get("JavaAdapter", topLevel);
					if (v != ScriptableConstants.NOT_FOUND)
					{
						Function f = (Function)v;
						// Args are (interface, js object)
						object[] adapterArgs = new object[] { this, args[0] };
						return f.Construct(cx, topLevel, adapterArgs);
					}
				}
				catch (Exception ex)
				{
					// fall through to error
					string m = ex.Message;
					if (m != null)
					{
						msg = m;
					}
				}
				throw Context.ReportRuntimeError2("msg.cant.instantiate", msg, classObject.FullName);
			}
		}

		internal static Scriptable ConstructSpecific(Context cx, Scriptable scope, object[] args, MemberBox ctor)
		{
			object instance = ConstructInternal(args, ctor);
			// we need to force this to be wrapped, because construct _has_
			// to return a scriptable
			Scriptable topLevel = ScriptableObject.GetTopLevelScope(scope);
			return cx.GetWrapFactory().WrapNewObject(cx, topLevel, instance);
		}

		internal static object ConstructInternal(object[] args, MemberBox ctor)
		{
			Type[] argTypes = ctor.argTypes;
			if (ctor.vararg)
			{
				// marshall the explicit parameter
				object[] newArgs = new object[argTypes.Length];
				for (int i = 0; i < argTypes.Length - 1; i++)
				{
					newArgs[i] = Context.JsToJava(args[i], argTypes[i]);
				}
				object varArgs;
				// Handle special situation where a single variable parameter
				// is given and it is a Java or ECMA array.
				if (args.Length == argTypes.Length && (args[args.Length - 1] == null || args[args.Length - 1] is NativeArray || args[args.Length - 1] is NativeJavaArray))
				{
					// convert the ECMA array into a native array
					varArgs = Context.JsToJava(args[args.Length - 1], argTypes[argTypes.Length - 1]);
				}
				else
				{
					// marshall the variable parameter
					Type componentType = argTypes[argTypes.Length - 1].GetElementType();
					varArgs = System.Array.CreateInstance(componentType, args.Length - argTypes.Length + 1);
					for (int i_1 = 0; i_1 < Sharpen.Runtime.GetArrayLength(varArgs); i_1++)
					{
						object value = Context.JsToJava(args[argTypes.Length - 1 + i_1], componentType);
						Sharpen.Runtime.SetArrayValue(varArgs, i_1, value);
					}
				}
				// add varargs
				newArgs[argTypes.Length - 1] = varArgs;
				// replace the original args with the new one
				args = newArgs;
			}
			else
			{
				object[] origArgs = args;
				for (int i = 0; i < args.Length; i++)
				{
					object arg = args[i];
					object x = Context.JsToJava(arg, argTypes[i]);
					if (x != arg)
					{
						if (args == origArgs)
						{
							args = origArgs.Clone();
						}
						args[i] = x;
					}
				}
			}
			return ctor.NewInstance(args);
		}

		public override string ToString()
		{
			return "[JavaClass " + GetClassObject().FullName + "]";
		}

		/// <summary>
		/// Determines if prototype is a wrapped Java object and performs
		/// a Java "instanceof".
		/// </summary>
		/// <remarks>
		/// Determines if prototype is a wrapped Java object and performs
		/// a Java "instanceof".
		/// Exception: if value is an instance of NativeJavaClass, it isn't
		/// considered an instance of the Java class; this forestalls any
		/// name conflicts between java.lang.Class's methods and the
		/// static methods exposed by a JavaNativeClass.
		/// </remarks>
		public override bool HasInstance(Scriptable value)
		{
			if (value is Wrapper && !(value is Rhino.NativeJavaClass))
			{
				object instance = ((Wrapper)value).Unwrap();
				return GetClassObject().IsInstanceOfType(instance);
			}
			// value wasn't something we understand
			return false;
		}

		private static Type FindNestedClass(Type parentClass, string name)
		{
			string nestedClassName = parentClass.FullName + '$' + name;
			ClassLoader loader = parentClass.GetClassLoader();
			if (loader == null)
			{
				// ALERT: if loader is null, nested class should be loaded
				// via system class loader which can be different from the
				// loader that brought Rhino classes that Class.forName() would
				// use, but ClassLoader.getSystemClassLoader() is Java 2 only
				return Kit.ClassOrNull(nestedClassName);
			}
			else
			{
				return Kit.ClassOrNull(loader, nestedClassName);
			}
		}

		private IDictionary<string, FieldAndMethods> staticFieldAndMethods;
	}
}
