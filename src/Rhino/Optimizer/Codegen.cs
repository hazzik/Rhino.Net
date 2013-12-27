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
using System.Security;
using System.Text;
using Org.Mozilla.Classfile;
using Rhino;
using Rhino.Ast;
using Rhino.Optimizer;
using Sharpen;

namespace Rhino.Optimizer
{
	/// <summary>This class generates code for a given IR tree.</summary>
	/// <remarks>This class generates code for a given IR tree.</remarks>
	/// <author>Norris Boyd</author>
	/// <author>Roger Lawrence</author>
	public class Codegen : Evaluator
	{
		public virtual void CaptureStackInfo(RhinoException ex)
		{
			throw new NotSupportedException();
		}

		public virtual string GetSourcePositionFromStack(Context cx, int[] linep)
		{
			throw new NotSupportedException();
		}

		public virtual string GetPatchedStack(RhinoException ex, string nativeStackTrace)
		{
			throw new NotSupportedException();
		}

		public virtual IList<string> GetScriptStack(RhinoException ex)
		{
			throw new NotSupportedException();
		}

		public virtual void SetEvalScriptFlag(Script script)
		{
			throw new NotSupportedException();
		}

		public virtual object Compile(CompilerEnvirons compilerEnv, ScriptNode tree, string encodedSource, bool returnFunction)
		{
			int serial;
			lock (globalLock)
			{
				serial = ++globalSerialClassCounter;
			}
			string baseName = "c";
			if (tree.GetSourceName().Length > 0)
			{
				baseName = tree.GetSourceName().ReplaceAll("\\W", "_");
				if (!char.IsJavaIdentifierStart(baseName[0]))
				{
					baseName = "_" + baseName;
				}
			}
			string mainClassName = "Rhino.Gen." + baseName + "_" + serial;
			byte[] mainClassBytes = CompileToClassFile(compilerEnv, mainClassName, tree, encodedSource, returnFunction);
			return new object[] { mainClassName, mainClassBytes };
		}

		public virtual Script CreateScriptObject(object bytecode, object staticSecurityDomain)
		{
			Type cl = DefineClass(bytecode, staticSecurityDomain);
			Script script;
			try
			{
				script = (Script)System.Activator.CreateInstance(cl);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to instantiate compiled class:" + ex.ToString());
			}
			return script;
		}

		public virtual Function CreateFunctionObject(Context cx, Scriptable scope, object bytecode, object staticSecurityDomain)
		{
			Type cl = DefineClass(bytecode, staticSecurityDomain);
			NativeFunction f;
			try
			{
				ConstructorInfo ctor = cl.GetConstructors()[0];
				object[] initArgs = new object[] { scope, cx, Sharpen.Extensions.ValueOf(0) };
				f = (NativeFunction)ctor.NewInstance(initArgs);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to instantiate compiled class:" + ex.ToString());
			}
			return f;
		}

		private Type DefineClass(object bytecode, object staticSecurityDomain)
		{
			object[] nameBytesPair = (object[])bytecode;
			string className = (string)nameBytesPair[0];
			byte[] classBytes = (byte[])nameBytesPair[1];
			// The generated classes in this case refer only to Rhino classes
			// which must be accessible through this class loader
			ClassLoader rhinoLoader = GetType().GetClassLoader();
			GeneratedClassLoader loader;
			loader = SecurityController.CreateLoader(rhinoLoader, staticSecurityDomain);
			Exception e;
			try
			{
				Type cl = loader.DefineClass(className, classBytes);
				loader.LinkClass(cl);
				return cl;
			}
			catch (SecurityException x)
			{
				e = x;
			}
			catch (ArgumentException x)
			{
				e = x;
			}
			throw new Exception("Malformed optimizer package " + e);
		}

		public virtual byte[] CompileToClassFile(CompilerEnvirons compilerEnv, string mainClassName, ScriptNode scriptOrFn, string encodedSource, bool returnFunction)
		{
			this.compilerEnv = compilerEnv;
			Transform(scriptOrFn);
			if (returnFunction)
			{
				scriptOrFn = scriptOrFn.GetFunctionNode(0);
			}
			InitScriptNodesData(scriptOrFn);
			this.mainClassName = mainClassName;
			this.mainClassSignature = ClassFileWriter.ClassNameToSignature(mainClassName);
			try
			{
				return GenerateCode(encodedSource);
			}
			catch (ClassFileWriter.ClassFileFormatException e)
			{
				throw ReportClassFileFormatException(scriptOrFn, e.Message);
			}
		}

		private Exception ReportClassFileFormatException(ScriptNode scriptOrFn, string message)
		{
			string msg = scriptOrFn is FunctionNode ? ScriptRuntime.GetMessage2("msg.while.compiling.fn", ((FunctionNode)scriptOrFn).GetFunctionName(), message) : ScriptRuntime.GetMessage1("msg.while.compiling.script", message);
			return Context.ReportRuntimeError(msg, scriptOrFn.GetSourceName(), scriptOrFn.GetLineno(), null, 0);
		}

		private void Transform(ScriptNode tree)
		{
			InitOptFunctions_r(tree);
			int optLevel = compilerEnv.GetOptimizationLevel();
			IDictionary<string, OptFunctionNode> possibleDirectCalls = null;
			if (optLevel > 0)
			{
				if (tree.GetType() == Token.SCRIPT)
				{
					int functionCount = tree.GetFunctionCount();
					for (int i = 0; i != functionCount; ++i)
					{
						OptFunctionNode ofn = OptFunctionNode.Get(tree, i);
						if (ofn.fnode.GetFunctionType() == FunctionNode.FUNCTION_STATEMENT)
						{
							string name = ofn.fnode.GetName();
							if (name.Length != 0)
							{
								if (possibleDirectCalls == null)
								{
									possibleDirectCalls = new Dictionary<string, OptFunctionNode>();
								}
								possibleDirectCalls.Put(name, ofn);
							}
						}
					}
				}
			}
			if (possibleDirectCalls != null)
			{
				directCallTargets = new ObjArray();
			}
			OptTransformer ot = new OptTransformer(possibleDirectCalls, directCallTargets);
			ot.Transform(tree);
			if (optLevel > 0)
			{
				(new Rhino.Optimizer.Optimizer()).Optimize(tree);
			}
		}

		private static void InitOptFunctions_r(ScriptNode scriptOrFn)
		{
			for (int i = 0, N = scriptOrFn.GetFunctionCount(); i != N; ++i)
			{
				FunctionNode fn = scriptOrFn.GetFunctionNode(i);
				new OptFunctionNode(fn);
				InitOptFunctions_r(fn);
			}
		}

		private void InitScriptNodesData(ScriptNode scriptOrFn)
		{
			ObjArray x = new ObjArray();
			CollectScriptNodes_r(scriptOrFn, x);
			int count = x.Size();
			scriptOrFnNodes = new ScriptNode[count];
			x.ToArray(scriptOrFnNodes);
			scriptOrFnIndexes = new ObjToIntMap(count);
			for (int i = 0; i != count; ++i)
			{
				scriptOrFnIndexes.Put(scriptOrFnNodes[i], i);
			}
		}

		private static void CollectScriptNodes_r(ScriptNode n, ObjArray x)
		{
			x.Add(n);
			int nestedCount = n.GetFunctionCount();
			for (int i = 0; i != nestedCount; ++i)
			{
				CollectScriptNodes_r(n.GetFunctionNode(i), x);
			}
		}

		private byte[] GenerateCode(string encodedSource)
		{
			bool hasScript = (scriptOrFnNodes[0].GetType() == Token.SCRIPT);
			bool hasFunctions = (scriptOrFnNodes.Length > 1 || !hasScript);
			string sourceFile = null;
			if (compilerEnv.IsGenerateDebugInfo())
			{
				sourceFile = scriptOrFnNodes[0].GetSourceName();
			}
			ClassFileWriter cfw = new ClassFileWriter(mainClassName, SUPER_CLASS_NAME, sourceFile);
			cfw.AddField(ID_FIELD_NAME, "I", ClassFileWriter.ACC_PRIVATE);
			if (hasFunctions)
			{
				GenerateFunctionConstructor(cfw);
			}
			if (hasScript)
			{
				cfw.AddInterface("org/mozilla/javascript/Script");
				GenerateScriptCtor(cfw);
				GenerateMain(cfw);
				GenerateExecute(cfw);
			}
			GenerateCallMethod(cfw);
			GenerateResumeGenerator(cfw);
			GenerateNativeFunctionOverrides(cfw, encodedSource);
			int count = scriptOrFnNodes.Length;
			for (int i = 0; i != count; ++i)
			{
				ScriptNode n = scriptOrFnNodes[i];
				BodyCodegen bodygen = new BodyCodegen();
				bodygen.cfw = cfw;
				bodygen.codegen = this;
				bodygen.compilerEnv = compilerEnv;
				bodygen.scriptOrFn = n;
				bodygen.scriptOrFnIndex = i;
				try
				{
					bodygen.GenerateBodyCode();
				}
				catch (ClassFileWriter.ClassFileFormatException e)
				{
					throw ReportClassFileFormatException(n, e.Message);
				}
				if (n.GetType() == Token.FUNCTION)
				{
					OptFunctionNode ofn = OptFunctionNode.Get(n);
					GenerateFunctionInit(cfw, ofn);
					if (ofn.IsTargetOfDirectCall())
					{
						EmitDirectConstructor(cfw, ofn);
					}
				}
			}
			EmitRegExpInit(cfw);
			EmitConstantDudeInitializers(cfw);
			return cfw.ToByteArray();
		}

		private void EmitDirectConstructor(ClassFileWriter cfw, OptFunctionNode ofn)
		{
			cfw.StartMethod(GetDirectCtorName(ofn.fnode), GetBodyMethodSignature(ofn.fnode), (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE));
			int argCount = ofn.fnode.GetParamCount();
			int firstLocal = (4 + argCount * 3) + 1;
			cfw.AddALoad(0);
			// this
			cfw.AddALoad(1);
			// cx
			cfw.AddALoad(2);
			// scope
			cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, "org/mozilla/javascript/BaseFunction", "createObject", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.AddAStore(firstLocal);
			cfw.AddALoad(0);
			cfw.AddALoad(1);
			cfw.AddALoad(2);
			cfw.AddALoad(firstLocal);
			for (int i = 0; i < argCount; i++)
			{
				cfw.AddALoad(4 + (i * 3));
				cfw.AddDLoad(5 + (i * 3));
			}
			cfw.AddALoad(4 + argCount * 3);
			cfw.AddInvoke(ByteCode.INVOKESTATIC, mainClassName, GetBodyMethodName(ofn.fnode), GetBodyMethodSignature(ofn.fnode));
			int exitLabel = cfw.AcquireLabel();
			cfw.Add(ByteCode.DUP);
			// make a copy of direct call result
			cfw.Add(ByteCode.INSTANCEOF, "org/mozilla/javascript/Scriptable");
			cfw.Add(ByteCode.IFEQ, exitLabel);
			// cast direct call result
			cfw.Add(ByteCode.CHECKCAST, "org/mozilla/javascript/Scriptable");
			cfw.Add(ByteCode.ARETURN);
			cfw.MarkLabel(exitLabel);
			cfw.AddALoad(firstLocal);
			cfw.Add(ByteCode.ARETURN);
			cfw.StopMethod((short)(firstLocal + 1));
		}

		internal static bool IsGenerator(ScriptNode node)
		{
			return (node.GetType() == Token.FUNCTION) && ((FunctionNode)node).IsGenerator();
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
		private void GenerateResumeGenerator(ClassFileWriter cfw)
		{
			bool hasGenerators = false;
			for (int i = 0; i < scriptOrFnNodes.Length; i++)
			{
				if (IsGenerator(scriptOrFnNodes[i]))
				{
					hasGenerators = true;
				}
			}
			// if there are no generators defined, we don't implement a
			// resumeGenerator(). The base class provides a default implementation.
			if (!hasGenerators)
			{
				return;
			}
			cfw.StartMethod("resumeGenerator", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "ILjava/lang/Object;" + "Ljava/lang/Object;)Ljava/lang/Object;", (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_FINAL));
			// load arguments for dispatch to the corresponding *_gen method
			cfw.AddALoad(0);
			cfw.AddALoad(1);
			cfw.AddALoad(2);
			cfw.AddALoad(4);
			cfw.AddALoad(5);
			cfw.AddILoad(3);
			cfw.AddLoadThis();
			cfw.Add(ByteCode.GETFIELD, cfw.GetClassName(), ID_FIELD_NAME, "I");
			int startSwitch = cfw.AddTableSwitch(0, scriptOrFnNodes.Length - 1);
			cfw.MarkTableSwitchDefault(startSwitch);
			int endlabel = cfw.AcquireLabel();
			for (int i_1 = 0; i_1 < scriptOrFnNodes.Length; i_1++)
			{
				ScriptNode n = scriptOrFnNodes[i_1];
				cfw.MarkTableSwitchCase(startSwitch, i_1, (short)6);
				if (IsGenerator(n))
				{
					string type = "(" + mainClassSignature + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + "Ljava/lang/Object;I)Ljava/lang/Object;";
					cfw.AddInvoke(ByteCode.INVOKESTATIC, mainClassName, GetBodyMethodName(n) + "_gen", type);
					cfw.Add(ByteCode.ARETURN);
				}
				else
				{
					cfw.Add(ByteCode.GOTO, endlabel);
				}
			}
			cfw.MarkLabel(endlabel);
			PushUndefined(cfw);
			cfw.Add(ByteCode.ARETURN);
			// this method uses as many locals as there are arguments (hence 6)
			cfw.StopMethod((short)6);
		}

		private void GenerateCallMethod(ClassFileWriter cfw)
		{
			cfw.StartMethod("call", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;)Ljava/lang/Object;", (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_FINAL));
			// Generate code for:
			// if (!ScriptRuntime.hasTopCall(cx)) {
			//     return ScriptRuntime.doTopCall(this, cx, scope, thisObj, args);
			// }
			int nonTopCallLabel = cfw.AcquireLabel();
			cfw.AddALoad(1);
			//cx
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/ScriptRuntime", "hasTopCall", "(Lorg/mozilla/javascript/Context;" + ")Z");
			cfw.Add(ByteCode.IFNE, nonTopCallLabel);
			cfw.AddALoad(0);
			cfw.AddALoad(1);
			cfw.AddALoad(2);
			cfw.AddALoad(3);
			cfw.AddALoad(4);
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/ScriptRuntime", "doTopCall", "(Lorg/mozilla/javascript/Callable;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Ljava/lang/Object;");
			cfw.Add(ByteCode.ARETURN);
			cfw.MarkLabel(nonTopCallLabel);
			// Now generate switch to call the real methods
			cfw.AddALoad(0);
			cfw.AddALoad(1);
			cfw.AddALoad(2);
			cfw.AddALoad(3);
			cfw.AddALoad(4);
			int end = scriptOrFnNodes.Length;
			bool generateSwitch = (2 <= end);
			int switchStart = 0;
			int switchStackTop = 0;
			if (generateSwitch)
			{
				cfw.AddLoadThis();
				cfw.Add(ByteCode.GETFIELD, cfw.GetClassName(), ID_FIELD_NAME, "I");
				// do switch from (1,  end - 1) mapping 0 to
				// the default case
				switchStart = cfw.AddTableSwitch(1, end - 1);
			}
			for (int i = 0; i != end; ++i)
			{
				ScriptNode n = scriptOrFnNodes[i];
				if (generateSwitch)
				{
					if (i == 0)
					{
						cfw.MarkTableSwitchDefault(switchStart);
						switchStackTop = cfw.GetStackTop();
					}
					else
					{
						cfw.MarkTableSwitchCase(switchStart, i - 1, switchStackTop);
					}
				}
				if (n.GetType() == Token.FUNCTION)
				{
					OptFunctionNode ofn = OptFunctionNode.Get(n);
					if (ofn.IsTargetOfDirectCall())
					{
						int pcount = ofn.fnode.GetParamCount();
						if (pcount != 0)
						{
							// loop invariant:
							// stack top == arguments array from addALoad4()
							for (int p = 0; p != pcount; ++p)
							{
								cfw.Add(ByteCode.ARRAYLENGTH);
								cfw.AddPush(p);
								int undefArg = cfw.AcquireLabel();
								int beyond = cfw.AcquireLabel();
								cfw.Add(ByteCode.IF_ICMPLE, undefArg);
								// get array[p]
								cfw.AddALoad(4);
								cfw.AddPush(p);
								cfw.Add(ByteCode.AALOAD);
								cfw.Add(ByteCode.GOTO, beyond);
								cfw.MarkLabel(undefArg);
								PushUndefined(cfw);
								cfw.MarkLabel(beyond);
								// Only one push
								cfw.AdjustStackTop(-1);
								cfw.AddPush(0.0);
								// restore invariant
								cfw.AddALoad(4);
							}
						}
					}
				}
				cfw.AddInvoke(ByteCode.INVOKESTATIC, mainClassName, GetBodyMethodName(n), GetBodyMethodSignature(n));
				cfw.Add(ByteCode.ARETURN);
			}
			cfw.StopMethod((short)5);
		}

		// 5: this, cx, scope, js this, args[]
		private void GenerateMain(ClassFileWriter cfw)
		{
			cfw.StartMethod("main", "([Ljava/lang/String;)V", (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_STATIC));
			// load new ScriptImpl()
			cfw.Add(ByteCode.NEW, cfw.GetClassName());
			cfw.Add(ByteCode.DUP);
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, cfw.GetClassName(), "<init>", "()V");
			// load 'args'
			cfw.Add(ByteCode.ALOAD_0);
			// Call mainMethodClass.main(Script script, String[] args)
			cfw.AddInvoke(ByteCode.INVOKESTATIC, mainMethodClass, "main", "(Lorg/mozilla/javascript/Script;[Ljava/lang/String;)V");
			cfw.Add(ByteCode.RETURN);
			// 1 = String[] args
			cfw.StopMethod((short)1);
		}

		private void GenerateExecute(ClassFileWriter cfw)
		{
			cfw.StartMethod("exec", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;", (short)(ClassFileWriter.ACC_PUBLIC | ClassFileWriter.ACC_FINAL));
			int CONTEXT_ARG = 1;
			int SCOPE_ARG = 2;
			cfw.AddLoadThis();
			cfw.AddALoad(CONTEXT_ARG);
			cfw.AddALoad(SCOPE_ARG);
			cfw.Add(ByteCode.DUP);
			cfw.Add(ByteCode.ACONST_NULL);
			cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, cfw.GetClassName(), "call", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Ljava/lang/Object;");
			cfw.Add(ByteCode.ARETURN);
			// 3 = this + context + scope
			cfw.StopMethod((short)3);
		}

		private void GenerateScriptCtor(ClassFileWriter cfw)
		{
			cfw.StartMethod("<init>", "()V", ClassFileWriter.ACC_PUBLIC);
			cfw.AddLoadThis();
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, SUPER_CLASS_NAME, "<init>", "()V");
			// set id to 0
			cfw.AddLoadThis();
			cfw.AddPush(0);
			cfw.Add(ByteCode.PUTFIELD, cfw.GetClassName(), ID_FIELD_NAME, "I");
			cfw.Add(ByteCode.RETURN);
			// 1 parameter = this
			cfw.StopMethod((short)1);
		}

		private void GenerateFunctionConstructor(ClassFileWriter cfw)
		{
			int SCOPE_ARG = 1;
			int CONTEXT_ARG = 2;
			int ID_ARG = 3;
			cfw.StartMethod("<init>", FUNCTION_CONSTRUCTOR_SIGNATURE, ClassFileWriter.ACC_PUBLIC);
			cfw.AddALoad(0);
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, SUPER_CLASS_NAME, "<init>", "()V");
			cfw.AddLoadThis();
			cfw.AddILoad(ID_ARG);
			cfw.Add(ByteCode.PUTFIELD, cfw.GetClassName(), ID_FIELD_NAME, "I");
			cfw.AddLoadThis();
			cfw.AddALoad(CONTEXT_ARG);
			cfw.AddALoad(SCOPE_ARG);
			int start = (scriptOrFnNodes[0].GetType() == Token.SCRIPT) ? 1 : 0;
			int end = scriptOrFnNodes.Length;
			if (start == end)
			{
				throw BadTree();
			}
			bool generateSwitch = (2 <= end - start);
			int switchStart = 0;
			int switchStackTop = 0;
			if (generateSwitch)
			{
				cfw.AddILoad(ID_ARG);
				// do switch from (start + 1,  end - 1) mapping start to
				// the default case
				switchStart = cfw.AddTableSwitch(start + 1, end - 1);
			}
			for (int i = start; i != end; ++i)
			{
				if (generateSwitch)
				{
					if (i == start)
					{
						cfw.MarkTableSwitchDefault(switchStart);
						switchStackTop = cfw.GetStackTop();
					}
					else
					{
						cfw.MarkTableSwitchCase(switchStart, i - 1 - start, switchStackTop);
					}
				}
				OptFunctionNode ofn = OptFunctionNode.Get(scriptOrFnNodes[i]);
				cfw.AddInvoke(ByteCode.INVOKESPECIAL, mainClassName, GetFunctionInitMethodName(ofn), FUNCTION_INIT_SIGNATURE);
				cfw.Add(ByteCode.RETURN);
			}
			// 4 = this + scope + context + id
			cfw.StopMethod((short)4);
		}

