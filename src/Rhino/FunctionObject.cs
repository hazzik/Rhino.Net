/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using System.Security;
using Sharpen;

namespace Rhino
{
	[Serializable]
	public class FunctionObject : BaseFunction
	{
		/// <summary>Create a JavaScript function object from a Java method.</summary>
		/// <remarks>
		/// Create a JavaScript function object from a Java method.
		/// <p>The <code>member</code> argument must be either a java.lang.reflect.Method
		/// or a java.lang.reflect.Constructor and must match one of two forms.<p>
		/// The first form is a member with zero or more parameters
		/// of the following types: Object, String, boolean, Scriptable,
		/// int, or double. The Long type is not supported
		/// because the double representation of a long (which is the
		/// EMCA-mandated storage type for Numbers) may lose precision.
		/// If the member is a Method, the return value must be void or one
		/// of the types allowed for parameters.<p>
		/// The runtime will perform appropriate conversions based
		/// upon the type of the parameter. A parameter type of
		/// Object specifies that no conversions are to be done. A parameter
		/// of type String will use Context.toString to convert arguments.
		/// Similarly, parameters of type double, boolean, and Scriptable
		/// will cause Context.toNumber, Context.toBoolean, and
		/// Context.toObject, respectively, to be called.<p>
		/// If the method is not static, the Java 'this' value will
		/// correspond to the JavaScript 'this' value. Any attempt
		/// to call the function with a 'this' value that is not
		/// of the right Java type will result in an error.<p>
		/// The second form is the variable arguments (or "varargs")
		/// form. If the FunctionObject will be used as a constructor,
		/// the member must have the following parameters
		/// <pre>
		/// (Context cx, Object[] args, Function ctorObj,
		/// boolean inNewExpr)</pre>
		/// and if it is a Method, be static and return an Object result.<p>
		/// Otherwise, if the FunctionObject will <i>not</i> be used to define a
		/// constructor, the member must be a static Method with parameters
		/// <pre>
		/// (Context cx, Scriptable thisObj, Object[] args,
		/// Function funObj) </pre>
		/// and an Object result.<p>
		/// When the function varargs form is called as part of a function call,
		/// the <code>args</code> parameter contains the
		/// arguments, with <code>thisObj</code>
		/// set to the JavaScript 'this' value. <code>funObj</code>
		/// is the function object for the invoked function.<p>
		/// When the constructor varargs form is called or invoked while evaluating
		/// a <code>new</code> expression, <code>args</code> contains the
		/// arguments, <code>ctorObj</code> refers to this FunctionObject, and
		/// <code>inNewExpr</code> is true if and only if  a <code>new</code>
		/// expression caused the call. This supports defining a function that
		/// has different behavior when called as a constructor than when
		/// invoked as a normal function call. (For example, the Boolean
		/// constructor, when called as a function,
		/// will convert to boolean rather than creating a new object.)<p>
		/// </remarks>
		/// <param name="name">the name of the function</param>
		/// <param name="methodOrConstructor">
		/// a java.lang.reflect.Method or a java.lang.reflect.Constructor
		/// that defines the object
		/// </param>
		/// <param name="scope">enclosing scope of function</param>
		/// <seealso cref="Scriptable">Scriptable</seealso>
		public FunctionObject(string name, MethodBase methodOrConstructor, Scriptable scope)
		{
			// API class
			member = new MemberBox(methodOrConstructor);
			isStatic = methodOrConstructor.IsConstructor || methodOrConstructor.IsStatic;
			functionName = name;
			Type[] types = member.argTypes;
			int arity = types.Length;
			if (arity == 4 && (types[1].IsArray || types[2].IsArray))
			{
				// Either variable args or an error.
				if (types[1].IsArray)
				{
					if (!isStatic || types[0] != ScriptRuntime.ContextClass || types[1].GetElementType() != ScriptRuntime.ObjectClass || types[2] != ScriptRuntime.FunctionClass || types[3] != typeof(bool))
					{
						throw Context.ReportRuntimeError1("msg.varargs.ctor", methodOrConstructor.Name);
					}
					parmsLength = VARARGS_CTOR;
				}
				else
				{
					if (!isStatic || types[0] != ScriptRuntime.ContextClass || types[1] != ScriptRuntime.ScriptableClass || types[2].GetElementType() != ScriptRuntime.ObjectClass || types[3] != ScriptRuntime.FunctionClass)
					{
						throw Context.ReportRuntimeError1("msg.varargs.fun", methodOrConstructor.Name);
					}
					parmsLength = VARARGS_METHOD;
				}
			}
			else
			{
				parmsLength = arity;
				if (arity > 0)
				{
					typeTags = new byte[arity];
					for (int i = 0; i != arity; ++i)
					{
						int tag = GetTypeTag(types[i]);
						if (tag == JAVA_UNSUPPORTED_TYPE)
						{
							throw Context.ReportRuntimeError2("msg.bad.parms", types[i].FullName, methodOrConstructor.Name);
						}
						typeTags[i] = unchecked((byte)tag);
					}
				}
			}
			if (!member.method.IsConstructor)
			{
				MethodInfo method = member.Method();
				Type returnType = method.ReturnType;
				if (returnType == typeof(void))
				{
					hasVoidReturn = true;
				}
				else
				{
					returnTypeTag = GetTypeTag(returnType);
				}
			}
			else
			{
				Type ctorType = member.GetDeclaringClass();
				if (!ScriptRuntime.ScriptableClass.IsAssignableFrom(ctorType))
				{
					throw Context.ReportRuntimeError1("msg.bad.ctor.return", ctorType.FullName);
				}
			}
			ScriptRuntime.SetFunctionProtoAndParent(this, scope);
		}

