/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Org.Mozilla.Classfile;
using Rhino;
using Sharpen;

namespace Rhino
{
	public sealed class JavaAdapter : IdFunctionCall
	{
		/// <summary>
		/// Provides a key with which to distinguish previously generated
		/// adapter classes stored in a hash table.
		/// </summary>
		/// <remarks>
		/// Provides a key with which to distinguish previously generated
		/// adapter classes stored in a hash table.
		/// </remarks>
		internal class JavaAdapterSignature
		{
			internal Type superClass;

			internal Type[] interfaces;

			internal ObjToIntMap names;

			internal JavaAdapterSignature(Type superClass, Type[] interfaces, ObjToIntMap names)
			{
				this.superClass = superClass;
				this.interfaces = interfaces;
				this.names = names;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is JavaAdapter.JavaAdapterSignature))
				{
					return false;
				}
				JavaAdapter.JavaAdapterSignature sig = (JavaAdapter.JavaAdapterSignature)obj;
				if (superClass != sig.superClass)
				{
					return false;
				}
				if (interfaces != sig.interfaces)
				{
					if (interfaces.Length != sig.interfaces.Length)
					{
						return false;
					}
					for (int i = 0; i < interfaces.Length; i++)
					{
						if (interfaces[i] != sig.interfaces[i])
						{
							return false;
						}
					}
				}
				if (names.Size() != sig.names.Size())
				{
					return false;
				}
				ObjToIntMap.Iterator iter = new ObjToIntMap.Iterator(names);
				for (iter.Start(); !iter.Done(); iter.Next())
				{
					string name = (string)iter.GetKey();
					int arity = iter.GetValue();
					if (arity != sig.names.Get(name, arity + 1))
					{
						return false;
					}
				}
				return true;
			}

