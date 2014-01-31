#if COMPILATION
/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using Org.Mozilla.Classfile;
using Rhino.Ast;
using Sharpen;
using Label = System.Reflection.Emit.Label;

namespace Rhino.Optimizer
{
	/// <summary>This class generates code for a given IR tree.</summary>
	/// <remarks>This class generates code for a given IR tree.</remarks>
	/// <author>Norris Boyd</author>
	/// <author>Roger Lawrence</author>
	public class Codegen : Evaluator
	{
		public void CaptureStackInfo(RhinoException ex)
		{
			throw new NotSupportedException();
		}

		public string GetSourcePositionFromStack(Context cx, int[] linep)
		{
			throw new NotSupportedException();
		}

		public string GetPatchedStack(RhinoException ex, string nativeStackTrace)
		{
			throw new NotSupportedException();
		}

		public IList<string> GetScriptStack(RhinoException ex)
		{
			throw new NotSupportedException();
		}

		public void SetEvalScriptFlag(Script script)
		{
			throw new NotSupportedException();
		}

		private Tuple<string, Type> Compile(CompilerEnvirons compilerEnv, ScriptNode tree, string encodedSource, bool returnFunction)
		{
			int serial;
			lock (globalLock)
			{
				serial = ++globalSerialClassCounter;
			}
			var baseName = "c";
			if (tree.GetSourceName().Length > 0)
			{
				baseName = tree.GetSourceName().ReplaceAll("\\W", "_");
				if (!CharEx.IsJavaIdentifierStart(baseName[0]))
				{
					baseName = "_" + baseName;
				}
			}
			var mainClassName = "Rhino.Net.gen." + baseName + "_" + serial;
			var mainClassBytes = CompileToClassFile(compilerEnv, mainClassName, tree, encodedSource, returnFunction);
			
			return Tuple.Create(mainClassName, mainClassBytes);
		}

		public Script CreateScriptObject(CompilerEnvirons compilerEnv, ScriptNode tree, object staticSecurityDomain, Action<object> debug)
		{
			var bytecode = Compile(compilerEnv, tree, tree.GetEncodedSource(), false);
			debug(bytecode);
			//var cl = DefineClass(staticSecurityDomain, bytecode.Item1, bytecode.Item2);
			var cl = bytecode.Item2;
			Script script;
			try
			{
				script = (Script)Activator.CreateInstance(cl);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to instantiate compiled class:" + ex);
			}
			return script;
		}

		public Function CreateFunctionObject(CompilerEnvirons compilerEnv, ScriptNode tree, Context cx, Scriptable scope, object staticSecurityDomain, Action<object> debug)
		{
			var bytecode = Compile(compilerEnv, tree, tree.GetEncodedSource(), true);
			debug(bytecode);

			//var cl = DefineClass(staticSecurityDomain, bytecode.Item1, bytecode.Item2);
			var cl = bytecode.Item2;
			try
			{
				return (NativeFunction) Activator.CreateInstance(cl, scope, cx, 0);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to instantiate compiled class:" + ex);
			}
		}

		class SecurityController
		{
			public static GeneratedClassLoader CreateLoader(ClassLoader rhinoLoader, object staticSecurityDomain)
			{
				throw new NotImplementedException();
				//return new GeneratedClassLoader();
			}
		}

		private Type DefineClass(object staticSecurityDomain, string className, byte[] classBytes)
		{
			// The generated classes in this case refer only to Rhino classes
			// which must be accessible through this class loader
			var rhinoLoader = GetType().GetClassLoader();
			GeneratedClassLoader loader = SecurityController.CreateLoader(rhinoLoader, staticSecurityDomain);
			try
			{
				var cl = loader.DefineClass(className, classBytes);
				loader.LinkClass(cl);
				return cl;
			}
			catch (SecurityException x)
			{
				throw new Exception("Malformed optimizer package " + x);
			}
			catch (ArgumentException x)
			{
				throw new Exception("Malformed optimizer package " + x);
			}
		}

		public Type CompileToClassFile(CompilerEnvirons compilerEnv, string mainClassName, ScriptNode scriptOrFn, string encodedSource, bool returnFunction)
		{
			this.compilerEnv = compilerEnv;
			Transform(scriptOrFn);
			if (returnFunction)
			{
				scriptOrFn = scriptOrFn.GetFunctionNode(0);
			}
			InitScriptNodesData(scriptOrFn);
			mainClass = new CachingTypeBuilder(module.DefineType(mainClassName, TypeAttributes.Public, SUPER_CLASS));
			try
			{
				return GenerateCode(encodedSource);
			}
			catch (ClassFileWriter.ClassFileFormatException e)
			{
				throw ReportClassFileFormatException(scriptOrFn, e.Message);
			}
		}

