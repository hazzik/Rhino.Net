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
using System.Reflection.Emit;
using System.Text;
using Rhino.Annotations;
using Rhino.Optimizer;
using Sharpen;
using Arrays = Rhino.Utils.Arrays;

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
			private Type superClass;

			private Type[] interfaces;

			private ObjToIntMap names;

			internal JavaAdapterSignature(Type superClass, Type[] interfaces, ObjToIntMap names)
			{
				this.superClass = superClass;
				this.interfaces = interfaces;
				this.names = names;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is JavaAdapterSignature))
				{
					return false;
				}
				JavaAdapterSignature sig = (JavaAdapterSignature)obj;
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

		[UsedImplicitly]
		public static object ConvertResult(object result, Type c)
		{
			if (result == Undefined.instance && (c != ScriptRuntime.ObjectClass && c != ScriptRuntime.StringClass))
			{
				// Avoid an error for an undefined value; return null instead.
				return null;
			}
			return Context.JsToJava(result, c);
		}

		[UsedImplicitly]
		public static Scriptable CreateAdapterWrapper(Scriptable obj, object adapter)
		{
			Scriptable scope = ScriptableObject.GetTopLevelScope(obj);
			NativeJavaObject res = new NativeJavaObject(scope, adapter, null, true);
			res.Prototype = obj;
			return res;
		}

		/// <exception cref="System.MissingFieldException"></exception>
		/// <exception cref="System.MemberAccessException"></exception>
		public static object GetAdapterSelf(Type adapterClass, object adapter)
		{
			FieldInfo self = adapterClass.GetDeclaredField("self");
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
			Array.Copy(intfs, 0, interfaces, 0, interfaceCount);
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
					Array.Copy(args, classCount + 1, ctorArgs, 2, argsCount);
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
				var wrapper = self as Wrapper;
				if (wrapper != null)
				{
					object unwrapped = wrapper.Unwrap();
					if (unwrapped is Scriptable)
					{
						var scriptableObject = unwrapped as ScriptableObject;
						if (scriptableObject != null)
						{
							ScriptRuntime.SetObjectProtoAndParent(scriptableObject, scope);
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
			catch (MissingFieldException)
			{
			}
			catch (MemberAccessException)
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
			Type superClass = Runtime.GetType((string)@in.ReadObject());
			string[] interfaceNames = (string[])@in.ReadObject();
			Type[] interfaces = new Type[interfaceNames.Length];
			for (int i = 0; i < interfaceNames.Length; i++)
			{
				interfaces[i] = Runtime.GetType(interfaceNames[i]);
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
			catch (MissingMethodException)
			{
			}
			catch (MemberAccessException)
			{
			}
			catch (TargetInvocationException)
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
				var value = ScriptableObject.GetProperty(obj, id) as Function;
				if (value != null)
				{
					int length = ScriptRuntime.ToInt32(ScriptableObject.GetProperty(value, "length"));
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
			IDictionary<JavaAdapterSignature, Type> generated = cache.GetInterfaceAdapterCacheMap();
			ObjToIntMap names = GetObjectFunctionNames(obj);
			var sig = new JavaAdapterSignature(superClass, interfaces, names);
			Type adapterClass = generated.Get(sig);
			if (adapterClass == null)
			{
				string adapterName = "adapter" + cache.NewClassSerialNumber();
				adapterClass = CreateAdapterCode(names, adapterName, superClass, interfaces, null);
				if (cache.IsCachingEnabled())
				{
					generated[sig] = adapterClass;
				}
			}
			return adapterClass;
		}

		public static Type CreateAdapterCode(ObjToIntMap functionNames, string name, Type baseType, Type[] interfaces, Type scriptClassName)
		{
			var module = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("TempAssembly" + DateTime.Now.Millisecond), AssemblyBuilderAccess.Run)
				.DefineDynamicModule("TempModule" + DateTime.Now.Millisecond);

			return CreateAdapterCode(functionNames, name, baseType, interfaces, scriptClassName, module);
		}

		public static Type CreateAdapterCode(ObjToIntMap functionNames, string name, Type baseType, Type[] interfaces, Type scriptClassName, ModuleBuilder module)
		{
			interfaces = interfaces ?? Type.EmptyTypes;

			CachingTypeBuilder type = new CachingTypeBuilder(module.DefineType(name, TypeAttributes.Public, baseType));

			var factory = type.DefineField("factory", typeof (ContextFactory), FieldAttributes.Public | FieldAttributes.InitOnly);
			var delegee = type.DefineField("delegee", typeof(Scriptable), FieldAttributes.Public | FieldAttributes.InitOnly);
			var self = type.DefineField("self", typeof (Scriptable), FieldAttributes.Public | FieldAttributes.InitOnly);

			foreach (var @interface in interfaces)
			{
				if (@interface != null)
				{
					type.AddInterfaceImplementation(@interface);
				}
			}

			foreach (var ctor in baseType.GetDeclaredConstructors())
			{
				if (ctor.IsPublic || ctor.IsFamily)
				{
					GenerateCtor(type, ctor, factory, delegee, self);
				}
			}

			GenerateSerialCtor(type, baseType, factory, delegee, self);

			if (scriptClassName != null)
			{
				GenerateEmptyCtor(type, baseType, scriptClassName, delegee, self);
			}
			ObjToIntMap generatedOverrides = new ObjToIntMap();
			ObjToIntMap generatedMethods = new ObjToIntMap();

			// generate methods to satisfy all specified interfaces.
			foreach (Type @interface in interfaces)
			{
				MethodInfo[] methods = @interface.GetMethods();
				foreach (MethodInfo method in methods)
				{
					if (method.IsStatic || method.IsFinal)
					{
						continue;
					}
					string methodName = method.Name;
					Type[] argTypes = method.GetParameterTypes();
					if (!functionNames.Has(methodName))
					{
						try
						{
							baseType.GetMethod(methodName, argTypes);
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
						GenerateMethod(type, methodName, argTypes, method.ReturnType, true, factory, delegee, self);
						generatedOverrides.Put(methodKey, 0);
						generatedMethods.Put(methodName, 0);
					}
				}
			}

			// Now, go through the superclass's methods, checking for abstract
			// methods or additional methods to override.
			// generate any additional overrides that the object might contain.
			foreach (MethodInfo method in GetOverridableMethods(baseType))
			{
				// if a method is marked abstract, must implement it or the
				// resulting class won't be instantiable. otherwise, if the object
				// has a property of the same name, then an override is intended.
				bool isAbstractMethod = method.IsAbstract;
				string methodName = method.Name;
				if (isAbstractMethod || functionNames.Has(methodName))
				{
					// make sure to generate only one instance of a particular
					// method/signature.
					Type[] argTypes = method.GetParameterTypes();
					string methodSignature = GetMethodSignature(method, argTypes);
					string methodKey = methodName + methodSignature;
					if (!generatedOverrides.Has(methodKey))
					{
						GenerateMethod(type, methodName, argTypes, method.ReturnType, true, factory, delegee, self);
						generatedOverrides.Put(methodKey, 0);
						generatedMethods.Put(methodName, 0);
						// if a method was overridden, generate a "super$method"
						// which lets the delegate call the superclass' version.
						if (!isAbstractMethod)
						{
							GenerateSuper(type, methodName, argTypes, method);
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
				GenerateMethod(type, functionName, parms, ScriptRuntime.ObjectClass, false, factory, delegee, self);
			}
			return type.CreateType();
		}

		internal static IEnumerable<MethodInfo> GetOverridableMethods(Type type)
		{
			var list = new List<MethodInfo>();
			var skip = new HashSet<string>();
			// Check superclasses before interfaces so we always choose
			// implemented methods over abstract ones, even if a subclass
			// re-implements an interface already implemented in a superclass
			// (e.g. java.util.ArrayList)
			for (Type t = type; t != null; t = t.BaseType)
			{
				AppendOverridableMethods(t, list, skip);
			}
			for (var t = type; t != null; t = t.BaseType)
			{
				foreach (Type @interface in t.GetInterfaces())
				{
					AppendOverridableMethods(@interface, list, skip);
				}
			}
			return list.ToArray();
		}

		private static void AppendOverridableMethods(Type c, IList<MethodInfo> list, ICollection<string> skip)
		{
			foreach (var method in c.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				string methodKey = method.Name + GetMethodSignature(method, method.GetParameterTypes());
				if (skip.Contains(methodKey))
				{
					continue;
				}
				// skip this method
				if (!method.IsStatic)
				{
					if (method.IsVirtual)
					{
						if (method.IsPublic || method.IsFamily)
						{
							list.Add(method);
							skip.Add(methodKey);
						}
					}
					else
					{
						// Make sure we don't add a final method to the list
						// of overridable methods.
						skip.Add(methodKey);
					}
				}
			}
		}

		[UsedImplicitly]
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
		[UsedImplicitly]
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
			Scriptable scope = f.ParentScope;
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
				return factory.Call(context => DoCall(context, scope, thisObj, f, args, argsToWrap));
			}
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

		[UsedImplicitly]
		public static Scriptable RunScript(Script script)
		{
			return (Scriptable) ContextFactory.GetGlobal().Call(cx =>
			{
				ScriptableObject global = ScriptRuntime.GetGlobal(cx);
				script.Exec(cx, global);
				return global;
			});
		}

		private static void GenerateCtor(CachingTypeBuilder tb, ConstructorInfo baseTypeConstructor, FieldInfo factory, FieldInfo delegee, FieldInfo self)
		{
			Type[] baseParameterTypes = baseTypeConstructor.GetParameterTypes();
			// Note that we swapped arguments in app-facing constructors to avoid
			// conflicting signatures with serial constructor defined below.

			var parameterTypes = new Type[2 + baseParameterTypes.Length];
			parameterTypes[0] = typeof (Scriptable);
			parameterTypes[1] = typeof (ContextFactory);
			Array.Copy(baseParameterTypes, 0, parameterTypes, 2, baseParameterTypes.Length);

			var constructor = tb.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, parameterTypes);
			var il = constructor.GetILGenerator();
			
			// Invoke base class constructor
			il.Emit(OpCodes.Ldarg_0); // this
			for (int index = 0; index < baseParameterTypes.Length; index++)
			{
				il.EmitLoadArgument(index + 3);
			}
			il.Emit(OpCodes.Call, baseTypeConstructor);
			
			// Save parameter in instance variable "delegee"
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // first arg: Scriptable delegee
			il.Emit(OpCodes.Stfld, delegee);

			// Save parameter in instance variable "factory"
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_2); // second arg: ContextFactory instance
			il.Emit(OpCodes.Stfld, factory);

			il.Emit(OpCodes.Ldarg_0); // this for the following PUTFIELD for self

			// create a wrapper object to be used as "this" in method calls
			il.Emit(OpCodes.Ldarg_1); // the Scriptable delegee
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Call, typeof (JavaAdapter).GetMethod("CreateAdapterWrapper", new[] { typeof (Scriptable), typeof (Object) }));
			il.Emit(OpCodes.Stfld, self);

			il.Emit(OpCodes.Ret);
		}

		private static void GenerateSerialCtor(CachingTypeBuilder tb, Type baseType, FieldInfo factory, FieldInfo delegee, FieldInfo self)
		{
			/*  public XXX (ContextFactory factory, Scriptable delegee, Scriptable self) : base ()
			 *  {
			 *      this.factory = factory;
			 *      this.delegee = delegee;
			 *      this.self = self;
			 *  }
			 */
			var constructor = tb.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, new[] { typeof (ContextFactory), typeof (Scriptable), typeof (Scriptable) });
			var il = constructor.GetILGenerator();

			// Invoke base class constructor
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));

			// Save parameter in instance variable "factory"
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // first arg: ContextFactory instance
			il.Emit(OpCodes.Stfld, factory);

			// Save parameter in instance variable "delegee"
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_2); // second arg: Scriptable instance
			il.Emit(OpCodes.Stfld, delegee);

			// save self
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_3); // third arg: Scriptable instance
			il.Emit(OpCodes.Stfld, self);

			il.Emit(OpCodes.Ret);
		}

		private static void GenerateEmptyCtor(CachingTypeBuilder type, Type baseType, Type scriptType, FieldInfo delegee, FieldInfo self)
		{
			/*  public XXX () : base() 
			 *  {
			 *      Scriptable scriptable = JavaAdapter.RunScript(new Script());
			 *      this.delegee = scriptable;
			 *      this.self = JavaAdapter.CreateAdapterWrapper(scriptable, this);
			 *  }
			 */
			var constructor = type.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, Type.EmptyTypes);
			var il = constructor.GetILGenerator();

			// Invoke base class constructor
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Call, baseType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));

			// Load script class
			il.Emit(OpCodes.Newobj, scriptType.GetConstructor(Type.EmptyTypes));

			// Run script and save resulting scope
			il.Emit(OpCodes.Call, typeof (JavaAdapter).GetMethod("RunScript", new[] { typeof (Script) }));
			var scriptable = il.DeclareLocal(typeof (Scriptable));
			il.Emit(OpCodes.Stloc, scriptable);

			// Save the Scriptable in instance variable "delegee"
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldloc, scriptable); // Scriptable
			il.Emit(OpCodes.Stfld, delegee);

			il.Emit(OpCodes.Ldarg_0); // this for the following Stfld for self

			// create a wrapper object to be used as "this" in method calls
			il.Emit(OpCodes.Ldloc, scriptable); // the Scriptable
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Call, typeof (JavaAdapter).GetMethod("CreateAdapterWrapper", new[] { typeof (Scriptable), typeof (Object) }));
			il.Emit(OpCodes.Stfld, self);

			il.Emit(OpCodes.Ret);
		}

		// this + delegee
		/// <summary>Generates code to wrap Java arguments into Object[].</summary>
		/// <remarks>
		/// Generates code to wrap Java arguments into Object[].
		/// Non-primitive Java types are left as-is pending conversion
		/// in the helper method. Leaves the array object on the top of the stack.
		/// </remarks>
		private static void GeneratePushWrappedArgs(ILGenerator il, Type[] argumentTypes, int arrayLength)
		{
			// push arguments
			il.EmitLoadConstant(arrayLength);
			il.Emit(OpCodes.Newarr, typeof (object));
			for (int i = 0; i != argumentTypes.Length; ++i)
			{
				il.Emit(OpCodes.Dup); // duplicate array reference
				il.EmitLoadConstant(i);
				il.EmitLoadArgument(i + 1);
				if (argumentTypes [i].IsValueType)
				{
					il.Emit(OpCodes.Box, argumentTypes [i]);
				}
				il.Emit(OpCodes.Stelem_Ref);
			}
		}

		/// <summary>Generates code to convert a wrapped value type to a primitive type.</summary>
		/// <remarks>
		/// Generates code to convert a wrapped value type to a primitive type.
		/// Handles unwrapping java.lang.Boolean, and java.lang.Number types.
		/// Generates the appropriate RETURN bytecode.
		/// </remarks>
		private static void GenerateReturnResult(ILGenerator il, Type type, bool callConvertResult)
		{
			// wrap boolean values with java.lang.Boolean, convert all other
			// primitive values to java.lang.Double.
			if (type == typeof(void))
			{
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);
				return;
			}
			if (type == typeof(bool))
			{
				il.Emit(OpCodes.Call, typeof (Context).GetMethod("ToBoolean", new[] { typeof (object) }));
				il.Emit(OpCodes.Ret);
				return;
			}
			if (type == typeof(char))
			{
				// characters are represented as strings in JavaScript.
				// return the first character.
				// first convert the value to a string if possible.
				il.Emit(OpCodes.Call, typeof (Context).GetMethod("ToString", new[] { typeof (object) }));
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Callvirt, typeof (String).GetMethod("get_Chars", new[] { typeof (int) }));
				il.Emit(OpCodes.Ret);
				return;
			}
			if (type.IsPrimitive)
			{
				il.Emit(OpCodes.Call, typeof (Context).GetMethod("ToNumber", new[] { typeof (object) }));
				switch (type.FullName)
				{
					case "System.Byte":
					case "System.Int16":
					case "System.Int32":
					{
						il.Emit(OpCodes.Conv_I4);
						il.Emit(OpCodes.Ret);
						return;
					}

					case "System.Int64":
					{
						il.Emit(OpCodes.Conv_I8);
						il.Emit(OpCodes.Ret);
						return;
					}

					case "System.Single":
					{
						il.Emit(OpCodes.Conv_R4);
						il.Emit(OpCodes.Ret);
						return;
					}

					case "System.Double":
					{
						il.Emit(OpCodes.Ret);
						return;
					}

					default:
					{
						throw new Exception("Unexpected return type " + type);
					}
				}
			}
			if (callConvertResult)
			{
				il.Emit(OpCodes.Ldtoken, type);
				il.Emit(OpCodes.Call, typeof (JavaAdapter).GetMethod("ConvertResult", new[] { typeof (object), typeof (Type) }));
			}
			// Now cast to return type
			il.Emit(OpCodes.Castclass, type);
			il.Emit(OpCodes.Ret);
		}

		private static void GenerateMethod(CachingTypeBuilder type, string methodName, Type[] parameterTypes, Type returnType, bool convertResult, FieldInfo factory, FieldInfo delegee, FieldInfo self)
		{
			/*  public TRet Method(T1, ...) 
			 *  {
			 *      Function function = JavaAdapter.GetFunction(this.delegee, "Method");
			 *      object result = JavaAdapter.CallMethod(this.factory, this.self, function, new [] { T1, ... }, mask);
			 *      return (TRet)result;
			 *  }
			 */
			// push bits to indicate which parameters should be wrapped
			if (parameterTypes.Length > 64)
			{
				// If it will be an issue, then passing a static boolean array
				// can be an option, but for now using simple bitmask
				throw Context.ReportRuntimeError0("JavaAdapter can not subclass methods with more then" + " 64 arguments.");
			}

			var method = type.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual, returnType, parameterTypes);
			var il = method.GetILGenerator();

			// Prepare stack to call method

			// push factory
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldfld, factory);

			// push self
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldfld, self);

			// push function
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldfld, delegee);

			il.EmitLoadConstant(methodName);

			il.Emit(OpCodes.Call, typeof (JavaAdapter).GetMethod("GetFunction", new[] {typeof (Scriptable), typeof (string)}));

			// push arguments
			GeneratePushWrappedArgs(il, parameterTypes, parameterTypes.Length);

			long convertionMask = 0;
			for (int i = 0; i != parameterTypes.Length; ++i)
			{
				if (!parameterTypes [i].IsPrimitive)
				{
					convertionMask |= (1 << i);
				}
			}

			il.EmitLoadConstant(convertionMask);

			// go through utility method, which creates a Context to run the method in.
			il.Emit(OpCodes.Call, typeof (JavaAdapter).GetMethod("CallMethod", new[] { typeof (ContextFactory), typeof (Scriptable), typeof (Function), typeof (object[]), typeof (long) }));
			
			GenerateReturnResult(il, returnType, convertResult);
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
		private static void GenerateSuper(CachingTypeBuilder type, string methodName, Type[] argTypes, MethodInfo baseMethod)
		{
			var method1 = type.DefineMethod("super$" + methodName, MethodAttributes.Public, baseMethod.ReturnType, argTypes);
			var il = method1.GetILGenerator();

			il.Emit(OpCodes.Ldloc, 0); // this
			// push the rest of the parameters.
			for (int index = 0; index < argTypes.Length; index++)
			{
				il.EmitLoadArgument(index + 1);
			}
			il.Emit(OpCodes.Call, method1);
			il.Emit(OpCodes.Ret);
		}

		/// <summary>Returns a fully qualified method name concatenated with its signature.</summary>
		/// <remarks>Returns a fully qualified method name concatenated with its signature.</remarks>
		private static string GetMethodSignature(MethodInfo method, Type[] argTypes)
		{
			var sb = new StringBuilder();
			AppendMethodSignature(argTypes, method.ReturnType, sb);
			return sb.ToString();
		}

		private static void AppendMethodSignature(ICollection<Type> argTypes, Type returnType, StringBuilder sb)
		{
			sb.Append('(');
			// includes this.
			foreach (Type type in argTypes)
			{
				AppendTypeString(sb, type);
			}
			sb.Append(')');
			AppendTypeString(sb, returnType);
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
						typeLetter = Char.ToUpper(typeName[0]);
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

		private static readonly object FTAG = "JavaAdapter";

		private const int Id_JavaAdapter = 1;
	}
}
