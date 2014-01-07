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
	/// <summary>
	/// The base class for Function objects
	/// See ECMA 15.3.
	/// </summary>
	/// <remarks>
	/// The base class for Function objects
	/// See ECMA 15.3.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public class BaseFunction : IdScriptableObject, Function
	{
		internal const long serialVersionUID = 5311394446546053859L;

		private static readonly object FUNCTION_TAG = "Function";

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.BaseFunction obj = new Rhino.BaseFunction();
			// Function.prototype attributes: see ECMA 15.3.3.1
			obj.prototypePropertyAttributes = PropertyAttributes.DONTENUM | PropertyAttributes.READONLY | PropertyAttributes.PERMANENT;
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		public BaseFunction()
		{
		}

		public BaseFunction(Scriptable scope, Scriptable prototype) : base(scope, prototype)
		{
		}

		public override string GetClassName()
		{
			return "Function";
		}

		/// <summary>Gets the value returned by calling the typeof operator on this object.</summary>
		/// <remarks>Gets the value returned by calling the typeof operator on this object.</remarks>
		/// <seealso cref="ScriptableObject.GetTypeOf()">ScriptableObject.GetTypeOf()</seealso>
		/// <returns>
		/// "function" or "undefined" if
		/// <see cref="ScriptableObject.AvoidObjectDetection()">ScriptableObject.AvoidObjectDetection()</see>
		/// returns <code>true</code>
		/// </returns>
		public override string GetTypeOf()
		{
			return AvoidObjectDetection() ? "undefined" : "function";
		}

		/// <summary>Implements the instanceof operator for JavaScript Function objects.</summary>
		/// <remarks>
		/// Implements the instanceof operator for JavaScript Function objects.
		/// <p>
		/// <code>
		/// foo = new Foo();<br />
		/// foo instanceof Foo;  // true<br />
		/// </code>
		/// </remarks>
		/// <param name="instance">
		/// The value that appeared on the LHS of the instanceof
		/// operator
		/// </param>
		/// <returns>
		/// true if the "prototype" property of "this" appears in
		/// value's prototype chain
		/// </returns>
		public override bool HasInstance(Scriptable instance)
		{
			object protoProp = ScriptableObject.GetProperty(this, "prototype");
			if (protoProp is Scriptable)
			{
				return ScriptRuntime.JsDelegatesTo(instance, (Scriptable)protoProp);
			}
			throw ScriptRuntime.TypeError1("msg.instanceof.bad.prototype", GetFunctionName());
		}

		private const int Id_length = 1;

		private const int Id_arity = 2;

		private const int Id_name = 3;

		private const int Id_prototype = 4;

		private const int Id_arguments = 5;

		private const int MAX_INSTANCE_ID = 5;

		// #string_id_map#
		protected internal override int GetMaxInstanceId()
		{
			return MAX_INSTANCE_ID;
		}

		protected internal override InstanceIdInfo FindInstanceIdInfo(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:15:15 EDT
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 4:
				{
					X = "name";
					id = Id_name;
					goto L_break;
				}

				case 5:
				{
					X = "arity";
					id = Id_arity;
					goto L_break;
				}

				case 6:
				{
					X = "length";
					id = Id_length;
					goto L_break;
				}

				case 9:
				{
					c = s[0];
					if (c == 'a')
					{
						X = "arguments";
						id = Id_arguments;
					}
					else
					{
						if (c == 'p')
						{
							X = "prototype";
							id = Id_prototype;
						}
					}
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			// #/string_id_map#
			if (id == 0)
			{
				return base.FindInstanceIdInfo(s);
			}
			PropertyAttributes attr;
			switch (id)
			{
				case Id_length:
				case Id_arity:
				case Id_name:
				{
					attr = PropertyAttributes.DONTENUM | PropertyAttributes.READONLY | PropertyAttributes.PERMANENT;
					break;
				}

				case Id_prototype:
				{
					// some functions such as built-ins don't have a prototype property
					if (!HasPrototypeProperty())
					{
						return null;
					}
					attr = prototypePropertyAttributes;
					break;
				}

				case Id_arguments:
				{
					attr = PropertyAttributes.DONTENUM | PropertyAttributes.PERMANENT;
					break;
				}

				default:
				{
					throw new InvalidOperationException();
				}
			}
			return InstanceIdInfo(attr, id);
		}

		protected internal override string GetInstanceIdName(int id)
		{
			switch (id)
			{
				case Id_length:
				{
					return "length";
				}

				case Id_arity:
				{
					return "arity";
				}

				case Id_name:
				{
					return "name";
				}

				case Id_prototype:
				{
					return "prototype";
				}

				case Id_arguments:
				{
					return "arguments";
				}
			}
			return base.GetInstanceIdName(id);
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			switch (id)
			{
				case Id_length:
				{
					return ScriptRuntime.WrapInt(GetLength());
				}

				case Id_arity:
				{
					return ScriptRuntime.WrapInt(GetArity());
				}

				case Id_name:
				{
					return GetFunctionName();
				}

				case Id_prototype:
				{
					return GetPrototypeProperty();
				}

				case Id_arguments:
				{
					return GetArguments();
				}
			}
			return base.GetInstanceIdValue(id);
		}

		protected internal override void SetInstanceIdValue(int id, object value)
		{
			switch (id)
			{
				case Id_prototype:
				{
					if ((prototypePropertyAttributes & PropertyAttributes.READONLY) == 0)
					{
						prototypeProperty = (value != null) ? value : UniqueTag.NULL_VALUE;
					}
					return;
				}

				case Id_arguments:
				{
					if (value == ScriptableConstants.NOT_FOUND)
					{
						// This should not be called since "arguments" is PERMANENT
						Kit.CodeBug();
					}
					DefaultPut("arguments", value);
					return;
				}

				case Id_name:
				case Id_arity:
				case Id_length:
				{
					return;
				}
			}
			base.SetInstanceIdValue(id, value);
		}

		protected internal override void FillConstructorProperties(IdFunctionObject ctor)
		{
			// Fix up bootstrapping problem: getPrototype of the IdFunctionObject
			// can not return Function.prototype because Function object is not
			// yet defined.
			ctor.SetPrototype(this);
			base.FillConstructorProperties(ctor);
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 1;
					s = "constructor";
					break;
				}

				case Id_toString:
				{
					arity = 1;
					s = "toString";
					break;
				}

				case Id_toSource:
				{
					arity = 1;
					s = "toSource";
					break;
				}

				case Id_apply:
				{
					arity = 2;
					s = "apply";
					break;
				}

				case Id_call:
				{
					arity = 1;
					s = "call";
					break;
				}

				case Id_bind:
				{
					arity = 1;
					s = "bind";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(FUNCTION_TAG, id, s, arity);
		}

		internal static bool IsApply(IdFunctionObject f)
		{
			return f.HasTag(FUNCTION_TAG) && f.MethodId() == Id_apply;
		}

		internal static bool IsApplyOrCall(IdFunctionObject f)
		{
			if (f.HasTag(FUNCTION_TAG))
			{
				switch (f.MethodId())
				{
					case Id_apply:
					case Id_call:
					{
						return true;
					}
				}
			}
			return false;
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(FUNCTION_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_constructor:
				{
					return JsConstructor(cx, scope, args);
				}

				case Id_toString:
				{
					Rhino.BaseFunction realf = RealFunction(thisObj, f);
					int indent = ScriptRuntime.ToInt32(args, 0);
					return realf.Decompile(indent, 0);
				}

				case Id_toSource:
				{
					Rhino.BaseFunction realf = RealFunction(thisObj, f);
					int indent = 0;
					int flags = Decompiler.TO_SOURCE_FLAG;
					if (args.Length != 0)
					{
						indent = ScriptRuntime.ToInt32(args[0]);
						if (indent >= 0)
						{
							flags = 0;
						}
						else
						{
							indent = 0;
						}
					}
					return realf.Decompile(indent, flags);
				}

				case Id_apply:
				case Id_call:
				{
					return ScriptRuntime.ApplyOrCall(id == Id_apply, cx, scope, thisObj, args);
				}

				case Id_bind:
				{
					if (!(thisObj is Callable))
					{
						throw ScriptRuntime.NotFunctionError(thisObj);
					}
					Callable targetFunction = (Callable)thisObj;
					int argc = args.Length;
					Scriptable boundThis;
					object[] boundArgs;
					if (argc > 0)
					{
						boundThis = ScriptRuntime.ToObjectOrNull(cx, args[0], scope);
						boundArgs = new object[argc - 1];
						System.Array.Copy(args, 1, boundArgs, 0, argc - 1);
					}
					else
					{
						boundThis = null;
						boundArgs = ScriptRuntime.emptyArgs;
					}
					return new BoundFunction(cx, scope, targetFunction, boundThis, boundArgs);
				}
			}
			throw new ArgumentException(id.ToString());
		}

		private Rhino.BaseFunction RealFunction(Scriptable thisObj, IdFunctionObject f)
		{
			object x = thisObj.GetDefaultValue(ScriptRuntime.FunctionClass);
			if (x is Delegator)
			{
				x = ((Delegator)x).GetDelegee();
			}
			if (x is Rhino.BaseFunction)
			{
				return (Rhino.BaseFunction)x;
			}
			throw ScriptRuntime.TypeError1("msg.incompat.call", f.GetFunctionName());
		}

		/// <summary>
		/// Make value as DontEnum, DontDelete, ReadOnly
		/// prototype property of this Function object
		/// </summary>
		public virtual void SetImmunePrototypeProperty(object value)
		{
			if ((prototypePropertyAttributes & PropertyAttributes.READONLY) != 0)
			{
				throw new InvalidOperationException();
			}
			prototypeProperty = (value != null) ? value : UniqueTag.NULL_VALUE;
			prototypePropertyAttributes = PropertyAttributes.DONTENUM | PropertyAttributes.PERMANENT | PropertyAttributes.READONLY;
		}

		protected internal virtual Scriptable GetClassPrototype()
		{
			object protoVal = GetPrototypeProperty();
			if (protoVal is Scriptable)
			{
				return (Scriptable)protoVal;
			}
			return ScriptableObject.GetObjectPrototype(this);
		}

		/// <summary>Should be overridden.</summary>
		/// <remarks>Should be overridden.</remarks>
		public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			return Undefined.instance;
		}

		public virtual Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			Scriptable result = CreateObject(cx, scope);
			if (result != null)
			{
				object val = Call(cx, scope, result, args);
				if (val is Scriptable)
				{
					result = (Scriptable)val;
				}
			}
			else
			{
				object val = Call(cx, scope, null, args);
				if (!(val is Scriptable))
				{
					// It is program error not to return Scriptable from
					// the call method if createObject returns null.
					throw new InvalidOperationException("Bad implementaion of call as constructor, name=" + GetFunctionName() + " in " + GetType().FullName);
				}
				result = (Scriptable)val;
				if (result.GetPrototype() == null)
				{
					Scriptable proto = GetClassPrototype();
					if (result != proto)
					{
						result.SetPrototype(proto);
					}
				}
				if (result.GetParentScope() == null)
				{
					Scriptable parent = GetParentScope();
					if (result != parent)
					{
						result.SetParentScope(parent);
					}
				}
			}
			return result;
		}

		/// <summary>Creates new script object.</summary>
		/// <remarks>
		/// Creates new script object.
		/// The default implementation of
		/// <see cref="Construct(Context, Scriptable, object[])">Construct(Context, Scriptable, object[])</see>
		/// uses the method to
		/// to get the value for <tt>thisObj</tt> argument when invoking
		/// <see cref="Call(Context, Scriptable, Scriptable, object[])">Call(Context, Scriptable, Scriptable, object[])</see>
		/// .
		/// The methos is allowed to return <tt>null</tt> to indicate that
		/// <see cref="Call(Context, Scriptable, Scriptable, object[])">Call(Context, Scriptable, Scriptable, object[])</see>
		/// will create a new object itself. In this case
		/// <see cref="Construct(Context, Scriptable, object[])">Construct(Context, Scriptable, object[])</see>
		/// will set scope and prototype on the result
		/// <see cref="Call(Context, Scriptable, Scriptable, object[])">Call(Context, Scriptable, Scriptable, object[])</see>
		/// unless they are already set.
		/// </remarks>
		public virtual Scriptable CreateObject(Context cx, Scriptable scope)
		{
			Scriptable newInstance = new NativeObject();
			newInstance.SetPrototype(GetClassPrototype());
			newInstance.SetParentScope(GetParentScope());
			return newInstance;
		}

		/// <summary>
		/// Decompile the source information associated with this js
		/// function/script back into a string.
		/// </summary>
		/// <remarks>
		/// Decompile the source information associated with this js
		/// function/script back into a string.
		/// </remarks>
		/// <param name="indent">How much to indent the decompiled result.</param>
		/// <param name="flags">Flags specifying format of decompilation output.</param>
		internal virtual string Decompile(int indent, int flags)
		{
			StringBuilder sb = new StringBuilder();
			bool justbody = (0 != (flags & Decompiler.ONLY_BODY_FLAG));
			if (!justbody)
			{
				sb.Append("function ");
				sb.Append(GetFunctionName());
				sb.Append("() {\n\t");
			}
			sb.Append("[native code, arity=");
			sb.Append(GetArity());
			sb.Append("]\n");
			if (!justbody)
			{
				sb.Append("}\n");
			}
			return sb.ToString();
		}

		public virtual int GetArity()
		{
			return 0;
		}

		public virtual int GetLength()
		{
			return 0;
		}

		public virtual string GetFunctionName()
		{
			return string.Empty;
		}

		protected internal virtual bool HasPrototypeProperty()
		{
			return prototypeProperty != null || this is NativeFunction;
		}

		protected internal virtual object GetPrototypeProperty()
		{
			object result = prototypeProperty;
			if (result == null)
			{
				// only create default prototype on native JavaScript functions,
				// not on built-in functions, java methods, host objects etc.
				if (this is NativeFunction)
				{
					result = SetupDefaultPrototype();
				}
				else
				{
					result = Undefined.instance;
				}
			}
			else
			{
				if (result == UniqueTag.NULL_VALUE)
				{
					result = null;
				}
			}
			return result;
		}

		private object SetupDefaultPrototype()
		{
			lock (this)
			{
				if (prototypeProperty != null)
				{
					return prototypeProperty;
				}
				NativeObject obj = new NativeObject();
				PropertyAttributes attr = PropertyAttributes.DONTENUM;
				obj.DefineProperty("constructor", this, attr);
				// put the prototype property into the object now, then in the
				// wacky case of a user defining a function Object(), we don't
				// get an infinite loop trying to find the prototype.
				prototypeProperty = obj;
				Scriptable proto = GetObjectPrototype(this);
				if (proto != obj)
				{
					// not the one we just made, it must remain grounded
					obj.SetPrototype(proto);
				}
				return obj;
			}
		}

		private object GetArguments()
		{
			// <Function name>.arguments is deprecated, so we use a slow
			// way of getting it that doesn't add to the invocation cost.
			// TODO: add warning, error based on version
			object value = DefaultGet("arguments");
			if (value != ScriptableConstants.NOT_FOUND)
			{
				// Should after changing <Function name>.arguments its
				// activation still be available during Function call?
				// This code assumes it should not:
				// defaultGet("arguments") != NOT_FOUND
				// means assigned arguments
				return value;
			}
			Context cx = Context.GetContext();
			NativeCall activation = ScriptRuntime.FindFunctionActivation(cx, this);
			return (activation == null) ? null : activation.Get("arguments", activation);
		}

		private static object JsConstructor(Context cx, Scriptable scope, object[] args)
		{
			int arglen = args.Length;
			StringBuilder sourceBuf = new StringBuilder();
			sourceBuf.Append("function ");
			if (cx.GetLanguageVersion() != LanguageVersion.VERSION_1_2)
			{
				sourceBuf.Append("anonymous");
			}
			sourceBuf.Append('(');
			// Append arguments as coma separated strings
			for (int i = 0; i < arglen - 1; i++)
			{
				if (i > 0)
				{
					sourceBuf.Append(',');
				}
				sourceBuf.Append(ScriptRuntime.ToString(args[i]));
			}
			sourceBuf.Append(") {");
			if (arglen != 0)
			{
				// append function body
				string funBody = ScriptRuntime.ToString(args[arglen - 1]);
				sourceBuf.Append(funBody);
			}
			sourceBuf.Append("\n}");
			string source = sourceBuf.ToString();
			int[] linep = new int[1];
			string filename = Context.GetSourcePositionFromStack(linep);
			if (filename == null)
			{
				filename = "<eval'ed string>";
				linep[0] = 1;
			}
			string sourceURI = ScriptRuntime.MakeUrlForGeneratedScript(false, filename, linep[0]);
			Scriptable global = ScriptableObject.GetTopLevelScope(scope);
			ErrorReporter reporter;
			reporter = DefaultErrorReporter.ForEval(cx.GetErrorReporter());
			Evaluator evaluator = Context.CreateInterpreter();
			if (evaluator == null)
			{
				throw new JavaScriptException("Interpreter not present", filename, linep[0]);
			}
			// Compile with explicit interpreter instance to force interpreter
			// mode.
			return cx.CompileFunction(global, source, evaluator, reporter, sourceURI, 1, null);
		}

		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #string_id_map#
			// #generated# Last update: 2009-07-24 16:00:52 EST
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 4:
				{
					c = s[0];
					if (c == 'b')
					{
						X = "bind";
						id = Id_bind;
					}
					else
					{
						if (c == 'c')
						{
							X = "call";
							id = Id_call;
						}
					}
					goto L_break;
				}

				case 5:
				{
					X = "apply";
					id = Id_apply;
					goto L_break;
				}

				case 8:
				{
					c = s[3];
					if (c == 'o')
					{
						X = "toSource";
						id = Id_toSource;
					}
					else
					{
						if (c == 't')
						{
							X = "toString";
							id = Id_toString;
						}
					}
					goto L_break;
				}

				case 11:
				{
					X = "constructor";
					id = Id_constructor;
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toSource = 3;

		private const int Id_apply = 4;

		private const int Id_call = 5;

		private const int Id_bind = 6;

		private const int MAX_PROTOTYPE_ID = Id_bind;

		private object prototypeProperty;

		private PropertyAttributes prototypePropertyAttributes = PropertyAttributes.PERMANENT | PropertyAttributes.DONTENUM;
		// #/string_id_map#
		// For function object instances, attributes are
		//  {configurable:false, enumerable:false};
		// see ECMA 15.3.5.2
	}
}