		private static Exception ReportClassFileFormatException(ScriptNode scriptOrFn, string message)
		{
			var msg = scriptOrFn is FunctionNode ? ScriptRuntime.GetMessage2("msg.while.compiling.fn", ((FunctionNode)scriptOrFn).GetFunctionName(), message) : ScriptRuntime.GetMessage1("msg.while.compiling.script", message);
			return Context.ReportRuntimeError(msg, scriptOrFn.GetSourceName(), scriptOrFn.GetLineno(), null, 0);
		}

		private void Transform(ScriptNode tree)
		{
			InitOptFunctions_r(tree);
			var optLevel = compilerEnv.GetOptimizationLevel();
			IDictionary<string, OptFunctionNode> possibleDirectCalls = null;
			if (optLevel > 0)
			{
				if (tree.GetType() == Token.SCRIPT)
				{
					var functionCount = tree.GetFunctionCount();
					for (var i = 0; i != functionCount; ++i)
					{
						var ofn = OptFunctionNode.Get(tree, i);
						if (ofn.fnode.GetFunctionType() == FunctionNode.FUNCTION_STATEMENT)
						{
							var name = ofn.fnode.GetName();
							if (name.Length != 0)
							{
								if (possibleDirectCalls == null)
								{
									possibleDirectCalls = new Dictionary<string, OptFunctionNode>();
								}
								possibleDirectCalls[name] = ofn;
							}
						}
					}
				}
			}
			if (possibleDirectCalls != null)
			{
				directCallTargets = new ObjArray();
			}
			var ot = new OptTransformer(possibleDirectCalls, directCallTargets);
			ot.Transform(tree);
			if (optLevel > 0)
			{
				(new Optimizer()).Optimize(tree);
			}
		}

		private static void InitOptFunctions_r(ScriptNode scriptOrFn)
		{
			for (int i = 0, n = scriptOrFn.GetFunctionCount(); i != n; ++i)
			{
				var fn = scriptOrFn.GetFunctionNode(i);
				new OptFunctionNode(fn);
				InitOptFunctions_r(fn);
			}
		}

		private void InitScriptNodesData(ScriptNode scriptOrFn)
		{
			var x = new ObjArray();
			CollectScriptNodes_r(scriptOrFn, x);
			var count = x.Size();
			scriptOrFnNodes = new ScriptNode[count];
			x.ToArray(scriptOrFnNodes);
			scriptOrFnIndexes = new ObjToIntMap(count);
			for (var i = 0; i != count; ++i)
			{
				scriptOrFnIndexes.Put(scriptOrFnNodes[i], i);
			}
		}

		private static void CollectScriptNodes_r(ScriptNode n, ObjArray x)
		{
			x.Add(n);
			var nestedCount = n.GetFunctionCount();
			for (var i = 0; i != nestedCount; ++i)
			{
				CollectScriptNodes_r(n.GetFunctionNode(i), x);
			}
		}

		public Codegen()
		{
			var name = "TempAssembly" + DateTime.UtcNow.Ticks;
			dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.RunAndSave);
			module = dynamicAssembly.DefineDynamicModule(name + ".mod", name + ".dll", true);
		}

		private readonly ModuleBuilder module;

		public ModuleBuilder ModuleBuilder
		{
			get { return module; }
		}