			public override int GetHashCode()
			{
				return (superClass.GetHashCode() + Arrays.HashCode(interfaces)) ^ names.Size();
			}
		}

		public static void Init(Context cx, Scriptable scope, bool @sealed)
		{
			JavaAdapter obj = new JavaAdapter();
			IdFunctionObject ctor = new IdFunctionObject(obj, FTAG, Id_JavaAdapter, "JavaAdapter", 1, scope);
			ctor.MarkAsConstructor(null);
			if (@sealed)
			{
				ctor.SealObject();
			}
			ctor.ExportAsScopeProperty();
		}

		public object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (f.HasTag(FTAG))
			{
				if (f.MethodId() == Id_JavaAdapter)
				{
					return Js_createAdapter(cx, scope, args);
				}
			}
			throw f.Unknown();
		}

		public static object ConvertResult(object result, Type c)
		{
			if (result == Undefined.instance && (c != ScriptRuntime.ObjectClass && c != ScriptRuntime.StringClass))
			{
				// Avoid an error for an undefined value; return null instead.
				return null;
			}
			return Context.JsToJava(result, c);
		}

		public static Scriptable CreateAdapterWrapper(Scriptable obj, object adapter)
		{
			Scriptable scope = ScriptableObject.GetTopLevelScope(obj);
			NativeJavaObject res = new NativeJavaObject(scope, adapter, null, true);
			res.SetPrototype(obj);
			return res;
		}

		/// <exception cref="System.MissingFieldException"></exception>
		/// <exception cref="System.MemberAccessException"></exception>
		public static object GetAdapterSelf(Type adapterClass, object adapter)
		{
			FieldInfo self = Sharpen.Runtime.GetDeclaredField(adapterClass, "self");
			return self.GetValue(adapter);
		}

		internal static object Js_createAdapter(Context cx, Scriptable scope, object[] args)
		{
			int N = args.Length;
			if (N == 0)
			{
				throw ScriptRuntime.TypeError0("msg.adapter.zero.args");
			}
			// Expected arguments:
			// Any number of NativeJavaClass objects representing the super-class
			// and/or interfaces to implement, followed by one NativeObject providing
			// the implementation, followed by any number of arguments to pass on
			// to the (super-class) constructor.
			int classCount;
			for (classCount = 0; classCount < N - 1; classCount++)
			{
				object arg = args[classCount];
				// We explicitly test for NativeObject here since checking for
				// instanceof ScriptableObject or !(instanceof NativeJavaClass)
				// would fail for a Java class that isn't found in the class path
				// as NativeJavaPackage extends ScriptableObject.
				if (arg is NativeObject)
				{
					break;
				}
				if (!(arg is NativeJavaClass))
				{
					throw ScriptRuntime.TypeError2("msg.not.java.class.arg", classCount.ToString(), ScriptRuntime.ToString(arg));
				}
			}
			Type superClass = null;
			Type[] intfs = new Type[classCount];
			int interfaceCount = 0;
			for (int i = 0; i < classCount; ++i)
			{
				Type c = ((NativeJavaClass)args[i]).GetClassObject();
				if (!c.IsInterface)
				{
					if (superClass != null)
					{
						throw ScriptRuntime.TypeError2("msg.only.one.super", superClass.FullName, c.FullName);
					}
					superClass = c;
				}
				else
				{
					intfs[interfaceCount++] = c;
				}
			}
			if (superClass == null)
			{
				superClass = ScriptRuntime.ObjectClass;
			}
			Type[] interfaces = new Type[interfaceCount];
			System.Array.Copy(intfs, 0, interfaces, 0, interfaceCount);
			// next argument is implementation, must be scriptable
			Scriptable obj = ScriptableObject.EnsureScriptable(args[classCount]);
			Type adapterClass = GetAdapterClass(scope, superClass, interfaces, obj);
			object adapter;
			int argsCount = N - classCount - 1;
			try
			{
				if (argsCount > 0)
				{
					// Arguments contain parameters for super-class constructor.
					// We use the generic Java method lookup logic to find and
					// invoke the right constructor.
					object[] ctorArgs = new object[argsCount + 2];
					ctorArgs[0] = obj;
					ctorArgs[1] = cx.GetFactory();
					System.Array.Copy(args, classCount + 1, ctorArgs, 2, argsCount);
					// TODO: cache class wrapper?
					NativeJavaClass classWrapper = new NativeJavaClass(scope, adapterClass, true);
					NativeJavaMethod ctors = classWrapper.members.ctors;
					int index = ctors.FindCachedFunction(cx, ctorArgs);
					if (index < 0)
					{
						string sig = NativeJavaMethod.ScriptSignature(args);
						throw Context.ReportRuntimeError2("msg.no.java.ctor", adapterClass.FullName, sig);
					}
					// Found the constructor, so try invoking it.
					adapter = NativeJavaClass.ConstructInternal(ctorArgs, ctors.methods[index]);
				}
				else
				{
					Type[] ctorParms = new Type[] { ScriptRuntime.ScriptableClass, ScriptRuntime.ContextFactoryClass };
					object[] ctorArgs = new object[] { obj, cx.GetFactory() };
					adapter = adapterClass.GetConstructor(ctorParms).NewInstance(ctorArgs);
				}
				object self = GetAdapterSelf(adapterClass, adapter);
				// Return unwrapped JavaAdapter if it implements Scriptable
				if (self is Wrapper)
				{
					object unwrapped = ((Wrapper)self).Unwrap();
					if (unwrapped is Scriptable)
					{
						if (unwrapped is ScriptableObject)
						{
							ScriptRuntime.SetObjectProtoAndParent((ScriptableObject)unwrapped, scope);
						}
						return unwrapped;
					}
				}
				return self;
			}
			catch (Exception ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
		}

		// Needed by NativeJavaObject serializer
		/// <exception cref="System.IO.IOException"></exception>
		public static void WriteAdapterObject(object javaObject, ObjectOutputStream @out)
		{
			Type cl = javaObject.GetType();
			@out.WriteObject(cl.BaseType.FullName);
			Type[] interfaces = cl.GetInterfaces();
			string[] interfaceNames = new string[interfaces.Length];
			for (int i = 0; i < interfaces.Length; i++)
			{
				interfaceNames[i] = interfaces[i].FullName;
			}
			@out.WriteObject(interfaceNames);
			try
			{
				object delegee = cl.GetField("delegee").GetValue(javaObject);
				@out.WriteObject(delegee);
				return;
			}
			catch (MemberAccessException)
			{
			}
			catch (MissingFieldException)
			{
			}
			throw new IOException();
		}

		// Needed by NativeJavaObject de-serializer
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		public static object ReadAdapterObject(Scriptable self, ObjectInputStream @in)
		{
			ContextFactory factory;
			Context cx = Context.GetCurrentContext();
			if (cx != null)
			{
				factory = cx.GetFactory();
			}
			else
			{
				factory = null;
			}
			Type superClass = Sharpen.Runtime.GetType((string)@in.ReadObject());
			string[] interfaceNames = (string[])@in.ReadObject();
			Type[] interfaces = new Type[interfaceNames.Length];
			for (int i = 0; i < interfaceNames.Length; i++)
			{
				interfaces[i] = Sharpen.Runtime.GetType(interfaceNames[i]);
			}
			Scriptable delegee = (Scriptable)@in.ReadObject();
			Type adapterClass = GetAdapterClass(self, superClass, interfaces, delegee);
			Type[] ctorParms = new Type[] { ScriptRuntime.ContextFactoryClass, ScriptRuntime.ScriptableClass, ScriptRuntime.ScriptableClass };
			object[] ctorArgs = new object[] { factory, delegee, self };
			try
			{
				return adapterClass.GetConstructor(ctorParms).NewInstance(ctorArgs);
			}
			catch (InstantiationException)
			{
			}
			catch (MemberAccessException)
			{
			}
			catch (TargetInvocationException)
			{
			}
			catch (MissingMethodException)
			{
			}
			throw new TypeLoadException("adapter");
		}

		private static ObjToIntMap GetObjectFunctionNames(Scriptable obj)
		{
			object[] ids = ScriptableObject.GetPropertyIds(obj);
			ObjToIntMap map = new ObjToIntMap(ids.Length);
			for (int i = 0; i != ids.Length; ++i)
			{
				if (!(ids[i] is string))
				{
					continue;
				}
				string id = (string)ids[i];
				object value = ScriptableObject.GetProperty(obj, id);
				if (value is Function)
				{
					Function f = (Function)value;
					int length = ScriptRuntime.ToInt32(ScriptableObject.GetProperty(f, "length"));
					if (length < 0)
					{
						length = 0;
					}
					map.Put(id, length);
				}
			}
			return map;
		}

		private static Type GetAdapterClass(Scriptable scope, Type superClass, Type[] interfaces, Scriptable obj)
		{
			ClassCache cache = ClassCache.Get(scope);
			IDictionary<JavaAdapter.JavaAdapterSignature, Type> generated = cache.GetInterfaceAdapterCacheMap();
			ObjToIntMap names = GetObjectFunctionNames(obj);
			JavaAdapter.JavaAdapterSignature sig;
			sig = new JavaAdapter.JavaAdapterSignature(superClass, interfaces, names);
			Type adapterClass = generated.Get(sig);
			if (adapterClass == null)
			{
				string adapterName = "adapter" + cache.NewClassSerialNumber();
				byte[] code = CreateAdapterCode(names, adapterName, superClass, interfaces, null);
				adapterClass = LoadAdapterClass(adapterName, code);
				if (cache.IsCachingEnabled())
				{
					generated.Put(sig, adapterClass);
				}
			}
			return adapterClass;
		}

		public static byte[] CreateAdapterCode(ObjToIntMap functionNames, string adapterName, Type superClass, Type[] interfaces, string scriptClassName)
		{
			ClassFileWriter cfw = new ClassFileWriter(adapterName, superClass.FullName, "<adapter>");
			cfw.AddField("factory", "Lorg/mozilla/javascript/ContextFactory;", (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_FINAL));
			cfw.AddField("delegee", "Lorg/mozilla/javascript/Scriptable;", (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_FINAL));
			cfw.AddField("self", "Lorg/mozilla/javascript/Scriptable;", (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_FINAL));
			int interfacesCount = interfaces == null ? 0 : interfaces.Length;
			for (int i = 0; i < interfacesCount; i++)
			{
				if (interfaces[i] != null)
				{
					cfw.AddInterface(interfaces[i].FullName);
				}
			}
			string superName = superClass.FullName.Replace('.', '/');
			ConstructorInfo<object>[] ctors = superClass.GetDeclaredConstructors();
			foreach (ConstructorInfo<object> ctor in ctors)
			{
				int mod = ctor.Attributes;
				if (Modifier.IsPublic(mod) || Modifier.IsProtected(mod))
				{
					GenerateCtor(cfw, adapterName, superName, ctor);
				}
			}
			GenerateSerialCtor(cfw, adapterName, superName);
			if (scriptClassName != null)
			{
				GenerateEmptyCtor(cfw, adapterName, superName, scriptClassName);
			}
			ObjToIntMap generatedOverrides = new ObjToIntMap();
			ObjToIntMap generatedMethods = new ObjToIntMap();
			// generate methods to satisfy all specified interfaces.
			for (int i_1 = 0; i_1 < interfacesCount; i_1++)
			{
				MethodInfo[] methods = interfaces[i_1].GetMethods();
				for (int j = 0; j < methods.Length; j++)
				{
					MethodInfo method = methods[j];
					int mods = method.Attributes;
					if (Modifier.IsStatic(mods) || Modifier.IsFinal(mods))
					{
						continue;
					}
					string methodName = method.Name;
					Type[] argTypes = Sharpen.Runtime.GetParameterTypes(method);
					if (!functionNames.Has(methodName))
					{
						try
						{
							superClass.GetMethod(methodName, argTypes);
							// The class we're extending implements this method and
							// the JavaScript object doesn't have an override. See
							// bug 61226.
							continue;
						}
						catch (MissingMethodException)
						{
						}
					}
					// Not implemented by superclass; fall through
					// make sure to generate only one instance of a particular
					// method/signature.
					string methodSignature = GetMethodSignature(method, argTypes);
					string methodKey = methodName + methodSignature;
					if (!generatedOverrides.Has(methodKey))
					{
						GenerateMethod(cfw, adapterName, methodName, argTypes, method.ReturnType, true);
						generatedOverrides.Put(methodKey, 0);
						generatedMethods.Put(methodName, 0);
					}
				}
			}
			// Now, go through the superclass's methods, checking for abstract
			// methods or additional methods to override.
			// generate any additional overrides that the object might contain.
			MethodInfo[] methods_1 = GetOverridableMethods(superClass);
			for (int j_1 = 0; j_1 < methods_1.Length; j_1++)
			{
				MethodInfo method = methods_1[j_1];
				int mods = method.Attributes;
				// if a method is marked abstract, must implement it or the
				// resulting class won't be instantiable. otherwise, if the object
				// has a property of the same name, then an override is intended.
				bool isAbstractMethod = Modifier.IsAbstract(mods);
				string methodName = method.Name;
				if (isAbstractMethod || functionNames.Has(methodName))
				{
					// make sure to generate only one instance of a particular
					// method/signature.
					Type[] argTypes = Sharpen.Runtime.GetParameterTypes(method);
					string methodSignature = GetMethodSignature(method, argTypes);
					string methodKey = methodName + methodSignature;
					if (!generatedOverrides.Has(methodKey))
					{
						GenerateMethod(cfw, adapterName, methodName, argTypes, method.ReturnType, true);
						generatedOverrides.Put(methodKey, 0);
						generatedMethods.Put(methodName, 0);
						// if a method was overridden, generate a "super$method"
						// which lets the delegate call the superclass' version.
						if (!isAbstractMethod)
						{
							GenerateSuper(cfw, adapterName, superName, methodName, methodSignature, argTypes, method.ReturnType);
						}
					}
				}
			}
			// Generate Java methods for remaining properties that are not
			// overrides.
			ObjToIntMap.Iterator iter = new ObjToIntMap.Iterator(functionNames);
			for (iter.Start(); !iter.Done(); iter.Next())
			{
				string functionName = (string)iter.GetKey();
				if (generatedMethods.Has(functionName))
				{
					continue;
				}
				int length = iter.GetValue();
				Type[] parms = new Type[length];
				for (int k = 0; k < length; k++)
				{
					parms[k] = ScriptRuntime.ObjectClass;
				}
				GenerateMethod(cfw, adapterName, functionName, parms, ScriptRuntime.ObjectClass, false);
			}
			return cfw.ToByteArray();
		}

		internal static MethodInfo[] GetOverridableMethods(Type clazz)
		{
			AList<MethodInfo> list = new AList<MethodInfo>();
			HashSet<string> skip = new HashSet<string>();
			// Check superclasses before interfaces so we always choose
			// implemented methods over abstract ones, even if a subclass
			// re-implements an interface already implemented in a superclass
			// (e.g. java.util.ArrayList)
			for (Type c = clazz; c != null; c = c.BaseType)
			{
				AppendOverridableMethods(c, list, skip);
			}
			for (Type c_1 = clazz; c_1 != null; c_1 = c_1.BaseType)
			{
				foreach (Type intf in c_1.GetInterfaces())
				{
					AppendOverridableMethods(intf, list, skip);
				}
			}
			return Sharpen.Collections.ToArray(list, new MethodInfo[list.Count]);
		}

		private static void AppendOverridableMethods(Type c, AList<MethodInfo> list, HashSet<string> skip)
		{
			MethodInfo[] methods = Sharpen.Runtime.GetDeclaredMethods(c);
			for (int i = 0; i < methods.Length; i++)
			{
				string methodKey = methods[i].Name + GetMethodSignature(methods[i], Sharpen.Runtime.GetParameterTypes(methods[i]));
				if (skip.Contains(methodKey))
				{
					continue;
				}
				// skip this method
				int mods = methods[i].Attributes;
				if (Modifier.IsStatic(mods))
				{
					continue;
				}
				if (Modifier.IsFinal(mods))
				{
					// Make sure we don't add a final method to the list
					// of overridable methods.
					skip.AddItem(methodKey);
					continue;
				}
				if (Modifier.IsPublic(mods) || Modifier.IsProtected(mods))
				{
					list.AddItem(methods[i]);
					skip.AddItem(methodKey);
				}
			}
		}

		internal static Type LoadAdapterClass(string className, byte[] classBytes)
		{
			object staticDomain;
			Type domainClass = SecurityController.GetStaticSecurityDomainClass();
			if (domainClass == typeof(CodeSource) || domainClass == typeof(ProtectionDomain))
			{
				// use the calling script's security domain if available
				ProtectionDomain protectionDomain = SecurityUtilities.GetScriptProtectionDomain();
				if (protectionDomain == null)
				{
					protectionDomain = typeof(JavaAdapter).GetProtectionDomain();
				}
				if (domainClass == typeof(CodeSource))
				{
					staticDomain = protectionDomain == null ? null : protectionDomain.GetCodeSource();
				}
				else
				{
					staticDomain = protectionDomain;
				}
			}
			else
			{
				staticDomain = null;
			}
			GeneratedClassLoader loader = SecurityController.CreateLoader(null, staticDomain);
			Type result = loader.DefineClass(className, classBytes);
			loader.LinkClass(result);
			return result;
		}

		public static Function GetFunction(Scriptable obj, string functionName)
		{
			object x = ScriptableObject.GetProperty(obj, functionName);
			if (x == ScriptableConstants.NOT_FOUND)
			{
				// This method used to swallow the exception from calling
				// an undefined method. People have come to depend on this
				// somewhat dubious behavior. It allows people to avoid
				// implementing listener methods that they don't care about,
				// for instance.
				return null;
			}
			if (!(x is Function))
			{
				throw ScriptRuntime.NotFunctionError(x, functionName);
			}
			return (Function)x;
		}

		/// <summary>
		/// Utility method which dynamically binds a Context to the current thread,
		/// if none already exists.
		/// </summary>
		/// <remarks>
		/// Utility method which dynamically binds a Context to the current thread,
		/// if none already exists.
		/// </remarks>
		public static object CallMethod(ContextFactory factory, Scriptable thisObj, Function f, object[] args, long argsToWrap)
		{
			if (f == null)
			{
				// See comments in getFunction
				return null;
			}
			if (factory == null)
			{
				factory = ContextFactory.GetGlobal();
			}
			Scriptable scope = f.GetParentScope();
			if (argsToWrap == 0)
			{
				return Context.Call(factory, f, scope, thisObj, args);
			}
			Context cx = Context.GetCurrentContext();
			if (cx != null)
			{
				return DoCall(cx, scope, thisObj, f, args, argsToWrap);
			}
			else
			{
				return factory.Call(new _ContextAction_583(scope, thisObj, f, args, argsToWrap));
			}
		}

		private sealed class _ContextAction_583 : ContextAction
		{
			public _ContextAction_583(Scriptable scope, Scriptable thisObj, Function f, object[] args, long argsToWrap)
			{
				this.scope = scope;
				this.thisObj = thisObj;
				this.f = f;
				this.args = args;
				this.argsToWrap = argsToWrap;
			}

			public object Run(Context cx)
			{
				return JavaAdapter.DoCall(cx, scope, thisObj, f, args, argsToWrap);
			}

			private readonly Scriptable scope;

			private readonly Scriptable thisObj;

			private readonly Function f;

			private readonly object[] args;

			private readonly long argsToWrap;
		}

		private static object DoCall(Context cx, Scriptable scope, Scriptable thisObj, Function f, object[] args, long argsToWrap)
		{
			// Wrap the rest of objects
			for (int i = 0; i != args.Length; ++i)
			{
				if (0 != (argsToWrap & (1 << i)))
				{
					object arg = args[i];
					if (!(arg is Scriptable))
					{
						args[i] = cx.GetWrapFactory().Wrap(cx, scope, arg, null);
					}
				}
			}
			return f.Call(cx, scope, thisObj, args);
		}

		public static Scriptable RunScript(Script script)
		{
			return (Scriptable)ContextFactory.GetGlobal().Call(new _ContextAction_612(script));
		}

		private sealed class _ContextAction_612 : ContextAction
		{
			public _ContextAction_612(Script script)
			{
				this.script = script;
			}

			public object Run(Context cx)
			{
				ScriptableObject global = ScriptRuntime.GetGlobal(cx);
				script.Exec(cx, global);
				return global;
			}

			private readonly Script script;
		}

		private static void GenerateCtor<_T0>(ClassFileWriter cfw, string adapterName, string superName, ConstructorInfo<_T0> superCtor)
		{
			short locals = 3;
			// this + factory + delegee
			Type[] parameters = superCtor.GetParameterTypes();
			// Note that we swapped arguments in app-facing constructors to avoid
			// conflicting signatures with serial constructor defined below.
			if (parameters.Length == 0)
			{
				cfw.StartMethod("<init>", "(Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/ContextFactory;)V", ClassFileWriter.ACC_PUBLIC);
				// Invoke base class constructor
				cfw.Add(ByteCode.ALOAD_0);
				// this
				cfw.AddInvoke(ByteCode.INVOKESPECIAL, superName, "<init>", "()V");
			}
			else
			{
				StringBuilder sig = new StringBuilder("(Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/ContextFactory;");
				int marker = sig.Length;
				// lets us reuse buffer for super signature
				foreach (Type c in parameters)
				{
					AppendTypeString(sig, c);
				}
				sig.Append(")V");
				cfw.StartMethod("<init>", sig.ToString(), ClassFileWriter.ACC_PUBLIC);
				// Invoke base class constructor
				cfw.Add(ByteCode.ALOAD_0);
				// this
				short paramOffset = 3;
				foreach (Type parameter in parameters)
				{
					paramOffset += GeneratePushParam(cfw, paramOffset, parameter);
				}
				locals = paramOffset;
				sig.Delete(1, marker);
				cfw.AddInvoke(ByteCode.INVOKESPECIAL, superName, "<init>", sig.ToString());
			}
			// Save parameter in instance variable "delegee"
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.Add(ByteCode.ALOAD_1);
			// first arg: Scriptable delegee
			cfw.Add(ByteCode.PUTFIELD, adapterName, "delegee", "Lorg/mozilla/javascript/Scriptable;");
			// Save parameter in instance variable "factory"
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.Add(ByteCode.ALOAD_2);
			// second arg: ContextFactory instance
			cfw.Add(ByteCode.PUTFIELD, adapterName, "factory", "Lorg/mozilla/javascript/ContextFactory;");
			cfw.Add(ByteCode.ALOAD_0);
			// this for the following PUTFIELD for self
			// create a wrapper object to be used as "this" in method calls
			cfw.Add(ByteCode.ALOAD_1);
			// the Scriptable delegee
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/JavaAdapter", "createAdapterWrapper", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.PUTFIELD, adapterName, "self", "Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.RETURN);
			cfw.StopMethod(locals);
		}

		private static void GenerateSerialCtor(ClassFileWriter cfw, string adapterName, string superName)
		{
			cfw.StartMethod("<init>", "(Lorg/mozilla/javascript/ContextFactory;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + ")V", ClassFileWriter.ACC_PUBLIC);
			// Invoke base class constructor
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, superName, "<init>", "()V");
			// Save parameter in instance variable "factory"
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.Add(ByteCode.ALOAD_1);
			// first arg: ContextFactory instance
			cfw.Add(ByteCode.PUTFIELD, adapterName, "factory", "Lorg/mozilla/javascript/ContextFactory;");
			// Save parameter in instance variable "delegee"
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.Add(ByteCode.ALOAD_2);
			// second arg: Scriptable delegee
			cfw.Add(ByteCode.PUTFIELD, adapterName, "delegee", "Lorg/mozilla/javascript/Scriptable;");
			// save self
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.Add(ByteCode.ALOAD_3);
			// third arg: Scriptable self
			cfw.Add(ByteCode.PUTFIELD, adapterName, "self", "Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.RETURN);
			cfw.StopMethod((short)4);
		}

		// 4: this + factory + delegee + self
		private static void GenerateEmptyCtor(ClassFileWriter cfw, string adapterName, string superName, string scriptClassName)
		{
			cfw.StartMethod("<init>", "()V", ClassFileWriter.ACC_PUBLIC);
			// Invoke base class constructor
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, superName, "<init>", "()V");
			// Set factory to null to use current global when necessary
			cfw.Add(ByteCode.ALOAD_0);
			cfw.Add(ByteCode.ACONST_NULL);
			cfw.Add(ByteCode.PUTFIELD, adapterName, "factory", "Lorg/mozilla/javascript/ContextFactory;");
			// Load script class
			cfw.Add(ByteCode.NEW, scriptClassName);
			cfw.Add(ByteCode.DUP);
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, scriptClassName, "<init>", "()V");
			// Run script and save resulting scope
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/JavaAdapter", "runScript", "(Lorg/mozilla/javascript/Script;" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.ASTORE_1);
			// Save the Scriptable in instance variable "delegee"
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.Add(ByteCode.ALOAD_1);
			// the Scriptable
			cfw.Add(ByteCode.PUTFIELD, adapterName, "delegee", "Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.ALOAD_0);
			// this for the following PUTFIELD for self
			// create a wrapper object to be used as "this" in method calls
			cfw.Add(ByteCode.ALOAD_1);
			// the Scriptable
			cfw.Add(ByteCode.ALOAD_0);
			// this
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/JavaAdapter", "createAdapterWrapper", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.PUTFIELD, adapterName, "self", "Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.RETURN);
			cfw.StopMethod((short)2);
		}

		// this + delegee
		/// <summary>Generates code to wrap Java arguments into Object[].</summary>
		/// <remarks>
		/// Generates code to wrap Java arguments into Object[].
		/// Non-primitive Java types are left as-is pending conversion
		/// in the helper method. Leaves the array object on the top of the stack.
		/// </remarks>
		internal static void GeneratePushWrappedArgs(ClassFileWriter cfw, Type[] argTypes, int arrayLength)
		{
			// push arguments
			cfw.AddPush(arrayLength);
			cfw.Add(ByteCode.ANEWARRAY, "java/lang/Object");
			int paramOffset = 1;
			for (int i = 0; i != argTypes.Length; ++i)
			{
				cfw.Add(ByteCode.DUP);
				// duplicate array reference
				cfw.AddPush(i);
				paramOffset += GenerateWrapArg(cfw, paramOffset, argTypes[i]);
				cfw.Add(ByteCode.AASTORE);
			}
		}

		/// <summary>Generates code to wrap Java argument into Object.</summary>
		/// <remarks>
		/// Generates code to wrap Java argument into Object.
		/// Non-primitive Java types are left unconverted pending conversion
		/// in the helper method. Leaves the wrapper object on the top of the stack.
		/// </remarks>
		private static int GenerateWrapArg(ClassFileWriter cfw, int paramOffset, Type argType)
		{
			int size = 1;
			if (!argType.IsPrimitive)
			{
				cfw.Add(ByteCode.ALOAD, paramOffset);
			}
			else
			{
				if (argType == typeof(bool))
				{
					// wrap boolean values with java.lang.Boolean.
					cfw.Add(ByteCode.NEW, "java/lang/Boolean");
					cfw.Add(ByteCode.DUP);
					cfw.Add(ByteCode.ILOAD, paramOffset);
					cfw.AddInvoke(ByteCode.INVOKESPECIAL, "java/lang/Boolean", "<init>", "(Z)V");
				}
				else
				{
					if (argType == typeof(char))
					{
						// Create a string of length 1 using the character parameter.
						cfw.Add(ByteCode.ILOAD, paramOffset);
						cfw.AddInvoke(ByteCode.INVOKESTATIC, "java/lang/String", "valueOf", "(C)Ljava/lang/String;");
					}
					else
					{
						// convert all numeric values to java.lang.Double.
						cfw.Add(ByteCode.NEW, "java/lang/Double");
						cfw.Add(ByteCode.DUP);
						string typeName = argType.FullName;
						switch (typeName[0])
						{
							case 'b':
							case 's':
							case 'i':
							{
								// load an int value, convert to double.
								cfw.Add(ByteCode.ILOAD, paramOffset);
								cfw.Add(ByteCode.I2D);
								break;
							}

							case 'l':
							{
								// load a long, convert to double.
								cfw.Add(ByteCode.LLOAD, paramOffset);
								cfw.Add(ByteCode.L2D);
								size = 2;
								break;
							}

							case 'f':
							{
								// load a float, convert to double.
								cfw.Add(ByteCode.FLOAD, paramOffset);
								cfw.Add(ByteCode.F2D);
								break;
							}

							case 'd':
							{
								cfw.Add(ByteCode.DLOAD, paramOffset);
								size = 2;
								break;
							}
						}
						cfw.AddInvoke(ByteCode.INVOKESPECIAL, "java/lang/Double", "<init>", "(D)V");
					}
				}
			}
			return size;
		}

		/// <summary>Generates code to convert a wrapped value type to a primitive type.</summary>
		/// <remarks>
		/// Generates code to convert a wrapped value type to a primitive type.
		/// Handles unwrapping java.lang.Boolean, and java.lang.Number types.
		/// Generates the appropriate RETURN bytecode.
		/// </remarks>
		internal static void GenerateReturnResult(ClassFileWriter cfw, Type retType, bool callConvertResult)
		{
			// wrap boolean values with java.lang.Boolean, convert all other
			// primitive values to java.lang.Double.
			if (retType == typeof(void))
			{
				cfw.Add(ByteCode.POP);
				cfw.Add(ByteCode.RETURN);
			}
			else
			{
				if (retType == typeof(bool))
				{
					cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/Context", "toBoolean", "(Ljava/lang/Object;)Z");
					cfw.Add(ByteCode.IRETURN);
				}
				else
				{
					if (retType == typeof(char))
					{
						// characters are represented as strings in JavaScript.
						// return the first character.
						// first convert the value to a string if possible.
						cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/Context", "toString", "(Ljava/lang/Object;)Ljava/lang/String;");
						cfw.Add(ByteCode.ICONST_0);
						cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, "java/lang/String", "charAt", "(I)C");
						cfw.Add(ByteCode.IRETURN);
					}
					else
					{
						if (retType.IsPrimitive)
						{
							cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/Context", "toNumber", "(Ljava/lang/Object;)D");
							string typeName = retType.FullName;
							switch (typeName[0])
							{
								case 'b':
								case 's':
								case 'i':
								{
									cfw.Add(ByteCode.D2I);
									cfw.Add(ByteCode.IRETURN);
									break;
								}

								case 'l':
								{
									cfw.Add(ByteCode.D2L);
									cfw.Add(ByteCode.LRETURN);
									break;
								}

								case 'f':
								{
									cfw.Add(ByteCode.D2F);
									cfw.Add(ByteCode.FRETURN);
									break;
								}

								case 'd':
								{
									cfw.Add(ByteCode.DRETURN);
									break;
								}

								default:
								{
									throw new Exception("Unexpected return type " + retType.ToString());
								}
							}
						}
						else
						{
							string retTypeStr = retType.FullName;
							if (callConvertResult)
							{
								cfw.AddLoadConstant(retTypeStr);
								cfw.AddInvoke(ByteCode.INVOKESTATIC, "java/lang/Class", "forName", "(Ljava/lang/String;)Ljava/lang/Class;");
								cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/JavaAdapter", "convertResult", "(Ljava/lang/Object;" + "Ljava/lang/Class;" + ")Ljava/lang/Object;");
							}
							// Now cast to return type
							cfw.Add(ByteCode.CHECKCAST, retTypeStr);
							cfw.Add(ByteCode.ARETURN);
						}
					}
				}
			}
		}

		private static void GenerateMethod(ClassFileWriter cfw, string genName, string methodName, Type[] parms, Type returnType, bool convertResult)
		{
			StringBuilder sb = new StringBuilder();
			int paramsEnd = AppendMethodSignature(parms, returnType, sb);
			string methodSignature = sb.ToString();
			cfw.StartMethod(methodName, methodSignature, ClassFileWriter.ACC_PUBLIC);
			// Prepare stack to call method
			// push factory
			cfw.Add(ByteCode.ALOAD_0);
			cfw.Add(ByteCode.GETFIELD, genName, "factory", "Lorg/mozilla/javascript/ContextFactory;");
			// push self
			cfw.Add(ByteCode.ALOAD_0);
			cfw.Add(ByteCode.GETFIELD, genName, "self", "Lorg/mozilla/javascript/Scriptable;");
			// push function
			cfw.Add(ByteCode.ALOAD_0);
			cfw.Add(ByteCode.GETFIELD, genName, "delegee", "Lorg/mozilla/javascript/Scriptable;");
			cfw.AddPush(methodName);
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/JavaAdapter", "getFunction", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + ")Lorg/mozilla/javascript/Function;");
			// push arguments
			GeneratePushWrappedArgs(cfw, parms, parms.Length);
			// push bits to indicate which parameters should be wrapped
			if (parms.Length > 64)
			{
				// If it will be an issue, then passing a static boolean array
				// can be an option, but for now using simple bitmask
				throw Context.ReportRuntimeError0("JavaAdapter can not subclass methods with more then" + " 64 arguments.");
			}
			long convertionMask = 0;
			for (int i = 0; i != parms.Length; ++i)
			{
				if (!parms[i].IsPrimitive)
				{
					convertionMask |= (1 << i);
				}
			}
			cfw.AddPush(convertionMask);
			// go through utility method, which creates a Context to run the
			// method in.
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/JavaAdapter", "callMethod", "(Lorg/mozilla/javascript/ContextFactory;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Function;" + "[Ljava/lang/Object;" + "J" + ")Ljava/lang/Object;");
			GenerateReturnResult(cfw, returnType, convertResult);
			cfw.StopMethod((short)paramsEnd);
		}

		/// <summary>
		/// Generates code to push typed parameters onto the operand stack
		/// prior to a direct Java method call.
		/// </summary>
		/// <remarks>
		/// Generates code to push typed parameters onto the operand stack
		/// prior to a direct Java method call.
		/// </remarks>
		private static int GeneratePushParam(ClassFileWriter cfw, int paramOffset, Type paramType)
		{
			if (!paramType.IsPrimitive)
			{
				cfw.AddALoad(paramOffset);
				return 1;
			}
			string typeName = paramType.FullName;
			switch (typeName[0])
			{
				case 'z':
				case 'b':
				case 'c':
				case 's':
				case 'i':
				{
					// load an int value, convert to double.
					cfw.AddILoad(paramOffset);
					return 1;
				}

				case 'l':
				{
					// load a long, convert to double.
					cfw.AddLLoad(paramOffset);
					return 2;
				}

				case 'f':
				{
					// load a float, convert to double.
					cfw.AddFLoad(paramOffset);
					return 1;
				}

				case 'd':
				{
					cfw.AddDLoad(paramOffset);
					return 2;
				}
			}
			throw Kit.CodeBug();
		}

		/// <summary>
		/// Generates code to return a Java type, after calling a Java method
		/// that returns the same type.
		/// </summary>
		/// <remarks>
		/// Generates code to return a Java type, after calling a Java method
		/// that returns the same type.
		/// Generates the appropriate RETURN bytecode.
		/// </remarks>
		private static void GeneratePopResult(ClassFileWriter cfw, Type retType)
		{
			if (retType.IsPrimitive)
			{
				string typeName = retType.FullName;
				switch (typeName[0])
				{
					case 'b':
					case 'c':
					case 's':
					case 'i':
					case 'z':
					{
						cfw.Add(ByteCode.IRETURN);
						break;
					}

					case 'l':
					{
						cfw.Add(ByteCode.LRETURN);
						break;
					}

					case 'f':
					{
						cfw.Add(ByteCode.FRETURN);
						break;
					}

					case 'd':
					{
						cfw.Add(ByteCode.DRETURN);
						break;
					}
				}
			}
			else
			{
				cfw.Add(ByteCode.ARETURN);
			}
		}

		/// <summary>
		/// Generates a method called "super$methodName()" which can be called
		/// from JavaScript that is equivalent to calling "super.methodName()"
		/// from Java.
		/// </summary>
		/// <remarks>
		/// Generates a method called "super$methodName()" which can be called
		/// from JavaScript that is equivalent to calling "super.methodName()"
		/// from Java. Eventually, this may be supported directly in JavaScript.
		/// </remarks>
		private static void GenerateSuper(ClassFileWriter cfw, string genName, string superName, string methodName, string methodSignature, Type[] parms, Type returnType)
		{
			cfw.StartMethod("super$" + methodName, methodSignature, ClassFileWriter.ACC_PUBLIC);
			// push "this"
			cfw.Add(ByteCode.ALOAD, 0);
			// push the rest of the parameters.
			int paramOffset = 1;
			foreach (Type parm in parms)
			{
				paramOffset += GeneratePushParam(cfw, paramOffset, parm);
			}
			// call the superclass implementation of the method.
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, superName, methodName, methodSignature);
			// now, handle the return type appropriately.
			Type retType = returnType;
			if (!retType.Equals(typeof(void)))
			{
				GeneratePopResult(cfw, retType);
			}
			else
			{
				cfw.Add(ByteCode.RETURN);
			}
			cfw.StopMethod((short)(paramOffset + 1));
		}

		/// <summary>Returns a fully qualified method name concatenated with its signature.</summary>
		/// <remarks>Returns a fully qualified method name concatenated with its signature.</remarks>
		private static string GetMethodSignature(MethodInfo method, Type[] argTypes)
		{
			StringBuilder sb = new StringBuilder();
			AppendMethodSignature(argTypes, method.ReturnType, sb);
			return sb.ToString();
		}

		internal static int AppendMethodSignature(Type[] argTypes, Type returnType, StringBuilder sb)
		{
			sb.Append('(');
			int firstLocal = 1 + argTypes.Length;
			// includes this.
			foreach (Type type in argTypes)
			{
				AppendTypeString(sb, type);
				if (type == typeof(long) || type == typeof(double))
				{
					// adjust for double slot
					++firstLocal;
				}
			}
			sb.Append(')');
			AppendTypeString(sb, returnType);
			return firstLocal;
		}

		private static StringBuilder AppendTypeString(StringBuilder sb, Type type)
		{
			while (type.IsArray)
			{
				sb.Append('[');
				type = type.GetElementType();
			}
			if (type.IsPrimitive)
			{
				char typeLetter;
				if (type == typeof(bool))
				{
					typeLetter = 'Z';
				}
				else
				{
					if (type == typeof(long))
					{
						typeLetter = 'J';
					}
					else
					{
						string typeName = type.FullName;
						typeLetter = System.Char.ToUpper(typeName[0]);
					}
				}
				sb.Append(typeLetter);
			}
			else
			{
				sb.Append('L');
				sb.Append(type.FullName.Replace('.', '/'));
				sb.Append(';');
			}
			return sb;
		}

		internal static int[] GetArgsToConvert(Type[] argTypes)
		{
			int count = 0;
			for (int i = 0; i != argTypes.Length; ++i)
			{
				if (!argTypes[i].IsPrimitive)
				{
					++count;
				}
			}
			if (count == 0)
			{
				return null;
			}
			int[] array = new int[count];
			count = 0;
			for (int i_1 = 0; i_1 != argTypes.Length; ++i_1)
			{
				if (!argTypes[i_1].IsPrimitive)
				{
					array[count++] = i_1;
				}
			}
			return array;
		}

		private static readonly object FTAG = "JavaAdapter";

		private const int Id_JavaAdapter = 1;
	}
}