		private void GenerateFunctionInit(ClassFileWriter cfw, OptFunctionNode ofn)
		{
			int CONTEXT_ARG = 1;
			int SCOPE_ARG = 2;
			cfw.StartMethod(GetFunctionInitMethodName(ofn), FUNCTION_INIT_SIGNATURE, (short)(ClassFileWriter.ACC_PRIVATE | ClassFileWriter.ACC_FINAL));
			// Call NativeFunction.initScriptFunction
			cfw.AddLoadThis();
			cfw.AddALoad(CONTEXT_ARG);
			cfw.AddALoad(SCOPE_ARG);
			cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, "org/mozilla/javascript/NativeFunction", "initScriptFunction", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")V");
			// precompile all regexp literals
			if (ofn.fnode.GetRegexpCount() != 0)
			{
				cfw.AddALoad(CONTEXT_ARG);
				cfw.AddInvoke(ByteCode.INVOKESTATIC, mainClassName, REGEXP_INIT_METHOD_NAME, REGEXP_INIT_METHOD_SIGNATURE);
			}
			cfw.Add(ByteCode.RETURN);
			// 3 = (scriptThis/functionRef) + scope + context
			cfw.StopMethod((short)3);
		}

		private void GenerateNativeFunctionOverrides(ClassFileWriter cfw, string encodedSource)
		{
			// Override NativeFunction.getLanguageVersion() with
			// public int getLanguageVersion() { return <version-constant>; }
			cfw.StartMethod("getLanguageVersion", "()I", ClassFileWriter.ACC_PUBLIC);
			cfw.AddPush(compilerEnv.GetLanguageVersion());
			cfw.Add(ByteCode.IRETURN);
			// 1: this and no argument or locals
			cfw.StopMethod((short)1);
			// The rest of NativeFunction overrides require specific code for each
			// script/function id
			int Do_getFunctionName = 0;
			int Do_getParamCount = 1;
			int Do_getParamAndVarCount = 2;
			int Do_getParamOrVarName = 3;
			int Do_getEncodedSource = 4;
			int Do_getParamOrVarConst = 5;
			int SWITCH_COUNT = 6;
			for (int methodIndex = 0; methodIndex != SWITCH_COUNT; ++methodIndex)
			{
				if (methodIndex == Do_getEncodedSource && encodedSource == null)
				{
					continue;
				}
				// Generate:
				//   prologue;
				//   switch over function id to implement function-specific action
				//   epilogue
				short methodLocals;
				switch (methodIndex)
				{
					case Do_getFunctionName:
					{
						methodLocals = 1;
						// Only this
						cfw.StartMethod("getFunctionName", "()Ljava/lang/String;", ClassFileWriter.ACC_PUBLIC);
						break;
					}

					case Do_getParamCount:
					{
						methodLocals = 1;
						// Only this
						cfw.StartMethod("getParamCount", "()I", ClassFileWriter.ACC_PUBLIC);
						break;
					}

					case Do_getParamAndVarCount:
					{
						methodLocals = 1;
						// Only this
						cfw.StartMethod("getParamAndVarCount", "()I", ClassFileWriter.ACC_PUBLIC);
						break;
					}

					case Do_getParamOrVarName:
					{
						methodLocals = 1 + 1;
						// this + paramOrVarIndex
						cfw.StartMethod("getParamOrVarName", "(I)Ljava/lang/String;", ClassFileWriter.ACC_PUBLIC);
						break;
					}

					case Do_getParamOrVarConst:
					{
						methodLocals = 1 + 1 + 1;
						// this + paramOrVarName
						cfw.StartMethod("getParamOrVarConst", "(I)Z", ClassFileWriter.ACC_PUBLIC);
						break;
					}

					case Do_getEncodedSource:
					{
						methodLocals = 1;
						// Only this
						cfw.StartMethod("getEncodedSource", "()Ljava/lang/String;", ClassFileWriter.ACC_PUBLIC);
						cfw.AddPush(encodedSource);
						break;
					}

					default:
					{
						throw Kit.CodeBug();
					}
				}
				int count = scriptOrFnNodes.Length;
				int switchStart = 0;
				int switchStackTop = 0;
				if (count > 1)
				{
					// Generate switch but only if there is more then one
					// script/function
					cfw.AddLoadThis();
					cfw.Add(ByteCode.GETFIELD, cfw.GetClassName(), ID_FIELD_NAME, "I");
					// do switch from 1 .. count - 1 mapping 0 to the default case
					switchStart = cfw.AddTableSwitch(1, count - 1);
				}
				for (int i = 0; i != count; ++i)
				{
					ScriptNode n = scriptOrFnNodes[i];
					if (i == 0)
					{
						if (count > 1)
						{
							cfw.MarkTableSwitchDefault(switchStart);
							switchStackTop = cfw.GetStackTop();
						}
					}
					else
					{
						cfw.MarkTableSwitchCase(switchStart, i - 1, switchStackTop);
					}
					switch (methodIndex)
					{
						case Do_getFunctionName:
						{
							// Impelemnet method-specific switch code
							// Push function name
							if (n.GetType() == Token.SCRIPT)
							{
								cfw.AddPush(string.Empty);
							}
							else
							{
								string name = ((FunctionNode)n).GetName();
								cfw.AddPush(name);
							}
							cfw.Add(ByteCode.ARETURN);
							break;
						}

						case Do_getParamCount:
						{
							// Push number of defined parameters
							cfw.AddPush(n.GetParamCount());
							cfw.Add(ByteCode.IRETURN);
							break;
						}

						case Do_getParamAndVarCount:
						{
							// Push number of defined parameters and declared variables
							cfw.AddPush(n.GetParamAndVarCount());
							cfw.Add(ByteCode.IRETURN);
							break;
						}

						case Do_getParamOrVarName:
						{
							// Push name of parameter using another switch
							// over paramAndVarCount
							int paramAndVarCount = n.GetParamAndVarCount();
							if (paramAndVarCount == 0)
							{
								// The runtime should never call the method in this
								// case but to make bytecode verifier happy return null
								// as throwing execption takes more code
								cfw.Add(ByteCode.ACONST_NULL);
								cfw.Add(ByteCode.ARETURN);
							}
							else
							{
								if (paramAndVarCount == 1)
								{
									// As above do not check for valid index but always
									// return the name of the first param
									cfw.AddPush(n.GetParamOrVarName(0));
									cfw.Add(ByteCode.ARETURN);
								}
								else
								{
									// Do switch over getParamOrVarName
									cfw.AddILoad(1);
									// param or var index
									// do switch from 1 .. paramAndVarCount - 1 mapping 0
									// to the default case
									int paramSwitchStart = cfw.AddTableSwitch(1, paramAndVarCount - 1);
									for (int j = 0; j != paramAndVarCount; ++j)
									{
										if (cfw.GetStackTop() != 0)
										{
											Kit.CodeBug();
										}
										string s = n.GetParamOrVarName(j);
										if (j == 0)
										{
											cfw.MarkTableSwitchDefault(paramSwitchStart);
										}
										else
										{
											cfw.MarkTableSwitchCase(paramSwitchStart, j - 1, 0);
										}
										cfw.AddPush(s);
										cfw.Add(ByteCode.ARETURN);
									}
								}
							}
							break;
						}

						case Do_getParamOrVarConst:
						{
							// Push name of parameter using another switch
							// over paramAndVarCount
							paramAndVarCount = n.GetParamAndVarCount();
							bool[] constness = n.GetParamAndVarConst();
							if (paramAndVarCount == 0)
							{
								// The runtime should never call the method in this
								// case but to make bytecode verifier happy return null
								// as throwing execption takes more code
								cfw.Add(ByteCode.ICONST_0);
								cfw.Add(ByteCode.IRETURN);
							}
							else
							{
								if (paramAndVarCount == 1)
								{
									// As above do not check for valid index but always
									// return the name of the first param
									cfw.AddPush(constness[0]);
									cfw.Add(ByteCode.IRETURN);
								}
								else
								{
									// Do switch over getParamOrVarName
									cfw.AddILoad(1);
									// param or var index
									// do switch from 1 .. paramAndVarCount - 1 mapping 0
									// to the default case
									int paramSwitchStart = cfw.AddTableSwitch(1, paramAndVarCount - 1);
									for (int j = 0; j != paramAndVarCount; ++j)
									{
										if (cfw.GetStackTop() != 0)
										{
											Kit.CodeBug();
										}
										if (j == 0)
										{
											cfw.MarkTableSwitchDefault(paramSwitchStart);
										}
										else
										{
											cfw.MarkTableSwitchCase(paramSwitchStart, j - 1, 0);
										}
										cfw.AddPush(constness[j]);
										cfw.Add(ByteCode.IRETURN);
									}
								}
							}
							break;
						}

						case Do_getEncodedSource:
						{
							// Push number encoded source start and end
							// to prepare for encodedSource.substring(start, end)
							cfw.AddPush(n.GetEncodedSourceStart());
							cfw.AddPush(n.GetEncodedSourceEnd());
							cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, "java/lang/String", "substring", "(II)Ljava/lang/String;");
							cfw.Add(ByteCode.ARETURN);
							break;
						}

						default:
						{
							throw Kit.CodeBug();
						}
					}
				}
				cfw.StopMethod(methodLocals);
			}
		}

		private void EmitRegExpInit(ClassFileWriter cfw)
		{
			// precompile all regexp literals
			int totalRegCount = 0;
			for (int i = 0; i != scriptOrFnNodes.Length; ++i)
			{
				totalRegCount += scriptOrFnNodes[i].GetRegexpCount();
			}
			if (totalRegCount == 0)
			{
				return;
			}
			cfw.StartMethod(REGEXP_INIT_METHOD_NAME, REGEXP_INIT_METHOD_SIGNATURE, (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE));
			cfw.AddField("_reInitDone", "Z", (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE | ClassFileWriter.ACC_VOLATILE));
			cfw.Add(ByteCode.GETSTATIC, mainClassName, "_reInitDone", "Z");
			int doInit = cfw.AcquireLabel();
			cfw.Add(ByteCode.IFEQ, doInit);
			cfw.Add(ByteCode.RETURN);
			cfw.MarkLabel(doInit);
			// get regexp proxy and store it in local slot 1
			cfw.AddALoad(0);
			// context
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/ScriptRuntime", "checkRegExpProxy", "(Lorg/mozilla/javascript/Context;" + ")Lorg/mozilla/javascript/RegExpProxy;");
			cfw.AddAStore(1);
			// proxy
			// We could apply double-checked locking here but concurrency
			// shouldn't be a problem in practice
			for (int i_1 = 0; i_1 != scriptOrFnNodes.Length; ++i_1)
			{
				ScriptNode n = scriptOrFnNodes[i_1];
				int regCount = n.GetRegexpCount();
				for (int j = 0; j != regCount; ++j)
				{
					string reFieldName = GetCompiledRegexpName(n, j);
					string reFieldType = "Ljava/lang/Object;";
					string reString = n.GetRegexpString(j);
					string reFlags = n.GetRegexpFlags(j);
					cfw.AddField(reFieldName, reFieldType, (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE));
					cfw.AddALoad(1);
					// proxy
					cfw.AddALoad(0);
					// context
					cfw.AddPush(reString);
					if (reFlags == null)
					{
						cfw.Add(ByteCode.ACONST_NULL);
					}
					else
					{
						cfw.AddPush(reFlags);
					}
					cfw.AddInvoke(ByteCode.INVOKEINTERFACE, "org/mozilla/javascript/RegExpProxy", "compileRegExp", "(Lorg/mozilla/javascript/Context;" + "Ljava/lang/String;Ljava/lang/String;" + ")Ljava/lang/Object;");
					cfw.Add(ByteCode.PUTSTATIC, mainClassName, reFieldName, reFieldType);
				}
			}
			cfw.AddPush(1);
			cfw.Add(ByteCode.PUTSTATIC, mainClassName, "_reInitDone", "Z");
			cfw.Add(ByteCode.RETURN);
			cfw.StopMethod((short)2);
		}

		private void EmitConstantDudeInitializers(ClassFileWriter cfw)
		{
			int N = itsConstantListSize;
			if (N == 0)
			{
				return;
			}
			cfw.StartMethod("<clinit>", "()V", (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_FINAL));
			double[] array = itsConstantList;
			for (int i = 0; i != N; ++i)
			{
				double num = array[i];
				string constantName = "_k" + i;
				string constantType = GetStaticConstantWrapperType(num);
				cfw.AddField(constantName, constantType, (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE));
				int inum = (int)num;
				if (inum == num)
				{
					cfw.AddPush(inum);
					cfw.AddInvoke(ByteCode.INVOKESTATIC, "java/lang/Integer", "valueOf", "(I)Ljava/lang/Integer;");
				}
				else
				{
					cfw.AddPush(num);
					AddDoubleWrap(cfw);
				}
				cfw.Add(ByteCode.PUTSTATIC, mainClassName, constantName, constantType);
			}
			cfw.Add(ByteCode.RETURN);
			cfw.StopMethod((short)0);
		}

		internal virtual void PushNumberAsObject(ClassFileWriter cfw, double num)
		{
			if (num == 0.0)
			{
				if (1 / num > 0)
				{
					// +0.0
					cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/optimizer/OptRuntime", "zeroObj", "Ljava/lang/Double;");
				}
				else
				{
					cfw.AddPush(num);
					AddDoubleWrap(cfw);
				}
			}
			else
			{
				if (num == 1.0)
				{
					cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/optimizer/OptRuntime", "oneObj", "Ljava/lang/Double;");
					return;
				}
				else
				{
					if (num == -1.0)
					{
						cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/optimizer/OptRuntime", "minusOneObj", "Ljava/lang/Double;");
					}
					else
					{
						if (num != num)
						{
							cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/ScriptRuntime", "NaNobj", "Ljava/lang/Double;");
						}
						else
						{
							if (itsConstantListSize >= 2000)
							{
								// There appears to be a limit in the JVM on either the number
								// of static fields in a class or the size of the class
								// initializer. Either way, we can't have any more than 2000
								// statically init'd constants.
								cfw.AddPush(num);
								AddDoubleWrap(cfw);
							}
							else
							{
								int N = itsConstantListSize;
								int index = 0;
								if (N == 0)
								{
									itsConstantList = new double[64];
								}
								else
								{
									double[] array = itsConstantList;
									while (index != N && array[index] != num)
									{
										++index;
									}
									if (N == array.Length)
									{
										array = new double[N * 2];
										System.Array.Copy(itsConstantList, 0, array, 0, N);
										itsConstantList = array;
									}
								}
								if (index == N)
								{
									itsConstantList[N] = num;
									itsConstantListSize = N + 1;
								}
								string constantName = "_k" + index;
								string constantType = GetStaticConstantWrapperType(num);
								cfw.Add(ByteCode.GETSTATIC, mainClassName, constantName, constantType);
							}
						}
					}
				}
			}
		}

		private static void AddDoubleWrap(ClassFileWriter cfw)
		{
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/optimizer/OptRuntime", "wrapDouble", "(D)Ljava/lang/Double;");
		}

		private static string GetStaticConstantWrapperType(double num)
		{
			int inum = (int)num;
			if (inum == num)
			{
				return "Ljava/lang/Integer;";
			}
			else
			{
				return "Ljava/lang/Double;";
			}
		}

		internal static void PushUndefined(ClassFileWriter cfw)
		{
			cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/Undefined", "instance", "Ljava/lang/Object;");
		}

		internal virtual int GetIndex(ScriptNode n)
		{
			return scriptOrFnIndexes.GetExisting(n);
		}

		internal virtual string GetDirectCtorName(ScriptNode n)
		{
			return "_n" + GetIndex(n);
		}

		internal virtual string GetBodyMethodName(ScriptNode n)
		{
			return "_c_" + CleanName(n) + "_" + GetIndex(n);
		}

		/// <summary>Gets a Java-compatible "informative" name for the the ScriptOrFnNode</summary>
		internal virtual string CleanName(ScriptNode n)
		{
			string result = string.Empty;
			if (n is FunctionNode)
			{
				Name name = ((FunctionNode)n).GetFunctionName();
				if (name == null)
				{
					result = "anonymous";
				}
				else
				{
					result = name.GetIdentifier();
				}
			}
			else
			{
				result = "script";
			}
			return result;
		}

		internal virtual string GetBodyMethodSignature(ScriptNode n)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('(');
			sb.Append(mainClassSignature);
			sb.Append("Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;");
			if (n.GetType() == Token.FUNCTION)
			{
				OptFunctionNode ofn = OptFunctionNode.Get(n);
				if (ofn.IsTargetOfDirectCall())
				{
					int pCount = ofn.fnode.GetParamCount();
					for (int i = 0; i != pCount; i++)
					{
						sb.Append("Ljava/lang/Object;D");
					}
				}
			}
			sb.Append("[Ljava/lang/Object;)Ljava/lang/Object;");
			return sb.ToString();
		}

		internal virtual string GetFunctionInitMethodName(OptFunctionNode ofn)
		{
			return "_i" + GetIndex(ofn.fnode);
		}

		internal virtual string GetCompiledRegexpName(ScriptNode n, int regexpIndex)
		{
			return "_re" + GetIndex(n) + "_" + regexpIndex;
		}

		internal static Exception BadTree()
		{
			throw new Exception("Bad tree in codegen");
		}

		public virtual void SetMainMethodClass(string className)
		{
			mainMethodClass = className;
		}

		internal const string DEFAULT_MAIN_METHOD_CLASS = "Rhino.Optimizer.OptRuntime";

		private const string SUPER_CLASS_NAME = "Rhino.NativeFunction";

		internal const string ID_FIELD_NAME = "_id";

		internal const string REGEXP_INIT_METHOD_NAME = "_reInit";

		internal const string REGEXP_INIT_METHOD_SIGNATURE = "(Lorg/mozilla/javascript/Context;)V";

		internal const string FUNCTION_INIT_SIGNATURE = "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")V";

		internal const string FUNCTION_CONSTRUCTOR_SIGNATURE = "(Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Context;I)V";

		private static readonly object globalLock = new object();

		private static int globalSerialClassCounter;

		private CompilerEnvirons compilerEnv;

		private ObjArray directCallTargets;

		internal ScriptNode[] scriptOrFnNodes;

		private ObjToIntMap scriptOrFnIndexes;

		private string mainMethodClass = DEFAULT_MAIN_METHOD_CLASS;

		internal string mainClassName;

		internal string mainClassSignature;

		private double[] itsConstantList;

		private int itsConstantListSize;
	}

	internal class BodyCodegen
	{
		internal virtual void GenerateBodyCode()
		{
			isGenerator = Codegen.IsGenerator(scriptOrFn);
			// generate the body of the current function or script object
			InitBodyGeneration();
			if (isGenerator)
			{
				// All functions in the generated bytecode have a unique name. Every
				// generator has a unique prefix followed by _gen
				string type = "(" + codegen.mainClassSignature + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + "Ljava/lang/Object;I)Ljava/lang/Object;";
				cfw.StartMethod(codegen.GetBodyMethodName(scriptOrFn) + "_gen", type, (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE));
			}
			else
			{
				cfw.StartMethod(codegen.GetBodyMethodName(scriptOrFn), codegen.GetBodyMethodSignature(scriptOrFn), (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE));
			}
			GeneratePrologue();
			Node treeTop;
			if (fnCurrent != null)
			{
				treeTop = scriptOrFn.GetLastChild();
			}
			else
			{
				treeTop = scriptOrFn;
			}
			GenerateStatement(treeTop);
			GenerateEpilogue();
			cfw.StopMethod((short)(localsMax + 1));
			if (isGenerator)
			{
				// generate the user visible method which when invoked will
				// return a generator object
				GenerateGenerator();
			}
			if (literals != null)
			{
				// literals list may grow while we're looping
				for (int i = 0; i < literals.Count; i++)
				{
					Node node = literals[i];
					int type = node.GetType();
					switch (type)
					{
						case Token.OBJECTLIT:
						{
							GenerateObjectLiteralFactory(node, i + 1);
							break;
						}

						case Token.ARRAYLIT:
						{
							GenerateArrayLiteralFactory(node, i + 1);
							break;
						}

						default:
						{
							Kit.CodeBug(Token.TypeToName(type));
							break;
						}
					}
				}
			}
		}

		// This creates a the user-facing function that returns a NativeGenerator
		// object.
		private void GenerateGenerator()
		{
			cfw.StartMethod(codegen.GetBodyMethodName(scriptOrFn), codegen.GetBodyMethodSignature(scriptOrFn), (short)(ClassFileWriter.ACC_STATIC | ClassFileWriter.ACC_PRIVATE));
			InitBodyGeneration();
			argsLocal = firstFreeLocal++;
			localsMax = firstFreeLocal;
			// get top level scope
			if (fnCurrent != null)
			{
				// Unless we're in a direct call use the enclosing scope
				// of the function as our variable object.
				cfw.AddALoad(funObjLocal);
				cfw.AddInvoke(ByteCode.INVOKEINTERFACE, "org/mozilla/javascript/Scriptable", "getParentScope", "()Lorg/mozilla/javascript/Scriptable;");
				cfw.AddAStore(variableObjectLocal);
			}
			// generators are forced to have an activation record
			cfw.AddALoad(funObjLocal);
			cfw.AddALoad(variableObjectLocal);
			cfw.AddALoad(argsLocal);
			AddScriptRuntimeInvoke("createFunctionActivation", "(Lorg/mozilla/javascript/NativeFunction;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.AddAStore(variableObjectLocal);
			// create a function object
			cfw.Add(ByteCode.NEW, codegen.mainClassName);
			// Call function constructor
			cfw.Add(ByteCode.DUP);
			cfw.AddALoad(variableObjectLocal);
			cfw.AddALoad(contextLocal);
			// load 'cx'
			cfw.AddPush(scriptOrFnIndex);
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, codegen.mainClassName, "<init>", Codegen.FUNCTION_CONSTRUCTOR_SIGNATURE);
			GenerateNestedFunctionInits();
			// create the NativeGenerator object that we return
			cfw.AddALoad(variableObjectLocal);
			cfw.AddALoad(thisObjLocal);
			cfw.AddLoadConstant(maxLocals);
			cfw.AddLoadConstant(maxStack);
			AddOptRuntimeInvoke("createNativeGenerator", "(Lorg/mozilla/javascript/NativeFunction;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;II" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.Add(ByteCode.ARETURN);
			cfw.StopMethod((short)(localsMax + 1));
		}

		private void GenerateNestedFunctionInits()
		{
			int functionCount = scriptOrFn.GetFunctionCount();
			for (int i = 0; i != functionCount; i++)
			{
				OptFunctionNode ofn = OptFunctionNode.Get(scriptOrFn, i);
				if (ofn.fnode.GetFunctionType() == FunctionNode.FUNCTION_STATEMENT)
				{
					VisitFunction(ofn, FunctionNode.FUNCTION_STATEMENT);
				}
			}
		}

		private void InitBodyGeneration()
		{
			varRegisters = null;
			if (scriptOrFn.GetType() == Token.FUNCTION)
			{
				fnCurrent = OptFunctionNode.Get(scriptOrFn);
				hasVarsInRegs = !fnCurrent.fnode.RequiresActivation();
				if (hasVarsInRegs)
				{
					int n = fnCurrent.fnode.GetParamAndVarCount();
					if (n != 0)
					{
						varRegisters = new short[n];
					}
				}
				inDirectCallFunction = fnCurrent.IsTargetOfDirectCall();
				if (inDirectCallFunction && !hasVarsInRegs)
				{
					Codegen.BadTree();
				}
			}
			else
			{
				fnCurrent = null;
				hasVarsInRegs = false;
				inDirectCallFunction = false;
			}
			locals = new int[MAX_LOCALS];
			funObjLocal = 0;
			contextLocal = 1;
			variableObjectLocal = 2;
			thisObjLocal = 3;
			localsMax = (short)4;
			// number of parms + "this"
			firstFreeLocal = 4;
			popvLocal = -1;
			argsLocal = -1;
			itsZeroArgArray = -1;
			itsOneArgArray = -1;
			epilogueLabel = -1;
			enterAreaStartLabel = -1;
			generatorStateLocal = -1;
		}

		/// <summary>Generate the prologue for a function or script.</summary>
		/// <remarks>Generate the prologue for a function or script.</remarks>
		private void GeneratePrologue()
		{
			if (inDirectCallFunction)
			{
				int directParameterCount = scriptOrFn.GetParamCount();
				// 0 is reserved for function Object 'this'
				// 1 is reserved for context
				// 2 is reserved for parentScope
				// 3 is reserved for script 'this'
				if (firstFreeLocal != 4)
				{
					Kit.CodeBug();
				}
				for (int i = 0; i != directParameterCount; ++i)
				{
					varRegisters[i] = firstFreeLocal;
					// 3 is 1 for Object parm and 2 for double parm
					firstFreeLocal += 3;
				}
				if (!fnCurrent.GetParameterNumberContext())
				{
					// make sure that all parameters are objects
					itsForcedObjectParameters = true;
					for (int i_1 = 0; i_1 != directParameterCount; ++i_1)
					{
						short reg = varRegisters[i_1];
						cfw.AddALoad(reg);
						cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
						int isObjectLabel = cfw.AcquireLabel();
						cfw.Add(ByteCode.IF_ACMPNE, isObjectLabel);
						cfw.AddDLoad(reg + 1);
						AddDoubleWrap();
						cfw.AddAStore(reg);
						cfw.MarkLabel(isObjectLabel);
					}
				}
			}
			if (fnCurrent != null)
			{
				// Use the enclosing scope of the function as our variable object.
				cfw.AddALoad(funObjLocal);
				cfw.AddInvoke(ByteCode.INVOKEINTERFACE, "org/mozilla/javascript/Scriptable", "getParentScope", "()Lorg/mozilla/javascript/Scriptable;");
				cfw.AddAStore(variableObjectLocal);
			}
			// reserve 'args[]'
			argsLocal = firstFreeLocal++;
			localsMax = firstFreeLocal;
			// Generate Generator specific prelude
			if (isGenerator)
			{
				// reserve 'args[]'
				operationLocal = firstFreeLocal++;
				localsMax = firstFreeLocal;
				// Local 3 is a reference to a GeneratorState object. The rest
				// of codegen expects local 3 to be a reference to the thisObj.
				// So move the value in local 3 to generatorStateLocal, and load
				// the saved thisObj from the GeneratorState object.
				cfw.AddALoad(thisObjLocal);
				generatorStateLocal = firstFreeLocal++;
				localsMax = firstFreeLocal;
				cfw.Add(ByteCode.CHECKCAST, OptRuntime.GeneratorState.CLASS_NAME);
				cfw.Add(ByteCode.DUP);
				cfw.AddAStore(generatorStateLocal);
				cfw.Add(ByteCode.GETFIELD, OptRuntime.GeneratorState.CLASS_NAME, OptRuntime.GeneratorState.thisObj_NAME, OptRuntime.GeneratorState.thisObj_TYPE);
				cfw.AddAStore(thisObjLocal);
				if (epilogueLabel == -1)
				{
					epilogueLabel = cfw.AcquireLabel();
				}
				IList<Node> targets = ((FunctionNode)scriptOrFn).GetResumptionPoints();
				if (targets != null)
				{
					// get resumption point
					GenerateGetGeneratorResumptionPoint();
					// generate dispatch table
					generatorSwitch = cfw.AddTableSwitch(0, targets.Count + GENERATOR_START);
					GenerateCheckForThrowOrClose(-1, false, GENERATOR_START);
				}
			}
			// Compile RegExp literals if this is a script. For functions
			// this is performed during instantiation in functionInit
			if (fnCurrent == null && scriptOrFn.GetRegexpCount() != 0)
			{
				cfw.AddALoad(contextLocal);
				cfw.AddInvoke(ByteCode.INVOKESTATIC, codegen.mainClassName, Codegen.REGEXP_INIT_METHOD_NAME, Codegen.REGEXP_INIT_METHOD_SIGNATURE);
			}
			if (compilerEnv.IsGenerateObserverCount())
			{
				SaveCurrentCodeOffset();
			}
			if (hasVarsInRegs)
			{
				// No need to create activation. Pad arguments if need be.
				int parmCount = scriptOrFn.GetParamCount();
				if (parmCount > 0 && !inDirectCallFunction)
				{
					// Set up args array
					// check length of arguments, pad if need be
					cfw.AddALoad(argsLocal);
					cfw.Add(ByteCode.ARRAYLENGTH);
					cfw.AddPush(parmCount);
					int label = cfw.AcquireLabel();
					cfw.Add(ByteCode.IF_ICMPGE, label);
					cfw.AddALoad(argsLocal);
					cfw.AddPush(parmCount);
					AddScriptRuntimeInvoke("padArguments", "([Ljava/lang/Object;I" + ")[Ljava/lang/Object;");
					cfw.AddAStore(argsLocal);
					cfw.MarkLabel(label);
				}
				int paramCount = fnCurrent.fnode.GetParamCount();
				int varCount = fnCurrent.fnode.GetParamAndVarCount();
				bool[] constDeclarations = fnCurrent.fnode.GetParamAndVarConst();
				// REMIND - only need to initialize the vars that don't get a value
				// before the next call and are used in the function
				short firstUndefVar = -1;
				for (int i = 0; i != varCount; ++i)
				{
					short reg = -1;
					if (i < paramCount)
					{
						if (!inDirectCallFunction)
						{
							reg = GetNewWordLocal();
							cfw.AddALoad(argsLocal);
							cfw.AddPush(i);
							cfw.Add(ByteCode.AALOAD);
							cfw.AddAStore(reg);
						}
					}
					else
					{
						if (fnCurrent.IsNumberVar(i))
						{
							reg = GetNewWordPairLocal(constDeclarations[i]);
							cfw.AddPush(0.0);
							cfw.AddDStore(reg);
						}
						else
						{
							reg = GetNewWordLocal(constDeclarations[i]);
							if (firstUndefVar == -1)
							{
								Codegen.PushUndefined(cfw);
								firstUndefVar = reg;
							}
							else
							{
								cfw.AddALoad(firstUndefVar);
							}
							cfw.AddAStore(reg);
						}
					}
					if (reg >= 0)
					{
						if (constDeclarations[i])
						{
							cfw.AddPush(0);
							cfw.AddIStore(reg + (fnCurrent.IsNumberVar(i) ? 2 : 1));
						}
						varRegisters[i] = reg;
					}
					// Add debug table entry if we're generating debug info
					if (compilerEnv.IsGenerateDebugInfo())
					{
						string name = fnCurrent.fnode.GetParamOrVarName(i);
						string type = fnCurrent.IsNumberVar(i) ? "D" : "Ljava/lang/Object;";
						int startPC = cfw.GetCurrentCodeOffset();
						if (reg < 0)
						{
							reg = varRegisters[i];
						}
						cfw.AddVariableDescriptor(name, type, startPC, reg);
					}
				}
				// Skip creating activation object.
				return;
			}
			// skip creating activation object for the body of a generator. The
			// activation record required by a generator has already been created
			// in generateGenerator().
			if (isGenerator)
			{
				return;
			}
			string debugVariableName;
			if (fnCurrent != null)
			{
				debugVariableName = "activation";
				cfw.AddALoad(funObjLocal);
				cfw.AddALoad(variableObjectLocal);
				cfw.AddALoad(argsLocal);
				AddScriptRuntimeInvoke("createFunctionActivation", "(Lorg/mozilla/javascript/NativeFunction;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
				cfw.AddAStore(variableObjectLocal);
				cfw.AddALoad(contextLocal);
				cfw.AddALoad(variableObjectLocal);
				AddScriptRuntimeInvoke("enterActivationFunction", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")V");
			}
			else
			{
				debugVariableName = "global";
				cfw.AddALoad(funObjLocal);
				cfw.AddALoad(thisObjLocal);
				cfw.AddALoad(contextLocal);
				cfw.AddALoad(variableObjectLocal);
				cfw.AddPush(0);
				// false to indicate it is not eval script
				AddScriptRuntimeInvoke("initScript", "(Lorg/mozilla/javascript/NativeFunction;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Z" + ")V");
			}
			enterAreaStartLabel = cfw.AcquireLabel();
			epilogueLabel = cfw.AcquireLabel();
			cfw.MarkLabel(enterAreaStartLabel);
			GenerateNestedFunctionInits();
			// default is to generate debug info
			if (compilerEnv.IsGenerateDebugInfo())
			{
				cfw.AddVariableDescriptor(debugVariableName, "Lorg/mozilla/javascript/Scriptable;", cfw.GetCurrentCodeOffset(), variableObjectLocal);
			}
			if (fnCurrent == null)
			{
				// OPT: use dataflow to prove that this assignment is dead
				popvLocal = GetNewWordLocal();
				Codegen.PushUndefined(cfw);
				cfw.AddAStore(popvLocal);
				int linenum = scriptOrFn.GetEndLineno();
				if (linenum != -1)
				{
					cfw.AddLineNumberEntry((short)linenum);
				}
			}
			else
			{
				if (fnCurrent.itsContainsCalls0)
				{
					itsZeroArgArray = GetNewWordLocal();
					cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/ScriptRuntime", "emptyArgs", "[Ljava/lang/Object;");
					cfw.AddAStore(itsZeroArgArray);
				}
				if (fnCurrent.itsContainsCalls1)
				{
					itsOneArgArray = GetNewWordLocal();
					cfw.AddPush(1);
					cfw.Add(ByteCode.ANEWARRAY, "java/lang/Object");
					cfw.AddAStore(itsOneArgArray);
				}
			}
		}

		private void GenerateGetGeneratorResumptionPoint()
		{
			cfw.AddALoad(generatorStateLocal);
			cfw.Add(ByteCode.GETFIELD, OptRuntime.GeneratorState.CLASS_NAME, OptRuntime.GeneratorState.resumptionPoint_NAME, OptRuntime.GeneratorState.resumptionPoint_TYPE);
		}

		private void GenerateSetGeneratorResumptionPoint(int nextState)
		{
			cfw.AddALoad(generatorStateLocal);
			cfw.AddLoadConstant(nextState);
			cfw.Add(ByteCode.PUTFIELD, OptRuntime.GeneratorState.CLASS_NAME, OptRuntime.GeneratorState.resumptionPoint_NAME, OptRuntime.GeneratorState.resumptionPoint_TYPE);
		}

		private void GenerateGetGeneratorStackState()
		{
			cfw.AddALoad(generatorStateLocal);
			AddOptRuntimeInvoke("getGeneratorStackState", "(Ljava/lang/Object;)[Ljava/lang/Object;");
		}

		private void GenerateEpilogue()
		{
			if (compilerEnv.IsGenerateObserverCount())
			{
				AddInstructionCount();
			}
			if (isGenerator)
			{
				// generate locals initialization
				IDictionary<Node, int[]> liveLocals = ((FunctionNode)scriptOrFn).GetLiveLocals();
				if (liveLocals != null)
				{
					IList<Node> nodes = ((FunctionNode)scriptOrFn).GetResumptionPoints();
					for (int i = 0; i < nodes.Count; i++)
					{
						Node node = nodes[i];
						int[] live = liveLocals.Get(node);
						if (live != null)
						{
							cfw.MarkTableSwitchCase(generatorSwitch, GetNextGeneratorState(node));
							GenerateGetGeneratorLocalsState();
							for (int j = 0; j < live.Length; j++)
							{
								cfw.Add(ByteCode.DUP);
								cfw.AddLoadConstant(j);
								cfw.Add(ByteCode.AALOAD);
								cfw.AddAStore(live[j]);
							}
							cfw.Add(ByteCode.POP);
							cfw.Add(ByteCode.GOTO, GetTargetLabel(node));
						}
					}
				}
				// generate dispatch tables for finally
				if (finallys != null)
				{
					foreach (Node n in finallys.Keys)
					{
						if (n.GetType() == Token.FINALLY)
						{
							BodyCodegen.FinallyReturnPoint ret = finallys.Get(n);
							// the finally will jump here
							cfw.MarkLabel(ret.tableLabel, (short)1);
							// start generating a dispatch table
							int startSwitch = cfw.AddTableSwitch(0, ret.jsrPoints.Count - 1);
							int c = 0;
							cfw.MarkTableSwitchDefault(startSwitch);
							for (int i = 0; i < ret.jsrPoints.Count; i++)
							{
								// generate gotos back to the JSR location
								cfw.MarkTableSwitchCase(startSwitch, c);
								cfw.Add(ByteCode.GOTO, System.Convert.ToInt32(ret.jsrPoints[i]));
								c++;
							}
						}
					}
				}
			}
			if (epilogueLabel != -1)
			{
				cfw.MarkLabel(epilogueLabel);
			}
			if (hasVarsInRegs)
			{
				cfw.Add(ByteCode.ARETURN);
				return;
			}
			else
			{
				if (isGenerator)
				{
					if (((FunctionNode)scriptOrFn).GetResumptionPoints() != null)
					{
						cfw.MarkTableSwitchDefault(generatorSwitch);
					}
					// change state for re-entry
					GenerateSetGeneratorResumptionPoint(GENERATOR_TERMINATE);
					// throw StopIteration
					cfw.AddALoad(variableObjectLocal);
					AddOptRuntimeInvoke("throwStopIteration", "(Ljava/lang/Object;)V");
					Codegen.PushUndefined(cfw);
					cfw.Add(ByteCode.ARETURN);
				}
				else
				{
					if (fnCurrent == null)
					{
						cfw.AddALoad(popvLocal);
						cfw.Add(ByteCode.ARETURN);
					}
					else
					{
						GenerateActivationExit();
						cfw.Add(ByteCode.ARETURN);
						// Generate catch block to catch all and rethrow to call exit code
						// under exception propagation as well.
						int finallyHandler = cfw.AcquireLabel();
						cfw.MarkHandler(finallyHandler);
						short exceptionObject = GetNewWordLocal();
						cfw.AddAStore(exceptionObject);
						// Duplicate generateActivationExit() in the catch block since it
						// takes less space then full-featured ByteCode.JSR/ByteCode.RET
						GenerateActivationExit();
						cfw.AddALoad(exceptionObject);
						ReleaseWordLocal(exceptionObject);
						// rethrow
						cfw.Add(ByteCode.ATHROW);
						// mark the handler
						cfw.AddExceptionHandler(enterAreaStartLabel, epilogueLabel, finallyHandler, null);
					}
				}
			}
		}

		// catch any
		private void GenerateGetGeneratorLocalsState()
		{
			cfw.AddALoad(generatorStateLocal);
			AddOptRuntimeInvoke("getGeneratorLocalsState", "(Ljava/lang/Object;)[Ljava/lang/Object;");
		}

		private void GenerateActivationExit()
		{
			if (fnCurrent == null || hasVarsInRegs)
			{
				throw Kit.CodeBug();
			}
			cfw.AddALoad(contextLocal);
			AddScriptRuntimeInvoke("exitActivationFunction", "(Lorg/mozilla/javascript/Context;)V");
		}

		private void GenerateStatement(Node node)
		{
			UpdateLineNumber(node);
			int type = node.GetType();
			Node child = node.GetFirstChild();
			switch (type)
			{
				case Token.LOOP:
				case Token.LABEL:
				case Token.WITH:
				case Token.SCRIPT:
				case Token.BLOCK:
				case Token.EMPTY:
				{
					// no-ops.
					if (compilerEnv.IsGenerateObserverCount())
					{
						// Need to add instruction count even for no-ops to catch
						// cases like while (1) {}
						AddInstructionCount(1);
					}
					while (child != null)
					{
						GenerateStatement(child);
						child = child.GetNext();
					}
					break;
				}

				case Token.LOCAL_BLOCK:
				{
					bool prevLocal = inLocalBlock;
					inLocalBlock = true;
					int local = GetNewWordLocal();
					if (isGenerator)
					{
						cfw.Add(ByteCode.ACONST_NULL);
						cfw.AddAStore(local);
					}
					node.PutIntProp(Node.LOCAL_PROP, local);
					while (child != null)
					{
						GenerateStatement(child);
						child = child.GetNext();
					}
					ReleaseWordLocal((short)local);
					node.RemoveProp(Node.LOCAL_PROP);
					inLocalBlock = prevLocal;
					break;
				}

				case Token.FUNCTION:
				{
					int fnIndex = node.GetExistingIntProp(Node.FUNCTION_PROP);
					OptFunctionNode ofn = OptFunctionNode.Get(scriptOrFn, fnIndex);
					int t = ofn.fnode.GetFunctionType();
					if (t == FunctionNode.FUNCTION_EXPRESSION_STATEMENT)
					{
						VisitFunction(ofn, t);
					}
					else
					{
						if (t != FunctionNode.FUNCTION_STATEMENT)
						{
							throw Codegen.BadTree();
						}
					}
					break;
				}

				case Token.TRY:
				{
					VisitTryCatchFinally((Jump)node, child);
					break;
				}

				case Token.CATCH_SCOPE:
				{
					// nothing stays on the stack on entry into a catch scope
					cfw.SetStackTop((short)0);
					int local = GetLocalBlockRegister(node);
					int scopeIndex = node.GetExistingIntProp(Node.CATCH_SCOPE_PROP);
					string name = child.GetString();
					// name of exception
					child = child.GetNext();
					GenerateExpression(child, node);
					// load expression object
					if (scopeIndex == 0)
					{
						cfw.Add(ByteCode.ACONST_NULL);
					}
					else
					{
						// Load previous catch scope object
						cfw.AddALoad(local);
					}
					cfw.AddPush(name);
					cfw.AddALoad(contextLocal);
					cfw.AddALoad(variableObjectLocal);
					AddScriptRuntimeInvoke("newCatchScope", "(Ljava/lang/Throwable;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
					cfw.AddAStore(local);
					break;
				}

				case Token.THROW:
				{
					GenerateExpression(child, node);
					if (compilerEnv.IsGenerateObserverCount())
					{
						AddInstructionCount();
					}
					GenerateThrowJavaScriptException();
					break;
				}

				case Token.RETHROW:
				{
					if (compilerEnv.IsGenerateObserverCount())
					{
						AddInstructionCount();
					}
					cfw.AddALoad(GetLocalBlockRegister(node));
					cfw.Add(ByteCode.ATHROW);
					break;
				}

				case Token.RETURN_RESULT:
				case Token.RETURN:
				{
					if (!isGenerator)
					{
						if (child != null)
						{
							GenerateExpression(child, node);
						}
						else
						{
							if (type == Token.RETURN)
							{
								Codegen.PushUndefined(cfw);
							}
							else
							{
								if (popvLocal < 0)
								{
									throw Codegen.BadTree();
								}
								cfw.AddALoad(popvLocal);
							}
						}
					}
					if (compilerEnv.IsGenerateObserverCount())
					{
						AddInstructionCount();
					}
					if (epilogueLabel == -1)
					{
						if (!hasVarsInRegs)
						{
							throw Codegen.BadTree();
						}
						epilogueLabel = cfw.AcquireLabel();
					}
					cfw.Add(ByteCode.GOTO, epilogueLabel);
					break;
				}

				case Token.SWITCH:
				{
					if (compilerEnv.IsGenerateObserverCount())
					{
						AddInstructionCount();
					}
					VisitSwitch((Jump)node, child);
					break;
				}

				case Token.ENTERWITH:
				{
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					cfw.AddALoad(variableObjectLocal);
					AddScriptRuntimeInvoke("enterWith", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
					cfw.AddAStore(variableObjectLocal);
					IncReferenceWordLocal(variableObjectLocal);
					break;
				}

				case Token.LEAVEWITH:
				{
					cfw.AddALoad(variableObjectLocal);
					AddScriptRuntimeInvoke("leaveWith", "(Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
					cfw.AddAStore(variableObjectLocal);
					DecReferenceWordLocal(variableObjectLocal);
					break;
				}

				case Token.ENUM_INIT_KEYS:
				case Token.ENUM_INIT_VALUES:
				case Token.ENUM_INIT_ARRAY:
				{
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					int enumType = type == Token.ENUM_INIT_KEYS ? ScriptRuntime.ENUMERATE_KEYS : type == Token.ENUM_INIT_VALUES ? ScriptRuntime.ENUMERATE_VALUES : ScriptRuntime.ENUMERATE_ARRAY;
					cfw.AddPush(enumType);
					AddScriptRuntimeInvoke("enumInit", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "I" + ")Ljava/lang/Object;");
					cfw.AddAStore(GetLocalBlockRegister(node));
					break;
				}

				case Token.EXPR_VOID:
				{
					if (child.GetType() == Token.SETVAR)
					{
						VisitSetVar(child, child.GetFirstChild(), false);
					}
					else
					{
						if (child.GetType() == Token.SETCONSTVAR)
						{
							VisitSetConstVar(child, child.GetFirstChild(), false);
						}
						else
						{
							if (child.GetType() == Token.YIELD)
							{
								GenerateYieldPoint(child, false);
							}
							else
							{
								GenerateExpression(child, node);
								if (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1)
								{
									cfw.Add(ByteCode.POP2);
								}
								else
								{
									cfw.Add(ByteCode.POP);
								}
							}
						}
					}
					break;
				}

				case Token.EXPR_RESULT:
				{
					GenerateExpression(child, node);
					if (popvLocal < 0)
					{
						popvLocal = GetNewWordLocal();
					}
					cfw.AddAStore(popvLocal);
					break;
				}

				case Token.TARGET:
				{
					if (compilerEnv.IsGenerateObserverCount())
					{
						AddInstructionCount();
					}
					int label = GetTargetLabel(node);
					cfw.MarkLabel(label);
					if (compilerEnv.IsGenerateObserverCount())
					{
						SaveCurrentCodeOffset();
					}
					break;
				}

				case Token.JSR:
				case Token.GOTO:
				case Token.IFEQ:
				case Token.IFNE:
				{
					if (compilerEnv.IsGenerateObserverCount())
					{
						AddInstructionCount();
					}
					VisitGoto((Jump)node, type, child);
					break;
				}

				case Token.FINALLY:
				{
					// This is the non-exception case for a finally block. In
					// other words, since we inline finally blocks wherever
					// jsr was previously used, and jsr is only used when the
					// function is not a generator, we don't need to generate
					// this case if the function isn't a generator.
					if (!isGenerator)
					{
						break;
					}
					if (compilerEnv.IsGenerateObserverCount())
					{
						SaveCurrentCodeOffset();
					}
					// there is exactly one value on the stack when enterring
					// finally blocks: the return address (or its int encoding)
					cfw.SetStackTop((short)1);
					// Save return address in a new local
					int finallyRegister = GetNewWordLocal();
					int finallyStart = cfw.AcquireLabel();
					int finallyEnd = cfw.AcquireLabel();
					cfw.MarkLabel(finallyStart);
					GenerateIntegerWrap();
					cfw.AddAStore(finallyRegister);
					while (child != null)
					{
						GenerateStatement(child);
						child = child.GetNext();
					}
					cfw.AddALoad(finallyRegister);
					cfw.Add(ByteCode.CHECKCAST, "java/lang/Integer");
					GenerateIntegerUnwrap();
					BodyCodegen.FinallyReturnPoint ret = finallys.Get(node);
					ret.tableLabel = cfw.AcquireLabel();
					cfw.Add(ByteCode.GOTO, ret.tableLabel);
					ReleaseWordLocal((short)finallyRegister);
					cfw.MarkLabel(finallyEnd);
					break;
				}

				case Token.DEBUGGER:
				{
					break;
				}

				default:
				{
					throw Codegen.BadTree();
				}
			}
		}

		private void GenerateIntegerWrap()
		{
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "java/lang/Integer", "valueOf", "(I)Ljava/lang/Integer;");
		}

		private void GenerateIntegerUnwrap()
		{
			cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, "java/lang/Integer", "intValue", "()I");
		}

		private void GenerateThrowJavaScriptException()
		{
			cfw.Add(ByteCode.NEW, "org/mozilla/javascript/JavaScriptException");
			cfw.Add(ByteCode.DUP_X1);
			cfw.Add(ByteCode.SWAP);
			cfw.AddPush(scriptOrFn.GetSourceName());
			cfw.AddPush(itsLineNumber);
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, "org/mozilla/javascript/JavaScriptException", "<init>", "(Ljava/lang/Object;Ljava/lang/String;I)V");
			cfw.Add(ByteCode.ATHROW);
		}

		private int GetNextGeneratorState(Node node)
		{
			int nodeIndex = ((FunctionNode)scriptOrFn).GetResumptionPoints().IndexOf(node);
			return nodeIndex + GENERATOR_YIELD_START;
		}

		private void GenerateExpression(Node node, Node parent)
		{
			int type = node.GetType();
			Node child = node.GetFirstChild();
			switch (type)
			{
				case Token.USE_STACK:
				{
					break;
				}

				case Token.FUNCTION:
				{
					if (fnCurrent != null || parent.GetType() != Token.SCRIPT)
					{
						int fnIndex = node.GetExistingIntProp(Node.FUNCTION_PROP);
						OptFunctionNode ofn = OptFunctionNode.Get(scriptOrFn, fnIndex);
						int t = ofn.fnode.GetFunctionType();
						if (t != FunctionNode.FUNCTION_EXPRESSION)
						{
							throw Codegen.BadTree();
						}
						VisitFunction(ofn, t);
					}
					break;
				}

				case Token.NAME:
				{
					cfw.AddALoad(contextLocal);
					cfw.AddALoad(variableObjectLocal);
					cfw.AddPush(node.GetString());
					AddScriptRuntimeInvoke("name", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + ")Ljava/lang/Object;");
					break;
				}

				case Token.CALL:
				case Token.NEW:
				{
					int specialType = node.GetIntProp(Node.SPECIALCALL_PROP, Node.NON_SPECIALCALL);
					if (specialType == Node.NON_SPECIALCALL)
					{
						OptFunctionNode target;
						target = (OptFunctionNode)node.GetProp(Node.DIRECTCALL_PROP);
						if (target != null)
						{
							VisitOptimizedCall(node, target, type, child);
						}
						else
						{
							if (type == Token.CALL)
							{
								VisitStandardCall(node, child);
							}
							else
							{
								VisitStandardNew(node, child);
							}
						}
					}
					else
					{
						VisitSpecialCall(node, type, specialType, child);
					}
					break;
				}

				case Token.REF_CALL:
				{
					GenerateFunctionAndThisObj(child, node);
					// stack: ... functionObj thisObj
					child = child.GetNext();
					GenerateCallArgArray(node, child, false);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("callRef", "(Lorg/mozilla/javascript/Callable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Lorg/mozilla/javascript/Ref;");
					break;
				}

				case Token.NUMBER:
				{
					double num = node.GetDouble();
					if (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1)
					{
						cfw.AddPush(num);
					}
					else
					{
						codegen.PushNumberAsObject(cfw, num);
					}
					break;
				}

				case Token.STRING:
				{
					cfw.AddPush(node.GetString());
					break;
				}

				case Token.THIS:
				{
					cfw.AddALoad(thisObjLocal);
					break;
				}

				case Token.THISFN:
				{
					cfw.Add(ByteCode.ALOAD_0);
					break;
				}

				case Token.NULL:
				{
					cfw.Add(ByteCode.ACONST_NULL);
					break;
				}

				case Token.TRUE:
				{
					cfw.Add(ByteCode.GETSTATIC, "java/lang/Boolean", "TRUE", "Ljava/lang/Boolean;");
					break;
				}

				case Token.FALSE:
				{
					cfw.Add(ByteCode.GETSTATIC, "java/lang/Boolean", "FALSE", "Ljava/lang/Boolean;");
					break;
				}

				case Token.REGEXP:
				{
					// Create a new wrapper around precompiled regexp
					cfw.AddALoad(contextLocal);
					cfw.AddALoad(variableObjectLocal);
					int i = node.GetExistingIntProp(Node.REGEXP_PROP);
					cfw.Add(ByteCode.GETSTATIC, codegen.mainClassName, codegen.GetCompiledRegexpName(scriptOrFn, i), "Ljava/lang/Object;");
					cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/ScriptRuntime", "wrapRegExp", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
					break;
				}

				case Token.COMMA:
				{
					Node next = child.GetNext();
					while (next != null)
					{
						GenerateExpression(child, node);
						cfw.Add(ByteCode.POP);
						child = next;
						next = next.GetNext();
					}
					GenerateExpression(child, node);
					break;
				}

				case Token.ENUM_NEXT:
				case Token.ENUM_ID:
				{
					int local = GetLocalBlockRegister(node);
					cfw.AddALoad(local);
					if (type == Token.ENUM_NEXT)
					{
						AddScriptRuntimeInvoke("enumNext", "(Ljava/lang/Object;)Ljava/lang/Boolean;");
					}
					else
					{
						cfw.AddALoad(contextLocal);
						AddScriptRuntimeInvoke("enumId", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
					}
					break;
				}

				case Token.ARRAYLIT:
				{
					VisitArrayLiteral(node, child, false);
					break;
				}

				case Token.OBJECTLIT:
				{
					VisitObjectLiteral(node, child, false);
					break;
				}

				case Token.NOT:
				{
					int trueTarget = cfw.AcquireLabel();
					int falseTarget = cfw.AcquireLabel();
					int beyond = cfw.AcquireLabel();
					GenerateIfJump(child, node, trueTarget, falseTarget);
					cfw.MarkLabel(trueTarget);
					cfw.Add(ByteCode.GETSTATIC, "java/lang/Boolean", "FALSE", "Ljava/lang/Boolean;");
					cfw.Add(ByteCode.GOTO, beyond);
					cfw.MarkLabel(falseTarget);
					cfw.Add(ByteCode.GETSTATIC, "java/lang/Boolean", "TRUE", "Ljava/lang/Boolean;");
					cfw.MarkLabel(beyond);
					cfw.AdjustStackTop(-1);
					break;
				}

				case Token.BITNOT:
				{
					GenerateExpression(child, node);
					AddScriptRuntimeInvoke("toInt32", "(Ljava/lang/Object;)I");
					cfw.AddPush(-1);
					// implement ~a as (a ^ -1)
					cfw.Add(ByteCode.IXOR);
					cfw.Add(ByteCode.I2D);
					AddDoubleWrap();
					break;
				}

				case Token.VOID:
				{
					GenerateExpression(child, node);
					cfw.Add(ByteCode.POP);
					Codegen.PushUndefined(cfw);
					break;
				}

				case Token.TYPEOF:
				{
					GenerateExpression(child, node);
					AddScriptRuntimeInvoke("typeof", "(Ljava/lang/Object;" + ")Ljava/lang/String;");
					break;
				}

				case Token.TYPEOFNAME:
				{
					VisitTypeofname(node);
					break;
				}

				case Token.INC:
				case Token.DEC:
				{
					VisitIncDec(node);
					break;
				}

				case Token.OR:
				case Token.AND:
				{
					GenerateExpression(child, node);
					cfw.Add(ByteCode.DUP);
					AddScriptRuntimeInvoke("toBoolean", "(Ljava/lang/Object;)Z");
					int falseTarget = cfw.AcquireLabel();
					if (type == Token.AND)
					{
						cfw.Add(ByteCode.IFEQ, falseTarget);
					}
					else
					{
						cfw.Add(ByteCode.IFNE, falseTarget);
					}
					cfw.Add(ByteCode.POP);
					GenerateExpression(child.GetNext(), node);
					cfw.MarkLabel(falseTarget);
					break;
				}

				case Token.HOOK:
				{
					Node ifThen = child.GetNext();
					Node ifElse = ifThen.GetNext();
					GenerateExpression(child, node);
					AddScriptRuntimeInvoke("toBoolean", "(Ljava/lang/Object;)Z");
					int elseTarget = cfw.AcquireLabel();
					cfw.Add(ByteCode.IFEQ, elseTarget);
					short stack = cfw.GetStackTop();
					GenerateExpression(ifThen, node);
					int afterHook = cfw.AcquireLabel();
					cfw.Add(ByteCode.GOTO, afterHook);
					cfw.MarkLabel(elseTarget, stack);
					GenerateExpression(ifElse, node);
					cfw.MarkLabel(afterHook);
					break;
				}

				case Token.ADD:
				{
					GenerateExpression(child, node);
					GenerateExpression(child.GetNext(), node);
					switch (node.GetIntProp(Node.ISNUMBER_PROP, -1))
					{
						case Node.BOTH:
						{
							cfw.Add(ByteCode.DADD);
							break;
						}

						case Node.LEFT:
						{
							AddOptRuntimeInvoke("add", "(DLjava/lang/Object;)Ljava/lang/Object;");
							break;
						}

						case Node.RIGHT:
						{
							AddOptRuntimeInvoke("add", "(Ljava/lang/Object;D)Ljava/lang/Object;");
							break;
						}

						default:
						{
							if (child.GetType() == Token.STRING)
							{
								AddScriptRuntimeInvoke("add", "(Ljava/lang/CharSequence;" + "Ljava/lang/Object;" + ")Ljava/lang/CharSequence;");
							}
							else
							{
								if (child.GetNext().GetType() == Token.STRING)
								{
									AddScriptRuntimeInvoke("add", "(Ljava/lang/Object;" + "Ljava/lang/CharSequence;" + ")Ljava/lang/CharSequence;");
								}
								else
								{
									cfw.AddALoad(contextLocal);
									AddScriptRuntimeInvoke("add", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
								}
							}
							break;
						}
					}
					break;
				}

				case Token.MUL:
				{
					VisitArithmetic(node, ByteCode.DMUL, child, parent);
					break;
				}

				case Token.SUB:
				{
					VisitArithmetic(node, ByteCode.DSUB, child, parent);
					break;
				}

				case Token.DIV:
				case Token.MOD:
				{
					VisitArithmetic(node, type == Token.DIV ? ByteCode.DDIV : ByteCode.DREM, child, parent);
					break;
				}

				case Token.BITOR:
				case Token.BITXOR:
				case Token.BITAND:
				case Token.LSH:
				case Token.RSH:
				case Token.URSH:
				{
					VisitBitOp(node, type, child);
					break;
				}

				case Token.POS:
				case Token.NEG:
				{
					GenerateExpression(child, node);
					AddObjectToDouble();
					if (type == Token.NEG)
					{
						cfw.Add(ByteCode.DNEG);
					}
					AddDoubleWrap();
					break;
				}

				case Token.TO_DOUBLE:
				{
					// cnvt to double (not Double)
					GenerateExpression(child, node);
					AddObjectToDouble();
					break;
				}

				case Token.TO_OBJECT:
				{
					// convert from double
					int prop = -1;
					if (child.GetType() == Token.NUMBER)
					{
						prop = child.GetIntProp(Node.ISNUMBER_PROP, -1);
					}
					if (prop != -1)
					{
						child.RemoveProp(Node.ISNUMBER_PROP);
						GenerateExpression(child, node);
						child.PutIntProp(Node.ISNUMBER_PROP, prop);
					}
					else
					{
						GenerateExpression(child, node);
						AddDoubleWrap();
					}
					break;
				}

				case Token.IN:
				case Token.INSTANCEOF:
				case Token.LE:
				case Token.LT:
				case Token.GE:
				case Token.GT:
				{
					int trueGOTO = cfw.AcquireLabel();
					int falseGOTO = cfw.AcquireLabel();
					VisitIfJumpRelOp(node, child, trueGOTO, falseGOTO);
					AddJumpedBooleanWrap(trueGOTO, falseGOTO);
					break;
				}

				case Token.EQ:
				case Token.NE:
				case Token.SHEQ:
				case Token.SHNE:
				{
					int trueGOTO = cfw.AcquireLabel();
					int falseGOTO = cfw.AcquireLabel();
					VisitIfJumpEqOp(node, child, trueGOTO, falseGOTO);
					AddJumpedBooleanWrap(trueGOTO, falseGOTO);
					break;
				}

				case Token.GETPROP:
				case Token.GETPROPNOWARN:
				{
					VisitGetProp(node, child);
					break;
				}

				case Token.GETELEM:
				{
					GenerateExpression(child, node);
					// object
					GenerateExpression(child.GetNext(), node);
					// id
					cfw.AddALoad(contextLocal);
					if (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1)
					{
						AddScriptRuntimeInvoke("getObjectIndex", "(Ljava/lang/Object;D" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
					}
					else
					{
						cfw.AddALoad(variableObjectLocal);
						AddScriptRuntimeInvoke("getObjectElem", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;");
					}
					break;
				}

				case Token.GET_REF:
				{
					GenerateExpression(child, node);
					// reference
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("refGet", "(Lorg/mozilla/javascript/Ref;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
					break;
				}

				case Token.GETVAR:
				{
					VisitGetVar(node);
					break;
				}

				case Token.SETVAR:
				{
					VisitSetVar(node, child, true);
					break;
				}

				case Token.SETNAME:
				{
					VisitSetName(node, child);
					break;
				}

				case Token.STRICT_SETNAME:
				{
					VisitStrictSetName(node, child);
					break;
				}

				case Token.SETCONST:
				{
					VisitSetConst(node, child);
					break;
				}

				case Token.SETCONSTVAR:
				{
					VisitSetConstVar(node, child, true);
					break;
				}

				case Token.SETPROP:
				case Token.SETPROP_OP:
				{
					VisitSetProp(type, node, child);
					break;
				}

				case Token.SETELEM:
				case Token.SETELEM_OP:
				{
					VisitSetElem(type, node, child);
					break;
				}

				case Token.SET_REF:
				case Token.SET_REF_OP:
				{
					GenerateExpression(child, node);
					child = child.GetNext();
					if (type == Token.SET_REF_OP)
					{
						cfw.Add(ByteCode.DUP);
						cfw.AddALoad(contextLocal);
						AddScriptRuntimeInvoke("refGet", "(Lorg/mozilla/javascript/Ref;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
					}
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("refSet", "(Lorg/mozilla/javascript/Ref;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
					break;
				}

				case Token.DEL_REF:
				{
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("refDel", "(Lorg/mozilla/javascript/Ref;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
					break;
				}

				case Token.DELPROP:
				{
					bool isName = child.GetType() == Token.BINDNAME;
					GenerateExpression(child, node);
					child = child.GetNext();
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					cfw.AddPush(isName);
					AddScriptRuntimeInvoke("delete", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Z)Ljava/lang/Object;");
					break;
				}

				case Token.BINDNAME:
				{
					while (child != null)
					{
						GenerateExpression(child, node);
						child = child.GetNext();
					}
					// Generate code for "ScriptRuntime.bind(varObj, "s")"
					cfw.AddALoad(contextLocal);
					cfw.AddALoad(variableObjectLocal);
					cfw.AddPush(node.GetString());
					AddScriptRuntimeInvoke("bind", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + ")Lorg/mozilla/javascript/Scriptable;");
					break;
				}

				case Token.LOCAL_LOAD:
				{
					cfw.AddALoad(GetLocalBlockRegister(node));
					break;
				}

				case Token.REF_SPECIAL:
				{
					string special = (string)node.GetProp(Node.NAME_PROP);
					GenerateExpression(child, node);
					cfw.AddPush(special);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("specialRef", "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + ")Lorg/mozilla/javascript/Ref;");
					break;
				}

				case Token.REF_MEMBER:
				case Token.REF_NS_MEMBER:
				case Token.REF_NAME:
				case Token.REF_NS_NAME:
				{
					int memberTypeFlags = node.GetIntProp(Node.MEMBER_TYPE_PROP, 0);
					do
					{
						// generate possible target, possible namespace and member
						GenerateExpression(child, node);
						child = child.GetNext();
					}
					while (child != null);
					cfw.AddALoad(contextLocal);
					string methodName;
					string signature;
					switch (type)
					{
						case Token.REF_MEMBER:
						{
							methodName = "memberRef";
							signature = "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "I" + ")Lorg/mozilla/javascript/Ref;";
							break;
						}

						case Token.REF_NS_MEMBER:
						{
							methodName = "memberRef";
							signature = "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "I" + ")Lorg/mozilla/javascript/Ref;";
							break;
						}

						case Token.REF_NAME:
						{
							methodName = "nameRef";
							signature = "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "I" + ")Lorg/mozilla/javascript/Ref;";
							cfw.AddALoad(variableObjectLocal);
							break;
						}

						case Token.REF_NS_NAME:
						{
							methodName = "nameRef";
							signature = "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "I" + ")Lorg/mozilla/javascript/Ref;";
							cfw.AddALoad(variableObjectLocal);
							break;
						}

						default:
						{
							throw Kit.CodeBug();
						}
					}
					cfw.AddPush(memberTypeFlags);
					AddScriptRuntimeInvoke(methodName, signature);
					break;
				}

				case Token.DOTQUERY:
				{
					VisitDotQuery(node, child);
					break;
				}

				case Token.ESCXMLATTR:
				{
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("escapeAttributeValue", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/String;");
					break;
				}

				case Token.ESCXMLTEXT:
				{
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("escapeTextValue", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/String;");
					break;
				}

				case Token.DEFAULTNAMESPACE:
				{
					GenerateExpression(child, node);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("setDefaultNamespace", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
					break;
				}

				case Token.YIELD:
				{
					GenerateYieldPoint(node, true);
					break;
				}

				case Token.WITHEXPR:
				{
					Node enterWith = child;
					Node with = enterWith.GetNext();
					Node leaveWith = with.GetNext();
					GenerateStatement(enterWith);
					GenerateExpression(with.GetFirstChild(), with);
					GenerateStatement(leaveWith);
					break;
				}

				case Token.ARRAYCOMP:
				{
					Node initStmt = child;
					Node expr = child.GetNext();
					GenerateStatement(initStmt);
					GenerateExpression(expr, node);
					break;
				}

				default:
				{
					throw new Exception("Unexpected node type " + type);
				}
			}
		}

		private void GenerateYieldPoint(Node node, bool exprContext)
		{
			// save stack state
			int top = cfw.GetStackTop();
			maxStack = maxStack > top ? maxStack : top;
			if (cfw.GetStackTop() != 0)
			{
				GenerateGetGeneratorStackState();
				for (int i = 0; i < top; i++)
				{
					cfw.Add(ByteCode.DUP_X1);
					cfw.Add(ByteCode.SWAP);
					cfw.AddLoadConstant(i);
					cfw.Add(ByteCode.SWAP);
					cfw.Add(ByteCode.AASTORE);
				}
				// pop the array object
				cfw.Add(ByteCode.POP);
			}
			// generate the yield argument
			Node child = node.GetFirstChild();
			if (child != null)
			{
				GenerateExpression(child, node);
			}
			else
			{
				Codegen.PushUndefined(cfw);
			}
			// change the resumption state
			int nextState = GetNextGeneratorState(node);
			GenerateSetGeneratorResumptionPoint(nextState);
			bool hasLocals = GenerateSaveLocals(node);
			cfw.Add(ByteCode.ARETURN);
			GenerateCheckForThrowOrClose(GetTargetLabel(node), hasLocals, nextState);
			// reconstruct the stack
			if (top != 0)
			{
				GenerateGetGeneratorStackState();
				for (int i = 0; i < top; i++)
				{
					cfw.Add(ByteCode.DUP);
					cfw.AddLoadConstant(top - i - 1);
					cfw.Add(ByteCode.AALOAD);
					cfw.Add(ByteCode.SWAP);
				}
				cfw.Add(ByteCode.POP);
			}
			// load return value from yield
			if (exprContext)
			{
				cfw.AddALoad(argsLocal);
			}
		}

		private void GenerateCheckForThrowOrClose(int label, bool hasLocals, int nextState)
		{
			int throwLabel = cfw.AcquireLabel();
			int closeLabel = cfw.AcquireLabel();
			// throw the user provided object, if the operation is .throw()
			cfw.MarkLabel(throwLabel);
			cfw.AddALoad(argsLocal);
			GenerateThrowJavaScriptException();
			// throw our special internal exception if the generator is being closed
			cfw.MarkLabel(closeLabel);
			cfw.AddALoad(argsLocal);
			cfw.Add(ByteCode.CHECKCAST, "java/lang/Throwable");
			cfw.Add(ByteCode.ATHROW);
			// mark the re-entry point
			// jump here after initializing the locals
			if (label != -1)
			{
				cfw.MarkLabel(label);
			}
			if (!hasLocals)
			{
				// jump here directly if there are no locals
				cfw.MarkTableSwitchCase(generatorSwitch, nextState);
			}
			// see if we need to dispatch for .close() or .throw()
			cfw.AddILoad(operationLocal);
			cfw.AddLoadConstant(NativeGenerator.GENERATOR_CLOSE);
			cfw.Add(ByteCode.IF_ICMPEQ, closeLabel);
			cfw.AddILoad(operationLocal);
			cfw.AddLoadConstant(NativeGenerator.GENERATOR_THROW);
			cfw.Add(ByteCode.IF_ICMPEQ, throwLabel);
		}

		private void GenerateIfJump(Node node, Node parent, int trueLabel, int falseLabel)
		{
			// System.out.println("gen code for " + node.toString());
			int type = node.GetType();
			Node child = node.GetFirstChild();
			switch (type)
			{
				case Token.NOT:
				{
					GenerateIfJump(child, node, falseLabel, trueLabel);
					break;
				}

				case Token.OR:
				case Token.AND:
				{
					int interLabel = cfw.AcquireLabel();
					if (type == Token.AND)
					{
						GenerateIfJump(child, node, interLabel, falseLabel);
					}
					else
					{
						GenerateIfJump(child, node, trueLabel, interLabel);
					}
					cfw.MarkLabel(interLabel);
					child = child.GetNext();
					GenerateIfJump(child, node, trueLabel, falseLabel);
					break;
				}

				case Token.IN:
				case Token.INSTANCEOF:
				case Token.LE:
				case Token.LT:
				case Token.GE:
				case Token.GT:
				{
					VisitIfJumpRelOp(node, child, trueLabel, falseLabel);
					break;
				}

				case Token.EQ:
				case Token.NE:
				case Token.SHEQ:
				case Token.SHNE:
				{
					VisitIfJumpEqOp(node, child, trueLabel, falseLabel);
					break;
				}

				default:
				{
					// Generate generic code for non-optimized jump
					GenerateExpression(node, parent);
					AddScriptRuntimeInvoke("toBoolean", "(Ljava/lang/Object;)Z");
					cfw.Add(ByteCode.IFNE, trueLabel);
					cfw.Add(ByteCode.GOTO, falseLabel);
					break;
				}
			}
		}

		private void VisitFunction(OptFunctionNode ofn, int functionType)
		{
			int fnIndex = codegen.GetIndex(ofn.fnode);
			cfw.Add(ByteCode.NEW, codegen.mainClassName);
			// Call function constructor
			cfw.Add(ByteCode.DUP);
			cfw.AddALoad(variableObjectLocal);
			cfw.AddALoad(contextLocal);
			// load 'cx'
			cfw.AddPush(fnIndex);
			cfw.AddInvoke(ByteCode.INVOKESPECIAL, codegen.mainClassName, "<init>", Codegen.FUNCTION_CONSTRUCTOR_SIGNATURE);
			if (functionType == FunctionNode.FUNCTION_EXPRESSION)
			{
				// Leave closure object on stack and do not pass it to
				// initFunction which suppose to connect statements to scope
				return;
			}
			cfw.AddPush(functionType);
			cfw.AddALoad(variableObjectLocal);
			cfw.AddALoad(contextLocal);
			// load 'cx'
			AddOptRuntimeInvoke("initFunction", "(Lorg/mozilla/javascript/NativeFunction;" + "I" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Context;" + ")V");
		}

		private int GetTargetLabel(Node target)
		{
			int labelId = target.LabelId();
			if (labelId == -1)
			{
				labelId = cfw.AcquireLabel();
				target.LabelId(labelId);
			}
			return labelId;
		}

		private void VisitGoto(Jump node, int type, Node child)
		{
			Node target = node.target;
			if (type == Token.IFEQ || type == Token.IFNE)
			{
				if (child == null)
				{
					throw Codegen.BadTree();
				}
				int targetLabel = GetTargetLabel(target);
				int fallThruLabel = cfw.AcquireLabel();
				if (type == Token.IFEQ)
				{
					GenerateIfJump(child, node, targetLabel, fallThruLabel);
				}
				else
				{
					GenerateIfJump(child, node, fallThruLabel, targetLabel);
				}
				cfw.MarkLabel(fallThruLabel);
			}
			else
			{
				if (type == Token.JSR)
				{
					if (isGenerator)
					{
						AddGotoWithReturn(target);
					}
					else
					{
						// This assumes that JSR is only ever used for finally
						InlineFinally(target);
					}
				}
				else
				{
					AddGoto(target, ByteCode.GOTO);
				}
			}
		}

		private void AddGotoWithReturn(Node target)
		{
			BodyCodegen.FinallyReturnPoint ret = finallys.Get(target);
			cfw.AddLoadConstant(ret.jsrPoints.Count);
			AddGoto(target, ByteCode.GOTO);
			int retLabel = cfw.AcquireLabel();
			cfw.MarkLabel(retLabel);
			ret.jsrPoints.Add(Sharpen.Extensions.ValueOf(retLabel));
		}

		private void GenerateArrayLiteralFactory(Node node, int count)
		{
			string methodName = codegen.GetBodyMethodName(scriptOrFn) + "_literal" + count;
			InitBodyGeneration();
			argsLocal = firstFreeLocal++;
			localsMax = firstFreeLocal;
			cfw.StartMethod(methodName, "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;", ClassFileWriter.ACC_PRIVATE);
			VisitArrayLiteral(node, node.GetFirstChild(), true);
			cfw.Add(ByteCode.ARETURN);
			cfw.StopMethod((short)(localsMax + 1));
		}

		private void GenerateObjectLiteralFactory(Node node, int count)
		{
			string methodName = codegen.GetBodyMethodName(scriptOrFn) + "_literal" + count;
			InitBodyGeneration();
			argsLocal = firstFreeLocal++;
			localsMax = firstFreeLocal;
			cfw.StartMethod(methodName, "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;", ClassFileWriter.ACC_PRIVATE);
			VisitObjectLiteral(node, node.GetFirstChild(), true);
			cfw.Add(ByteCode.ARETURN);
			cfw.StopMethod((short)(localsMax + 1));
		}

		private void VisitArrayLiteral(Node node, Node child, bool topLevel)
		{
			int count = 0;
			for (Node cursor = child; cursor != null; cursor = cursor.GetNext())
			{
				++count;
			}
			// If code budget is tight swap out literals into separate method
			if (!topLevel && (count > 10 || cfw.GetCurrentCodeOffset() > 30000) && !hasVarsInRegs && !isGenerator && !inLocalBlock)
			{
				if (literals == null)
				{
					literals = new List<Node>();
				}
				literals.Add(node);
				string methodName = codegen.GetBodyMethodName(scriptOrFn) + "_literal" + literals.Count;
				cfw.AddALoad(funObjLocal);
				cfw.AddALoad(contextLocal);
				cfw.AddALoad(variableObjectLocal);
				cfw.AddALoad(thisObjLocal);
				cfw.AddALoad(argsLocal);
				cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, codegen.mainClassName, methodName, "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
				return;
			}
			// load array to store array literal objects
			AddNewObjectArray(count);
			for (int i = 0; i != count; ++i)
			{
				cfw.Add(ByteCode.DUP);
				cfw.AddPush(i);
				GenerateExpression(child, node);
				cfw.Add(ByteCode.AASTORE);
				child = child.GetNext();
			}
			int[] skipIndexes = (int[])node.GetProp(Node.SKIP_INDEXES_PROP);
			if (skipIndexes == null)
			{
				cfw.Add(ByteCode.ACONST_NULL);
				cfw.Add(ByteCode.ICONST_0);
			}
			else
			{
				cfw.AddPush(OptRuntime.EncodeIntArray(skipIndexes));
				cfw.AddPush(skipIndexes.Length);
			}
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			AddOptRuntimeInvoke("newArrayLiteral", "([Ljava/lang/Object;" + "Ljava/lang/String;" + "I" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
		}

		private void VisitObjectLiteral(Node node, Node child, bool topLevel)
		{
			object[] properties = (object[])node.GetProp(Node.OBJECT_IDS_PROP);
			int count = properties.Length;
			// If code budget is tight swap out literals into separate method
			if (!topLevel && (count > 10 || cfw.GetCurrentCodeOffset() > 30000) && !hasVarsInRegs && !isGenerator && !inLocalBlock)
			{
				if (literals == null)
				{
					literals = new List<Node>();
				}
				literals.Add(node);
				string methodName = codegen.GetBodyMethodName(scriptOrFn) + "_literal" + literals.Count;
				cfw.AddALoad(funObjLocal);
				cfw.AddALoad(contextLocal);
				cfw.AddALoad(variableObjectLocal);
				cfw.AddALoad(thisObjLocal);
				cfw.AddALoad(argsLocal);
				cfw.AddInvoke(ByteCode.INVOKEVIRTUAL, codegen.mainClassName, methodName, "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
				return;
			}
			// load array with property ids
			AddNewObjectArray(count);
			for (int i = 0; i != count; ++i)
			{
				cfw.Add(ByteCode.DUP);
				cfw.AddPush(i);
				object id = properties[i];
				if (id is string)
				{
					cfw.AddPush((string)id);
				}
				else
				{
					cfw.AddPush(System.Convert.ToInt32(((int)id)));
					AddScriptRuntimeInvoke("wrapInt", "(I)Ljava/lang/Integer;");
				}
				cfw.Add(ByteCode.AASTORE);
			}
			// load array with property values
			AddNewObjectArray(count);
			Node child2 = child;
			for (int i_1 = 0; i_1 != count; ++i_1)
			{
				cfw.Add(ByteCode.DUP);
				cfw.AddPush(i_1);
				int childType = child2.GetType();
				if (childType == Token.GET || childType == Token.SET)
				{
					GenerateExpression(child2.GetFirstChild(), node);
				}
				else
				{
					GenerateExpression(child2, node);
				}
				cfw.Add(ByteCode.AASTORE);
				child2 = child2.GetNext();
			}
			// check if object literal actually has any getters or setters
			bool hasGetterSetters = false;
			child2 = child;
			for (int i_2 = 0; i_2 != count; ++i_2)
			{
				int childType = child2.GetType();
				if (childType == Token.GET || childType == Token.SET)
				{
					hasGetterSetters = true;
					break;
				}
				child2 = child2.GetNext();
			}
			// create getter/setter flag array
			if (hasGetterSetters)
			{
				cfw.AddPush(count);
				cfw.Add(ByteCode.NEWARRAY, ByteCode.T_INT);
				child2 = child;
				for (int i_3 = 0; i_3 != count; ++i_3)
				{
					cfw.Add(ByteCode.DUP);
					cfw.AddPush(i_3);
					int childType = child2.GetType();
					if (childType == Token.GET)
					{
						cfw.Add(ByteCode.ICONST_M1);
					}
					else
					{
						if (childType == Token.SET)
						{
							cfw.Add(ByteCode.ICONST_1);
						}
						else
						{
							cfw.Add(ByteCode.ICONST_0);
						}
					}
					cfw.Add(ByteCode.IASTORE);
					child2 = child2.GetNext();
				}
			}
			else
			{
				cfw.Add(ByteCode.ACONST_NULL);
			}
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			AddScriptRuntimeInvoke("newObjectLiteral", "([Ljava/lang/Object;" + "[Ljava/lang/Object;" + "[I" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
		}

		private void VisitSpecialCall(Node node, int type, int specialType, Node child)
		{
			cfw.AddALoad(contextLocal);
			if (type == Token.NEW)
			{
				GenerateExpression(child, node);
			}
			else
			{
				// stack: ... cx functionObj
				GenerateFunctionAndThisObj(child, node);
			}
			// stack: ... cx functionObj thisObj
			child = child.GetNext();
			GenerateCallArgArray(node, child, false);
			string methodName;
			string callSignature;
			if (type == Token.NEW)
			{
				methodName = "newObjectSpecial";
				callSignature = "(Lorg/mozilla/javascript/Context;" + "Ljava/lang/Object;" + "[Ljava/lang/Object;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "I" + ")Ljava/lang/Object;";
				// call type
				cfw.AddALoad(variableObjectLocal);
				cfw.AddALoad(thisObjLocal);
				cfw.AddPush(specialType);
			}
			else
			{
				methodName = "callSpecial";
				callSignature = "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Callable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "I" + "Ljava/lang/String;I" + ")Ljava/lang/Object;";
				// call type
				// filename, linenumber
				cfw.AddALoad(variableObjectLocal);
				cfw.AddALoad(thisObjLocal);
				cfw.AddPush(specialType);
				string sourceName = scriptOrFn.GetSourceName();
				cfw.AddPush(sourceName == null ? string.Empty : sourceName);
				cfw.AddPush(itsLineNumber);
			}
			AddOptRuntimeInvoke(methodName, callSignature);
		}

		private void VisitStandardCall(Node node, Node child)
		{
			if (node.GetType() != Token.CALL)
			{
				throw Codegen.BadTree();
			}
			Node firstArgChild = child.GetNext();
			int childType = child.GetType();
			string methodName;
			string signature;
			if (firstArgChild == null)
			{
				if (childType == Token.NAME)
				{
					// name() call
					string name = child.GetString();
					cfw.AddPush(name);
					methodName = "callName0";
					signature = "(Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;";
				}
				else
				{
					if (childType == Token.GETPROP)
					{
						// x.name() call
						Node propTarget = child.GetFirstChild();
						GenerateExpression(propTarget, node);
						Node id = propTarget.GetNext();
						string property = id.GetString();
						cfw.AddPush(property);
						methodName = "callProp0";
						signature = "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;";
					}
					else
					{
						if (childType == Token.GETPROPNOWARN)
						{
							throw Kit.CodeBug();
						}
						else
						{
							GenerateFunctionAndThisObj(child, node);
							methodName = "call0";
							signature = "(Lorg/mozilla/javascript/Callable;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;";
						}
					}
				}
			}
			else
			{
				if (childType == Token.NAME)
				{
					// XXX: this optimization is only possible if name
					// resolution
					// is not affected by arguments evaluation and currently
					// there are no checks for it
					string name = child.GetString();
					GenerateCallArgArray(node, firstArgChild, false);
					cfw.AddPush(name);
					methodName = "callName";
					signature = "([Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;";
				}
				else
				{
					int argCount = 0;
					for (Node arg = firstArgChild; arg != null; arg = arg.GetNext())
					{
						++argCount;
					}
					GenerateFunctionAndThisObj(child, node);
					// stack: ... functionObj thisObj
					if (argCount == 1)
					{
						GenerateExpression(firstArgChild, node);
						methodName = "call1";
						signature = "(Lorg/mozilla/javascript/Callable;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;";
					}
					else
					{
						if (argCount == 2)
						{
							GenerateExpression(firstArgChild, node);
							GenerateExpression(firstArgChild.GetNext(), node);
							methodName = "call2";
							signature = "(Lorg/mozilla/javascript/Callable;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;";
						}
						else
						{
							GenerateCallArgArray(node, firstArgChild, false);
							methodName = "callN";
							signature = "(Lorg/mozilla/javascript/Callable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;";
						}
					}
				}
			}
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			AddOptRuntimeInvoke(methodName, signature);
		}

		private void VisitStandardNew(Node node, Node child)
		{
			if (node.GetType() != Token.NEW)
			{
				throw Codegen.BadTree();
			}
			Node firstArgChild = child.GetNext();
			GenerateExpression(child, node);
			// stack: ... functionObj
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			// stack: ... functionObj cx scope
			GenerateCallArgArray(node, firstArgChild, false);
			AddScriptRuntimeInvoke("newObject", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
		}

		private void VisitOptimizedCall(Node node, OptFunctionNode target, int type, Node child)
		{
			Node firstArgChild = child.GetNext();
			string className = codegen.mainClassName;
			short thisObjLocal = 0;
			if (type == Token.NEW)
			{
				GenerateExpression(child, node);
			}
			else
			{
				GenerateFunctionAndThisObj(child, node);
				thisObjLocal = GetNewWordLocal();
				cfw.AddAStore(thisObjLocal);
			}
			// stack: ... functionObj
			int beyond = cfw.AcquireLabel();
			int regularCall = cfw.AcquireLabel();
			cfw.Add(ByteCode.DUP);
			cfw.Add(ByteCode.INSTANCEOF, className);
			cfw.Add(ByteCode.IFEQ, regularCall);
			cfw.Add(ByteCode.CHECKCAST, className);
			cfw.Add(ByteCode.DUP);
			cfw.Add(ByteCode.GETFIELD, className, Codegen.ID_FIELD_NAME, "I");
			cfw.AddPush(codegen.GetIndex(target.fnode));
			cfw.Add(ByteCode.IF_ICMPNE, regularCall);
			// stack: ... directFunct
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			// stack: ... directFunc cx scope
			if (type == Token.NEW)
			{
				cfw.Add(ByteCode.ACONST_NULL);
			}
			else
			{
				cfw.AddALoad(thisObjLocal);
			}
			// stack: ... directFunc cx scope thisObj
			Node argChild = firstArgChild;
			while (argChild != null)
			{
				int dcp_register = NodeIsDirectCallParameter(argChild);
				if (dcp_register >= 0)
				{
					cfw.AddALoad(dcp_register);
					cfw.AddDLoad(dcp_register + 1);
				}
				else
				{
					if (argChild.GetIntProp(Node.ISNUMBER_PROP, -1) == Node.BOTH)
					{
						cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
						GenerateExpression(argChild, node);
					}
					else
					{
						GenerateExpression(argChild, node);
						cfw.AddPush(0.0);
					}
				}
				argChild = argChild.GetNext();
			}
			cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/ScriptRuntime", "emptyArgs", "[Ljava/lang/Object;");
			cfw.AddInvoke(ByteCode.INVOKESTATIC, codegen.mainClassName, (type == Token.NEW) ? codegen.GetDirectCtorName(target.fnode) : codegen.GetBodyMethodName(target.fnode), codegen.GetBodyMethodSignature(target.fnode));
			cfw.Add(ByteCode.GOTO, beyond);
			cfw.MarkLabel(regularCall);
			// stack: ... functionObj
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			// stack: ... functionObj cx scope
			if (type != Token.NEW)
			{
				cfw.AddALoad(thisObjLocal);
				ReleaseWordLocal(thisObjLocal);
			}
			// stack: ... functionObj cx scope thisObj
			// XXX: this will generate code for the child array the second time,
			// so expression code generation better not to alter tree structure...
			GenerateCallArgArray(node, firstArgChild, true);
			if (type == Token.NEW)
			{
				AddScriptRuntimeInvoke("newObject", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Lorg/mozilla/javascript/Scriptable;");
			}
			else
			{
				cfw.AddInvoke(ByteCode.INVOKEINTERFACE, "org/mozilla/javascript/Callable", "call", "(Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Lorg/mozilla/javascript/Scriptable;" + "[Ljava/lang/Object;" + ")Ljava/lang/Object;");
			}
			cfw.MarkLabel(beyond);
		}

		private void GenerateCallArgArray(Node node, Node argChild, bool directCall)
		{
			int argCount = 0;
			for (Node child = argChild; child != null; child = child.GetNext())
			{
				++argCount;
			}
			// load array object to set arguments
			if (argCount == 1 && itsOneArgArray >= 0)
			{
				cfw.AddALoad(itsOneArgArray);
			}
			else
			{
				AddNewObjectArray(argCount);
			}
			// Copy arguments into it
			for (int i = 0; i != argCount; ++i)
			{
				// If we are compiling a generator an argument could be the result
				// of a yield. In that case we will have an immediate on the stack
				// which we need to avoid
				if (!isGenerator)
				{
					cfw.Add(ByteCode.DUP);
					cfw.AddPush(i);
				}
				if (!directCall)
				{
					GenerateExpression(argChild, node);
				}
				else
				{
					// If this has also been a directCall sequence, the Number
					// flag will have remained set for any parameter so that
					// the values could be copied directly into the outgoing
					// args. Here we want to force it to be treated as not in
					// a Number context, so we set the flag off.
					int dcp_register = NodeIsDirectCallParameter(argChild);
					if (dcp_register >= 0)
					{
						DcpLoadAsObject(dcp_register);
					}
					else
					{
						GenerateExpression(argChild, node);
						int childNumberFlag = argChild.GetIntProp(Node.ISNUMBER_PROP, -1);
						if (childNumberFlag == Node.BOTH)
						{
							AddDoubleWrap();
						}
					}
				}
				// When compiling generators, any argument to a method may be a
				// yield expression. Hence we compile the argument first and then
				// load the argument index and assign the value to the args array.
				if (isGenerator)
				{
					short tempLocal = GetNewWordLocal();
					cfw.AddAStore(tempLocal);
					cfw.Add(ByteCode.CHECKCAST, "[Ljava/lang/Object;");
					cfw.Add(ByteCode.DUP);
					cfw.AddPush(i);
					cfw.AddALoad(tempLocal);
					ReleaseWordLocal(tempLocal);
				}
				cfw.Add(ByteCode.AASTORE);
				argChild = argChild.GetNext();
			}
		}

		private void GenerateFunctionAndThisObj(Node node, Node parent)
		{
			// Place on stack (function object, function this) pair
			int type = node.GetType();
			switch (node.GetType())
			{
				case Token.GETPROPNOWARN:
				{
					throw Kit.CodeBug();
				}

				case Token.GETPROP:
				case Token.GETELEM:
				{
					Node target = node.GetFirstChild();
					GenerateExpression(target, node);
					Node id = target.GetNext();
					if (type == Token.GETPROP)
					{
						string property = id.GetString();
						cfw.AddPush(property);
						cfw.AddALoad(contextLocal);
						cfw.AddALoad(variableObjectLocal);
						AddScriptRuntimeInvoke("getPropFunctionAndThis", "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Callable;");
					}
					else
					{
						GenerateExpression(id, node);
						// id
						if (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1)
						{
							AddDoubleWrap();
						}
						cfw.AddALoad(contextLocal);
						AddScriptRuntimeInvoke("getElemFunctionAndThis", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Lorg/mozilla/javascript/Callable;");
					}
					break;
				}

				case Token.NAME:
				{
					string name = node.GetString();
					cfw.AddPush(name);
					cfw.AddALoad(contextLocal);
					cfw.AddALoad(variableObjectLocal);
					AddScriptRuntimeInvoke("getNameFunctionAndThis", "(Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Callable;");
					break;
				}

				default:
				{
					// including GETVAR
					GenerateExpression(node, parent);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("getValueFunctionAndThis", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Lorg/mozilla/javascript/Callable;");
					break;
				}
			}
			// Get thisObj prepared by get(Name|Prop|Elem|Value)FunctionAndThis
			cfw.AddALoad(contextLocal);
			AddScriptRuntimeInvoke("lastStoredScriptable", "(Lorg/mozilla/javascript/Context;" + ")Lorg/mozilla/javascript/Scriptable;");
		}

		private void UpdateLineNumber(Node node)
		{
			itsLineNumber = node.GetLineno();
			if (itsLineNumber == -1)
			{
				return;
			}
			cfw.AddLineNumberEntry((short)itsLineNumber);
		}

		private void VisitTryCatchFinally(Jump node, Node child)
		{
			// OPT we only need to do this if there are enclosed WITH
			// statements; could statically check and omit this if there aren't any.
			// XXX OPT Maybe instead do syntactic transforms to associate
			// each 'with' with a try/finally block that does the exitwith.
			short savedVariableObject = GetNewWordLocal();
			cfw.AddALoad(variableObjectLocal);
			cfw.AddAStore(savedVariableObject);
			int startLabel = cfw.AcquireLabel();
			cfw.MarkLabel(startLabel, (short)0);
			Node catchTarget = node.target;
			Node finallyTarget = node.GetFinally();
			int[] handlerLabels = new int[EXCEPTION_MAX];
			exceptionManager.PushExceptionInfo(node);
			if (catchTarget != null)
			{
				handlerLabels[JAVASCRIPT_EXCEPTION] = cfw.AcquireLabel();
				handlerLabels[EVALUATOR_EXCEPTION] = cfw.AcquireLabel();
				handlerLabels[ECMAERROR_EXCEPTION] = cfw.AcquireLabel();
				Context cx = Context.GetCurrentContext();
				if (cx != null && cx.HasFeature(Context.FEATURE_ENHANCED_JAVA_ACCESS))
				{
					handlerLabels[THROWABLE_EXCEPTION] = cfw.AcquireLabel();
				}
			}
			if (finallyTarget != null)
			{
				handlerLabels[FINALLY_EXCEPTION] = cfw.AcquireLabel();
			}
			exceptionManager.SetHandlers(handlerLabels, startLabel);
			// create a table for the equivalent of JSR returns
			if (isGenerator && finallyTarget != null)
			{
				BodyCodegen.FinallyReturnPoint ret = new BodyCodegen.FinallyReturnPoint();
				if (finallys == null)
				{
					finallys = new Dictionary<Node, BodyCodegen.FinallyReturnPoint>();
				}
				// add the finally target to hashtable
				finallys.Put(finallyTarget, ret);
				// add the finally node as well to the hash table
				finallys.Put(finallyTarget.GetNext(), ret);
			}
			while (child != null)
			{
				if (child == catchTarget)
				{
					int catchLabel = GetTargetLabel(catchTarget);
					exceptionManager.RemoveHandler(JAVASCRIPT_EXCEPTION, catchLabel);
					exceptionManager.RemoveHandler(EVALUATOR_EXCEPTION, catchLabel);
					exceptionManager.RemoveHandler(ECMAERROR_EXCEPTION, catchLabel);
					exceptionManager.RemoveHandler(THROWABLE_EXCEPTION, catchLabel);
				}
				GenerateStatement(child);
				child = child.GetNext();
			}
			// control flow skips the handlers
			int realEnd = cfw.AcquireLabel();
			cfw.Add(ByteCode.GOTO, realEnd);
			int exceptionLocal = GetLocalBlockRegister(node);
			// javascript handler; unwrap exception and GOTO to javascript
			// catch area.
			if (catchTarget != null)
			{
				// get the label to goto
				int catchLabel = catchTarget.LabelId();
				// If the function is a generator, then handlerLabels will consist
				// of zero labels. generateCatchBlock will create its own label
				// in this case. The extra parameter for the label is added for
				// the case of non-generator functions that inline finally blocks.
				GenerateCatchBlock(JAVASCRIPT_EXCEPTION, savedVariableObject, catchLabel, exceptionLocal, handlerLabels[JAVASCRIPT_EXCEPTION]);
				GenerateCatchBlock(EVALUATOR_EXCEPTION, savedVariableObject, catchLabel, exceptionLocal, handlerLabels[EVALUATOR_EXCEPTION]);
				GenerateCatchBlock(ECMAERROR_EXCEPTION, savedVariableObject, catchLabel, exceptionLocal, handlerLabels[ECMAERROR_EXCEPTION]);
				Context cx = Context.GetCurrentContext();
				if (cx != null && cx.HasFeature(Context.FEATURE_ENHANCED_JAVA_ACCESS))
				{
					GenerateCatchBlock(THROWABLE_EXCEPTION, savedVariableObject, catchLabel, exceptionLocal, handlerLabels[THROWABLE_EXCEPTION]);
				}
			}
			// finally handler; catch all exceptions, store to a local; JSR to
			// the finally, then re-throw.
			if (finallyTarget != null)
			{
				int finallyHandler = cfw.AcquireLabel();
				int finallyEnd = cfw.AcquireLabel();
				cfw.MarkHandler(finallyHandler);
				if (!isGenerator)
				{
					cfw.MarkLabel(handlerLabels[FINALLY_EXCEPTION]);
				}
				cfw.AddAStore(exceptionLocal);
				// reset the variable object local
				cfw.AddALoad(savedVariableObject);
				cfw.AddAStore(variableObjectLocal);
				// get the label to JSR to
				int finallyLabel = finallyTarget.LabelId();
				if (isGenerator)
				{
					AddGotoWithReturn(finallyTarget);
				}
				else
				{
					InlineFinally(finallyTarget, handlerLabels[FINALLY_EXCEPTION], finallyEnd);
				}
				// rethrow
				cfw.AddALoad(exceptionLocal);
				if (isGenerator)
				{
					cfw.Add(ByteCode.CHECKCAST, "java/lang/Throwable");
				}
				cfw.Add(ByteCode.ATHROW);
				cfw.MarkLabel(finallyEnd);
				// mark the handler
				if (isGenerator)
				{
					cfw.AddExceptionHandler(startLabel, finallyLabel, finallyHandler, null);
				}
			}
			// catch any
			ReleaseWordLocal(savedVariableObject);
			cfw.MarkLabel(realEnd);
			if (!isGenerator)
			{
				exceptionManager.PopExceptionInfo();
			}
		}

		private const int JAVASCRIPT_EXCEPTION = 0;

		private const int EVALUATOR_EXCEPTION = 1;

		private const int ECMAERROR_EXCEPTION = 2;

		private const int THROWABLE_EXCEPTION = 3;

		private const int FINALLY_EXCEPTION = 4;

		private const int EXCEPTION_MAX = 5;

		// Finally catch-alls are technically Throwable, but we want a distinction
		// for the exception manager and we want to use a null string instead of
		// an explicit Throwable string.
		private void GenerateCatchBlock(int exceptionType, short savedVariableObject, int catchLabel, int exceptionLocal, int handler)
		{
			if (handler == 0)
			{
				handler = cfw.AcquireLabel();
			}
			cfw.MarkHandler(handler);
			// MS JVM gets cranky if the exception object is left on the stack
			cfw.AddAStore(exceptionLocal);
			// reset the variable object local
			cfw.AddALoad(savedVariableObject);
			cfw.AddAStore(variableObjectLocal);
			string exceptionName = ExceptionTypeToName(exceptionType);
			cfw.Add(ByteCode.GOTO, catchLabel);
		}

		private string ExceptionTypeToName(int exceptionType)
		{
			if (exceptionType == JAVASCRIPT_EXCEPTION)
			{
				return "org/mozilla/javascript/JavaScriptException";
			}
			else
			{
				if (exceptionType == EVALUATOR_EXCEPTION)
				{
					return "org/mozilla/javascript/EvaluatorException";
				}
				else
				{
					if (exceptionType == ECMAERROR_EXCEPTION)
					{
						return "org/mozilla/javascript/EcmaError";
					}
					else
					{
						if (exceptionType == THROWABLE_EXCEPTION)
						{
							return "java/lang/Throwable";
						}
						else
						{
							if (exceptionType == FINALLY_EXCEPTION)
							{
								return null;
							}
							else
							{
								throw Kit.CodeBug();
							}
						}
					}
				}
			}
		}

		/// <summary>Manages placement of exception handlers for non-generator functions.</summary>
		/// <remarks>
		/// Manages placement of exception handlers for non-generator functions.
		/// For generator functions, there are mechanisms put into place to emulate
		/// jsr by using a goto with a return label. That is one mechanism for
		/// implementing finally blocks. The other, which is implemented by Sun,
		/// involves duplicating the finally block where jsr instructions would
		/// normally be. However, inlining finally blocks causes problems with
		/// translating exception handlers. Instead of having one big bytecode range
		/// for each exception, we now have to skip over the inlined finally blocks.
		/// This class is meant to help implement this.
		/// Every time a try block is encountered during translation, exception
		/// information should be pushed into the manager, which is treated as a
		/// stack. The addHandler() and setHandlers() methods may be used to register
		/// exceptionHandlers for the try block; removeHandler() is used to reverse
		/// the operation. At the end of the try/catch/finally, the exception state
		/// for it should be popped.
		/// The important function here is markInlineFinally. This finds which
		/// finally block on the exception state stack is being inlined and skips
		/// the proper exception handlers until the finally block is generated.
		/// </remarks>
		private class ExceptionManager
		{
			internal ExceptionManager(BodyCodegen _enclosing)
			{
				this._enclosing = _enclosing;
				this.exceptionInfo = new List<BodyCodegen.ExceptionManager.ExceptionInfo>();
			}

			/// <summary>Push a new try block onto the exception information stack.</summary>
			/// <remarks>Push a new try block onto the exception information stack.</remarks>
			/// <param name="node">
			/// an exception handling node (node.getType() ==
			/// Token.TRY)
			/// </param>
			internal virtual void PushExceptionInfo(Jump node)
			{
				Node fBlock = this._enclosing.GetFinallyAtTarget(node.GetFinally());
				BodyCodegen.ExceptionManager.ExceptionInfo ei = new BodyCodegen.ExceptionManager.ExceptionInfo(this, node, fBlock);
				this.exceptionInfo.Add(ei);
			}

			/// <summary>
			/// Register an exception handler for the try block at the top of the
			/// exception information stack.
			/// </summary>
			/// <remarks>
			/// Register an exception handler for the try block at the top of the
			/// exception information stack.
			/// </remarks>
			/// <param name="exceptionType">
			/// one of the integer constants representing an
			/// exception type
			/// </param>
			/// <param name="handlerLabel">the label of the exception handler</param>
			/// <param name="startLabel">the label where the exception handling begins</param>
			internal virtual void AddHandler(int exceptionType, int handlerLabel, int startLabel)
			{
				BodyCodegen.ExceptionManager.ExceptionInfo top = this.GetTop();
				top.handlerLabels[exceptionType] = handlerLabel;
				top.exceptionStarts[exceptionType] = startLabel;
			}

			/// <summary>Register multiple exception handlers for the top try block.</summary>
			/// <remarks>
			/// Register multiple exception handlers for the top try block. If the
			/// exception type maps to a zero label, then it is ignored.
			/// </remarks>
			/// <param name="handlerLabels">
			/// a map from integer constants representing an
			/// exception type to the label of the exception
			/// handler
			/// </param>
			/// <param name="startLabel">
			/// the label where all of the exception handling
			/// begins
			/// </param>
			internal virtual void SetHandlers(int[] handlerLabels, int startLabel)
			{
				BodyCodegen.ExceptionManager.ExceptionInfo top = this.GetTop();
				for (int i = 0; i < handlerLabels.Length; i++)
				{
					if (handlerLabels[i] != 0)
					{
						this.AddHandler(i, handlerLabels[i], startLabel);
					}
				}
			}

			/// <summary>Remove an exception handler for the top try block.</summary>
			/// <remarks>Remove an exception handler for the top try block.</remarks>
			/// <param name="exceptionType">
			/// one of the integer constants representing an
			/// exception type
			/// </param>
			/// <param name="endLabel">
			/// a label representing the end of the last bytecode
			/// that should be handled by the exception
			/// </param>
			/// <returns>
			/// the label of the exception handler associated with the
			/// exception type
			/// </returns>
			internal virtual int RemoveHandler(int exceptionType, int endLabel)
			{
				BodyCodegen.ExceptionManager.ExceptionInfo top = this.GetTop();
				if (top.handlerLabels[exceptionType] != 0)
				{
					int handlerLabel = top.handlerLabels[exceptionType];
					this.EndCatch(top, exceptionType, endLabel);
					top.handlerLabels[exceptionType] = 0;
					return handlerLabel;
				}
				return 0;
			}

			/// <summary>Remove the top try block from the exception information stack.</summary>
			/// <remarks>Remove the top try block from the exception information stack.</remarks>
			internal virtual void PopExceptionInfo()
			{
				this.exceptionInfo.RemoveLast();
			}

			/// <summary>Mark the start of an inlined finally block.</summary>
			/// <remarks>
			/// Mark the start of an inlined finally block.
			/// When a finally block is inlined, any exception handlers that are
			/// lexically inside of its try block should not cover the range of the
			/// exception block. We scan from the innermost try block outward until
			/// we find the try block that matches the finally block. For any block
			/// whose exception handlers that aren't currently stopped by a finally
			/// block, we stop the handlers at the beginning of the finally block
			/// and set it as the finally block that has stopped the handlers. This
			/// prevents other inlined finally blocks from prematurely ending skip
			/// ranges and creating bad exception handler ranges.
			/// </remarks>
			/// <param name="finallyBlock">the finally block that is being inlined</param>
			/// <param name="finallyStart">the label of the beginning of the inlined code</param>
			internal virtual void MarkInlineFinallyStart(Node finallyBlock, int finallyStart)
			{
				// Traverse the stack in LIFO order until the try block
				// corresponding to the finally block has been reached. We must
				// traverse backwards because the earlier exception handlers in
				// the exception handler table have priority when determining which
				// handler to use. Therefore, we start with the most nested try
				// block and move outward.
				ListIterator<BodyCodegen.ExceptionManager.ExceptionInfo> iter = this.exceptionInfo.ListIterator(this.exceptionInfo.Count);
				while (iter.HasPrevious())
				{
					BodyCodegen.ExceptionManager.ExceptionInfo ei = iter.Previous();
					for (int i = 0; i < BodyCodegen.EXCEPTION_MAX; i++)
					{
						if (ei.handlerLabels[i] != 0 && ei.currentFinally == null)
						{
							this.EndCatch(ei, i, finallyStart);
							ei.exceptionStarts[i] = 0;
							ei.currentFinally = finallyBlock;
						}
					}
					if (ei.finallyBlock == finallyBlock)
					{
						break;
					}
				}
			}

			/// <summary>Mark the end of an inlined finally block.</summary>
			/// <remarks>
			/// Mark the end of an inlined finally block.
			/// For any set of exception handlers that have been stopped by the
			/// inlined block, resume exception handling at the end of the finally
			/// block.
			/// </remarks>
			/// <param name="finallyBlock">the finally block that is being inlined</param>
			/// <param name="finallyEnd">the label of the end of the inlined code</param>
			internal virtual void MarkInlineFinallyEnd(Node finallyBlock, int finallyEnd)
			{
				ListIterator<BodyCodegen.ExceptionManager.ExceptionInfo> iter = this.exceptionInfo.ListIterator(this.exceptionInfo.Count);
				while (iter.HasPrevious())
				{
					BodyCodegen.ExceptionManager.ExceptionInfo ei = iter.Previous();
					for (int i = 0; i < BodyCodegen.EXCEPTION_MAX; i++)
					{
						if (ei.handlerLabels[i] != 0 && ei.currentFinally == finallyBlock)
						{
							ei.exceptionStarts[i] = finallyEnd;
							ei.currentFinally = null;
						}
					}
					if (ei.finallyBlock == finallyBlock)
					{
						break;
					}
				}
			}

			/// <summary>
			/// Mark off the end of a bytecode chunk that should be handled by an
			/// exceptionHandler.
			/// </summary>
			/// <remarks>
			/// Mark off the end of a bytecode chunk that should be handled by an
			/// exceptionHandler.
			/// The caller of this method must appropriately mark the start of the
			/// next bytecode chunk or remove the handler.
			/// </remarks>
			private void EndCatch(BodyCodegen.ExceptionManager.ExceptionInfo ei, int exceptionType, int catchEnd)
			{
				if (ei.exceptionStarts[exceptionType] == 0)
				{
					throw new InvalidOperationException("bad exception start");
				}
				int currentStart = ei.exceptionStarts[exceptionType];
				int currentStartPC = this._enclosing.cfw.GetLabelPC(currentStart);
				int catchEndPC = this._enclosing.cfw.GetLabelPC(catchEnd);
				if (currentStartPC != catchEndPC)
				{
					this._enclosing.cfw.AddExceptionHandler(ei.exceptionStarts[exceptionType], catchEnd, ei.handlerLabels[exceptionType], this._enclosing.ExceptionTypeToName(exceptionType));
				}
			}

			private BodyCodegen.ExceptionManager.ExceptionInfo GetTop()
			{
				return this.exceptionInfo.GetLast();
			}

			private class ExceptionInfo
			{
				internal ExceptionInfo(ExceptionManager _enclosing, Jump node, Node finallyBlock)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.finallyBlock = finallyBlock;
					this.handlerLabels = new int[BodyCodegen.EXCEPTION_MAX];
					this.exceptionStarts = new int[BodyCodegen.EXCEPTION_MAX];
					this.currentFinally = null;
				}

				internal Jump node;

				internal Node finallyBlock;

				internal int[] handlerLabels;

				internal int[] exceptionStarts;

				internal Node currentFinally;

				private readonly ExceptionManager _enclosing;
				// The current finally block that has temporarily ended the
				// exception handler ranges
			}

			private List<BodyCodegen.ExceptionManager.ExceptionInfo> exceptionInfo;

			private readonly BodyCodegen _enclosing;
			// A stack of try/catch block information ordered by lexical scoping
		}

		private BodyCodegen.ExceptionManager exceptionManager;

		/// <summary>Inline a FINALLY node into the method bytecode.</summary>
		/// <remarks>
		/// Inline a FINALLY node into the method bytecode.
		/// This method takes a label that points to the real start of the finally
		/// block as implemented in the bytecode. This is because in some cases,
		/// the finally block really starts before any of the code in the Node. For
		/// example, the catch-all-rethrow finally block has a few instructions
		/// prior to the finally block made by the user.
		/// In addition, an end label that should be unmarked is given as a method
		/// parameter. It is the responsibility of any callers of this method to
		/// mark the label.
		/// The start and end labels of the finally block are used to exclude the
		/// inlined block from the proper exception handler. For example, an inlined
		/// finally block should not be handled by a catch-all-rethrow.
		/// </remarks>
		/// <param name="finallyTarget">
		/// a TARGET node directly preceding a FINALLY node or
		/// a FINALLY node itself
		/// </param>
		/// <param name="finallyStart">
		/// a pre-marked label that indicates the actual start
		/// of the finally block in the bytecode.
		/// </param>
		/// <param name="finallyEnd">
		/// an unmarked label that will indicate the actual end
		/// of the finally block in the bytecode.
		/// </param>
		private void InlineFinally(Node finallyTarget, int finallyStart, int finallyEnd)
		{
			Node fBlock = GetFinallyAtTarget(finallyTarget);
			fBlock.ResetTargets();
			Node child = fBlock.GetFirstChild();
			exceptionManager.MarkInlineFinallyStart(fBlock, finallyStart);
			while (child != null)
			{
				GenerateStatement(child);
				child = child.GetNext();
			}
			exceptionManager.MarkInlineFinallyEnd(fBlock, finallyEnd);
		}

		private void InlineFinally(Node finallyTarget)
		{
			int finallyStart = cfw.AcquireLabel();
			int finallyEnd = cfw.AcquireLabel();
			cfw.MarkLabel(finallyStart);
			InlineFinally(finallyTarget, finallyStart, finallyEnd);
			cfw.MarkLabel(finallyEnd);
		}

		/// <summary>Get a FINALLY node at a point in the IR.</summary>
		/// <remarks>
		/// Get a FINALLY node at a point in the IR.
		/// This is strongly dependent on the generated IR. If the node is a TARGET,
		/// it only check the next node to see if it is a FINALLY node.
		/// </remarks>
		private Node GetFinallyAtTarget(Node node)
		{
			if (node == null)
			{
				return null;
			}
			else
			{
				if (node.GetType() == Token.FINALLY)
				{
					return node;
				}
				else
				{
					if (node != null && node.GetType() == Token.TARGET)
					{
						Node fBlock = node.GetNext();
						if (fBlock != null && fBlock.GetType() == Token.FINALLY)
						{
							return fBlock;
						}
					}
				}
			}
			throw Kit.CodeBug("bad finally target");
		}

		private bool GenerateSaveLocals(Node node)
		{
			int count = 0;
			for (int i = 0; i < firstFreeLocal; i++)
			{
				if (locals[i] != 0)
				{
					count++;
				}
			}
			if (count == 0)
			{
				((FunctionNode)scriptOrFn).AddLiveLocals(node, null);
				return false;
			}
			// calculate the max locals
			maxLocals = maxLocals > count ? maxLocals : count;
			// create a locals list
			int[] ls = new int[count];
			int s = 0;
			for (int i_1 = 0; i_1 < firstFreeLocal; i_1++)
			{
				if (locals[i_1] != 0)
				{
					ls[s] = i_1;
					s++;
				}
			}
			// save the locals
			((FunctionNode)scriptOrFn).AddLiveLocals(node, ls);
			// save locals
			GenerateGetGeneratorLocalsState();
			for (int i_2 = 0; i_2 < count; i_2++)
			{
				cfw.Add(ByteCode.DUP);
				cfw.AddLoadConstant(i_2);
				cfw.AddALoad(ls[i_2]);
				cfw.Add(ByteCode.AASTORE);
			}
			// pop the array off the stack
			cfw.Add(ByteCode.POP);
			return true;
		}

		private void VisitSwitch(Jump switchNode, Node child)
		{
			// See comments in IRFactory.createSwitch() for description
			// of SWITCH node
			GenerateExpression(child, switchNode);
			// save selector value
			short selector = GetNewWordLocal();
			cfw.AddAStore(selector);
			for (Jump caseNode = (Jump)child.GetNext(); caseNode != null; caseNode = (Jump)caseNode.GetNext())
			{
				if (caseNode.GetType() != Token.CASE)
				{
					throw Codegen.BadTree();
				}
				Node test = caseNode.GetFirstChild();
				GenerateExpression(test, caseNode);
				cfw.AddALoad(selector);
				AddScriptRuntimeInvoke("shallowEq", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + ")Z");
				AddGoto(caseNode.target, ByteCode.IFNE);
			}
			ReleaseWordLocal(selector);
		}

		private void VisitTypeofname(Node node)
		{
			if (hasVarsInRegs)
			{
				int varIndex = fnCurrent.fnode.GetIndexForNameNode(node);
				if (varIndex >= 0)
				{
					if (fnCurrent.IsNumberVar(varIndex))
					{
						cfw.AddPush("number");
					}
					else
					{
						if (VarIsDirectCallParameter(varIndex))
						{
							int dcp_register = varRegisters[varIndex];
							cfw.AddALoad(dcp_register);
							cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
							int isNumberLabel = cfw.AcquireLabel();
							cfw.Add(ByteCode.IF_ACMPEQ, isNumberLabel);
							short stack = cfw.GetStackTop();
							cfw.AddALoad(dcp_register);
							AddScriptRuntimeInvoke("typeof", "(Ljava/lang/Object;" + ")Ljava/lang/String;");
							int beyond = cfw.AcquireLabel();
							cfw.Add(ByteCode.GOTO, beyond);
							cfw.MarkLabel(isNumberLabel, stack);
							cfw.AddPush("number");
							cfw.MarkLabel(beyond);
						}
						else
						{
							cfw.AddALoad(varRegisters[varIndex]);
							AddScriptRuntimeInvoke("typeof", "(Ljava/lang/Object;" + ")Ljava/lang/String;");
						}
					}
					return;
				}
			}
			cfw.AddALoad(variableObjectLocal);
			cfw.AddPush(node.GetString());
			AddScriptRuntimeInvoke("typeofName", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + ")Ljava/lang/String;");
		}

		/// <summary>Save the current code offset.</summary>
		/// <remarks>
		/// Save the current code offset. This saved code offset is used to
		/// compute instruction counts in subsequent calls to
		/// <see cref="AddInstructionCount()">AddInstructionCount()</see>
		/// .
		/// </remarks>
		private void SaveCurrentCodeOffset()
		{
			savedCodeOffset = cfw.GetCurrentCodeOffset();
		}

		/// <summary>
		/// Generate calls to ScriptRuntime.addInstructionCount to keep track of
		/// executed instructions and call <code>observeInstructionCount()</code>
		/// if a threshold is exceeded.<br />
		/// Calculates the count from getCurrentCodeOffset - savedCodeOffset
		/// </summary>
		private void AddInstructionCount()
		{
			int count = cfw.GetCurrentCodeOffset() - savedCodeOffset;
			// TODO we used to return for count == 0 but that broke the following:
			//    while(true) continue; (see bug 531600)
			// To be safe, we now always count at least 1 instruction when invoked.
			AddInstructionCount(Math.Max(count, 1));
		}

		/// <summary>
		/// Generate calls to ScriptRuntime.addInstructionCount to keep track of
		/// executed instructions and call <code>observeInstructionCount()</code>
		/// if a threshold is exceeded.<br />
		/// Takes the count as a parameter - used to add monitoring to loops and
		/// other blocks that don't have any ops - this allows
		/// for monitoring/killing of while(true) loops and such.
		/// </summary>
		/// <remarks>
		/// Generate calls to ScriptRuntime.addInstructionCount to keep track of
		/// executed instructions and call <code>observeInstructionCount()</code>
		/// if a threshold is exceeded.<br />
		/// Takes the count as a parameter - used to add monitoring to loops and
		/// other blocks that don't have any ops - this allows
		/// for monitoring/killing of while(true) loops and such.
		/// </remarks>
		private void AddInstructionCount(int count)
		{
			cfw.AddALoad(contextLocal);
			cfw.AddPush(count);
			AddScriptRuntimeInvoke("addInstructionCount", "(Lorg/mozilla/javascript/Context;" + "I)V");
		}

		private void VisitIncDec(Node node)
		{
			int incrDecrMask = node.GetExistingIntProp(Node.INCRDECR_PROP);
			Node child = node.GetFirstChild();
			switch (child.GetType())
			{
				case Token.GETVAR:
				{
					if (!hasVarsInRegs)
					{
						Kit.CodeBug();
					}
					bool post = ((incrDecrMask & Node.POST_FLAG) != 0);
					int varIndex = fnCurrent.GetVarIndex(child);
					short reg = varRegisters[varIndex];
					if (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1)
					{
						int offset = VarIsDirectCallParameter(varIndex) ? 1 : 0;
						cfw.AddDLoad(reg + offset);
						if (post)
						{
							cfw.Add(ByteCode.DUP2);
						}
						cfw.AddPush(1.0);
						if ((incrDecrMask & Node.DECR_FLAG) == 0)
						{
							cfw.Add(ByteCode.DADD);
						}
						else
						{
							cfw.Add(ByteCode.DSUB);
						}
						if (!post)
						{
							cfw.Add(ByteCode.DUP2);
						}
						cfw.AddDStore(reg + offset);
					}
					else
					{
						if (VarIsDirectCallParameter(varIndex))
						{
							DcpLoadAsObject(reg);
						}
						else
						{
							cfw.AddALoad(reg);
						}
						if (post)
						{
							cfw.Add(ByteCode.DUP);
						}
						AddObjectToDouble();
						cfw.AddPush(1.0);
						if ((incrDecrMask & Node.DECR_FLAG) == 0)
						{
							cfw.Add(ByteCode.DADD);
						}
						else
						{
							cfw.Add(ByteCode.DSUB);
						}
						AddDoubleWrap();
						if (!post)
						{
							cfw.Add(ByteCode.DUP);
						}
						cfw.AddAStore(reg);
						break;
					}
					break;
				}

				case Token.NAME:
				{
					cfw.AddALoad(variableObjectLocal);
					cfw.AddPush(child.GetString());
					// push name
					cfw.AddALoad(contextLocal);
					cfw.AddPush(incrDecrMask);
					AddScriptRuntimeInvoke("nameIncrDecr", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "I)Ljava/lang/Object;");
					break;
				}

				case Token.GETPROPNOWARN:
				{
					throw Kit.CodeBug();
				}

				case Token.GETPROP:
				{
					Node getPropChild = child.GetFirstChild();
					GenerateExpression(getPropChild, node);
					GenerateExpression(getPropChild.GetNext(), node);
					cfw.AddALoad(contextLocal);
					cfw.AddPush(incrDecrMask);
					AddScriptRuntimeInvoke("propIncrDecr", "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "I)Ljava/lang/Object;");
					break;
				}

				case Token.GETELEM:
				{
					Node elemChild = child.GetFirstChild();
					GenerateExpression(elemChild, node);
					GenerateExpression(elemChild.GetNext(), node);
					cfw.AddALoad(contextLocal);
					cfw.AddPush(incrDecrMask);
					if (elemChild.GetNext().GetIntProp(Node.ISNUMBER_PROP, -1) != -1)
					{
						AddOptRuntimeInvoke("elemIncrDecr", "(Ljava/lang/Object;" + "D" + "Lorg/mozilla/javascript/Context;" + "I" + ")Ljava/lang/Object;");
					}
					else
					{
						AddScriptRuntimeInvoke("elemIncrDecr", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "I" + ")Ljava/lang/Object;");
					}
					break;
				}

				case Token.GET_REF:
				{
					Node refChild = child.GetFirstChild();
					GenerateExpression(refChild, node);
					cfw.AddALoad(contextLocal);
					cfw.AddPush(incrDecrMask);
					AddScriptRuntimeInvoke("refIncrDecr", "(Lorg/mozilla/javascript/Ref;" + "Lorg/mozilla/javascript/Context;" + "I)Ljava/lang/Object;");
					break;
				}

				default:
				{
					Codegen.BadTree();
					break;
				}
			}
		}

		private static bool IsArithmeticNode(Node node)
		{
			int type = node.GetType();
			return (type == Token.SUB) || (type == Token.MOD) || (type == Token.DIV) || (type == Token.MUL);
		}

		private void VisitArithmetic(Node node, int opCode, Node child, Node parent)
		{
			int childNumberFlag = node.GetIntProp(Node.ISNUMBER_PROP, -1);
			if (childNumberFlag != -1)
			{
				GenerateExpression(child, node);
				GenerateExpression(child.GetNext(), node);
				cfw.Add(opCode);
			}
			else
			{
				bool childOfArithmetic = IsArithmeticNode(parent);
				GenerateExpression(child, node);
				if (!IsArithmeticNode(child))
				{
					AddObjectToDouble();
				}
				GenerateExpression(child.GetNext(), node);
				if (!IsArithmeticNode(child.GetNext()))
				{
					AddObjectToDouble();
				}
				cfw.Add(opCode);
				if (!childOfArithmetic)
				{
					AddDoubleWrap();
				}
			}
		}

		private void VisitBitOp(Node node, int type, Node child)
		{
			int childNumberFlag = node.GetIntProp(Node.ISNUMBER_PROP, -1);
			GenerateExpression(child, node);
			// special-case URSH; work with the target arg as a long, so
			// that we can return a 32-bit unsigned value, and call
			// toUint32 instead of toInt32.
			if (type == Token.URSH)
			{
				AddScriptRuntimeInvoke("toUint32", "(Ljava/lang/Object;)J");
				GenerateExpression(child.GetNext(), node);
				AddScriptRuntimeInvoke("toInt32", "(Ljava/lang/Object;)I");
				// Looks like we need to explicitly mask the shift to 5 bits -
				// LUSHR takes 6 bits.
				cfw.AddPush(31);
				cfw.Add(ByteCode.IAND);
				cfw.Add(ByteCode.LUSHR);
				cfw.Add(ByteCode.L2D);
				AddDoubleWrap();
				return;
			}
			if (childNumberFlag == -1)
			{
				AddScriptRuntimeInvoke("toInt32", "(Ljava/lang/Object;)I");
				GenerateExpression(child.GetNext(), node);
				AddScriptRuntimeInvoke("toInt32", "(Ljava/lang/Object;)I");
			}
			else
			{
				AddScriptRuntimeInvoke("toInt32", "(D)I");
				GenerateExpression(child.GetNext(), node);
				AddScriptRuntimeInvoke("toInt32", "(D)I");
			}
			switch (type)
			{
				case Token.BITOR:
				{
					cfw.Add(ByteCode.IOR);
					break;
				}

				case Token.BITXOR:
				{
					cfw.Add(ByteCode.IXOR);
					break;
				}

				case Token.BITAND:
				{
					cfw.Add(ByteCode.IAND);
					break;
				}

				case Token.RSH:
				{
					cfw.Add(ByteCode.ISHR);
					break;
				}

				case Token.LSH:
				{
					cfw.Add(ByteCode.ISHL);
					break;
				}

				default:
				{
					throw Codegen.BadTree();
				}
			}
			cfw.Add(ByteCode.I2D);
			if (childNumberFlag == -1)
			{
				AddDoubleWrap();
			}
		}

		private int NodeIsDirectCallParameter(Node node)
		{
			if (node.GetType() == Token.GETVAR && inDirectCallFunction && !itsForcedObjectParameters)
			{
				int varIndex = fnCurrent.GetVarIndex(node);
				if (fnCurrent.IsParameter(varIndex))
				{
					return varRegisters[varIndex];
				}
			}
			return -1;
		}

		private bool VarIsDirectCallParameter(int varIndex)
		{
			return fnCurrent.IsParameter(varIndex) && inDirectCallFunction && !itsForcedObjectParameters;
		}

		private void GenSimpleCompare(int type, int trueGOTO, int falseGOTO)
		{
			if (trueGOTO == -1)
			{
				throw Codegen.BadTree();
			}
			switch (type)
			{
				case Token.LE:
				{
					cfw.Add(ByteCode.DCMPG);
					cfw.Add(ByteCode.IFLE, trueGOTO);
					break;
				}

				case Token.GE:
				{
					cfw.Add(ByteCode.DCMPL);
					cfw.Add(ByteCode.IFGE, trueGOTO);
					break;
				}

				case Token.LT:
				{
					cfw.Add(ByteCode.DCMPG);
					cfw.Add(ByteCode.IFLT, trueGOTO);
					break;
				}

				case Token.GT:
				{
					cfw.Add(ByteCode.DCMPL);
					cfw.Add(ByteCode.IFGT, trueGOTO);
					break;
				}

				default:
				{
					throw Codegen.BadTree();
				}
			}
			if (falseGOTO != -1)
			{
				cfw.Add(ByteCode.GOTO, falseGOTO);
			}
		}

		private void VisitIfJumpRelOp(Node node, Node child, int trueGOTO, int falseGOTO)
		{
			if (trueGOTO == -1 || falseGOTO == -1)
			{
				throw Codegen.BadTree();
			}
			int type = node.GetType();
			Node rChild = child.GetNext();
			if (type == Token.INSTANCEOF || type == Token.IN)
			{
				GenerateExpression(child, node);
				GenerateExpression(rChild, node);
				cfw.AddALoad(contextLocal);
				AddScriptRuntimeInvoke((type == Token.INSTANCEOF) ? "instanceOf" : "in", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Z");
				cfw.Add(ByteCode.IFNE, trueGOTO);
				cfw.Add(ByteCode.GOTO, falseGOTO);
				return;
			}
			int childNumberFlag = node.GetIntProp(Node.ISNUMBER_PROP, -1);
			int left_dcp_register = NodeIsDirectCallParameter(child);
			int right_dcp_register = NodeIsDirectCallParameter(rChild);
			if (childNumberFlag != -1)
			{
				// Force numeric context on both parameters and optimize
				// direct call case as Optimizer currently does not handle it
				if (childNumberFlag != Node.RIGHT)
				{
					// Left already has number content
					GenerateExpression(child, node);
				}
				else
				{
					if (left_dcp_register != -1)
					{
						DcpLoadAsNumber(left_dcp_register);
					}
					else
					{
						GenerateExpression(child, node);
						AddObjectToDouble();
					}
				}
				if (childNumberFlag != Node.LEFT)
				{
					// Right already has number content
					GenerateExpression(rChild, node);
				}
				else
				{
					if (right_dcp_register != -1)
					{
						DcpLoadAsNumber(right_dcp_register);
					}
					else
					{
						GenerateExpression(rChild, node);
						AddObjectToDouble();
					}
				}
				GenSimpleCompare(type, trueGOTO, falseGOTO);
			}
			else
			{
				if (left_dcp_register != -1 && right_dcp_register != -1)
				{
					// Generate code to dynamically check for number content
					// if both operands are dcp
					short stack = cfw.GetStackTop();
					int leftIsNotNumber = cfw.AcquireLabel();
					cfw.AddALoad(left_dcp_register);
					cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
					cfw.Add(ByteCode.IF_ACMPNE, leftIsNotNumber);
					cfw.AddDLoad(left_dcp_register + 1);
					DcpLoadAsNumber(right_dcp_register);
					GenSimpleCompare(type, trueGOTO, falseGOTO);
					if (stack != cfw.GetStackTop())
					{
						throw Codegen.BadTree();
					}
					cfw.MarkLabel(leftIsNotNumber);
					int rightIsNotNumber = cfw.AcquireLabel();
					cfw.AddALoad(right_dcp_register);
					cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
					cfw.Add(ByteCode.IF_ACMPNE, rightIsNotNumber);
					cfw.AddALoad(left_dcp_register);
					AddObjectToDouble();
					cfw.AddDLoad(right_dcp_register + 1);
					GenSimpleCompare(type, trueGOTO, falseGOTO);
					if (stack != cfw.GetStackTop())
					{
						throw Codegen.BadTree();
					}
					cfw.MarkLabel(rightIsNotNumber);
					// Load both register as objects to call generic cmp_*
					cfw.AddALoad(left_dcp_register);
					cfw.AddALoad(right_dcp_register);
				}
				else
				{
					GenerateExpression(child, node);
					GenerateExpression(rChild, node);
				}
				if (type == Token.GE || type == Token.GT)
				{
					cfw.Add(ByteCode.SWAP);
				}
				string routine = ((type == Token.LT) || (type == Token.GT)) ? "cmp_LT" : "cmp_LE";
				AddScriptRuntimeInvoke(routine, "(Ljava/lang/Object;" + "Ljava/lang/Object;" + ")Z");
				cfw.Add(ByteCode.IFNE, trueGOTO);
				cfw.Add(ByteCode.GOTO, falseGOTO);
			}
		}

		private void VisitIfJumpEqOp(Node node, Node child, int trueGOTO, int falseGOTO)
		{
			if (trueGOTO == -1 || falseGOTO == -1)
			{
				throw Codegen.BadTree();
			}
			short stackInitial = cfw.GetStackTop();
			int type = node.GetType();
			Node rChild = child.GetNext();
			// Optimize if one of operands is null
			if (child.GetType() == Token.NULL || rChild.GetType() == Token.NULL)
			{
				// eq is symmetric in this case
				if (child.GetType() == Token.NULL)
				{
					child = rChild;
				}
				GenerateExpression(child, node);
				if (type == Token.SHEQ || type == Token.SHNE)
				{
					int testCode = (type == Token.SHEQ) ? ByteCode.IFNULL : ByteCode.IFNONNULL;
					cfw.Add(testCode, trueGOTO);
				}
				else
				{
					if (type != Token.EQ)
					{
						// swap false/true targets for !=
						if (type != Token.NE)
						{
							throw Codegen.BadTree();
						}
						int tmp = trueGOTO;
						trueGOTO = falseGOTO;
						falseGOTO = tmp;
					}
					cfw.Add(ByteCode.DUP);
					int undefCheckLabel = cfw.AcquireLabel();
					cfw.Add(ByteCode.IFNONNULL, undefCheckLabel);
					short stack = cfw.GetStackTop();
					cfw.Add(ByteCode.POP);
					cfw.Add(ByteCode.GOTO, trueGOTO);
					cfw.MarkLabel(undefCheckLabel, stack);
					Codegen.PushUndefined(cfw);
					cfw.Add(ByteCode.IF_ACMPEQ, trueGOTO);
				}
				cfw.Add(ByteCode.GOTO, falseGOTO);
			}
			else
			{
				int child_dcp_register = NodeIsDirectCallParameter(child);
				if (child_dcp_register != -1 && rChild.GetType() == Token.TO_OBJECT)
				{
					Node convertChild = rChild.GetFirstChild();
					if (convertChild.GetType() == Token.NUMBER)
					{
						cfw.AddALoad(child_dcp_register);
						cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
						int notNumbersLabel = cfw.AcquireLabel();
						cfw.Add(ByteCode.IF_ACMPNE, notNumbersLabel);
						cfw.AddDLoad(child_dcp_register + 1);
						cfw.AddPush(convertChild.GetDouble());
						cfw.Add(ByteCode.DCMPL);
						if (type == Token.EQ)
						{
							cfw.Add(ByteCode.IFEQ, trueGOTO);
						}
						else
						{
							cfw.Add(ByteCode.IFNE, trueGOTO);
						}
						cfw.Add(ByteCode.GOTO, falseGOTO);
						cfw.MarkLabel(notNumbersLabel);
					}
				}
				// fall thru into generic handling
				GenerateExpression(child, node);
				GenerateExpression(rChild, node);
				string name;
				int testCode;
				switch (type)
				{
					case Token.EQ:
					{
						name = "eq";
						testCode = ByteCode.IFNE;
						break;
					}

					case Token.NE:
					{
						name = "eq";
						testCode = ByteCode.IFEQ;
						break;
					}

					case Token.SHEQ:
					{
						name = "shallowEq";
						testCode = ByteCode.IFNE;
						break;
					}

					case Token.SHNE:
					{
						name = "shallowEq";
						testCode = ByteCode.IFEQ;
						break;
					}

					default:
					{
						throw Codegen.BadTree();
					}
				}
				AddScriptRuntimeInvoke(name, "(Ljava/lang/Object;" + "Ljava/lang/Object;" + ")Z");
				cfw.Add(testCode, trueGOTO);
				cfw.Add(ByteCode.GOTO, falseGOTO);
			}
			if (stackInitial != cfw.GetStackTop())
			{
				throw Codegen.BadTree();
			}
		}

		private void VisitSetName(Node node, Node child)
		{
			string name = node.GetFirstChild().GetString();
			while (child != null)
			{
				GenerateExpression(child, node);
				child = child.GetNext();
			}
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			cfw.AddPush(name);
			AddScriptRuntimeInvoke("setName", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + ")Ljava/lang/Object;");
		}

		private void VisitStrictSetName(Node node, Node child)
		{
			string name = node.GetFirstChild().GetString();
			while (child != null)
			{
				GenerateExpression(child, node);
				child = child.GetNext();
			}
			cfw.AddALoad(contextLocal);
			cfw.AddALoad(variableObjectLocal);
			cfw.AddPush(name);
			AddScriptRuntimeInvoke("strictSetName", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + ")Ljava/lang/Object;");
		}

		private void VisitSetConst(Node node, Node child)
		{
			string name = node.GetFirstChild().GetString();
			while (child != null)
			{
				GenerateExpression(child, node);
				child = child.GetNext();
			}
			cfw.AddALoad(contextLocal);
			cfw.AddPush(name);
			AddScriptRuntimeInvoke("setConst", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + "Ljava/lang/String;" + ")Ljava/lang/Object;");
		}

		private void VisitGetVar(Node node)
		{
			if (!hasVarsInRegs)
			{
				Kit.CodeBug();
			}
			int varIndex = fnCurrent.GetVarIndex(node);
			short reg = varRegisters[varIndex];
			if (VarIsDirectCallParameter(varIndex))
			{
				// Remember that here the isNumber flag means that we
				// want to use the incoming parameter in a Number
				// context, so test the object type and convert the
				//  value as necessary.
				if (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1)
				{
					DcpLoadAsNumber(reg);
				}
				else
				{
					DcpLoadAsObject(reg);
				}
			}
			else
			{
				if (fnCurrent.IsNumberVar(varIndex))
				{
					cfw.AddDLoad(reg);
				}
				else
				{
					cfw.AddALoad(reg);
				}
			}
		}

		private void VisitSetVar(Node node, Node child, bool needValue)
		{
			if (!hasVarsInRegs)
			{
				Kit.CodeBug();
			}
			int varIndex = fnCurrent.GetVarIndex(node);
			GenerateExpression(child.GetNext(), node);
			bool isNumber = (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1);
			short reg = varRegisters[varIndex];
			bool[] constDeclarations = fnCurrent.fnode.GetParamAndVarConst();
			if (constDeclarations[varIndex])
			{
				if (!needValue)
				{
					if (isNumber)
					{
						cfw.Add(ByteCode.POP2);
					}
					else
					{
						cfw.Add(ByteCode.POP);
					}
				}
			}
			else
			{
				if (VarIsDirectCallParameter(varIndex))
				{
					if (isNumber)
					{
						if (needValue)
						{
							cfw.Add(ByteCode.DUP2);
						}
						cfw.AddALoad(reg);
						cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
						int isNumberLabel = cfw.AcquireLabel();
						int beyond = cfw.AcquireLabel();
						cfw.Add(ByteCode.IF_ACMPEQ, isNumberLabel);
						short stack = cfw.GetStackTop();
						AddDoubleWrap();
						cfw.AddAStore(reg);
						cfw.Add(ByteCode.GOTO, beyond);
						cfw.MarkLabel(isNumberLabel, stack);
						cfw.AddDStore(reg + 1);
						cfw.MarkLabel(beyond);
					}
					else
					{
						if (needValue)
						{
							cfw.Add(ByteCode.DUP);
						}
						cfw.AddAStore(reg);
					}
				}
				else
				{
					bool isNumberVar = fnCurrent.IsNumberVar(varIndex);
					if (isNumber)
					{
						if (isNumberVar)
						{
							cfw.AddDStore(reg);
							if (needValue)
							{
								cfw.AddDLoad(reg);
							}
						}
						else
						{
							if (needValue)
							{
								cfw.Add(ByteCode.DUP2);
							}
							// Cannot save number in variable since !isNumberVar,
							// so convert to object
							AddDoubleWrap();
							cfw.AddAStore(reg);
						}
					}
					else
					{
						if (isNumberVar)
						{
							Kit.CodeBug();
						}
						cfw.AddAStore(reg);
						if (needValue)
						{
							cfw.AddALoad(reg);
						}
					}
				}
			}
		}

		private void VisitSetConstVar(Node node, Node child, bool needValue)
		{
			if (!hasVarsInRegs)
			{
				Kit.CodeBug();
			}
			int varIndex = fnCurrent.GetVarIndex(node);
			GenerateExpression(child.GetNext(), node);
			bool isNumber = (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1);
			short reg = varRegisters[varIndex];
			int beyond = cfw.AcquireLabel();
			int noAssign = cfw.AcquireLabel();
			if (isNumber)
			{
				cfw.AddILoad(reg + 2);
				cfw.Add(ByteCode.IFNE, noAssign);
				short stack = cfw.GetStackTop();
				cfw.AddPush(1);
				cfw.AddIStore(reg + 2);
				cfw.AddDStore(reg);
				if (needValue)
				{
					cfw.AddDLoad(reg);
					cfw.MarkLabel(noAssign, stack);
				}
				else
				{
					cfw.Add(ByteCode.GOTO, beyond);
					cfw.MarkLabel(noAssign, stack);
					cfw.Add(ByteCode.POP2);
				}
			}
			else
			{
				cfw.AddILoad(reg + 1);
				cfw.Add(ByteCode.IFNE, noAssign);
				short stack = cfw.GetStackTop();
				cfw.AddPush(1);
				cfw.AddIStore(reg + 1);
				cfw.AddAStore(reg);
				if (needValue)
				{
					cfw.AddALoad(reg);
					cfw.MarkLabel(noAssign, stack);
				}
				else
				{
					cfw.Add(ByteCode.GOTO, beyond);
					cfw.MarkLabel(noAssign, stack);
					cfw.Add(ByteCode.POP);
				}
			}
			cfw.MarkLabel(beyond);
		}

		private void VisitGetProp(Node node, Node child)
		{
			GenerateExpression(child, node);
			// object
			Node nameChild = child.GetNext();
			GenerateExpression(nameChild, node);
			// the name
			if (node.GetType() == Token.GETPROPNOWARN)
			{
				cfw.AddALoad(contextLocal);
				AddScriptRuntimeInvoke("getObjectPropNoWarn", "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
				return;
			}
			int childType = child.GetType();
			if (childType == Token.THIS && nameChild.GetType() == Token.STRING)
			{
				cfw.AddALoad(contextLocal);
				AddScriptRuntimeInvoke("getObjectProp", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
			}
			else
			{
				cfw.AddALoad(contextLocal);
				cfw.AddALoad(variableObjectLocal);
				AddScriptRuntimeInvoke("getObjectProp", "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;");
			}
		}

		private void VisitSetProp(int type, Node node, Node child)
		{
			Node objectChild = child;
			GenerateExpression(child, node);
			child = child.GetNext();
			if (type == Token.SETPROP_OP)
			{
				cfw.Add(ByteCode.DUP);
			}
			Node nameChild = child;
			GenerateExpression(child, node);
			child = child.GetNext();
			if (type == Token.SETPROP_OP)
			{
				// stack: ... object object name -> ... object name object name
				cfw.Add(ByteCode.DUP_X1);
				//for 'this.foo += ...' we call thisGet which can skip some
				//casting overhead.
				if (objectChild.GetType() == Token.THIS && nameChild.GetType() == Token.STRING)
				{
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("getObjectProp", "(Lorg/mozilla/javascript/Scriptable;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
				}
				else
				{
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("getObjectProp", "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
				}
			}
			GenerateExpression(child, node);
			cfw.AddALoad(contextLocal);
			AddScriptRuntimeInvoke("setObjectProp", "(Ljava/lang/Object;" + "Ljava/lang/String;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
		}

		private void VisitSetElem(int type, Node node, Node child)
		{
			GenerateExpression(child, node);
			child = child.GetNext();
			if (type == Token.SETELEM_OP)
			{
				cfw.Add(ByteCode.DUP);
			}
			GenerateExpression(child, node);
			child = child.GetNext();
			bool indexIsNumber = (node.GetIntProp(Node.ISNUMBER_PROP, -1) != -1);
			if (type == Token.SETELEM_OP)
			{
				if (indexIsNumber)
				{
					// stack: ... object object number
					//        -> ... object number object number
					cfw.Add(ByteCode.DUP2_X1);
					cfw.AddALoad(contextLocal);
					AddOptRuntimeInvoke("getObjectIndex", "(Ljava/lang/Object;D" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
				}
				else
				{
					// stack: ... object object indexObject
					//        -> ... object indexObject object indexObject
					cfw.Add(ByteCode.DUP_X1);
					cfw.AddALoad(contextLocal);
					AddScriptRuntimeInvoke("getObjectElem", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
				}
			}
			GenerateExpression(child, node);
			cfw.AddALoad(contextLocal);
			if (indexIsNumber)
			{
				AddScriptRuntimeInvoke("setObjectIndex", "(Ljava/lang/Object;" + "D" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
			}
			else
			{
				AddScriptRuntimeInvoke("setObjectElem", "(Ljava/lang/Object;" + "Ljava/lang/Object;" + "Ljava/lang/Object;" + "Lorg/mozilla/javascript/Context;" + ")Ljava/lang/Object;");
			}
		}

		private void VisitDotQuery(Node node, Node child)
		{
			UpdateLineNumber(node);
			GenerateExpression(child, node);
			cfw.AddALoad(variableObjectLocal);
			AddScriptRuntimeInvoke("enterDotQuery", "(Ljava/lang/Object;" + "Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.AddAStore(variableObjectLocal);
			// add push null/pop with label in between to simplify code for loop
			// continue when it is necessary to pop the null result from
			// updateDotQuery
			cfw.Add(ByteCode.ACONST_NULL);
			int queryLoopStart = cfw.AcquireLabel();
			cfw.MarkLabel(queryLoopStart);
			// loop continue jumps here
			cfw.Add(ByteCode.POP);
			GenerateExpression(child.GetNext(), node);
			AddScriptRuntimeInvoke("toBoolean", "(Ljava/lang/Object;)Z");
			cfw.AddALoad(variableObjectLocal);
			AddScriptRuntimeInvoke("updateDotQuery", "(Z" + "Lorg/mozilla/javascript/Scriptable;" + ")Ljava/lang/Object;");
			cfw.Add(ByteCode.DUP);
			cfw.Add(ByteCode.IFNULL, queryLoopStart);
			// stack: ... non_null_result_of_updateDotQuery
			cfw.AddALoad(variableObjectLocal);
			AddScriptRuntimeInvoke("leaveDotQuery", "(Lorg/mozilla/javascript/Scriptable;" + ")Lorg/mozilla/javascript/Scriptable;");
			cfw.AddAStore(variableObjectLocal);
		}

		private int GetLocalBlockRegister(Node node)
		{
			Node localBlock = (Node)node.GetProp(Node.LOCAL_BLOCK_PROP);
			int localSlot = localBlock.GetExistingIntProp(Node.LOCAL_PROP);
			return localSlot;
		}

		private void DcpLoadAsNumber(int dcp_register)
		{
			cfw.AddALoad(dcp_register);
			cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
			int isNumberLabel = cfw.AcquireLabel();
			cfw.Add(ByteCode.IF_ACMPEQ, isNumberLabel);
			short stack = cfw.GetStackTop();
			cfw.AddALoad(dcp_register);
			AddObjectToDouble();
			int beyond = cfw.AcquireLabel();
			cfw.Add(ByteCode.GOTO, beyond);
			cfw.MarkLabel(isNumberLabel, stack);
			cfw.AddDLoad(dcp_register + 1);
			cfw.MarkLabel(beyond);
		}

		private void DcpLoadAsObject(int dcp_register)
		{
			cfw.AddALoad(dcp_register);
			cfw.Add(ByteCode.GETSTATIC, "java/lang/Void", "TYPE", "Ljava/lang/Class;");
			int isNumberLabel = cfw.AcquireLabel();
			cfw.Add(ByteCode.IF_ACMPEQ, isNumberLabel);
			short stack = cfw.GetStackTop();
			cfw.AddALoad(dcp_register);
			int beyond = cfw.AcquireLabel();
			cfw.Add(ByteCode.GOTO, beyond);
			cfw.MarkLabel(isNumberLabel, stack);
			cfw.AddDLoad(dcp_register + 1);
			AddDoubleWrap();
			cfw.MarkLabel(beyond);
		}

		private void AddGoto(Node target, int jumpcode)
		{
			int targetLabel = GetTargetLabel(target);
			cfw.Add(jumpcode, targetLabel);
		}

		private void AddObjectToDouble()
		{
			AddScriptRuntimeInvoke("toNumber", "(Ljava/lang/Object;)D");
		}

		private void AddNewObjectArray(int size)
		{
			if (size == 0)
			{
				if (itsZeroArgArray >= 0)
				{
					cfw.AddALoad(itsZeroArgArray);
				}
				else
				{
					cfw.Add(ByteCode.GETSTATIC, "org/mozilla/javascript/ScriptRuntime", "emptyArgs", "[Ljava/lang/Object;");
				}
			}
			else
			{
				cfw.AddPush(size);
				cfw.Add(ByteCode.ANEWARRAY, "java/lang/Object");
			}
		}

		private void AddScriptRuntimeInvoke(string methodName, string methodSignature)
		{
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "Rhino.ScriptRuntime", methodName, methodSignature);
		}

		private void AddOptRuntimeInvoke(string methodName, string methodSignature)
		{
			cfw.AddInvoke(ByteCode.INVOKESTATIC, "org/mozilla/javascript/optimizer/OptRuntime", methodName, methodSignature);
		}

		private void AddJumpedBooleanWrap(int trueLabel, int falseLabel)
		{
			cfw.MarkLabel(falseLabel);
			int skip = cfw.AcquireLabel();
			cfw.Add(ByteCode.GETSTATIC, "java/lang/Boolean", "FALSE", "Ljava/lang/Boolean;");
			cfw.Add(ByteCode.GOTO, skip);
			cfw.MarkLabel(trueLabel);
			cfw.Add(ByteCode.GETSTATIC, "java/lang/Boolean", "TRUE", "Ljava/lang/Boolean;");
			cfw.MarkLabel(skip);
			cfw.AdjustStackTop(-1);
		}

		// only have 1 of true/false
		private void AddDoubleWrap()
		{
			AddOptRuntimeInvoke("wrapDouble", "(D)Ljava/lang/Double;");
		}

		/// <summary>
		/// Const locals use an extra slot to hold the has-been-assigned-once flag at
		/// runtime.
		/// </summary>
		/// <remarks>
		/// Const locals use an extra slot to hold the has-been-assigned-once flag at
		/// runtime.
		/// </remarks>
		/// <param name="isConst">true iff the variable is const</param>
		/// <returns>the register for the word pair (double/long)</returns>
		private short GetNewWordPairLocal(bool isConst)
		{
			short result = GetConsecutiveSlots(2, isConst);
			if (result < (MAX_LOCALS - 1))
			{
				locals[result] = 1;
				locals[result + 1] = 1;
				if (isConst)
				{
					locals[result + 2] = 1;
				}
				if (result == firstFreeLocal)
				{
					for (int i = firstFreeLocal + 2; i < MAX_LOCALS; i++)
					{
						if (locals[i] == 0)
						{
							firstFreeLocal = (short)i;
							if (localsMax < firstFreeLocal)
							{
								localsMax = firstFreeLocal;
							}
							return result;
						}
					}
				}
				else
				{
					return result;
				}
			}
			throw Context.ReportRuntimeError("Program too complex " + "(out of locals)");
		}

		private short GetNewWordLocal(bool isConst)
		{
			short result = GetConsecutiveSlots(1, isConst);
			if (result < (MAX_LOCALS - 1))
			{
				locals[result] = 1;
				if (isConst)
				{
					locals[result + 1] = 1;
				}
				if (result == firstFreeLocal)
				{
					for (int i = firstFreeLocal + 2; i < MAX_LOCALS; i++)
					{
						if (locals[i] == 0)
						{
							firstFreeLocal = (short)i;
							if (localsMax < firstFreeLocal)
							{
								localsMax = firstFreeLocal;
							}
							return result;
						}
					}
				}
				else
				{
					return result;
				}
			}
			throw Context.ReportRuntimeError("Program too complex " + "(out of locals)");
		}

		private short GetNewWordLocal()
		{
			short result = firstFreeLocal;
			locals[result] = 1;
			for (int i = firstFreeLocal + 1; i < MAX_LOCALS; i++)
			{
				if (locals[i] == 0)
				{
					firstFreeLocal = (short)i;
					if (localsMax < firstFreeLocal)
					{
						localsMax = firstFreeLocal;
					}
					return result;
				}
			}
			throw Context.ReportRuntimeError("Program too complex " + "(out of locals)");
		}

		private short GetConsecutiveSlots(int count, bool isConst)
		{
			if (isConst)
			{
				count++;
			}
			short result = firstFreeLocal;
			while (true)
			{
				if (result >= (MAX_LOCALS - 1))
				{
					break;
				}
				int i;
				for (i = 0; i < count; i++)
				{
					if (locals[result + i] != 0)
					{
						break;
					}
				}
				if (i >= count)
				{
					break;
				}
				result++;
			}
			return result;
		}

		// This is a valid call only for a local that is allocated by default.
		private void IncReferenceWordLocal(short local)
		{
			locals[local]++;
		}

		// This is a valid call only for a local that is allocated by default.
		private void DecReferenceWordLocal(short local)
		{
			locals[local]--;
		}

		private void ReleaseWordLocal(short local)
		{
			if (local < firstFreeLocal)
			{
				firstFreeLocal = local;
			}
			locals[local] = 0;
		}

		internal const int GENERATOR_TERMINATE = -1;

		internal const int GENERATOR_START = 0;

		internal const int GENERATOR_YIELD_START = 1;

		internal ClassFileWriter cfw;

		internal Codegen codegen;

		internal CompilerEnvirons compilerEnv;

		internal ScriptNode scriptOrFn;

		public int scriptOrFnIndex;

		private int savedCodeOffset;

		private OptFunctionNode fnCurrent;

		private const int MAX_LOCALS = 1024;

		private int[] locals;

		private short firstFreeLocal;

		private short localsMax;

		private int itsLineNumber;

		private bool hasVarsInRegs;

		private short[] varRegisters;

		private bool inDirectCallFunction;

		private bool itsForcedObjectParameters;

		private int enterAreaStartLabel;

		private int epilogueLabel;

		private bool inLocalBlock;

		private short variableObjectLocal;

		private short popvLocal;

		private short contextLocal;

		private short argsLocal;

		private short operationLocal;

		private short thisObjLocal;

		private short funObjLocal;

		private short itsZeroArgArray;

		private short itsOneArgArray;

		private short generatorStateLocal;

		private bool isGenerator;

		private int generatorSwitch;

		private int maxLocals = 0;

		private int maxStack = 0;

		private IDictionary<Node, BodyCodegen.FinallyReturnPoint> finallys;

		private IList<Node> literals;

		internal class FinallyReturnPoint
		{
			public IList<int> jsrPoints = new List<int>();

			public int tableLabel = 0;
			// special known locals. If you add a new local here, be sure
			// to initialize it to -1 in initBodyGeneration
		}

		public BodyCodegen()
		{
			exceptionManager = new BodyCodegen.ExceptionManager(this);
		}
	}
}