		private Type GenerateCode(string encodedSource)
		{
			var hasScript = (scriptOrFnNodes[0].GetType() == Token.SCRIPT);
			var hasFunctions = (scriptOrFnNodes.Length > 1 || !hasScript);
			string sourceFile = null;
			if (compilerEnv.IsGenerateDebugInfo())
			{
				sourceFile = scriptOrFnNodes[0].GetSourceName();
			}
			//TODO: set source file info.

			var idField = mainClass.DefineField(ID_FIELD_NAME, typeof (int), FieldAttributes.Private);
			GenerateNativeFunctionOverrides(mainClass, idField, encodedSource);

			var regExpInit = EmitRegExpInit(mainClass);
			for (int i = 0, count = scriptOrFnNodes.Length; i < count; i++)
			{
				var n = scriptOrFnNodes[i];
				if (n.GetType() == Token.FUNCTION)
				{
					var ofn = OptFunctionNode.Get(n);
					functionInits [ofn] = GenerateFunctionInit(mainClass, regExpInit, ofn);
					if (ofn.IsTargetOfDirectCall())
					{
						EmitDirectConstructor(mainClass, ofn);
					}
				}
			}
			ConstructorInfo constructor = null;
			if (hasFunctions)
			{
				constructor = GenerateFunctionConstructor(mainClass, idField);
			}
			for (int i = 0, count = scriptOrFnNodes.Length; i < count; i++)
			{
				var n = scriptOrFnNodes[i];
				var bodygen = BodyCodegen.CreateBodyCodegen(this, n, i, constructor, regExpInit, mainClass, compilerEnv, IsGenerator(n));
				try
				{
					bodygen.GenerateBodyCode();
				}
				catch (ClassFileWriter.ClassFileFormatException e)
				{
					throw ReportClassFileFormatException(n, e.Message);
				}
			}
			GenerateResumeGenerator(mainClass, idField);
			var callMethod = GenerateCallMethod(mainClass, idField);
			if (hasScript)
			{
				mainClass.AddInterfaceImplementation(typeof(Script));
				var scriptConstructor = GenerateScriptCtor(mainClass, SUPER_CLASS.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
				GenerateMain(mainClass, scriptConstructor);
				GenerateExecute(mainClass, callMethod);
			}
			EmitConstantDudeInitializers(mainClass);
			var type = mainClass.CreateType();
			//Save();
			return type;
		}

		public void Save()
		{
			dynamicAssembly.Save(dynamicAssembly.GetName().Name + ".dll");
		}

		private Dictionary<OptFunctionNode, MethodInfo> functionInits = new Dictionary<OptFunctionNode, MethodInfo>();

		private void EmitDirectConstructor(CachingTypeBuilder type, OptFunctionNode ofn)
		{
			Type[] parameterTypes = GetParameterTypes(ofn.fnode);

			var method = type.DefineMethod(GetDirectCtorName(ofn.fnode), MethodAttributes.Public | MethodAttributes.Static, typeof (object), parameterTypes);
			var il = method.GetILGenerator();

			var firstLocal = il.DeclareLocal(typeof (Scriptable));

			// var firstLocal = this.CreateObject(cx, scope);
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // cx
			il.Emit(OpCodes.Ldarg_2); // scope
			il.Emit(OpCodes.Callvirt, typeof (BaseFunction).GetMethod("CreateObject", new[] { typeof (Context), typeof (Scriptable) }));
			il.Emit(OpCodes.Stloc, firstLocal);

			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // cx
			il.Emit(OpCodes.Ldarg_2); // scope
			il.Emit(OpCodes.Ldloc, firstLocal);

			var argCount = ofn.fnode.GetParamCount();
			for (var i = 0; i < argCount; i++)
			{
				il.EmitLoadArgument(4 + i*2);
				il.EmitLoadArgument(5 + i*2);
			}
			il.EmitLoadArgument(4 + argCount*3);
			il.Emit(OpCodes.Call, mainClass.GetMethod(GetBodyMethodName(ofn.fnode), parameterTypes));

			var exitLabel = il.DefineLabel();

			il.Emit(OpCodes.Dup);
			// make a copy of direct call result
			il.Emit(OpCodes.Isinst, typeof (Scriptable));
			il.Emit(OpCodes.Brfalse, exitLabel);
			// cast direct call result
			il.Emit(OpCodes.Castclass, typeof (Scriptable));
			il.Emit(OpCodes.Ret);

			il.MarkLabel(exitLabel);
			il.EmitLoadLocal(firstLocal);
			il.Emit(OpCodes.Ret);
		}

		internal static bool IsGenerator(ScriptNode node)
		{
			return node.GetType() == Token.FUNCTION && ((FunctionNode)node).IsGenerator();
		}

		// How dispatch to generators works:
		// Two methods are generated corresponding to a user-written generator.
		// One of these creates a generator object (NativeGenerator), which is
		// returned to the user. The other method contains all of the body code
		// of the generator.
		// When a user calls a generator, the call() method dispatches control to
		// to the method that creates the NativeGenerator object. Subsequently when
		// the user invokes .next(), .send() or any such method on the generator
		// object, the resumeGenerator() below dispatches the call to the
		// method corresponding to the generator body. As a matter of convention
		// the generator body is given the name of the generator activation function
		// appended by "_gen".
		private void GenerateResumeGenerator(CachingTypeBuilder type, FieldInfo idField)
		{
			// if there are no generators defined, we don't implement a
			// resumeGenerator(). The base class provides a default implementation.
			if (!scriptOrFnNodes.Any(IsGenerator))
				return;

			var method = type.DefineMethod("ResumeGenerator", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual, typeof (object), new[] {typeof (Context), typeof (Scriptable), typeof (int), typeof (object), typeof (object)});
			var il = method.GetILGenerator();
			// load arguments for dispatch to the corresponding *_gen method
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // cx
			il.Emit(OpCodes.Ldarg_2); // scope
			il.Emit(OpCodes.Ldarg_S, (byte) 4); // state
			il.Emit(OpCodes.Ldarg_S, (byte) 5); // value
			il.Emit(OpCodes.Ldarg_3); // operation
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldfld, idField);

			Label[] switchTable = il.DefineSwitchTable(scriptOrFnNodes.Length);
			var endlabel = il.DefineLabel();
			for (var i = 0; i < scriptOrFnNodes.Length; i++)
			{
				var n = scriptOrFnNodes[i];
				il.MarkLabel(switchTable[i]);
				if (IsGenerator(n))
				{
					il.Emit(OpCodes.Call, mainClass.GetMethod(GetBodyMethodName(n) + "_gen", new[] { mainClass, typeof (Context), typeof (Scriptable), typeof (object), typeof (object), typeof (int) }));
					il.Emit(OpCodes.Ret);
				}
				else
				{
					il.Emit(OpCodes.Br, endlabel);
				}
			}
			il.MarkLabel(endlabel);
			PushUndefined(il);
			il.Emit(OpCodes.Ret);
		}