		/// <returns>
		/// One of <tt>JAVA_*_TYPE</tt> constants to indicate desired type
		/// or
		/// <see cref="JAVA_UNSUPPORTED_TYPE">JAVA_UNSUPPORTED_TYPE</see>
		/// if the convertion is not
		/// possible
		/// </returns>
		public static int GetTypeTag(Type type)
		{
			if (type == ScriptRuntime.StringClass)
			{
				return JAVA_STRING_TYPE;
			}
			if (type == ScriptRuntime.IntegerClass || type == typeof(int))
			{
				return JAVA_INT_TYPE;
			}
			if (type == ScriptRuntime.BooleanClass || type == typeof(bool))
			{
				return JAVA_BOOLEAN_TYPE;
			}
			if (type == ScriptRuntime.DoubleClass || type == typeof(double))
			{
				return JAVA_DOUBLE_TYPE;
			}
			if (ScriptRuntime.ScriptableClass.IsAssignableFrom(type))
			{
				return JAVA_SCRIPTABLE_TYPE;
			}
			if (type == ScriptRuntime.ObjectClass)
			{
				return JAVA_OBJECT_TYPE;
			}
			// Note that the long type is not supported; see the javadoc for
			// the constructor for this class
			return JAVA_UNSUPPORTED_TYPE;
		}

		public static object ConvertArg(Context cx, Scriptable scope, object arg, int typeTag)
		{
			switch (typeTag)
			{
				case JAVA_STRING_TYPE:
				{
					if (arg is string)
					{
						return arg;
					}
					return ScriptRuntime.ToString(arg);
				}

				case JAVA_INT_TYPE:
				{
					if (arg is int)
					{
						return arg;
					}
					return ScriptRuntime.ToInt32(arg);
				}

				case JAVA_BOOLEAN_TYPE:
				{
					if (arg is bool)
					{
						return arg;
					}
					return ScriptRuntime.ToBoolean(arg) ? true : false;
				}

				case JAVA_DOUBLE_TYPE:
				{
					if (arg is double)
					{
						return arg;
					}
					return ScriptRuntime.ToNumber(arg);
				}

				case JAVA_SCRIPTABLE_TYPE:
				{
					return ScriptRuntime.ToObjectOrNull(cx, arg, scope);
				}

				case JAVA_OBJECT_TYPE:
				{
					return arg;
				}

				default:
				{
					throw new ArgumentException();
				}
			}
		}

		/// <summary>
		/// Return the value defined by  the method used to construct the object
		/// (number of parameters of the method, or 1 if the method is a "varargs"
		/// form).
		/// </summary>
		/// <remarks>
		/// Return the value defined by  the method used to construct the object
		/// (number of parameters of the method, or 1 if the method is a "varargs"
		/// form).
		/// </remarks>
		public override int Arity
		{
			get { return parmsLength < 0 ? 1 : parmsLength; }
		}

		/// <summary>
		/// Return the same value as
		/// <see cref="GetArity()">GetArity()</see>
		/// .
		/// </summary>
		public override int Length
		{
			get { return Arity; }
		}

		public override string GetFunctionName()
		{
			return functionName ?? string.Empty;
		}

		/// <summary>Get Java method or constructor this function represent.</summary>
		/// <remarks>Get Java method or constructor this function represent.</remarks>
		public virtual MemberInfo GetMethodOrConstructor()
		{
			return member.method;
		}

		internal static MethodInfo FindSingleMethod(MethodInfo[] methods, string name)
		{
			MethodInfo found = null;
			for (int i = 0, N = methods.Length; i != N; ++i)
			{
				MethodInfo method = methods[i];
				if (method != null)
				{
					if (name == method.Name || char.ToUpperInvariant(name[0]) + name.Substring(1) == method.Name)
					{
						if (found != null)
						{
							throw Context.ReportRuntimeError2("msg.no.overload", name, method.DeclaringType.FullName);
						}
						found = method;
					}
				}
			}
			return found;
		}