		private MethodBuilder GenerateCallMethod(CachingTypeBuilder tb, FieldInfo ifField)
		{
			// Generate code for:
			// if (ScriptRuntime.hasTopCall(cx)) {
			//     return ScriptRuntime.doTopCall(this, cx, scope, thisObj, args);
			// }

			var method = tb.DefineMethod("Call", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof (object), new[] {typeof (Context), typeof (Scriptable), typeof (Scriptable), typeof (object[])});
			var il = method.GetILGenerator();

			il.Emit(OpCodes.Ldarg_1); // cx
			il.Emit(OpCodes.Call, typeof (ScriptRuntime).GetMethod("HasTopCall", new[] { typeof (Context) }));

			var nonTopCallLabel = il.DefineLabel();
			il.Emit(OpCodes.Brtrue, nonTopCallLabel);

			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // cx
			il.Emit(OpCodes.Ldarg_2); // scope
			il.Emit(OpCodes.Ldarg_3); // thisObj
			il.Emit(OpCodes.Ldarg_S, (byte) 4); // args
			il.Emit(OpCodes.Call, typeof (ScriptRuntime).GetMethod("DoTopCall", new[] { typeof (Callable), typeof (Context), typeof (Scriptable), typeof (Scriptable), typeof (object[]) }));
			il.Emit(OpCodes.Ret);

			il.MarkLabel(nonTopCallLabel);

			// Now generate switch to call the real methods
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // cx
			il.Emit(OpCodes.Ldarg_2); // scope
			il.Emit(OpCodes.Ldarg_3); // thisObj
			il.Emit(OpCodes.Ldarg_S, (byte)4); // args

			var end = scriptOrFnNodes.Length;
			var generateSwitch = (2 <= end);
			Label[] switchTable = null;
			if (generateSwitch)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, ifField);
				// do switch from (1,  end - 1) mapping 0 to
				// the default case
				switchTable = il.DefineSwitchTable(end);
				il.Emit(OpCodes.Switch, switchTable);
			}
			for (var i = 0; i < end; i++)
			{
				var n = scriptOrFnNodes[i];
				if (generateSwitch)
				{
					il.MarkLabel(switchTable [i]);
				}
				if (n.GetType() == Token.FUNCTION)
				{
					var ofn = OptFunctionNode.Get(n);
					if (ofn.IsTargetOfDirectCall())
					{
						var pcount = ofn.fnode.GetParamCount();
						if (pcount != 0)
						{
							// loop invariant:
							// stack top == arguments array from addALoad4()
							for (var p = 0; p < pcount; p++)
							{
								il.Emit(OpCodes.Ldlen);
								il.EmitLoadConstant(p);
								var undefArg = il.DefineLabel();
								var beyond = il.DefineLabel();
								il.Emit(ByteCode.IF_ICMPLE, undefArg);
								// get array[p]
								il.EmitLoadArgument(4);
								il.EmitLoadConstant(p);
								il.Emit(OpCodes.Ldind_Ref);
								il.Emit(OpCodes.Br, beyond);
								il.MarkLabel(undefArg);
								PushUndefined(il);
								il.MarkLabel(beyond);
								// Only one push
								//cfw.AdjustStackTop(-1);
								il.EmitLoadConstant(0.0);
								// restore invariant
								il.EmitLoadArgument(4);
							}
						}
					}
				}
				il.Emit(OpCodes.Call, mainClass.GetMethod(GetBodyMethodName(n), GetParameterTypes(n)));
				il.Emit(OpCodes.Ret);
			}
			return method;
		}

		private void GenerateMain(CachingTypeBuilder tb, ConstructorInfo constructor)
		{
			var method = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof (void), new[] {typeof (string[])});
			var il = method.GetILGenerator();
			il.Emit(OpCodes.Newobj, constructor); // new ScriptImpl()
			il.Emit(OpCodes.Ldarg_0); // args

			// Call mainMethodClass.Main(Script script, String[] args)
			il.Emit(OpCodes.Call, mainMethodClass.GetMethod("Main", new[] { typeof(Script), typeof(string[]) }));
			il.Emit(OpCodes.Ret);
		}

		private static void GenerateExecute(CachingTypeBuilder tb, MethodInfo callMethod)
		{
			var method = tb.DefineMethod("Exec", MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot, typeof (object), new[] {typeof (Context), typeof (Scriptable)});
			var il = method.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Callvirt, callMethod);
			il.Emit(OpCodes.Ret);
		}

		private static ConstructorBuilder GenerateScriptCtor(CachingTypeBuilder tb, ConstructorInfo baseConstructor)
		{
			var constructor = tb.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, Type.EmptyTypes);
			var il = constructor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, baseConstructor);
			il.Emit(OpCodes.Ret);
			return constructor;
		}

		private ConstructorBuilder GenerateFunctionConstructor(CachingTypeBuilder type, FieldInfo idField)
		{
			var constructor = type.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig, CallingConventions.Standard, new[] { typeof (Scriptable), typeof (Context), typeof (int) });
			var il = constructor.GetILGenerator();
			
			// call base constructor
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Call, SUPER_CLASS.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));

			// set _id
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_3); // id
			il.Emit(OpCodes.Stfld, idField);

			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_2); // context
			il.Emit(OpCodes.Ldarg_1); // scriptable

			var start = (scriptOrFnNodes [0].GetType() == Token.SCRIPT) ? 1 : 0;
			var end = scriptOrFnNodes.Length;
			if (start == end)
			{
				throw BadTree();
			}

			var generateSwitch = (2 <= end - start);
			Label[] switchTable = null;
			if (generateSwitch)
			{
				il.Emit(OpCodes.Ldarg_3); // id
				// do switch from (start + 1,  end - 1) mapping start to
				// the default case
				switchTable = il.DefineSwitchTable(end - start);
				il.Emit(OpCodes.Switch, switchTable);
			}
			for (var i = start; i < end; i++)
			{
				if (generateSwitch)
				{
					il.MarkLabel(switchTable [i - start]);
				}
				var ofn = OptFunctionNode.Get(scriptOrFnNodes [i]);
				il.Emit(OpCodes.Call, functionInits[ofn]);
				il.Emit(OpCodes.Ret);
			}
			return constructor;
		}

		private MethodInfo GenerateFunctionInit(CachingTypeBuilder type, MethodInfo regExpInit, OptFunctionNode ofn)
		{
			var method = type.DefineMethod(GetFunctionInitMethodName(ofn), MethodAttributes.Private | MethodAttributes.Final, typeof (void), new[] {typeof (Context), typeof (Scriptable)});
			var il = method.GetILGenerator();
			
			// Call NativeFunction.InitScriptFunction
			il.Emit(OpCodes.Ldarg_0); // this
			il.Emit(OpCodes.Ldarg_1); // cx
			il.Emit(OpCodes.Ldarg_2); // scope
			il.Emit(OpCodes.Callvirt, typeof (NativeFunction).GetMethod("InitScriptFunction", new[] {typeof (Context), typeof (Scriptable)}));

			// precompile all regexp literals
			if (ofn.fnode.GetRegExpCount() != 0)
			{
				il.Emit(OpCodes.Ldarg_1); //cx
				il.Emit(OpCodes.Call, regExpInit);
			}

			il.Emit(OpCodes.Ret);
			return method;
		}

		private void GenerateNativeFunctionOverrides(CachingTypeBuilder type, FieldInfo idField, string encodedSource)
		{
			GenerateGetFunctionName(type, idField);
			GenerateGetLanguageVersion(type);
			GenerateGetParamCount(type, idField);
			GenerateGetParamAndVarCount(type, idField);
			GenerateGetParamOrVarName(type, idField);
			GenerateGetEncodedSource(type, idField, encodedSource);
			GenerateGetParamOrVarConst(type, idField);
		}

		private void GenerateGetFunctionName(CachingTypeBuilder type, FieldInfo idField)
		{
			var method = type.DefineMethod("GetFunctionName", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof (string), Type.EmptyTypes);
			var il = method.GetILGenerator();

			GenerateNativeFunctionBody(idField, il, node =>
			{
				// Impelemnet method-specific switch code
				// Push function name
				if (node.GetType() == Token.SCRIPT)
				{
					il.EmitLoadConstant(string.Empty);
				}
				else
				{
					var name = ((FunctionNode) node).GetName();
					il.EmitLoadConstant(name);
				}
				il.Emit(OpCodes.Ret);
			});
		}

		private void GenerateGetLanguageVersion(CachingTypeBuilder type)
		{
			// Override NativeFunction.getLanguageVersion() with
			// public int getLanguageVersion() { return <version-constant>; }
			var method = type.DefineMethod("GetLanguageVersion", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(LanguageVersion), Type.EmptyTypes);
			var il = method.GetILGenerator();
			var version = compilerEnv.GetLanguageVersion();
			il.EmitLoadConstant((int) version);
			il.Emit(OpCodes.Ret);
		}

		private void GenerateGetEncodedSource(CachingTypeBuilder type, FieldInfo idField, string encodedSource)
		{
			if (encodedSource == null)
				return;

			var method = type.DefineMethod("GetEncodedSource", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(string), Type.EmptyTypes);
			var il = method.GetILGenerator();
			il.EmitLoadConstant(encodedSource);

			GenerateNativeFunctionBody(idField, il, node =>
			{
				// Push number encoded source start and end
				// to prepare for encodedSource.Substring(start, length)
				var start = node.GetEncodedSourceStart();
				var end = node.GetEncodedSourceEnd();
				var length = end - start;

				il.EmitLoadConstant(start);
				il.EmitLoadConstant(length);
				il.Emit(OpCodes.Callvirt, typeof (String).GetMethod("Substring", new[] { typeof (int), typeof (int) }));
				il.Emit(OpCodes.Ret);
			});
		}

		private void GenerateGetParamCount(CachingTypeBuilder type, FieldInfo idField)
		{
			var method = type.DefineMethod("GetParamCount", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);
			var il = method.GetILGenerator();

			GenerateNativeFunctionBody(idField, il, node =>
			{
				// Push number of defined parameters
				var count = node.GetParamCount();
				il.EmitLoadConstant(count);
				il.Emit(OpCodes.Ret);
			});
		}

		private void GenerateGetParamAndVarCount(CachingTypeBuilder type, FieldInfo idField)
		{
			// Only this
			var method = type.DefineMethod("GetParamAndVarCount", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(int), Type.EmptyTypes);
			var il = method.GetILGenerator();

			GenerateNativeFunctionBody(idField, il, node =>
			{
				// Push number of defined parameters and declared variables
				var k = node.GetParamAndVarCount();
				il.EmitLoadConstant(k);
				il.Emit(OpCodes.Ret);
			});
		}

		private void GenerateGetParamOrVarName(CachingTypeBuilder type, FieldInfo idField)
		{
			// this + paramOrVarIndex
			var method = type.DefineMethod("GetParamOrVarName", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(string), new[] { typeof(int) });
			var il = method.GetILGenerator();

			GenerateNativeFunctionBody(idField, il, node =>
			{
				// Push name of parameter using another switch
				// over paramAndVarCount
				var count = node.GetParamAndVarCount();
				if (count == 0)
				{
					// The runtime should never call the method in this
					// case but to make bytecode verifier happy return null
					// as throwing execption takes more code
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Ret);
				}
				else if (count == 1)
				{
					// As above do not check for valid index but always
					// return the name of the first param
					var name = node.GetParamOrVarName(0);
					il.EmitLoadConstant(name);
					il.Emit(OpCodes.Ret);
				}
				else
				{
					// Do switch over getParamOrVarName
					var table = il.DefineSwitchTable(count);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Switch, table);
					for (var j = 0; j < count; j++)
					{
						var name = node.GetParamOrVarName(j);
						il.MarkLabel(table[j]);
						il.EmitLoadConstant(name);
						il.Emit(OpCodes.Ret);
					}
				}
			});
		}

		private void GenerateGetParamOrVarConst(CachingTypeBuilder type, FieldInfo idField)
		{
			var method = type.DefineMethod("GetParamOrVarConst", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof (bool), new[] { typeof (int) });
			var il = method.GetILGenerator();

			GenerateNativeFunctionBody(idField, il, node =>
			{
				// Push name of parameter using another switch
				// over paramAndVarCount
				int count = node.GetParamAndVarCount();
				var constness = node.GetParamAndVarConst();
				if (count == 0)
				{
					// The runtime should never call the method in this
					// case but to make bytecode verifier happy return null
					// as throwing execption takes more code
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Ret);
				}
				else if (count == 1)
				{
					// As above do not check for valid index but always
					// return the name of the first param
					il.EmitLoadConstant(constness[0]);
					il.Emit(OpCodes.Ret);
				}
				else
				{
					// Do switch over getParamOrVarName
					var table = il.DefineSwitchTable(count);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Switch, table);
					for (var j = 0; j < count; j++)
					{
						il.MarkLabel(table[j]);
						il.EmitLoadConstant(constness[j]);
						il.Emit(OpCodes.Ret);
					}
				}
			});
		}

		private void GenerateNativeFunctionBody(FieldInfo idField, ILGenerator il, Action<ScriptNode> emitter)
		{
			var count = scriptOrFnNodes.Length;
			if (count == 1)
			{
				emitter(scriptOrFnNodes[0]);
			}
			else
			{
				// Generate switch but only if there is more then one
				// script/function

				var switchTable = il.DefineSwitchTable(count);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, idField);
				il.Emit(OpCodes.Switch, switchTable.ToArray());

				for (var i = 0; i < count; i++)
				{
					il.MarkLabel(switchTable[i]);
					emitter(scriptOrFnNodes[i]);
				}
			}
		}

		private MethodBuilder EmitRegExpInit(CachingTypeBuilder type)
		{
			// precompile all regexp literals
			if (!scriptOrFnNodes.Any(t => t.GetRegExpCount() > 0))
				return null;

			var method = type.DefineMethod("_reInit", MethodAttributes.Static | MethodAttributes.Private, typeof (void), new[] { typeof (Context) });
			var il = method.GetILGenerator();
			var regExpProxy = il.DeclareLocal(typeof (RegExpProxy));

			var reInitDone = type.DefineField("_reInitDone", typeof (bool), new[] { typeof (IsVolatile) }, Type.EmptyTypes, FieldAttributes.Static | FieldAttributes.Private);

			il.Emit(OpCodes.Ldsfld, reInitDone);

			var doInit = il.DefineLabel();
			il.Emit(OpCodes.Brfalse, doInit);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(doInit);

			// get regexp proxy and store it in local slot 1
			il.Emit(OpCodes.Ldarg_0); // context
			il.Emit(OpCodes.Call, typeof (ScriptRuntime).GetMethod("CheckRegExpProxy", new[] { typeof (Context) }));
			il.Emit(OpCodes.Stloc, regExpProxy); // proxy

			// We could apply double-checked locking here but concurrency
			// shouldn't be a problem in practice
			foreach (var scriptOrFnNode in scriptOrFnNodes)
			{
				var regexpCount = scriptOrFnNode.GetRegExpCount();
				for (var j = 0; j < regexpCount; j++)
				{
					var reString = scriptOrFnNode.GetRegExpString(j);
					var reFlags = scriptOrFnNode.GetRegExpFlags(j);

					var reField = type.DefineField(GetCompiledRegExpName(scriptOrFnNode, j), typeof (object), FieldAttributes.Static | FieldAttributes.Private);

					il.Emit(OpCodes.Ldloc, regExpProxy); // proxy
					il.Emit(OpCodes.Ldarg_0); // context
					il.Emit(OpCodes.Ldstr, reString);
					il.Emit(OpCodes.Ldstr, reFlags);
					il.Emit(OpCodes.Callvirt, typeof (RegExpProxy).GetMethod("CompileRegExp", new[] { typeof (Context), typeof (string), typeof (string) }));
					il.Emit(OpCodes.Stsfld, reField);
				}
			}
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Stsfld, reInitDone);
			il.Emit(OpCodes.Ret);
			return method;
		}

		private void EmitConstantDudeInitializers(CachingTypeBuilder type)
		{
			var n = itsConstantListSize;
			if (n == 0)
			{
				return;
			}
			ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
			var il = constructor.GetILGenerator();

			var array = itsConstantList;
			for (int i = 0; i < n; i++)
			{
				var number = array[i];
				var integer = (int) number;
				FieldBuilder field;
				if (integer == number)
				{
					field = type.DefineField("_k" + i, typeof (int), FieldAttributes.Static | FieldAttributes.Private);
					il.EmitLoadConstant(integer);
				}
				else
				{
					field = type.DefineField("_k" + i, typeof (Double), FieldAttributes.Static | FieldAttributes.Private);
					il.EmitLoadConstant(number);
				}
				constantFields ["_k" + i] = field;
				il.Emit(OpCodes.Stsfld, field);
			}
			il.Emit(OpCodes.Ret);
		}

		internal static void PushNumberAsObject(ILGenerator il, ClassFileWriter cfw, double num)
		{
			il.EmitLoadConstant(num);
			il.Emit(OpCodes.Box, typeof (double));
			//TODO: FOR SOME REASONS THERE WAS FOLLOWING CRAZY CODE
/*
			if (num == 0.0)
			{
				il.EmitLoadConstant(num);
			}
			else
			{
				if (num == 1.0)
				{
					il.EmitLoadConstant(1.0);
					return;
				}
				else
				{
					if (num == -1.0)
					{
						cfw.Add(OpCodes.Ldfld, typeof(OptRuntime), "minusOneObj", typeof(Double));
					}
					else
					{
						if (num != num)
						{
							cfw.Add(OpCodes.Ldfld, typeof(ScriptRuntime), "NaNobj", typeof(Double));
						}
						else
						{
							if (itsConstantListSize >= 2000)
							{
								// There appears to be a limit in the JVM on either the number
								// of static fields in a class or the size of the class
								// initializer. Either way, we can't have any more than 2000
								// statically init'd constants.
								il.EmitLoadConstant(num);
							}
							else
							{
								var N = itsConstantListSize;
								var index = 0;
								if (N == 0)
								{
									itsConstantList = new double[64];
								}
								else
								{
									var array = itsConstantList;
									while (index != N && array[index] != num)
									{
										++index;
									}
									if (N == array.Length)
									{
										array = new double[N * 2];
										Array.Copy(itsConstantList, 0, array, 0, N);
										itsConstantList = array;
									}
								}
								if (index == N)
								{
									itsConstantList[N] = num;
									itsConstantListSize = N + 1;
								}
								var constantName = "_k" + index;
								il.Emit(OpCodes.Ldfld, constantFields [constantName]);
							}
						}
					}
				}
			}
*/
		}

		private readonly Dictionary<string, FieldInfo> constantFields = new Dictionary<string, FieldInfo>();

		internal static void PushUndefined(ILGenerator il)
		{
			il.Emit(OpCodes.Ldsfld, typeof(Undefined).GetField("instance"));
		}

		internal int GetIndex(ScriptNode n)
		{
			return scriptOrFnIndexes.GetExisting(n);
		}

		internal string GetDirectCtorName(ScriptNode n)
		{
			return "_n" + GetIndex(n);
		}

		internal string GetBodyMethodName(ScriptNode n)
		{
			return "_c_" + CleanName(n) + "_" + GetIndex(n);
		}

		/// <summary>Gets a Java-compatible "informative" name for the the ScriptOrFnNode</summary>
		internal static string CleanName(ScriptNode n)
		{
			var node = n as FunctionNode;
			if (node == null)
				return "script";
			
			var name = node.GetFunctionName();
			return name == null
				? "anonymous"
				: name.GetIdentifier();
		}

		internal Type[] GetParameterTypes(ScriptNode n)
		{
			var list = new List<Type>
			{
				mainClass,
				typeof (Context),
				typeof (Scriptable),
				typeof (Scriptable)
			};

			if (n.GetType() == Token.FUNCTION)
			{
				var ofn = OptFunctionNode.Get(n);
				if (ofn.IsTargetOfDirectCall())
				{
					var pCount = ofn.fnode.GetParamCount();
					for (var i = 0; i != pCount; i++)
					{
						list.Add(typeof (object));
						list.Add(typeof (double));
					}
				}
			}
			list.Add(typeof (object[]));
			return list.ToArray();
		}

		private string GetFunctionInitMethodName(OptFunctionNode ofn)
		{
			return "_i" + GetIndex(ofn.fnode);
		}

		internal string GetCompiledRegExpName(ScriptNode n, int regexpIndex)
		{
			return "_re" + GetIndex(n) + "_" + regexpIndex;
		}

		internal static Exception BadTree()
		{
			throw new Exception("Bad tree in codegen");
		}

		public void SetMainMethodClass(string className)
		{
			mainMethodClass = Runtime.GetType(className);
		}

		public static readonly Type DEFAULT_MAIN_METHOD_CLASS = typeof (OptRuntime);

		private static readonly Type SUPER_CLASS = typeof (NativeFunction);

		internal const string ID_FIELD_NAME = "_id";

		private static readonly object globalLock = new object();

		private static int globalSerialClassCounter;

		private CompilerEnvirons compilerEnv;

		private ObjArray directCallTargets;

		internal ScriptNode[] scriptOrFnNodes;

		private ObjToIntMap scriptOrFnIndexes;

		private Type mainMethodClass = DEFAULT_MAIN_METHOD_CLASS;

		private CachingTypeBuilder mainClass;

		private double[] itsConstantList;

		private int itsConstantListSize;
		private AssemblyBuilder dynamicAssembly;

		public FieldInfo GetField(string name)
		{
			return mainClass.GetField(name);
		}
	}
}

#endif