		/// <summary>Returns all public methods declared by the specified class.</summary>
		/// <remarks>
		/// Returns all public methods declared by the specified class. This excludes
		/// inherited methods.
		/// </remarks>
		/// <param name="clazz">the class from which to pull public declared methods</param>
		/// <returns>the public methods declared in the specified class</returns>
		/// <seealso cref="Sharpen.Runtime.GetDeclaredMethods()">Sharpen.Runtime.GetDeclaredMethods()</seealso>
		internal static MethodInfo[] GetMethodList(Type clazz)
		{
			MethodInfo[] methods = null;
			try
			{
				// getDeclaredMethods may be rejected by the security manager
				// but getMethods is more expensive
				if (!sawSecurityException)
				{
					methods = Runtime.GetDeclaredMethods(clazz);
				}
			}
			catch (SecurityException)
			{
				// If we get an exception once, give up on getDeclaredMethods
				sawSecurityException = true;
			}
			if (methods == null)
			{
				methods = clazz.GetMethods();
			}
			int count = 0;
			for (int i = 0; i < methods.Length; i++)
			{
				if (sawSecurityException ? methods[i].DeclaringType != clazz : !methods[i].IsPublic)
				{
					methods[i] = null;
				}
				else
				{
					count++;
				}
			}
			MethodInfo[] result = new MethodInfo[count];
			int j = 0;
			for (int i_1 = 0; i_1 < methods.Length; i_1++)
			{
				if (methods[i_1] != null)
				{
					result[j++] = methods[i_1];
				}
			}
			return result;
		}

		/// <summary>Define this function as a JavaScript constructor.</summary>
		/// <remarks>
		/// Define this function as a JavaScript constructor.
		/// <p>
		/// Sets up the "prototype" and "constructor" properties. Also
		/// calls setParent and setPrototype with appropriate values.
		/// Then adds the function object as a property of the given scope, using
		/// <code>prototype.getClassName()</code>
		/// as the name of the property.
		/// </remarks>
		/// <param name="scope">
		/// the scope in which to define the constructor (typically
		/// the global object)
		/// </param>
		/// <param name="prototype">the prototype object</param>
		/// <seealso cref="Scriptable.SetParentScope(SIScriptable">Scriptable.SetParentScope(Scriptable)</seealso>
		/// <seealso cref="Scriptable.SetPrototype(SIScriptable">Scriptable.SetPrototype(Scriptable)</seealso>
		/// <seealso cref="Scriptable.GetClassName()">Scriptable.GetClassName()</seealso>
		public virtual void AddAsConstructor(Scriptable scope, Scriptable prototype)
		{
			InitAsConstructor(scope, prototype);
			DefineProperty(scope, prototype.GetClassName(), this, PropertyAttributes.DONTENUM);
		}

		internal virtual void InitAsConstructor(Scriptable scope, Scriptable prototype)
		{
			ScriptRuntime.SetFunctionProtoAndParent(this, scope);
			SetImmunePrototypeProperty(prototype);
			prototype.ParentScope = this;
			DefineProperty(prototype, "constructor", this, PropertyAttributes.DONTENUM | PropertyAttributes.PERMANENT | PropertyAttributes.READONLY);
			ParentScope = scope;
		}

		/// <summary>
		/// Performs conversions on argument types if needed and
		/// invokes the underlying Java method or constructor.
		/// </summary>
		/// <remarks>
		/// Performs conversions on argument types if needed and
		/// invokes the underlying Java method or constructor.
		/// <p>
		/// Implements Function.call.
		/// </remarks>
		/// <seealso cref="Function.Call(Context, Scriptable, Scriptable, object[])">Function.Call(Context, Scriptable, Scriptable, object[])</seealso>
		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			object result;
			bool checkMethodResult = false;
			int argsLength = args.Length;
			if (parmsLength < 0)
			{
				if (parmsLength == VARARGS_METHOD)
				{
					object[] invokeArgs = { cx, thisObj, args, this };
					result = member.Invoke(null, invokeArgs);
					checkMethodResult = true;
				}
				else
				{
					bool inNewExpr = (thisObj == null);
					object[] invokeArgs = { cx, args, this, inNewExpr };
					result = (member.IsCtor()) ? member.NewInstance(invokeArgs) : member.Invoke(null, invokeArgs);
				}
			}
			else
			{
				if (!isStatic)
				{
					Type clazz = member.GetDeclaringClass();
					if (!clazz.IsInstanceOfType(thisObj))
					{
						bool compatible = false;
						if (thisObj == scope)
						{
							Scriptable parentScope = ParentScope;
							if (scope != parentScope)
							{
								// Call with dynamic scope for standalone function,
								// use parentScope as thisObj
								compatible = clazz.IsInstanceOfType(parentScope);
								if (compatible)
								{
									thisObj = parentScope;
								}
							}
						}
						if (!compatible)
						{
							// Couldn't find an object to call this on.
							throw ScriptRuntime.TypeError1("msg.incompat.call", functionName);
						}
					}
				}
				object[] invokeArgs;
				if (parmsLength == argsLength)
				{
					// Do not allocate new argument array if java arguments are
					// the same as the original js ones.
					invokeArgs = args;
					for (int i = 0; i < parmsLength; i++)
					{
						object arg = args[i];
						object converted = ConvertArg(cx, scope, arg, typeTags[i]);
						if (arg != converted)
						{
							if (invokeArgs == args)
							{
								invokeArgs = (object[]) args.Clone();
							}
							invokeArgs[i] = converted;
						}
					}
				}
				else
				{
					if (parmsLength == 0)
					{
						invokeArgs = ScriptRuntime.emptyArgs;
					}
					else
					{
						invokeArgs = new object[parmsLength];
						for (int i_1 = 0; i_1 != parmsLength; ++i_1)
						{
							object arg = (i_1 < argsLength) ? args[i_1] : Undefined.instance;
							invokeArgs[i_1] = ConvertArg(cx, scope, arg, typeTags[i_1]);
						}
					}
				}
				if (member.IsMethod())
				{
					result = member.Invoke(thisObj, invokeArgs);
					checkMethodResult = true;
				}
				else
				{
					result = member.NewInstance(invokeArgs);
				}
			}
			if (checkMethodResult)
			{
				if (hasVoidReturn)
				{
					result = Undefined.instance;
				}
				else
				{
					if (returnTypeTag == JAVA_UNSUPPORTED_TYPE)
					{
						result = cx.GetWrapFactory().Wrap(cx, scope, result, null);
					}
				}
			}
			// XXX: the code assumes that if returnTypeTag == JAVA_OBJECT_TYPE
			// then the Java method did a proper job of converting the
			// result to JS primitive or Scriptable to avoid
			// potentially costly Context.javaToJS call.
			return result;
		}

		/// <summary>
		/// Return new
		/// <see cref="Scriptable">Scriptable</see>
		/// instance using the default
		/// constructor for the class of the underlying Java method.
		/// Return null to indicate that the call method should be used to create
		/// new objects.
		/// </summary>
		public override Scriptable CreateObject(Context cx, Scriptable scope)
		{
			if (member.IsCtor() || parmsLength == VARARGS_CTOR)
			{
				return null;
			}
			Scriptable result;
			try
			{
				result = (Scriptable)Activator.CreateInstance(member.GetDeclaringClass());
			}
			catch (Exception ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
			result.SetPrototype(GetClassPrototype());
			result.ParentScope = ParentScope;
			return result;
		}

		internal virtual bool IsVarArgsMethod()
		{
			return parmsLength == VARARGS_METHOD;
		}

		internal virtual bool IsVarArgsConstructor()
		{
			return parmsLength == VARARGS_CTOR;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			if (parmsLength > 0)
			{
				Type[] types = member.argTypes;
				typeTags = new byte[parmsLength];
				for (int i = 0; i != parmsLength; ++i)
				{
					typeTags[i] = unchecked((byte)GetTypeTag(types[i]));
				}
			}
			if (member.IsMethod())
			{
				MethodInfo method = member.Method();
				Type returnType = method.ReturnType;
				if (returnType == typeof(void))
				{
					hasVoidReturn = true;
				}
				else
				{
					returnTypeTag = GetTypeTag(returnType);
				}
			}
		}

		private const short VARARGS_METHOD = -1;

		private const short VARARGS_CTOR = -2;

		private static bool sawSecurityException;

		public const int JAVA_UNSUPPORTED_TYPE = 0;

		public const int JAVA_STRING_TYPE = 1;

		public const int JAVA_INT_TYPE = 2;

		public const int JAVA_BOOLEAN_TYPE = 3;

		public const int JAVA_DOUBLE_TYPE = 4;

		public const int JAVA_SCRIPTABLE_TYPE = 5;

		public const int JAVA_OBJECT_TYPE = 6;

		internal MemberBox member;

		private string functionName;

		[NonSerialized]
		private byte[] typeTags;

		private int parmsLength;

		[NonSerialized]
		private bool hasVoidReturn;

		[NonSerialized]
		private int returnTypeTag;

		private bool isStatic;
	}
}
