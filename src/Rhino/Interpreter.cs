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
using System.Linq;
using System.Text;
using Rhino.Ast;
using Rhino.Debug;
using Rhino.Utils;

namespace Rhino
{
	public sealed class Interpreter : Icode, Evaluator
	{
		internal const int EXCEPTION_TRY_START_SLOT = 0;

		internal const int EXCEPTION_TRY_END_SLOT = 1;

		internal const int EXCEPTION_HANDLER_SLOT = 2;

		internal const int EXCEPTION_TYPE_SLOT = 3;

		internal const int EXCEPTION_LOCAL_SLOT = 4;

		internal const int EXCEPTION_SCOPE_SLOT = 5;

		internal const int EXCEPTION_SLOT_SIZE = 6;

		/// <summary>Class to hold data corresponding to one interpreted call stack frame.</summary>
		/// <remarks>Class to hold data corresponding to one interpreted call stack frame.</remarks>
		[System.Serializable]
		private class CallFrame : ICloneable
		{
			internal Interpreter.CallFrame parentFrame;

			internal int frameIndex;

			internal bool frozen;

			internal InterpretedFunction fnOrScript;

			internal InterpreterData idata;

			internal object[] stack;

            internal PropertyAttributes[] stackAttributes;

			internal double[] sDbl;

			internal Interpreter.CallFrame varSource;

			internal int localShift;

			internal int emptyStackTop;

			internal DebugFrame debuggerFrame;

			internal bool useActivation;

			internal bool isContinuationsTopFrame;

			internal Scriptable thisObj;

			internal object result;

			internal double resultDbl;

			internal int pc;

			internal int pcPrevBranch;

			internal int pcSourceLineStart;

			internal Scriptable scope;

			internal int savedStackTop;

			internal int savedCallOp;

			internal object throwable;

			// data for parsing
			// SLOT_SIZE: space for try start/end, handler, start, handler type,
			//            exception local and scope local
			// amount of stack frames before this one on the interpretation stack
			// If true indicates read-only frame that is a part of continuation
			// Stack structure
			// stack[0 <= i < localShift]: arguments and local variables
			// stack[localShift <= i <= emptyStackTop]: used for local temporaries
			// stack[emptyStackTop < i < stack.length]: stack data
			// sDbl[i]: if stack[i] is UniqueTag.DOUBLE_MARK, sDbl[i] holds the number value
			// defaults to this unless continuation frame
			// The values that change during interpretation
			internal virtual Interpreter.CallFrame CloneFrozen()
			{
				if (!frozen)
				{
					Kit.CodeBug();
				}
				Interpreter.CallFrame copy;
				copy = (Interpreter.CallFrame) MemberwiseClone();
				// clone stack but keep varSource to point to values
				// from this frame to share variables.
				copy.stack = (object[]) stack.Clone();
				copy.stackAttributes = (PropertyAttributes[]) stackAttributes.Clone();
				copy.sDbl = (double[]) sDbl.Clone();
				copy.frozen = false;
				return copy;
			}

			public object Clone()
			{
				return MemberwiseClone();
			}
		}

		[System.Serializable]
		private sealed class ContinuationJump
		{
			internal Interpreter.CallFrame capturedFrame;

			internal Interpreter.CallFrame branchFrame;

			internal object result;

			internal double resultDbl;

			internal ContinuationJump(NativeContinuation c, Interpreter.CallFrame current)
			{
				this.capturedFrame = (Interpreter.CallFrame)c.GetImplementation();
				if (this.capturedFrame == null || current == null)
				{
					// Continuation and current execution does not share
					// any frames if there is nothing to capture or
					// if there is no currently executed frames
					this.branchFrame = null;
				}
				else
				{
					// Search for branch frame where parent frame chains starting
					// from captured and current meet.
					Interpreter.CallFrame chain1 = this.capturedFrame;
					Interpreter.CallFrame chain2 = current;
					// First work parents of chain1 or chain2 until the same
					// frame depth.
					int diff = chain1.frameIndex - chain2.frameIndex;
					if (diff != 0)
					{
						if (diff < 0)
						{
							// swap to make sure that
							// chain1.frameIndex > chain2.frameIndex and diff > 0
							chain1 = current;
							chain2 = this.capturedFrame;
							diff = -diff;
						}
						do
						{
							chain1 = chain1.parentFrame;
						}
						while (--diff != 0);
						if (chain1.frameIndex != chain2.frameIndex)
						{
							Kit.CodeBug();
						}
					}
					// Now walk parents in parallel until a shared frame is found
					// or until the root is reached.
					while (chain1 != chain2 && chain1 != null)
					{
						chain1 = chain1.parentFrame;
						chain2 = chain2.parentFrame;
					}
					this.branchFrame = chain1;
					if (this.branchFrame != null && !this.branchFrame.frozen)
					{
						Kit.CodeBug();
					}
				}
			}
		}

		private static Interpreter.CallFrame CaptureFrameForGenerator(Interpreter.CallFrame frame)
		{
			frame.frozen = true;
			Interpreter.CallFrame result = frame.CloneFrozen();
			frame.frozen = false;
			// now isolate this frame from its previous context
			result.parentFrame = null;
			result.frameIndex = 0;
			return result;
		}

		static Interpreter()
		{
			// Checks for byte code consistencies, good compiler can eliminate them
			if (Token.LAST_BYTECODE_TOKEN > 127)
			{
				string str = "Violation of Token.LAST_BYTECODE_TOKEN <= 127";
				System.Console.Error.WriteLine(str);
				throw new InvalidOperationException(str);
			}
			if (MIN_ICODE < -128)
			{
				string str = "Violation of Interpreter.MIN_ICODE >= -128";
				System.Console.Error.WriteLine(str);
				throw new InvalidOperationException(str);
			}
		}

		public Script CreateScriptObject(CompilerEnvirons compilerEnv, ScriptNode tree, object staticSecurityDomain, Action<object> debug)
		{
			CodeGenerator cgen = new CodeGenerator();
			var idata = cgen.Compile(compilerEnv, tree, tree.GetEncodedSource(), false);
			debug(idata);
			return InterpretedFunction.CreateScript(idata, staticSecurityDomain);
		}

		public Function CreateFunctionObject(CompilerEnvirons compilerEnv, ScriptNode tree, Context cx, Scriptable scope, object staticSecurityDomain, Action<object> debug)
		{
			CodeGenerator cgen = new CodeGenerator();
			var idata = cgen.Compile(compilerEnv, tree, tree.GetEncodedSource(), true);
			debug(idata);
			return InterpretedFunction.CreateFunction(cx, scope, idata, staticSecurityDomain);
		}

		public void SetEvalScriptFlag(Script script)
		{
			((InterpretedFunction)script).idata.evalScriptFlag = true;
		}

		private static int GetShort(sbyte[] iCode, int pc)
		{
			return (iCode[pc] << 8) | (iCode[pc + 1] & unchecked((int)(0xFF)));
		}

		private static int GetIndex(sbyte[] iCode, int pc)
		{
			return ((iCode[pc] & unchecked((int)(0xFF))) << 8) | (iCode[pc + 1] & unchecked((int)(0xFF)));
		}

		private static int GetInt(sbyte[] iCode, int pc)
		{
			return (iCode[pc] << 24) | ((iCode[pc + 1] & unchecked((int)(0xFF))) << 16) | ((iCode[pc + 2] & unchecked((int)(0xFF))) << 8) | (iCode[pc + 3] & unchecked((int)(0xFF)));
		}

		private static int GetExceptionHandler(Interpreter.CallFrame frame, bool onlyFinally)
		{
			int[] exceptionTable = frame.idata.itsExceptionTable;
			if (exceptionTable == null)
			{
				// No exception handlers
				return -1;
			}
			// Icode switch in the interpreter increments PC immediately
			// and it is necessary to subtract 1 from the saved PC
			// to point it before the start of the next instruction.
			int pc = frame.pc - 1;
			// OPT: use binary search
			int best = -1;
			int bestStart = 0;
			int bestEnd = 0;
			for (int i = 0; i != exceptionTable.Length; i += EXCEPTION_SLOT_SIZE)
			{
				int start = exceptionTable[i + EXCEPTION_TRY_START_SLOT];
				int end = exceptionTable[i + EXCEPTION_TRY_END_SLOT];
				if (!(start <= pc && pc < end))
				{
					continue;
				}
				if (onlyFinally && exceptionTable[i + EXCEPTION_TYPE_SLOT] != 1)
				{
					continue;
				}
				if (best >= 0)
				{
					// Since handlers always nest and they never have shared end
					// although they can share start  it is sufficient to compare
					// handlers ends
					if (bestEnd < end)
					{
						continue;
					}
					// Check the above assumption
					if (bestStart > start)
					{
						Kit.CodeBug();
					}
					// should be nested
					if (bestEnd == end)
					{
						Kit.CodeBug();
					}
				}
				// no ens sharing
				best = i;
				bestStart = start;
				bestEnd = end;
			}
			return best;
		}

		internal static void DumpICode(InterpreterData idata)
		{
			return;
			sbyte[] iCode = idata.itsICode;
			int iCodeLength = iCode.Length;
			string[] strings = idata.itsStringTable;
			TextWriter @out = System.Console.Out;
			@out.WriteLine("ICode dump, for " + idata.itsName + ", length = " + iCodeLength);
			@out.WriteLine("MaxStack = " + idata.itsMaxStack);
			int indexReg = 0;
			for (int pc = 0; pc < iCodeLength; )
			{
				@out.Flush();
				@out.Write(" [" + pc + "] ");
				int token = iCode[pc];
				int icodeLength = BytecodeSpan(token);
				string tname = Icode.BytecodeName(token);
				int old_pc = pc;
				++pc;
				switch (token)
				{
					default:
					{
						if (icodeLength != 1)
						{
							Kit.CodeBug();
						}
						@out.WriteLine(tname);
						break;
					}

					case Icode_GOSUB:
					case Token.GOTO:
					case Token.IFEQ:
					case Token.IFNE:
					case Icode_IFEQ_POP:
					case Icode_LEAVEDQ:
					{
						int newPC = pc + GetShort(iCode, pc) - 1;
						@out.WriteLine(tname + " " + newPC);
						pc += 2;
						break;
					}

					case Icode_VAR_INC_DEC:
					case Icode_NAME_INC_DEC:
					case Icode_PROP_INC_DEC:
					case Icode_ELEM_INC_DEC:
					case Icode_REF_INC_DEC:
					{
						int incrDecrType = iCode[pc];
						@out.WriteLine(tname + " " + incrDecrType);
						++pc;
						break;
					}

					case Icode_CALLSPECIAL:
					{
						int callType = iCode[pc] & unchecked((int)(0xFF));
						bool isNew = (iCode[pc + 1] != 0);
						int line = GetIndex(iCode, pc + 2);
						@out.WriteLine(tname + " " + callType + " " + isNew + " " + indexReg + " " + line);
						pc += 4;
						break;
					}

					case Token.CATCH_SCOPE:
					{
						bool afterFisrtFlag = (iCode[pc] != 0);
						@out.WriteLine(tname + " " + afterFisrtFlag);
						++pc;
						break;
					}

					case Token.REGEXP:
					{
						@out.WriteLine(tname + " " + idata.itsRegExpLiterals[indexReg]);
						break;
					}

					case Token.OBJECTLIT:
					case Icode_SPARE_ARRAYLIT:
					{
						@out.WriteLine(tname + " " + idata.literalIds[indexReg]);
						break;
					}

					case Icode_CLOSURE_EXPR:
					case Icode_CLOSURE_STMT:
					{
						@out.WriteLine(tname + " " + idata.itsNestedFunctions[indexReg]);
						break;
					}

					case Token.CALL:
					case Icode_TAIL_CALL:
					case Token.REF_CALL:
					case Token.NEW:
					{
						@out.WriteLine(tname + ' ' + indexReg);
						break;
					}

					case Token.THROW:
					case Token.YIELD:
					case Icode_GENERATOR:
					case Icode_GENERATOR_END:
					{
						int line = GetIndex(iCode, pc);
						@out.WriteLine(tname + " : " + line);
						pc += 2;
						break;
					}

					case Icode_SHORTNUMBER:
					{
						int value = GetShort(iCode, pc);
						@out.WriteLine(tname + " " + value);
						pc += 2;
						break;
					}

					case Icode_INTNUMBER:
					{
						int value = GetInt(iCode, pc);
						@out.WriteLine(tname + " " + value);
						pc += 4;
						break;
					}

					case Token.NUMBER:
					{
						double value = idata.itsDoubleTable[indexReg];
						@out.WriteLine(tname + " " + value);
						break;
					}

					case Icode_LINE:
					{
						int line = GetIndex(iCode, pc);
						@out.WriteLine(tname + " : " + line);
						pc += 2;
						break;
					}

					case Icode_REG_STR1:
					{
						string str = strings[unchecked((int)(0xFF)) & iCode[pc]];
						@out.WriteLine(tname + " \"" + str + '"');
						++pc;
						break;
					}

					case Icode_REG_STR2:
					{
						string str = strings[GetIndex(iCode, pc)];
						@out.WriteLine(tname + " \"" + str + '"');
						pc += 2;
						break;
					}

					case Icode_REG_STR4:
					{
						string str = strings[GetInt(iCode, pc)];
						@out.WriteLine(tname + " \"" + str + '"');
						pc += 4;
						break;
					}

					case Icode_REG_IND_C0:
					{
						indexReg = 0;
						@out.WriteLine(tname);
						break;
					}

					case Icode_REG_IND_C1:
					{
						indexReg = 1;
						@out.WriteLine(tname);
						break;
					}

					case Icode_REG_IND_C2:
					{
						indexReg = 2;
						@out.WriteLine(tname);
						break;
					}

					case Icode_REG_IND_C3:
					{
						indexReg = 3;
						@out.WriteLine(tname);
						break;
					}

					case Icode_REG_IND_C4:
					{
						indexReg = 4;
						@out.WriteLine(tname);
						break;
					}

					case Icode_REG_IND_C5:
					{
						indexReg = 5;
						@out.WriteLine(tname);
						break;
					}

					case Icode_REG_IND1:
					{
						indexReg = unchecked((int)(0xFF)) & iCode[pc];
						@out.WriteLine(tname + " " + indexReg);
						++pc;
						break;
					}

					case Icode_REG_IND2:
					{
						indexReg = GetIndex(iCode, pc);
						@out.WriteLine(tname + " " + indexReg);
						pc += 2;
						break;
					}

					case Icode_REG_IND4:
					{
						indexReg = GetInt(iCode, pc);
						@out.WriteLine(tname + " " + indexReg);
						pc += 4;
						break;
					}

					case Icode_GETVAR1:
					case Icode_SETVAR1:
					case Icode_SETCONSTVAR1:
					{
						indexReg = iCode[pc];
						@out.WriteLine(tname + " " + indexReg);
						++pc;
						break;
					}
				}
				if (old_pc + icodeLength != pc)
				{
					Kit.CodeBug();
				}
			}
			int[] table = idata.itsExceptionTable;
			if (table != null)
			{
				@out.WriteLine("Exception handlers: " + table.Length / EXCEPTION_SLOT_SIZE);
				for (int i = 0; i != table.Length; i += EXCEPTION_SLOT_SIZE)
				{
					int tryStart = table[i + EXCEPTION_TRY_START_SLOT];
					int tryEnd = table[i + EXCEPTION_TRY_END_SLOT];
					int handlerStart = table[i + EXCEPTION_HANDLER_SLOT];
					int type = table[i + EXCEPTION_TYPE_SLOT];
					int exceptionLocal = table[i + EXCEPTION_LOCAL_SLOT];
					int scopeLocal = table[i + EXCEPTION_SCOPE_SLOT];
					@out.WriteLine(" tryStart=" + tryStart + " tryEnd=" + tryEnd + " handlerStart=" + handlerStart + " type=" + (type == 0 ? "catch" : "finally") + " exceptionLocal=" + exceptionLocal);
				}
			}
			@out.Flush();
		}

		private static int BytecodeSpan(int bytecode)
		{
			switch (bytecode)
			{
				case Token.THROW:
				case Token.YIELD:
				case Icode_GENERATOR:
				case Icode_GENERATOR_END:
				{
					// source line
					return 1 + 2;
				}

				case Icode_GOSUB:
				case Token.GOTO:
				case Token.IFEQ:
				case Token.IFNE:
				case Icode_IFEQ_POP:
				case Icode_LEAVEDQ:
				{
					// target pc offset
					return 1 + 2;
				}

				case Icode_CALLSPECIAL:
				{
					// call type
					// is new
					// line number
					return 1 + 1 + 1 + 2;
				}

				case Token.CATCH_SCOPE:
				{
					// scope flag
					return 1 + 1;
				}

				case Icode_VAR_INC_DEC:
				case Icode_NAME_INC_DEC:
				case Icode_PROP_INC_DEC:
				case Icode_ELEM_INC_DEC:
				case Icode_REF_INC_DEC:
				{
					// type of ++/--
					return 1 + 1;
				}

				case Icode_SHORTNUMBER:
				{
					// short number
					return 1 + 2;
				}

				case Icode_INTNUMBER:
				{
					// int number
					return 1 + 4;
				}

				case Icode_REG_IND1:
				{
					// ubyte index
					return 1 + 1;
				}

				case Icode_REG_IND2:
				{
					// ushort index
					return 1 + 2;
				}

				case Icode_REG_IND4:
				{
					// int index
					return 1 + 4;
				}

				case Icode_REG_STR1:
				{
					// ubyte string index
					return 1 + 1;
				}

				case Icode_REG_STR2:
				{
					// ushort string index
					return 1 + 2;
				}

				case Icode_REG_STR4:
				{
					// int string index
					return 1 + 4;
				}

				case Icode_GETVAR1:
				case Icode_SETVAR1:
				case Icode_SETCONSTVAR1:
				{
					// byte var index
					return 1 + 1;
				}

				case Icode_LINE:
				{
					// line number
					return 1 + 2;
				}
			}
			if (!ValidBytecode(bytecode))
			{
				throw Kit.CodeBug();
			}
			return 1;
		}

		internal static int[] GetLineNumbers(InterpreterData data)
		{
			UintMap presentLines = new UintMap();
			sbyte[] iCode = data.itsICode;
			int iCodeLength = iCode.Length;
			for (int pc = 0; pc != iCodeLength; )
			{
				int bytecode = iCode[pc];
				int span = BytecodeSpan(bytecode);
				if (bytecode == Icode_LINE)
				{
					if (span != 3)
					{
						Kit.CodeBug();
					}
					int line = GetIndex(iCode, pc + 1);
					presentLines.Put(line, 0);
				}
				pc += span;
			}
			return presentLines.GetKeys();
		}

		public void CaptureStackInfo(RhinoException ex)
		{
			Context cx = Context.GetCurrentContext();
			if (cx == null || cx.lastInterpreterFrame == null)
			{
				// No interpreter invocations
				ex.interpreterStackInfo = null;
				ex.interpreterLineData = null;
				return;
			}
			// has interpreter frame on the stack
			Interpreter.CallFrame[] array;
			if (cx.previousInterpreterInvocations == null || cx.previousInterpreterInvocations.Size() == 0)
			{
				array = new Interpreter.CallFrame[1];
			}
			else
			{
				int previousCount = cx.previousInterpreterInvocations.Size();
				if (cx.previousInterpreterInvocations.Peek() == cx.lastInterpreterFrame)
				{
					// It can happen if exception was generated after
					// frame was pushed to cx.previousInterpreterInvocations
					// but before assignment to cx.lastInterpreterFrame.
					// In this case frames has to be ignored.
					--previousCount;
				}
				array = new Interpreter.CallFrame[previousCount + 1];
				cx.previousInterpreterInvocations.ToArray(array);
			}
			array[array.Length - 1] = (Interpreter.CallFrame)cx.lastInterpreterFrame;
			int interpreterFrameCount = 0;
			for (int i = 0; i != array.Length; ++i)
			{
				interpreterFrameCount += 1 + array[i].frameIndex;
			}
			int[] linePC = new int[interpreterFrameCount];
			// Fill linePC with pc positions from all interpreter frames.
			// Start from the most nested frame
			int linePCIndex = interpreterFrameCount;
			for (int i_1 = array.Length; i_1 != 0; )
			{
				--i_1;
				Interpreter.CallFrame frame = array[i_1];
				while (frame != null)
				{
					--linePCIndex;
					linePC[linePCIndex] = frame.pcSourceLineStart;
					frame = frame.parentFrame;
				}
			}
			if (linePCIndex != 0)
			{
				Kit.CodeBug();
			}
			ex.interpreterStackInfo = array;
			ex.interpreterLineData = linePC;
		}

		public string GetSourcePositionFromStack(Context cx, int[] linep)
		{
			Interpreter.CallFrame frame = (Interpreter.CallFrame)cx.lastInterpreterFrame;
			InterpreterData idata = frame.idata;
			if (frame.pcSourceLineStart >= 0)
			{
				linep[0] = GetIndex(idata.itsICode, frame.pcSourceLineStart);
			}
			else
			{
				linep[0] = 0;
			}
			return idata.itsSourceFile;
		}

		public string GetPatchedStack(RhinoException ex, string nativeStackTrace)
		{
			string tag = "Rhino.Interpreter.interpretLoop";
			StringBuilder sb = new StringBuilder(nativeStackTrace.Length + 1000);
			string lineSeparator = Environment.NewLine;
			Interpreter.CallFrame[] array = (Interpreter.CallFrame[])ex.interpreterStackInfo;
			int[] linePC = ex.interpreterLineData;
			int arrayIndex = array.Length;
			int linePCIndex = linePC.Length;
			int offset = 0;
			while (arrayIndex != 0)
			{
				--arrayIndex;
				int pos = nativeStackTrace.IndexOf(tag, offset);
				if (pos < 0)
				{
					break;
				}
				// Skip tag length
				pos += tag.Length;
				// Skip until the end of line
				for (; pos != nativeStackTrace.Length; ++pos)
				{
					char c = nativeStackTrace[pos];
					if (c == '\n' || c == '\r')
					{
						break;
					}
				}
				sb.Append(nativeStackTrace.Substring(offset, pos - offset));
				offset = pos;
				Interpreter.CallFrame frame = array[arrayIndex];
				while (frame != null)
				{
					if (linePCIndex == 0)
					{
						Kit.CodeBug();
					}
					--linePCIndex;
					InterpreterData idata = frame.idata;
					sb.Append(lineSeparator);
					sb.Append("\tat script");
					if (!string.IsNullOrEmpty(idata.itsName))
					{
						sb.Append('.');
						sb.Append(idata.itsName);
					}
					sb.Append('(');
					sb.Append(idata.itsSourceFile);
					int pc = linePC[linePCIndex];
					if (pc >= 0)
					{
						// Include line info only if available
						sb.Append(':');
						sb.Append(GetIndex(idata.itsICode, pc));
					}
					sb.Append(')');
					frame = frame.parentFrame;
				}
			}
			sb.Append(nativeStackTrace.Substring(offset));
			return sb.ToString();
		}

		public IList<string> GetScriptStack(RhinoException ex)
		{
			ScriptStackElement[][] stack = GetScriptStackElements(ex);
			IList<string> list = new List<string>(stack.Length);
			string lineSeparator = Environment.NewLine;
			foreach (ScriptStackElement[] group in stack)
			{
				StringBuilder sb = new StringBuilder();
				foreach (ScriptStackElement elem in group)
				{
					elem.RenderJavaStyle(sb);
					sb.Append(lineSeparator);
				}
				list.Add(sb.ToString());
			}
			return list;
		}

		public ScriptStackElement[][] GetScriptStackElements(RhinoException ex)
		{
			if (ex.interpreterStackInfo == null)
			{
				return null;
			}
			IList<ScriptStackElement[]> list = new List<ScriptStackElement[]>();
			Interpreter.CallFrame[] array = (Interpreter.CallFrame[])ex.interpreterStackInfo;
			int[] linePC = ex.interpreterLineData;
			int arrayIndex = array.Length;
			int linePCIndex = linePC.Length;
			while (arrayIndex != 0)
			{
				--arrayIndex;
				Interpreter.CallFrame frame = array[arrayIndex];
				IList<ScriptStackElement> group = new List<ScriptStackElement>();
				while (frame != null)
				{
					if (linePCIndex == 0)
					{
						Kit.CodeBug();
					}
					--linePCIndex;
					InterpreterData idata = frame.idata;
					string fileName = idata.itsSourceFile;
					string functionName = null;
					int lineNumber = -1;
					int pc = linePC[linePCIndex];
					if (pc >= 0)
					{
						lineNumber = GetIndex(idata.itsICode, pc);
					}
					if (!string.IsNullOrEmpty(idata.itsName))
					{
						functionName = idata.itsName;
					}
					frame = frame.parentFrame;
					@group.Add(new ScriptStackElement(fileName, functionName, lineNumber));
				}
				list.Add(@group.ToArray());
			}
			return list.ToArray();
		}

		internal static string GetEncodedSource(InterpreterData idata)
		{
			if (idata.encodedSource == null)
			{
				return null;
			}
			return idata.encodedSource.Substring(idata.encodedSourceStart, idata.encodedSourceEnd - idata.encodedSourceStart);
		}

		private static void InitFunction(Context cx, Scriptable scope, InterpretedFunction parent, int index)
		{
			InterpretedFunction fn;
			fn = InterpretedFunction.CreateFunction(cx, scope, parent, index);
			ScriptRuntime.InitFunction(cx, scope, fn, fn.idata.itsFunctionType, parent.idata.evalScriptFlag);
		}

		internal static object Interpret(InterpretedFunction ifun, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!ScriptRuntime.HasTopCall(cx))
			{
				Kit.CodeBug();
			}
#if ENCHANCED_SECURITY
			if (cx.interpreterSecurityDomain != ifun.securityDomain)
			{
				object savedDomain = cx.interpreterSecurityDomain;
				cx.interpreterSecurityDomain = ifun.securityDomain;
				try
				{
					return ifun.securityController.CallWithDomain(ifun.securityDomain, cx, ifun, scope, thisObj, args);
				}
				finally
				{
					cx.interpreterSecurityDomain = savedDomain;
				}
			}
#endif
			Interpreter.CallFrame frame = new Interpreter.CallFrame();
			InitFrame(cx, scope, thisObj, args, null, 0, args.Length, ifun, null, frame);
			frame.isContinuationsTopFrame = cx.isContinuationsTopCall;
			cx.isContinuationsTopCall = false;
			return InterpretLoop(cx, frame, null);
		}

		internal class GeneratorState
		{
			internal GeneratorState(int operation, object value)
			{
				this.operation = operation;
				this.value = value;
			}

			internal int operation;

			internal object value;

			internal Exception returnedException;
		}

		public static object ResumeGenerator(Context cx, Scriptable scope, int operation, object savedState, object value)
		{
			Interpreter.CallFrame frame = (Interpreter.CallFrame)savedState;
			Interpreter.GeneratorState generatorState = new Interpreter.GeneratorState(operation, value);
			if (operation == NativeGenerator.GENERATOR_CLOSE)
			{
				try
				{
					return InterpretLoop(cx, frame, generatorState);
				}
				catch (Exception e)
				{
					// Only propagate exceptions other than closingException
					if (e != value)
					{
						throw;
					}
				}
				return Undefined.instance;
			}
			object result = InterpretLoop(cx, frame, generatorState);
			if (generatorState.returnedException != null)
			{
				throw generatorState.returnedException;
			}
			return result;
		}

		public static object RestartContinuation(NativeContinuation c, Context cx, Scriptable scope, object[] args)
		{
			if (!ScriptRuntime.HasTopCall(cx))
			{
				return ScriptRuntime.DoTopCall(c, cx, scope, null, args);
			}
			object arg;
			if (args.Length == 0)
			{
				arg = Undefined.instance;
			}
			else
			{
				arg = args[0];
			}
			Interpreter.CallFrame capturedFrame = (Interpreter.CallFrame)c.GetImplementation();
			if (capturedFrame == null)
			{
				// No frames to restart
				return arg;
			}
			Interpreter.ContinuationJump cjump = new Interpreter.ContinuationJump(c, null);
			cjump.result = arg;
			return InterpretLoop(cx, null, cjump);
		}

		private static object InterpretLoop(Context cx, Interpreter.CallFrame frame, object throwable)
		{
			// throwable holds exception object to rethrow or catch
			// It is also used for continuation restart in which case
			// it holds ContinuationJump
			object DBL_MRK = UniqueTag.DOUBLE_MARK;
			object undefined = Undefined.instance;
			bool instructionCounting = (cx.instructionThreshold != 0);
			// arbitrary number to add to instructionCount when calling
			// other functions
			int INVOCATION_COST = 100;
			// arbitrary exception cost for instruction counting
			int EXCEPTION_COST = 100;
			string stringReg = null;
			int indexReg = -1;
			if (cx.lastInterpreterFrame != null)
			{
				// save the top frame from the previous interpretLoop
				// invocation on the stack
				if (cx.previousInterpreterInvocations == null)
				{
					cx.previousInterpreterInvocations = new ObjArray();
				}
				cx.previousInterpreterInvocations.Push(cx.lastInterpreterFrame);
			}
			// When restarting continuation throwable is not null and to jump
			// to the code that rewind continuation state indexReg should be set
			// to -1.
			// With the normal call throwable == null and indexReg == -1 allows to
			// catch bugs with using indeReg to access array elements before
			// initializing indexReg.
			Interpreter.GeneratorState generatorState = null;
			if (throwable != null)
			{
				if (throwable is Interpreter.GeneratorState)
				{
					generatorState = (Interpreter.GeneratorState)throwable;
					// reestablish this call frame
					EnterFrame(cx, frame, ScriptRuntime.emptyArgs, true);
					throwable = null;
				}
				else
				{
					if (!(throwable is Interpreter.ContinuationJump))
					{
						// It should be continuation
						Kit.CodeBug();
					}
				}
			}
			object interpreterResult = null;
			double interpreterResultDbl = 0.0;
			for (; ; )
			{
				try
				{
					if (throwable != null)
					{
						// Need to return both 'frame' and 'throwable' from
						// 'processThrowable', so just added a 'throwable'
						// member in 'frame'.
						frame = ProcessThrowable(cx, throwable, frame, indexReg, instructionCounting);
						throwable = frame.throwable;
						frame.throwable = null;
					}
					else
					{
						if (generatorState == null && frame.frozen)
						{
							Kit.CodeBug();
						}
					}
					// Use local variables for constant values in frame
					// for faster access
					object[] stack = frame.stack;
					double[] sDbl = frame.sDbl;
					object[] vars = frame.varSource.stack;
					double[] varDbls = frame.varSource.sDbl;
					PropertyAttributes[] varAttributes = frame.varSource.stackAttributes;
					sbyte[] iCode = frame.idata.itsICode;
					string[] strings = frame.idata.itsStringTable;
					// Use local for stackTop as well. Since execption handlers
					// can only exist at statement level where stack is empty,
					// it is necessary to save/restore stackTop only across
					// function calls and normal returns.
					int stackTop = frame.savedStackTop;
					// Store new frame in cx which is used for error reporting etc.
					cx.lastInterpreterFrame = frame;
					for (; ; )
					{
						// Exception handler assumes that PC is already incremented
						// pass the instruction start when it searches the
						// exception handler
						int op = (sbyte) iCode [frame.pc++];
						switch (op)
						{
							case Icode_GENERATOR:
							{
								// Back indent to ease implementation reading
								if (!frame.frozen)
								{
									// First time encountering this opcode: create new generator
									// object and return
									frame.pc--;
									// we want to come back here when we resume
									Interpreter.CallFrame generatorFrame = CaptureFrameForGenerator(frame);
									generatorFrame.frozen = true;
									NativeGenerator generator = new NativeGenerator(frame.scope, generatorFrame.fnOrScript, generatorFrame);
									frame.result = generator;
									goto Loop_break;
								}
								goto case Token.YIELD;
							}

							case Token.YIELD:
							{
								// We are now resuming execution. Fall through to YIELD case.
								// fall through...
								if (!frame.frozen)
								{
									return FreezeGenerator(cx, frame, stackTop, generatorState);
								}
								else
								{
									object obj = ThawGenerator(frame, stackTop, generatorState, op);
									if (obj != ScriptableConstants.NOT_FOUND)
									{
										throwable = obj;
										goto withoutExceptions_break;
									}
									goto Loop_continue;
								}
								goto case Icode_GENERATOR_END;
							}

							case Icode_GENERATOR_END:
							{
								// throw StopIteration
								frame.frozen = true;
								int sourceLine = GetIndex(iCode, frame.pc);
								generatorState.returnedException = new JavaScriptException(NativeIterator.GetStopIterationObject(frame.scope), frame.idata.itsSourceFile, sourceLine);
								goto Loop_break;
							}

							case Token.THROW:
							{
								object value = stack[stackTop];
								if (value == DBL_MRK)
								{
									value = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								int sourceLine = GetIndex(iCode, frame.pc);
								throwable = new JavaScriptException(value, frame.idata.itsSourceFile, sourceLine);
								goto withoutExceptions_break;
							}

							case Token.RETHROW:
							{
								indexReg += frame.localShift;
								throwable = stack[indexReg];
								goto withoutExceptions_break;
							}

							case Token.GE:
							case Token.LE:
							case Token.GT:
							case Token.LT:
							{
								stackTop = DoCompare(frame, op, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Token.IN:
							case Token.INSTANCEOF:
							{
								stackTop = DoInOrInstanceof(cx, op, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Token.EQ:
							case Token.NE:
							{
								--stackTop;
								bool valBln = DoEquals(stack, sDbl, stackTop);
								valBln ^= (op == Token.NE);
								stack[stackTop] = ScriptRuntime.WrapBoolean(valBln);
								goto Loop_continue;
							}

							case Token.SHEQ:
							case Token.SHNE:
							{
								--stackTop;
								bool valBln = DoShallowEquals(stack, sDbl, stackTop);
								valBln ^= (op == Token.SHNE);
								stack[stackTop] = ScriptRuntime.WrapBoolean(valBln);
								goto Loop_continue;
							}

							case Token.IFNE:
							{
								if (Stack_boolean(frame, stackTop--))
								{
									frame.pc += 2;
									goto Loop_continue;
								}
								goto jumplessRun_break;
							}

							case Token.IFEQ:
							{
								if (!Stack_boolean(frame, stackTop--))
								{
									frame.pc += 2;
									goto Loop_continue;
								}
								goto jumplessRun_break;
							}

							case Icode_IFEQ_POP:
							{
								if (!Stack_boolean(frame, stackTop--))
								{
									frame.pc += 2;
									goto Loop_continue;
								}
								stack[stackTop--] = null;
								goto jumplessRun_break;
							}

							case Token.GOTO:
							{
								goto jumplessRun_break;
							}

							case Icode_GOSUB:
							{
								++stackTop;
								stack[stackTop] = DBL_MRK;
								sDbl[stackTop] = frame.pc + 2;
								goto jumplessRun_break;
							}

							case Icode_STARTSUB:
							{
								if (stackTop == frame.emptyStackTop + 1)
								{
									// Call from Icode_GOSUB: store return PC address in the local
									indexReg += frame.localShift;
									stack[indexReg] = stack[stackTop];
									sDbl[indexReg] = sDbl[stackTop];
									--stackTop;
								}
								else
								{
									// Call from exception handler: exception object is already stored
									// in the local
									if (stackTop != frame.emptyStackTop)
									{
										Kit.CodeBug();
									}
								}
								goto Loop_continue;
							}

							case Icode_RETSUB:
							{
								// indexReg: local to store return address
								if (instructionCounting)
								{
									AddInstructionCount(cx, frame, 0);
								}
								indexReg += frame.localShift;
								object value = stack[indexReg];
								if (value != DBL_MRK)
								{
									// Invocation from exception handler, restore object to rethrow
									throwable = value;
									goto withoutExceptions_break;
								}
								// Normal return from GOSUB
								frame.pc = (int)sDbl[indexReg];
								if (instructionCounting)
								{
									frame.pcPrevBranch = frame.pc;
								}
								goto Loop_continue;
							}

							case Icode_POP:
							{
								stack[stackTop] = null;
								stackTop--;
								goto Loop_continue;
							}

							case Icode_POP_RESULT:
							{
								frame.result = stack[stackTop];
								frame.resultDbl = sDbl[stackTop];
								stack[stackTop] = null;
								--stackTop;
								goto Loop_continue;
							}

							case Icode_DUP:
							{
								stack[stackTop + 1] = stack[stackTop];
								sDbl[stackTop + 1] = sDbl[stackTop];
								stackTop++;
								goto Loop_continue;
							}

							case Icode_DUP2:
							{
								stack[stackTop + 1] = stack[stackTop - 1];
								sDbl[stackTop + 1] = sDbl[stackTop - 1];
								stack[stackTop + 2] = stack[stackTop];
								sDbl[stackTop + 2] = sDbl[stackTop];
								stackTop += 2;
								goto Loop_continue;
							}

							case Icode_SWAP:
							{
								object o = stack[stackTop];
								stack[stackTop] = stack[stackTop - 1];
								stack[stackTop - 1] = o;
								double d = sDbl[stackTop];
								sDbl[stackTop] = sDbl[stackTop - 1];
								sDbl[stackTop - 1] = d;
								goto Loop_continue;
							}

							case Token.RETURN:
							{
								frame.result = stack[stackTop];
								frame.resultDbl = sDbl[stackTop];
								--stackTop;
								goto Loop_break;
							}

							case Token.RETURN_RESULT:
							{
								goto Loop_break;
							}

							case Icode_RETUNDEF:
							{
								frame.result = undefined;
								goto Loop_break;
							}

							case Token.BITNOT:
							{
								int rIntValue = Stack_int32(frame, stackTop);
								stack[stackTop] = DBL_MRK;
								sDbl[stackTop] = ~rIntValue;
								goto Loop_continue;
							}

							case Token.BITAND:
							case Token.BITOR:
							case Token.BITXOR:
							case Token.LSH:
							case Token.RSH:
							{
								stackTop = DoBitOp(frame, op, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Token.URSH:
							{
								double lDbl = Stack_double(frame, stackTop - 1);
								int rIntValue = Stack_int32(frame, stackTop) & unchecked((int)(0x1F));
								stack[--stackTop] = DBL_MRK;
								sDbl[stackTop] = (long)(((ulong)ScriptRuntime.ToUInt32(lDbl)) >> rIntValue);
								goto Loop_continue;
							}

							case Token.NEG:
							case Token.POS:
							{
								double rDbl = Stack_double(frame, stackTop);
								stack[stackTop] = DBL_MRK;
								if (op == Token.NEG)
								{
									rDbl = -rDbl;
								}
								sDbl[stackTop] = rDbl;
								goto Loop_continue;
							}

							case Token.ADD:
							{
								--stackTop;
								DoAdd(stack, sDbl, stackTop, cx);
								goto Loop_continue;
							}

							case Token.SUB:
							case Token.MUL:
							case Token.DIV:
							case Token.MOD:
							{
								stackTop = DoArithmetic(frame, op, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Token.NOT:
							{
								stack[stackTop] = ScriptRuntime.WrapBoolean(!Stack_boolean(frame, stackTop));
								goto Loop_continue;
							}

							case Token.BINDNAME:
							{
								stack[++stackTop] = ScriptRuntime.Bind(cx, frame.scope, stringReg);
								goto Loop_continue;
							}

							case Token.STRICT_SETNAME:
							case Token.SETNAME:
							{
								object rhs = stack[stackTop];
								if (rhs == DBL_MRK)
								{
									rhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								Scriptable lhs = (Scriptable)stack[stackTop];
								stack[stackTop] = op == Token.SETNAME ? ScriptRuntime.SetName(lhs, rhs, cx, frame.scope, stringReg) : ScriptRuntime.StrictSetName(lhs, rhs, cx, frame.scope, stringReg);
								goto Loop_continue;
							}

							case Icode_SETCONST:
							{
								object rhs = stack[stackTop];
								if (rhs == DBL_MRK)
								{
									rhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								Scriptable lhs = (Scriptable)stack[stackTop];
								stack[stackTop] = ScriptRuntime.SetConst(lhs, rhs, cx, stringReg);
								goto Loop_continue;
							}

							case Token.DELPROP:
							case Icode_DELNAME:
							{
								stackTop = DoDelName(cx, op, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Token.GETPROPNOWARN:
							{
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.GetObjectPropNoWarn(lhs, stringReg, cx);
								goto Loop_continue;
							}

							case Token.GETPROP:
							{
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.GetObjectProp(lhs, stringReg, cx, frame.scope);
								goto Loop_continue;
							}

							case Token.SETPROP:
							{
								object rhs = stack[stackTop];
								if (rhs == DBL_MRK)
								{
									rhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.SetObjectProp(lhs, stringReg, rhs, cx);
								goto Loop_continue;
							}

							case Icode_PROP_INC_DEC:
							{
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.PropIncrDecr(lhs, stringReg, cx, iCode[frame.pc]);
								++frame.pc;
								goto Loop_continue;
							}

							case Token.GETELEM:
							{
								stackTop = DoGetElem(cx, frame, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Token.SETELEM:
							{
								stackTop = DoSetElem(cx, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Icode_ELEM_INC_DEC:
							{
								stackTop = DoElemIncDec(cx, frame, iCode, stack, sDbl, stackTop);
								goto Loop_continue;
							}

							case Token.GET_REF:
							{
								Ref @ref = (Ref)stack[stackTop];
								stack[stackTop] = ScriptRuntime.RefGet(@ref, cx);
								goto Loop_continue;
							}

							case Token.SET_REF:
							{
								object value = stack[stackTop];
								if (value == DBL_MRK)
								{
									value = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								Ref @ref = (Ref)stack[stackTop];
								stack[stackTop] = ScriptRuntime.RefSet(@ref, value, cx);
								goto Loop_continue;
							}

							case Token.DEL_REF:
							{
								Ref @ref = (Ref)stack[stackTop];
								stack[stackTop] = ScriptRuntime.RefDel(@ref, cx);
								goto Loop_continue;
							}

							case Icode_REF_INC_DEC:
							{
								Ref @ref = (Ref)stack[stackTop];
								stack[stackTop] = ScriptRuntime.RefIncrDecr(@ref, cx, iCode[frame.pc]);
								++frame.pc;
								goto Loop_continue;
							}

							case Token.LOCAL_LOAD:
							{
								++stackTop;
								indexReg += frame.localShift;
								stack[stackTop] = stack[indexReg];
								sDbl[stackTop] = sDbl[indexReg];
								goto Loop_continue;
							}

							case Icode_LOCAL_CLEAR:
							{
								indexReg += frame.localShift;
								stack[indexReg] = null;
								goto Loop_continue;
							}

							case Icode_NAME_AND_THIS:
							{
								// stringReg: name
								++stackTop;
								stack[stackTop] = ScriptRuntime.GetNameFunctionAndThis(stringReg, cx, frame.scope);
								++stackTop;
								stack[stackTop] = ScriptRuntime.LastStoredScriptable(cx);
								goto Loop_continue;
							}

							case Icode_PROP_AND_THIS:
							{
								object obj = stack[stackTop];
								if (obj == DBL_MRK)
								{
									obj = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								// stringReg: property
								stack[stackTop] = ScriptRuntime.GetPropFunctionAndThis(obj, stringReg, cx, frame.scope);
								++stackTop;
								stack[stackTop] = ScriptRuntime.LastStoredScriptable(cx);
								goto Loop_continue;
							}

							case Icode_ELEM_AND_THIS:
							{
								object obj = stack[stackTop - 1];
								if (obj == DBL_MRK)
								{
									obj = ScriptRuntime.WrapNumber(sDbl[stackTop - 1]);
								}
								object id = stack[stackTop];
								if (id == DBL_MRK)
								{
									id = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop - 1] = ScriptRuntime.GetElemFunctionAndThis(obj, id, cx);
								stack[stackTop] = ScriptRuntime.LastStoredScriptable(cx);
								goto Loop_continue;
							}

							case Icode_VALUE_AND_THIS:
							{
								object value = stack[stackTop];
								if (value == DBL_MRK)
								{
									value = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.GetValueFunctionAndThis(value, cx);
								++stackTop;
								stack[stackTop] = ScriptRuntime.LastStoredScriptable(cx);
								goto Loop_continue;
							}

							case Icode_CALLSPECIAL:
							{
								if (instructionCounting)
								{
									cx.instructionCount += INVOCATION_COST;
								}
								stackTop = DoCallSpecial(cx, frame, stack, sDbl, stackTop, iCode, indexReg);
								goto Loop_continue;
							}

							case Token.CALL:
							case Icode_TAIL_CALL:
							case Token.REF_CALL:
							{
								if (instructionCounting)
								{
									cx.instructionCount += INVOCATION_COST;
								}
								// stack change: function thisObj arg0 .. argN -> result
								// indexReg: number of arguments
								stackTop -= 1 + indexReg;
								// CALL generation ensures that fun and funThisObj
								// are already Scriptable and Callable objects respectively
								Callable fun = (Callable)stack[stackTop];
								Scriptable funThisObj = (Scriptable)stack[stackTop + 1];
								if (op == Token.REF_CALL)
								{
									object[] outArgs = GetArgsArray(stack, sDbl, stackTop + 2, indexReg);
									stack[stackTop] = ScriptRuntime.CallRef(fun, funThisObj, outArgs, cx);
									goto Loop_continue;
								}
								Scriptable calleeScope = frame.scope;
								if (frame.useActivation)
								{
									calleeScope = ScriptableObject.GetTopLevelScope(frame.scope);
								}
								if (fun is InterpretedFunction)
								{
									InterpretedFunction ifun = (InterpretedFunction)fun;
									#if ENCHANCED_SECURITY
									if (frame.fnOrScript.securityDomain == ifun.securityDomain)
									#endif
									{
										Interpreter.CallFrame callParentFrame = frame;
										Interpreter.CallFrame calleeFrame = new Interpreter.CallFrame();
										if (op == Icode_TAIL_CALL)
										{
											// In principle tail call can re-use the current
											// frame and its stack arrays but it is hard to
											// do properly. Any exceptions that can legally
											// happen during frame re-initialization including
											// StackOverflowException during innocent looking
											// System.arraycopy may leave the current frame
											// data corrupted leading to undefined behaviour
											// in the catch code bellow that unwinds JS stack
											// on exceptions. Then there is issue about frame release
											// end exceptions there.
											// To avoid frame allocation a released frame
											// can be cached for re-use which would also benefit
											// non-tail calls but it is not clear that this caching
											// would gain in performance due to potentially
											// bad interaction with GC.
											callParentFrame = frame.parentFrame;
											// Release the current frame. See Bug #344501 to see why
											// it is being done here.
											ExitFrame(cx, frame, null);
										}
										InitFrame(cx, calleeScope, funThisObj, stack, sDbl, stackTop + 2, indexReg, ifun, callParentFrame, calleeFrame);
										if (op != Icode_TAIL_CALL)
										{
											frame.savedStackTop = stackTop;
											frame.savedCallOp = op;
										}
										frame = calleeFrame;
										goto StateLoop_continue;
									}
								}
								if (fun is NativeContinuation)
								{
									// Jump to the captured continuation
									Interpreter.ContinuationJump cjump;
									cjump = new Interpreter.ContinuationJump((NativeContinuation)fun, frame);
									// continuation result is the first argument if any
									// of continuation call
									if (indexReg == 0)
									{
										cjump.result = undefined;
									}
									else
									{
										cjump.result = stack[stackTop + 2];
										cjump.resultDbl = sDbl[stackTop + 2];
									}
									// Start the real unwind job
									throwable = cjump;
									goto withoutExceptions_break;
								}
								if (fun is IdFunctionObject)
								{
									IdFunctionObject ifun = (IdFunctionObject)fun;
									if (NativeContinuation.IsContinuationConstructor(ifun))
									{
										frame.stack[stackTop] = CaptureContinuation(cx, frame.parentFrame, false);
										goto Loop_continue;
									}
									// Bug 405654 -- make best effort to keep Function.apply and
									// Function.call within this interpreter loop invocation
									if (BaseFunction.IsApplyOrCall(ifun))
									{
										Callable applyCallable = ScriptRuntime.GetCallable(funThisObj);
										if (applyCallable is InterpretedFunction)
										{
											InterpretedFunction iApplyCallable = (InterpretedFunction)applyCallable;
#if ENCHANCED_SECURITY
											if (frame.fnOrScript.securityDomain == iApplyCallable.securityDomain)
#endif
											{
												frame = InitFrameForApplyOrCall(cx, frame, indexReg, stack, sDbl, stackTop, op, calleeScope, ifun, iApplyCallable);
												goto StateLoop_continue;
											}
										}
									}
								}
								// Bug 447697 -- make best effort to keep __noSuchMethod__ within this
								// interpreter loop invocation
								if (fun is ScriptRuntime.NoSuchMethodShim)
								{
									// get the shim and the actual method
									ScriptRuntime.NoSuchMethodShim noSuchMethodShim = (ScriptRuntime.NoSuchMethodShim)fun;
									Callable noSuchMethodMethod = noSuchMethodShim.noSuchMethodMethod;
									// if the method is in fact an InterpretedFunction
									if (noSuchMethodMethod is InterpretedFunction)
									{
										InterpretedFunction ifun = (InterpretedFunction)noSuchMethodMethod;
#if ENCHANCED_SECURITY
										if (frame.fnOrScript.securityDomain == ifun.securityDomain)
#endif
										{
											frame = InitFrameForNoSuchMethod(cx, frame, indexReg, stack, sDbl, stackTop, op, funThisObj, calleeScope, noSuchMethodShim, ifun);
											goto StateLoop_continue;
										}
									}
								}
								cx.lastInterpreterFrame = frame;
								frame.savedCallOp = op;
								frame.savedStackTop = stackTop;
								stack[stackTop] = fun.Call(cx, calleeScope, funThisObj, GetArgsArray(stack, sDbl, stackTop + 2, indexReg));
								goto Loop_continue;
							}

							case Token.NEW:
							{
								if (instructionCounting)
								{
									cx.instructionCount += INVOCATION_COST;
								}
								// stack change: function arg0 .. argN -> newResult
								// indexReg: number of arguments
								stackTop -= indexReg;
								object lhs = stack[stackTop];
								if (lhs is InterpretedFunction)
								{
									InterpretedFunction f = (InterpretedFunction)lhs;
#if ENCHANCED_SECURITY
									if (frame.fnOrScript.securityDomain == f.securityDomain)
#endif
									{
										Scriptable newInstance = f.CreateObject(cx, frame.scope);
										Interpreter.CallFrame calleeFrame = new Interpreter.CallFrame();
										InitFrame(cx, frame.scope, newInstance, stack, sDbl, stackTop + 1, indexReg, f, frame, calleeFrame);
										stack[stackTop] = newInstance;
										frame.savedStackTop = stackTop;
										frame.savedCallOp = op;
										frame = calleeFrame;
										goto StateLoop_continue;
									}
								}
								if (!(lhs is Function))
								{
									if (lhs == DBL_MRK)
									{
										lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
									}
									throw ScriptRuntime.NotFunctionError(lhs);
								}
								Function fun = (Function)lhs;
								if (fun is IdFunctionObject)
								{
									IdFunctionObject ifun = (IdFunctionObject)fun;
									if (NativeContinuation.IsContinuationConstructor(ifun))
									{
										frame.stack[stackTop] = CaptureContinuation(cx, frame.parentFrame, false);
										goto Loop_continue;
									}
								}
								object[] outArgs = GetArgsArray(stack, sDbl, stackTop + 1, indexReg);
								stack[stackTop] = fun.Construct(cx, frame.scope, outArgs);
								goto Loop_continue;
							}

							case Token.TYPEOF:
							{
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.TypeOf(lhs);
								goto Loop_continue;
							}

							case Icode_TYPEOFNAME:
							{
								stack[++stackTop] = ScriptRuntime.TypeOfName(frame.scope, stringReg);
								goto Loop_continue;
							}

							case Token.STRING:
							{
								stack[++stackTop] = stringReg;
								goto Loop_continue;
							}

							case Icode_SHORTNUMBER:
							{
								++stackTop;
								stack[stackTop] = DBL_MRK;
								sDbl[stackTop] = GetShort(iCode, frame.pc);
								frame.pc += 2;
								goto Loop_continue;
							}

							case Icode_INTNUMBER:
							{
								++stackTop;
								stack[stackTop] = DBL_MRK;
								sDbl[stackTop] = GetInt(iCode, frame.pc);
								frame.pc += 4;
								goto Loop_continue;
							}

							case Token.NUMBER:
							{
								++stackTop;
								stack[stackTop] = DBL_MRK;
								sDbl[stackTop] = frame.idata.itsDoubleTable[indexReg];
								goto Loop_continue;
							}

							case Token.NAME:
							{
								stack[++stackTop] = ScriptRuntime.Name(cx, frame.scope, stringReg);
								goto Loop_continue;
							}

							case Icode_NAME_INC_DEC:
							{
								stack[++stackTop] = ScriptRuntime.NameIncrDecr(frame.scope, stringReg, cx, iCode[frame.pc]);
								++frame.pc;
								goto Loop_continue;
							}

							case Icode_SETCONSTVAR1:
							{
								indexReg = iCode[frame.pc++];
								goto case Token.SETCONSTVAR;
							}

							case Token.SETCONSTVAR:
							{
								// fallthrough
								stackTop = DoSetConstVar(frame, stack, sDbl, stackTop, vars, varDbls, varAttributes, indexReg);
								goto Loop_continue;
							}

							case Icode_SETVAR1:
							{
								indexReg = iCode[frame.pc++];
								goto case Token.SETVAR;
							}

							case Token.SETVAR:
							{
								// fallthrough
								stackTop = DoSetVar(frame, stack, sDbl, stackTop, vars, varDbls, varAttributes, indexReg);
								goto Loop_continue;
							}

							case Icode_GETVAR1:
							{
								indexReg = iCode[frame.pc++];
								goto case Token.GETVAR;
							}

							case Token.GETVAR:
							{
								// fallthrough
								stackTop = DoGetVar(frame, stack, sDbl, stackTop, vars, varDbls, indexReg);
								goto Loop_continue;
							}

							case Icode_VAR_INC_DEC:
							{
								stackTop = DoVarIncDec(cx, frame, stack, sDbl, stackTop, vars, varDbls, indexReg);
								goto Loop_continue;
							}

							case Icode_ZERO:
							{
								++stackTop;
								stack[stackTop] = DBL_MRK;
								sDbl[stackTop] = 0;
								goto Loop_continue;
							}

							case Icode_ONE:
							{
								++stackTop;
								stack[stackTop] = DBL_MRK;
								sDbl[stackTop] = 1;
								goto Loop_continue;
							}

							case Token.NULL:
							{
								stack[++stackTop] = null;
								goto Loop_continue;
							}

							case Token.THIS:
							{
								stack[++stackTop] = frame.thisObj;
								goto Loop_continue;
							}

							case Token.THISFN:
							{
								stack[++stackTop] = frame.fnOrScript;
								goto Loop_continue;
							}

							case Token.FALSE:
							{
								stack[++stackTop] = false;
								goto Loop_continue;
							}

							case Token.TRUE:
							{
								stack[++stackTop] = true;
								goto Loop_continue;
							}

							case Icode_UNDEF:
							{
								stack[++stackTop] = undefined;
								goto Loop_continue;
							}

							case Token.ENTERWITH:
							{
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								frame.scope = ScriptRuntime.EnterWith(lhs, cx, frame.scope);
								goto Loop_continue;
							}

							case Token.LEAVEWITH:
							{
								frame.scope = ScriptRuntime.LeaveWith(frame.scope);
								goto Loop_continue;
							}

							case Token.CATCH_SCOPE:
							{
								// stack top: exception object
								// stringReg: name of exception variable
								// indexReg: local for exception scope
								--stackTop;
								indexReg += frame.localShift;
								bool afterFirstScope = (frame.idata.itsICode[frame.pc] != 0);
								Exception caughtException = (Exception)stack[stackTop + 1];
								Scriptable lastCatchScope;
								if (!afterFirstScope)
								{
									lastCatchScope = null;
								}
								else
								{
									lastCatchScope = (Scriptable)stack[indexReg];
								}
								stack[indexReg] = ScriptRuntime.NewCatchScope(caughtException, lastCatchScope, stringReg, cx, frame.scope);
								++frame.pc;
								goto Loop_continue;
							}

							case Token.ENUM_INIT_KEYS:
							case Token.ENUM_INIT_VALUES:
							case Token.ENUM_INIT_ARRAY:
							{
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								indexReg += frame.localShift;
								int enumType = op == Token.ENUM_INIT_KEYS ? ScriptRuntime.ENUMERATE_KEYS : op == Token.ENUM_INIT_VALUES ? ScriptRuntime.ENUMERATE_VALUES : ScriptRuntime.ENUMERATE_ARRAY;
								stack[indexReg] = ScriptRuntime.EnumInit(lhs, cx, enumType);
								goto Loop_continue;
							}

							case Token.ENUM_NEXT:
							case Token.ENUM_ID:
							{
								indexReg += frame.localShift;
								object val = stack[indexReg];
								++stackTop;
								stack[stackTop] = (op == Token.ENUM_NEXT) ? (object)ScriptRuntime.EnumNext(val) : (object)ScriptRuntime.EnumId(val, cx);
								goto Loop_continue;
							}

							case Token.REF_SPECIAL:
							{
								//stringReg: name of special property
								object obj = stack[stackTop];
								if (obj == DBL_MRK)
								{
									obj = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.SpecialRef(obj, stringReg, cx);
								goto Loop_continue;
							}

							case Token.REF_MEMBER:
							{
								//indexReg: flags
								stackTop = DoRefMember(cx, stack, sDbl, stackTop, indexReg);
								goto Loop_continue;
							}

							case Token.REF_NS_MEMBER:
							{
								//indexReg: flags
								stackTop = DoRefNsMember(cx, stack, sDbl, stackTop, indexReg);
								goto Loop_continue;
							}

							case Token.REF_NAME:
							{
								//indexReg: flags
								object name = stack[stackTop];
								if (name == DBL_MRK)
								{
									name = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.NameRef(name, cx, frame.scope, indexReg);
								goto Loop_continue;
							}

							case Token.REF_NS_NAME:
							{
								//indexReg: flags
								stackTop = DoRefNsName(cx, frame, stack, sDbl, stackTop, indexReg);
								goto Loop_continue;
							}

							case Icode_SCOPE_LOAD:
							{
								indexReg += frame.localShift;
								frame.scope = (Scriptable)stack[indexReg];
								goto Loop_continue;
							}

							case Icode_SCOPE_SAVE:
							{
								indexReg += frame.localShift;
								stack[indexReg] = frame.scope;
								goto Loop_continue;
							}

							case Icode_CLOSURE_EXPR:
							{
								stack[++stackTop] = InterpretedFunction.CreateFunction(cx, frame.scope, frame.fnOrScript, indexReg);
								goto Loop_continue;
							}

							case Icode_CLOSURE_STMT:
							{
								InitFunction(cx, frame.scope, frame.fnOrScript, indexReg);
								goto Loop_continue;
							}

							case Token.REGEXP:
							{
								object re = frame.idata.itsRegExpLiterals[indexReg];
								stack[++stackTop] = ScriptRuntime.WrapRegExp(cx, frame.scope, re);
								goto Loop_continue;
							}

							case Icode_LITERAL_NEW:
							{
								// indexReg: number of values in the literal
								++stackTop;
								stack[stackTop] = new int[indexReg];
								++stackTop;
								stack[stackTop] = new object[indexReg];
								sDbl[stackTop] = 0;
								goto Loop_continue;
							}

							case Icode_LITERAL_SET:
							{
								object value = stack[stackTop];
								if (value == DBL_MRK)
								{
									value = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								int i = (int)sDbl[stackTop];
								((object[])stack[stackTop])[i] = value;
								sDbl[stackTop] = i + 1;
								goto Loop_continue;
							}

							case Icode_LITERAL_GETTER:
							{
								object value = stack[stackTop];
								--stackTop;
								int i = (int)sDbl[stackTop];
								((object[])stack[stackTop])[i] = value;
								((int[])stack[stackTop - 1])[i] = -1;
								sDbl[stackTop] = i + 1;
								goto Loop_continue;
							}

							case Icode_LITERAL_SETTER:
							{
								object value = stack[stackTop];
								--stackTop;
								int i = (int)sDbl[stackTop];
								((object[])stack[stackTop])[i] = value;
								((int[])stack[stackTop - 1])[i] = +1;
								sDbl[stackTop] = i + 1;
								goto Loop_continue;
							}

							case Token.ARRAYLIT:
							case Icode_SPARE_ARRAYLIT:
							case Token.OBJECTLIT:
							{
								object[] data = (object[])stack[stackTop];
								--stackTop;
								int[] getterSetters = (int[])stack[stackTop];
								object val;
								if (op == Token.OBJECTLIT)
								{
									object[] ids = (object[])frame.idata.literalIds[indexReg];
									val = ScriptRuntime.NewObjectLiteral(ids, data, getterSetters, cx, frame.scope);
								}
								else
								{
									int[] skipIndexces = null;
									if (op == Icode_SPARE_ARRAYLIT)
									{
										skipIndexces = (int[])frame.idata.literalIds[indexReg];
									}
									val = ScriptRuntime.NewArrayLiteral(data, skipIndexces, cx, frame.scope);
								}
								stack[stackTop] = val;
								goto Loop_continue;
							}

							case Icode_ENTERDQ:
							{
								object lhs = stack[stackTop];
								if (lhs == DBL_MRK)
								{
									lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								--stackTop;
								frame.scope = ScriptRuntime.EnterDotQuery(lhs, frame.scope);
								goto Loop_continue;
							}

							case Icode_LEAVEDQ:
							{
								bool valBln = Stack_boolean(frame, stackTop);
								object x = ScriptRuntime.UpdateDotQuery(valBln, frame.scope);
								if (x != null)
								{
									stack[stackTop] = x;
									frame.scope = ScriptRuntime.LeaveDotQuery(frame.scope);
									frame.pc += 2;
									goto Loop_continue;
								}
								// reset stack and PC to code after ENTERDQ
								--stackTop;
								goto jumplessRun_break;
							}

							case Token.DEFAULTNAMESPACE:
							{
								object value = stack[stackTop];
								if (value == DBL_MRK)
								{
									value = ScriptRuntime.WrapNumber(sDbl[stackTop]);
								}
								stack[stackTop] = ScriptRuntime.SetDefaultNamespace(value, cx);
								goto Loop_continue;
							}

							case Token.ESCXMLATTR:
							{
								object value = stack[stackTop];
								if (value != DBL_MRK)
								{
									stack[stackTop] = ScriptRuntime.EscapeAttributeValue(value, cx);
								}
								goto Loop_continue;
							}

							case Token.ESCXMLTEXT:
							{
								object value = stack[stackTop];
								if (value != DBL_MRK)
								{
									stack[stackTop] = ScriptRuntime.EscapeTextValue(value, cx);
								}
								goto Loop_continue;
							}

							case Icode_DEBUGGER:
							{
								if (frame.debuggerFrame != null)
								{
									frame.debuggerFrame.OnDebuggerStatement(cx);
								}
								goto Loop_continue;
							}

							case Icode_LINE:
							{
								frame.pcSourceLineStart = frame.pc;
								if (frame.debuggerFrame != null)
								{
									int line = GetIndex(iCode, frame.pc);
									frame.debuggerFrame.OnLineChange(cx, line);
								}
								frame.pc += 2;
								goto Loop_continue;
							}

							case Icode_REG_IND_C0:
							{
								indexReg = 0;
								goto Loop_continue;
							}

							case Icode_REG_IND_C1:
							{
								indexReg = 1;
								goto Loop_continue;
							}

							case Icode_REG_IND_C2:
							{
								indexReg = 2;
								goto Loop_continue;
							}

							case Icode_REG_IND_C3:
							{
								indexReg = 3;
								goto Loop_continue;
							}

							case Icode_REG_IND_C4:
							{
								indexReg = 4;
								goto Loop_continue;
							}

							case Icode_REG_IND_C5:
							{
								indexReg = 5;
								goto Loop_continue;
							}

							case Icode_REG_IND1:
							{
								indexReg = unchecked((int)(0xFF)) & iCode[frame.pc];
								++frame.pc;
								goto Loop_continue;
							}

							case Icode_REG_IND2:
							{
								indexReg = GetIndex(iCode, frame.pc);
								frame.pc += 2;
								goto Loop_continue;
							}

							case Icode_REG_IND4:
							{
								indexReg = GetInt(iCode, frame.pc);
								frame.pc += 4;
								goto Loop_continue;
							}

							case Icode_REG_STR_C0:
							{
								stringReg = strings[0];
								goto Loop_continue;
							}

							case Icode_REG_STR_C1:
							{
								stringReg = strings[1];
								goto Loop_continue;
							}

							case Icode_REG_STR_C2:
							{
								stringReg = strings[2];
								goto Loop_continue;
							}

							case Icode_REG_STR_C3:
							{
								stringReg = strings[3];
								goto Loop_continue;
							}

							case Icode_REG_STR1:
							{
								stringReg = strings[unchecked((int)(0xFF)) & iCode[frame.pc]];
								++frame.pc;
								goto Loop_continue;
							}

							case Icode_REG_STR2:
							{
								stringReg = strings[GetIndex(iCode, frame.pc)];
								frame.pc += 2;
								goto Loop_continue;
							}

							case Icode_REG_STR4:
							{
								stringReg = strings[GetInt(iCode, frame.pc)];
								frame.pc += 4;
								goto Loop_continue;
							}

							default:
							{
								DumpICode(frame.idata);
								throw new Exception("Unknown icode : " + op + " @ pc : " + (frame.pc - 1));
							}
						}
jumplessRun_break: ;
						// end of interpreter switch
						// end of jumplessRun label block
						// This should be reachable only for jump implementation
						// when pc points to encoded target offset
						if (instructionCounting)
						{
							AddInstructionCount(cx, frame, 2);
						}
						int offset = GetShort(iCode, frame.pc);
						if (offset != 0)
						{
							// -1 accounts for pc pointing to jump opcode + 1
							frame.pc += offset - 1;
						}
						else
						{
							frame.pc = frame.idata.longJumps.GetExistingInt(frame.pc);
						}
						if (instructionCounting)
						{
							frame.pcPrevBranch = frame.pc;
						}
						goto Loop_continue;
Loop_continue: ;
					}
Loop_break: ;
					// end of Loop: for
					ExitFrame(cx, frame, null);
					interpreterResult = frame.result;
					interpreterResultDbl = frame.resultDbl;
					if (frame.parentFrame != null)
					{
						frame = frame.parentFrame;
						if (frame.frozen)
						{
							frame = frame.CloneFrozen();
						}
						SetCallResult(frame, interpreterResult, interpreterResultDbl);
						interpreterResult = null;
						// Help GC
						goto StateLoop_continue;
					}
					goto StateLoop_break;
				}
				catch (Exception ex)
				{
					// end of interpreter withoutExceptions: try
					if (throwable != null)
					{
						// This is serious bug and it is better to track it ASAP
						System.Console.Error.WriteLine(ex);
						throw new InvalidOperationException();
					}
					throwable = ex;
				}
withoutExceptions_break: ;
				// This should be reachable only after above catch or from
				// finally when it needs to propagate exception or from
				// explicit throw
				if (throwable == null)
				{
					Kit.CodeBug();
				}
				// Exception type
				int EX_CATCH_STATE = 2;
				// Can execute JS catch
				int EX_FINALLY_STATE = 1;
				// Can execute JS finally
				int EX_NO_JS_STATE = 0;
				// Terminate JS execution
				int exState;
				Interpreter.ContinuationJump cjump_1 = null;
				if (generatorState != null && generatorState.operation == NativeGenerator.GENERATOR_CLOSE && throwable == generatorState.value)
				{
					exState = EX_FINALLY_STATE;
				}
				else
				{
					if (throwable is JavaScriptException)
					{
						exState = EX_CATCH_STATE;
					}
					else
					{
						if (throwable is EcmaError)
						{
							// an offical ECMA error object,
							exState = EX_CATCH_STATE;
						}
						else
						{
							if (throwable is EvaluatorException)
							{
								exState = EX_CATCH_STATE;
							}
							else
							{
								if (throwable is ContinuationPending)
								{
									exState = EX_NO_JS_STATE;
								}
								else
								{
									if (throwable is Exception)
									{
										exState = cx.HasFeature(LanguageFeatures.EnhancedJavaAccess) ? EX_CATCH_STATE : EX_FINALLY_STATE;
									}
									else
									{
										if (throwable is Exception)
										{
											exState = cx.HasFeature(LanguageFeatures.EnhancedJavaAccess) ? EX_CATCH_STATE : EX_NO_JS_STATE;
										}
										else
										{
											if (throwable is Interpreter.ContinuationJump)
											{
												// It must be ContinuationJump
												exState = EX_FINALLY_STATE;
												cjump_1 = (Interpreter.ContinuationJump)throwable;
											}
											else
											{
												exState = cx.HasFeature(LanguageFeatures.EnhancedJavaAccess) ? EX_CATCH_STATE : EX_FINALLY_STATE;
											}
										}
									}
								}
							}
						}
					}
				}
				if (instructionCounting)
				{
					try
					{
						AddInstructionCount(cx, frame, EXCEPTION_COST);
					}
					catch (Exception ex)
					{
						throwable = ex;
						exState = EX_FINALLY_STATE;
					}
					//TODO: Handle correctly
/*
 *
					catch (Exception ex)
					{
						// Error from instruction counting
						//     => unconditionally terminate JS
						throwable = ex;
						cjump_1 = null;
						exState = EX_NO_JS_STATE;
					}
*/
				}
				if (frame.debuggerFrame != null && throwable is Exception)
				{
					// Call debugger only for RuntimeException
					Exception rex = (Exception)throwable;
					try
					{
						frame.debuggerFrame.OnExceptionThrown(cx, rex);
					}
					catch (Exception ex)
					{
						// Any exception from debugger
						//     => unconditionally terminate JS
						throwable = ex;
						cjump_1 = null;
						exState = EX_NO_JS_STATE;
					}
				}
				for (; ; )
				{
					if (exState != EX_NO_JS_STATE)
					{
						bool onlyFinally = (exState != EX_CATCH_STATE);
						indexReg = GetExceptionHandler(frame, onlyFinally);
						if (indexReg >= 0)
						{
							// We caught an exception, restart the loop
							// with exception pending the processing at the loop
							// start
							goto StateLoop_continue;
						}
					}
					// No allowed exception handlers in this frame, unwind
					// to parent and try to look there
					ExitFrame(cx, frame, throwable);
					frame = frame.parentFrame;
					if (frame == null)
					{
						break;
					}
					if (cjump_1 != null && cjump_1.branchFrame == frame)
					{
						// Continuation branch point was hit,
						// restart the state loop to reenter continuation
						indexReg = -1;
						goto StateLoop_continue;
					}
				}
				// No more frames, rethrow the exception or deal with continuation
				if (cjump_1 != null)
				{
					if (cjump_1.branchFrame != null)
					{
						// The above loop should locate the top frame
						Kit.CodeBug();
					}
					if (cjump_1.capturedFrame != null)
					{
						// Restarting detached continuation
						indexReg = -1;
						goto StateLoop_continue;
					}
					// Return continuation result to the caller
					interpreterResult = cjump_1.result;
					interpreterResultDbl = cjump_1.resultDbl;
					throwable = null;
				}
				goto StateLoop_break;
StateLoop_continue: ;
			}
StateLoop_break: ;
			// end of StateLoop: for(;;)
			// Do cleanups/restorations before the final return or throw
			if (cx.previousInterpreterInvocations != null && cx.previousInterpreterInvocations.Size() != 0)
			{
				cx.lastInterpreterFrame = cx.previousInterpreterInvocations.Pop();
			}
			else
			{
				// It was the last interpreter frame on the stack
				cx.lastInterpreterFrame = null;
				// Force GC of the value cx.previousInterpreterInvocations
				cx.previousInterpreterInvocations = null;
			}
			if (throwable != null)
			{
				// Must be instance of Error or code bug
				throw (Exception) throwable;
			}
			return (interpreterResult != DBL_MRK) ? interpreterResult : ScriptRuntime.WrapNumber(interpreterResultDbl);
		}

		private static int DoInOrInstanceof(Context cx, int op, object[] stack, double[] sDbl, int stackTop)
		{
			object rhs = stack[stackTop];
			if (rhs == UniqueTag.DOUBLE_MARK)
			{
				rhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			--stackTop;
			object lhs = stack[stackTop];
			if (lhs == UniqueTag.DOUBLE_MARK)
			{
				lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			bool valBln;
			if (op == Token.IN)
			{
				valBln = ScriptRuntime.In(lhs, rhs, cx);
			}
			else
			{
				valBln = ScriptRuntime.InstanceOf(lhs, rhs, cx);
			}
			stack[stackTop] = ScriptRuntime.WrapBoolean(valBln);
			return stackTop;
		}

		private static int DoCompare(Interpreter.CallFrame frame, int op, object[] stack, double[] sDbl, int stackTop)
		{
			--stackTop;
			object rhs = stack[stackTop + 1];
			object lhs = stack[stackTop];
			bool valBln;
			double rDbl;
			double lDbl;
			if (rhs == UniqueTag.DOUBLE_MARK)
			{
				rDbl = sDbl[stackTop + 1];
				lDbl = Stack_double(frame, stackTop);
			}
			else
			{
				if (lhs == UniqueTag.DOUBLE_MARK)
				{
					rDbl = ScriptRuntime.ToNumber(rhs);
					lDbl = sDbl[stackTop];
				}
				else
				{
					goto number_compare_break;
				}
			}
			switch (op)
			{
				case Token.GE:
				{
					valBln = (lDbl >= rDbl);
					goto object_compare_break;
				}

				case Token.LE:
				{
					valBln = (lDbl <= rDbl);
					goto object_compare_break;
				}

				case Token.GT:
				{
					valBln = (lDbl > rDbl);
					goto object_compare_break;
				}

				case Token.LT:
				{
					valBln = (lDbl < rDbl);
					goto object_compare_break;
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
number_compare_break: ;
			switch (op)
			{
				case Token.GE:
				{
					valBln = ScriptRuntime.Cmp_LE(rhs, lhs);
					break;
				}

				case Token.LE:
				{
					valBln = ScriptRuntime.Cmp_LE(lhs, rhs);
					break;
				}

				case Token.GT:
				{
					valBln = ScriptRuntime.Cmp_LT(rhs, lhs);
					break;
				}

				case Token.LT:
				{
					valBln = ScriptRuntime.Cmp_LT(lhs, rhs);
					break;
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
object_compare_break: ;
			stack[stackTop] = ScriptRuntime.WrapBoolean(valBln);
			return stackTop;
		}

		private static int DoBitOp(Interpreter.CallFrame frame, int op, object[] stack, double[] sDbl, int stackTop)
		{
			int lIntValue = Stack_int32(frame, stackTop - 1);
			int rIntValue = Stack_int32(frame, stackTop);
			stack[--stackTop] = UniqueTag.DOUBLE_MARK;
			switch (op)
			{
				case Token.BITAND:
				{
					lIntValue &= rIntValue;
					break;
				}

				case Token.BITOR:
				{
					lIntValue |= rIntValue;
					break;
				}

				case Token.BITXOR:
				{
					lIntValue ^= rIntValue;
					break;
				}

				case Token.LSH:
				{
					lIntValue <<= rIntValue;
					break;
				}

				case Token.RSH:
				{
					lIntValue >>= rIntValue;
					break;
				}
			}
			sDbl[stackTop] = lIntValue;
			return stackTop;
		}

		private static int DoDelName(Context cx, int op, object[] stack, double[] sDbl, int stackTop)
		{
			object rhs = stack[stackTop];
			if (rhs == UniqueTag.DOUBLE_MARK)
			{
				rhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			--stackTop;
			object lhs = stack[stackTop];
			if (lhs == UniqueTag.DOUBLE_MARK)
			{
				lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			stack[stackTop] = ScriptRuntime.Delete(lhs, rhs, cx, op == Icode_DELNAME);
			return stackTop;
		}

		private static int DoGetElem(Context cx, Interpreter.CallFrame frame, object[] stack, double[] sDbl, int stackTop)
		{
			--stackTop;
			object lhs = stack[stackTop];
			if (lhs == UniqueTag.DOUBLE_MARK)
			{
				lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			object value;
			object id = stack[stackTop + 1];
			if (id != UniqueTag.DOUBLE_MARK)
			{
				value = ScriptRuntime.GetObjectElem(lhs, id, cx, frame.scope);
			}
			else
			{
				double d = sDbl[stackTop + 1];
				value = ScriptRuntime.GetObjectIndex(lhs, d, cx);
			}
			stack[stackTop] = value;
			return stackTop;
		}

		private static int DoSetElem(Context cx, object[] stack, double[] sDbl, int stackTop)
		{
			stackTop -= 2;
			object rhs = stack[stackTop + 2];
			if (rhs == UniqueTag.DOUBLE_MARK)
			{
				rhs = ScriptRuntime.WrapNumber(sDbl[stackTop + 2]);
			}
			object lhs = stack[stackTop];
			if (lhs == UniqueTag.DOUBLE_MARK)
			{
				lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			object value;
			object id = stack[stackTop + 1];
			if (id != UniqueTag.DOUBLE_MARK)
			{
				value = ScriptRuntime.SetObjectElem(lhs, id, rhs, cx);
			}
			else
			{
				double d = sDbl[stackTop + 1];
				value = ScriptRuntime.SetObjectIndex(lhs, d, rhs, cx);
			}
			stack[stackTop] = value;
			return stackTop;
		}

		private static int DoElemIncDec(Context cx, Interpreter.CallFrame frame, sbyte[] iCode, object[] stack, double[] sDbl, int stackTop)
		{
			object rhs = stack[stackTop];
			if (rhs == UniqueTag.DOUBLE_MARK)
			{
				rhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			--stackTop;
			object lhs = stack[stackTop];
			if (lhs == UniqueTag.DOUBLE_MARK)
			{
				lhs = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			stack[stackTop] = ScriptRuntime.ElemIncrDecr(lhs, rhs, cx, iCode[frame.pc]);
			++frame.pc;
			return stackTop;
		}

		private static int DoCallSpecial(Context cx, Interpreter.CallFrame frame, object[] stack, double[] sDbl, int stackTop, sbyte[] iCode, int indexReg)
		{
			int callType = iCode[frame.pc] & unchecked((int)(0xFF));
			bool isNew = (iCode[frame.pc + 1] != 0);
			int sourceLine = GetIndex(iCode, frame.pc + 2);
			// indexReg: number of arguments
			if (isNew)
			{
				// stack change: function arg0 .. argN -> newResult
				stackTop -= indexReg;
				object function = stack[stackTop];
				if (function == UniqueTag.DOUBLE_MARK)
				{
					function = ScriptRuntime.WrapNumber(sDbl[stackTop]);
				}
				object[] outArgs = GetArgsArray(stack, sDbl, stackTop + 1, indexReg);
				stack[stackTop] = ScriptRuntime.NewSpecial(cx, function, outArgs, frame.scope, callType);
			}
			else
			{
				// stack change: function thisObj arg0 .. argN -> result
				stackTop -= 1 + indexReg;
				// Call code generation ensure that stack here
				// is ... Callable Scriptable
				Scriptable functionThis = (Scriptable)stack[stackTop + 1];
				Callable function = (Callable)stack[stackTop];
				object[] outArgs = GetArgsArray(stack, sDbl, stackTop + 2, indexReg);
				stack[stackTop] = ScriptRuntime.CallSpecial(cx, function, functionThis, outArgs, frame.scope, frame.thisObj, callType, frame.idata.itsSourceFile, sourceLine);
			}
			frame.pc += 4;
			return stackTop;
		}

        private static int DoSetConstVar(Interpreter.CallFrame frame, object[] stack, double[] sDbl, int stackTop, object[] vars, double[] varDbls, PropertyAttributes[] varAttributes, int indexReg)
		{
			if (!frame.useActivation)
			{
				if ((varAttributes[indexReg] & PropertyAttributes.READONLY) == 0)
				{
					throw Context.ReportRuntimeError1("msg.var.redecl", frame.idata.argNames[indexReg]);
				}
				if ((varAttributes[indexReg] & PropertyAttributes.UNINITIALIZED_CONST) != 0)
				{
					vars[indexReg] = stack[stackTop];
					varAttributes[indexReg] &= ~PropertyAttributes.UNINITIALIZED_CONST;
					varDbls[indexReg] = sDbl[stackTop];
				}
			}
			else
			{
				object val = stack[stackTop];
				if (val == UniqueTag.DOUBLE_MARK)
				{
					val = ScriptRuntime.WrapNumber(sDbl[stackTop]);
				}
				string stringReg = frame.idata.argNames[indexReg];
				if (frame.scope is ConstProperties)
				{
					ConstProperties cp = (ConstProperties)frame.scope;
					cp.PutConst(stringReg, frame.scope, val);
				}
				else
				{
					throw Kit.CodeBug();
				}
			}
			return stackTop;
		}

        private static int DoSetVar(Interpreter.CallFrame frame, object[] stack, double[] sDbl, int stackTop, object[] vars, double[] varDbls, PropertyAttributes[] varAttributes, int indexReg)
		{
			if (!frame.useActivation)
			{
				if ((varAttributes[indexReg] & PropertyAttributes.READONLY) == 0)
				{
					vars[indexReg] = stack[stackTop];
					varDbls[indexReg] = sDbl[stackTop];
				}
			}
			else
			{
				object val = stack[stackTop];
				if (val == UniqueTag.DOUBLE_MARK)
				{
					val = ScriptRuntime.WrapNumber(sDbl[stackTop]);
				}
				string stringReg = frame.idata.argNames[indexReg];
				frame.scope.Put(stringReg, frame.scope, val);
			}
			return stackTop;
		}

		private static int DoGetVar(Interpreter.CallFrame frame, object[] stack, double[] sDbl, int stackTop, object[] vars, double[] varDbls, int indexReg)
		{
			++stackTop;
			if (!frame.useActivation)
			{
				stack[stackTop] = vars[indexReg];
				sDbl[stackTop] = varDbls[indexReg];
			}
			else
			{
				string stringReg = frame.idata.argNames[indexReg];
				stack[stackTop] = frame.scope.Get(stringReg, frame.scope);
			}
			return stackTop;
		}

		private static int DoVarIncDec(Context cx, Interpreter.CallFrame frame, object[] stack, double[] sDbl, int stackTop, object[] vars, double[] varDbls, int indexReg)
		{
			// indexReg : varindex
			++stackTop;
			int incrDecrMask = frame.idata.itsICode[frame.pc];
			if (!frame.useActivation)
			{
				stack[stackTop] = UniqueTag.DOUBLE_MARK;
				object varValue = vars[indexReg];
				double d;
				if (varValue == UniqueTag.DOUBLE_MARK)
				{
					d = varDbls[indexReg];
				}
				else
				{
					d = ScriptRuntime.ToNumber(varValue);
					vars[indexReg] = UniqueTag.DOUBLE_MARK;
				}
				double d2 = ((incrDecrMask & Node.DECR_FLAG) == 0) ? d + 1.0 : d - 1.0;
				varDbls[indexReg] = d2;
				sDbl[stackTop] = ((incrDecrMask & Node.POST_FLAG) == 0) ? d2 : d;
			}
			else
			{
				string varName = frame.idata.argNames[indexReg];
				stack[stackTop] = ScriptRuntime.NameIncrDecr(frame.scope, varName, cx, incrDecrMask);
			}
			++frame.pc;
			return stackTop;
		}

		private static int DoRefMember(Context cx, object[] stack, double[] sDbl, int stackTop, int flags)
		{
			object elem = stack[stackTop];
			if (elem == UniqueTag.DOUBLE_MARK)
			{
				elem = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			--stackTop;
			object obj = stack[stackTop];
			if (obj == UniqueTag.DOUBLE_MARK)
			{
				obj = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			stack[stackTop] = ScriptRuntime.MemberRef(obj, elem, cx, flags);
			return stackTop;
		}

		private static int DoRefNsMember(Context cx, object[] stack, double[] sDbl, int stackTop, int flags)
		{
			object elem = stack[stackTop];
			if (elem == UniqueTag.DOUBLE_MARK)
			{
				elem = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			--stackTop;
			object ns = stack[stackTop];
			if (ns == UniqueTag.DOUBLE_MARK)
			{
				ns = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			--stackTop;
			object obj = stack[stackTop];
			if (obj == UniqueTag.DOUBLE_MARK)
			{
				obj = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			stack[stackTop] = ScriptRuntime.MemberRef(obj, ns, elem, cx, flags);
			return stackTop;
		}

		private static int DoRefNsName(Context cx, Interpreter.CallFrame frame, object[] stack, double[] sDbl, int stackTop, int flags)
		{
			object name = stack[stackTop];
			if (name == UniqueTag.DOUBLE_MARK)
			{
				name = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			--stackTop;
			object ns = stack[stackTop];
			if (ns == UniqueTag.DOUBLE_MARK)
			{
				ns = ScriptRuntime.WrapNumber(sDbl[stackTop]);
			}
			stack[stackTop] = ScriptRuntime.NameRef(ns, name, cx, frame.scope, flags);
			return stackTop;
		}

		/// <summary>Call __noSuchMethod__.</summary>
		/// <remarks>Call __noSuchMethod__.</remarks>
		private static Interpreter.CallFrame InitFrameForNoSuchMethod(Context cx, Interpreter.CallFrame frame, int indexReg, object[] stack, double[] sDbl, int stackTop, int op, Scriptable funThisObj, Scriptable calleeScope, ScriptRuntime.NoSuchMethodShim noSuchMethodShim, InterpretedFunction ifun)
		{
			// create an args array from the stack
			object[] argsArray = null;
			// exactly like getArgsArray except that the first argument
			// is the method name from the shim
			int shift = stackTop + 2;
			object[] elements = new object[indexReg];
			for (int i = 0; i < indexReg; ++i, ++shift)
			{
				object val = stack[shift];
				if (val == UniqueTag.DOUBLE_MARK)
				{
					val = ScriptRuntime.WrapNumber(sDbl[shift]);
				}
				elements[i] = val;
			}
			argsArray = new object[2];
			argsArray[0] = noSuchMethodShim.methodName;
			argsArray[1] = cx.NewArray(calleeScope, elements);
			// exactly the same as if it's a regular InterpretedFunction
			Interpreter.CallFrame callParentFrame = frame;
			Interpreter.CallFrame calleeFrame = new Interpreter.CallFrame();
			if (op == Icode_TAIL_CALL)
			{
				callParentFrame = frame.parentFrame;
				ExitFrame(cx, frame, null);
			}
			// init the frame with the underlying method with the
			// adjusted args array and shim's function
			InitFrame(cx, calleeScope, funThisObj, argsArray, null, 0, 2, ifun, callParentFrame, calleeFrame);
			if (op != Icode_TAIL_CALL)
			{
				frame.savedStackTop = stackTop;
				frame.savedCallOp = op;
			}
			return calleeFrame;
		}

		private static bool DoEquals(object[] stack, double[] sDbl, int stackTop)
		{
			object rhs = stack[stackTop + 1];
			object lhs = stack[stackTop];
			if (rhs == UniqueTag.DOUBLE_MARK)
			{
				if (lhs == UniqueTag.DOUBLE_MARK)
				{
					return (sDbl[stackTop] == sDbl[stackTop + 1]);
				}
				else
				{
					return ScriptRuntime.EqNumber(sDbl[stackTop + 1], lhs);
				}
			}
			else
			{
				if (lhs == UniqueTag.DOUBLE_MARK)
				{
					return ScriptRuntime.EqNumber(sDbl[stackTop], rhs);
				}
				else
				{
					return ScriptRuntime.Eq(lhs, rhs);
				}
			}
		}

		private static bool DoShallowEquals(object[] stack, double[] sDbl, int stackTop)
		{
			object rhs = stack[stackTop + 1];
			object lhs = stack[stackTop];
			object DBL_MRK = UniqueTag.DOUBLE_MARK;
			double rdbl;
			double ldbl;
			if (rhs == DBL_MRK)
			{
				rdbl = sDbl[stackTop + 1];
				if (lhs == DBL_MRK)
				{
					ldbl = sDbl[stackTop];
				}
				else
				{
					if (lhs.IsNumber())
					{
						ldbl = System.Convert.ToDouble(lhs);
					}
					else
					{
						return false;
					}
				}
			}
			else
			{
				if (lhs == DBL_MRK)
				{
					ldbl = sDbl[stackTop];
					if (rhs.IsNumber())
					{
						rdbl = System.Convert.ToDouble(rhs);
					}
					else
					{
						return false;
					}
				}
				else
				{
					return ScriptRuntime.ShallowEq(lhs, rhs);
				}
			}
			return (ldbl == rdbl);
		}

		private static Interpreter.CallFrame ProcessThrowable(Context cx, object throwable, Interpreter.CallFrame frame, int indexReg, bool instructionCounting)
		{
			// Recovering from exception, indexReg contains
			// the index of handler
			if (indexReg >= 0)
			{
				// Normal exception handler, transfer
				// control appropriately
				if (frame.frozen)
				{
					// XXX Deal with exceptios!!!
					frame = frame.CloneFrozen();
				}
				int[] table = frame.idata.itsExceptionTable;
				frame.pc = table[indexReg + EXCEPTION_HANDLER_SLOT];
				if (instructionCounting)
				{
					frame.pcPrevBranch = frame.pc;
				}
				frame.savedStackTop = frame.emptyStackTop;
				int scopeLocal = frame.localShift + table[indexReg + EXCEPTION_SCOPE_SLOT];
				int exLocal = frame.localShift + table[indexReg + EXCEPTION_LOCAL_SLOT];
				frame.scope = (Scriptable)frame.stack[scopeLocal];
				frame.stack[exLocal] = throwable;
				throwable = null;
			}
			else
			{
				// Continuation restoration
				Interpreter.ContinuationJump cjump = (Interpreter.ContinuationJump)throwable;
				// Clear throwable to indicate that exceptions are OK
				throwable = null;
				if (cjump.branchFrame != frame)
				{
					Kit.CodeBug();
				}
				// Check that we have at least one frozen frame
				// in the case of detached continuation restoration:
				// unwind code ensure that
				if (cjump.capturedFrame == null)
				{
					Kit.CodeBug();
				}
				// Need to rewind branchFrame, capturedFrame
				// and all frames in between
				int rewindCount = cjump.capturedFrame.frameIndex + 1;
				if (cjump.branchFrame != null)
				{
					rewindCount -= cjump.branchFrame.frameIndex;
				}
				int enterCount = 0;
				Interpreter.CallFrame[] enterFrames = null;
				Interpreter.CallFrame x = cjump.capturedFrame;
				for (int i = 0; i != rewindCount; ++i)
				{
					if (!x.frozen)
					{
						Kit.CodeBug();
					}
					if (IsFrameEnterExitRequired(x))
					{
						if (enterFrames == null)
						{
							// Allocate enough space to store the rest
							// of rewind frames in case all of them
							// would require to enter
							enterFrames = new Interpreter.CallFrame[rewindCount - i];
						}
						enterFrames[enterCount] = x;
						++enterCount;
					}
					x = x.parentFrame;
				}
				while (enterCount != 0)
				{
					// execute enter: walk enterFrames in the reverse
					// order since they were stored starting from
					// the capturedFrame, not branchFrame
					--enterCount;
					x = enterFrames[enterCount];
					EnterFrame(cx, x, ScriptRuntime.emptyArgs, true);
				}
				// Continuation jump is almost done: capturedFrame
				// points to the call to the function that captured
				// continuation, so clone capturedFrame and
				// emulate return that function with the suplied result
				frame = cjump.capturedFrame.CloneFrozen();
				SetCallResult(frame, cjump.result, cjump.resultDbl);
			}
			// restart the execution
			frame.throwable = throwable;
			return frame;
		}

		private static object FreezeGenerator(Context cx, Interpreter.CallFrame frame, int stackTop, Interpreter.GeneratorState generatorState)
		{
			if (generatorState.operation == NativeGenerator.GENERATOR_CLOSE)
			{
				// Error: no yields when generator is closing
				throw ScriptRuntime.TypeError0("msg.yield.closing");
			}
			// return to our caller (which should be a method of NativeGenerator)
			frame.frozen = true;
			frame.result = frame.stack[stackTop];
			frame.resultDbl = frame.sDbl[stackTop];
			frame.savedStackTop = stackTop;
			frame.pc--;
			// we want to come back here when we resume
			ScriptRuntime.ExitActivationFunction(cx);
			return (frame.result != UniqueTag.DOUBLE_MARK) ? frame.result : ScriptRuntime.WrapNumber(frame.resultDbl);
		}

		private static object ThawGenerator(Interpreter.CallFrame frame, int stackTop, Interpreter.GeneratorState generatorState, int op)
		{
			// we are resuming execution
			frame.frozen = false;
			int sourceLine = GetIndex(frame.idata.itsICode, frame.pc);
			frame.pc += 2;
			// skip line number data
			if (generatorState.operation == NativeGenerator.GENERATOR_THROW)
			{
				// processing a call to <generator>.throw(exception): must
				// act as if exception was thrown from resumption point
				return new JavaScriptException(generatorState.value, frame.idata.itsSourceFile, sourceLine);
			}
			if (generatorState.operation == NativeGenerator.GENERATOR_CLOSE)
			{
				return generatorState.value;
			}
			if (generatorState.operation != NativeGenerator.GENERATOR_SEND)
			{
				throw Kit.CodeBug();
			}
			if (op == Token.YIELD)
			{
				frame.stack[stackTop] = generatorState.value;
			}
			return ScriptableConstants.NOT_FOUND;
		}

		private static Interpreter.CallFrame InitFrameForApplyOrCall(Context cx, Interpreter.CallFrame frame, int indexReg, object[] stack, double[] sDbl, int stackTop, int op, Scriptable calleeScope, IdFunctionObject ifun, InterpretedFunction iApplyCallable)
		{
			Scriptable applyThis;
			if (indexReg != 0)
			{
				object obj = stack[stackTop + 2];
				if (obj == UniqueTag.DOUBLE_MARK)
				{
					obj = ScriptRuntime.WrapNumber(sDbl[stackTop + 2]);
				}
				applyThis = ScriptRuntime.ToObjectOrNull(cx, obj);
			}
			else
			{
				applyThis = null;
			}
			if (applyThis == null)
			{
				// This covers the case of args[0] == (null|undefined) as well.
				applyThis = ScriptRuntime.GetTopCallScope(cx);
			}
			if (op == Icode_TAIL_CALL)
			{
				ExitFrame(cx, frame, null);
				frame = frame.parentFrame;
			}
			else
			{
				frame.savedStackTop = stackTop;
				frame.savedCallOp = op;
			}
			Interpreter.CallFrame calleeFrame = new Interpreter.CallFrame();
			if (BaseFunction.IsApply(ifun))
			{
				object[] callArgs = indexReg < 2 ? ScriptRuntime.emptyArgs : ScriptRuntime.GetApplyArguments(cx, stack[stackTop + 3]);
				InitFrame(cx, calleeScope, applyThis, callArgs, null, 0, callArgs.Length, iApplyCallable, frame, calleeFrame);
			}
			else
			{
				// Shift args left
				for (int i = 1; i < indexReg; ++i)
				{
					stack[stackTop + 1 + i] = stack[stackTop + 2 + i];
					sDbl[stackTop + 1 + i] = sDbl[stackTop + 2 + i];
				}
				int argCount = indexReg < 2 ? 0 : indexReg - 1;
				InitFrame(cx, calleeScope, applyThis, stack, sDbl, stackTop + 2, argCount, iApplyCallable, frame, calleeFrame);
			}
			frame = calleeFrame;
			return frame;
		}

		private static void InitFrame(Context cx, Scriptable callerScope, Scriptable thisObj, object[] args, double[] argsDbl, int argShift, int argCount, InterpretedFunction fnOrScript, Interpreter.CallFrame parentFrame, Interpreter.CallFrame frame)
		{
			InterpreterData idata = fnOrScript.idata;
			bool useActivation = idata.itsNeedsActivation;
			DebugFrame debuggerFrame = null;
			if (cx.debugger != null)
			{
				debuggerFrame = cx.debugger.GetFrame(cx, idata);
				if (debuggerFrame != null)
				{
					useActivation = true;
				}
			}
			if (useActivation)
			{
				// Copy args to new array to pass to enterActivationFunction
				// or debuggerFrame.onEnter
				if (argsDbl != null)
				{
					args = GetArgsArray(args, argsDbl, argShift, argCount);
				}
				argShift = 0;
				argsDbl = null;
			}
			Scriptable scope;
			if (idata.itsFunctionType != 0)
			{
				scope = fnOrScript.ParentScope;
				if (useActivation)
				{
					scope = ScriptRuntime.CreateFunctionActivation(fnOrScript, scope, args);
				}
			}
			else
			{
				scope = callerScope;
				ScriptRuntime.InitScript(fnOrScript, thisObj, cx, scope, fnOrScript.idata.evalScriptFlag);
			}
			if (idata.itsNestedFunctions != null)
			{
				if (idata.itsFunctionType != 0 && !idata.itsNeedsActivation)
				{
					Kit.CodeBug();
				}
				for (int i = 0; i < idata.itsNestedFunctions.Length; i++)
				{
					InterpreterData fdata = idata.itsNestedFunctions[i];
					if (fdata.itsFunctionType == FunctionNode.FUNCTION_STATEMENT)
					{
						InitFunction(cx, scope, fnOrScript, i);
					}
				}
			}
			// Initialize args, vars, locals and stack
			int emptyStackTop = idata.itsMaxVars + idata.itsMaxLocals - 1;
			int maxFrameArray = idata.itsMaxFrameArray;
			if (maxFrameArray != emptyStackTop + idata.itsMaxStack + 1)
			{
				Kit.CodeBug();
			}
			object[] stack;
            PropertyAttributes[] stackAttributes;
			double[] sDbl;
			bool stackReuse;
			if (frame.stack != null && maxFrameArray <= frame.stack.Length)
			{
				// Reuse stacks from old frame
				stackReuse = true;
				stack = frame.stack;
				stackAttributes = frame.stackAttributes;
				sDbl = frame.sDbl;
			}
			else
			{
				stackReuse = false;
				stack = new object[maxFrameArray];
                stackAttributes = new PropertyAttributes[maxFrameArray];
				sDbl = new double[maxFrameArray];
			}
			int varCount = idata.GetParamAndVarCount();
			for (int i_1 = 0; i_1 < varCount; i_1++)
			{
				if (idata.GetParamOrVarConst(i_1))
				{
					stackAttributes[i_1] = PropertyAttributes.CONST;
				}
			}
			int definedArgs = idata.argCount;
			if (definedArgs > argCount)
			{
				definedArgs = argCount;
			}
			// Fill the frame structure
			frame.parentFrame = parentFrame;
			frame.frameIndex = (parentFrame == null) ? 0 : parentFrame.frameIndex + 1;
			if (frame.frameIndex > cx.GetMaximumInterpreterStackDepth())
			{
				throw Context.ReportRuntimeError("Exceeded maximum stack depth");
			}
			frame.frozen = false;
			frame.fnOrScript = fnOrScript;
			frame.idata = idata;
			frame.stack = stack;
			frame.stackAttributes = stackAttributes;
			frame.sDbl = sDbl;
			frame.varSource = frame;
			frame.localShift = idata.itsMaxVars;
			frame.emptyStackTop = emptyStackTop;
			frame.debuggerFrame = debuggerFrame;
			frame.useActivation = useActivation;
			frame.thisObj = thisObj;
			// Initialize initial values of variables that change during
			// interpretation.
			frame.result = Undefined.instance;
			frame.pc = 0;
			frame.pcPrevBranch = 0;
			frame.pcSourceLineStart = idata.firstLinePC;
			frame.scope = scope;
			frame.savedStackTop = emptyStackTop;
			frame.savedCallOp = 0;
			System.Array.Copy(args, argShift, stack, 0, definedArgs);
			if (argsDbl != null)
			{
				System.Array.Copy(argsDbl, argShift, sDbl, 0, definedArgs);
			}
			for (int i_2 = definedArgs; i_2 != idata.itsMaxVars; ++i_2)
			{
				stack[i_2] = Undefined.instance;
			}
			if (stackReuse)
			{
				// Clean the stack part and space beyond stack if any
				// of the old array to allow to GC objects there
				for (int i = emptyStackTop + 1; i != stack.Length; ++i)
				{
					stack[i] = null;
				}
			}
			EnterFrame(cx, frame, args, false);
		}

		private static bool IsFrameEnterExitRequired(Interpreter.CallFrame frame)
		{
			return frame.debuggerFrame != null || frame.idata.itsNeedsActivation;
		}

		private static void EnterFrame(Context cx, Interpreter.CallFrame frame, object[] args, bool continuationRestart)
		{
			bool usesActivation = frame.idata.itsNeedsActivation;
			bool isDebugged = frame.debuggerFrame != null;
			if (usesActivation || isDebugged)
			{
				Scriptable scope = frame.scope;
				if (scope == null)
				{
					Kit.CodeBug();
				}
				else
				{
					if (continuationRestart)
					{
						// Walk the parent chain of frame.scope until a NativeCall is
						// found. Normally, frame.scope is a NativeCall when called
						// from initFrame() for a debugged or activatable function.
						// However, when called from interpretLoop() as part of
						// restarting a continuation, it can also be a NativeWith if
						// the continuation was captured within a "with" or "catch"
						// block ("catch" implicitly uses NativeWith to create a scope
						// to expose the exception variable).
						for (; ; )
						{
							if (scope is NativeWith)
							{
								scope = scope.ParentScope;
								if (scope == null || (frame.parentFrame != null && frame.parentFrame.scope == scope))
								{
									// If we get here, we didn't find a NativeCall in
									// the call chain before reaching parent frame's
									// scope. This should not be possible.
									Kit.CodeBug();
									break;
								}
							}
							else
							{
								// Never reached, but keeps the static analyzer
								// happy about "scope" not being null 5 lines above.
								break;
							}
						}
					}
				}
				if (isDebugged)
				{
					frame.debuggerFrame.OnEnter(cx, scope, frame.thisObj, args);
				}
				// Enter activation only when itsNeedsActivation true,
				// since debugger should not interfere with activation
				// chaining
				if (usesActivation)
				{
					ScriptRuntime.EnterActivationFunction(cx, scope);
				}
			}
		}

		private static void ExitFrame(Context cx, Interpreter.CallFrame frame, object throwable)
		{
			if (frame.idata.itsNeedsActivation)
			{
				ScriptRuntime.ExitActivationFunction(cx);
			}
			if (frame.debuggerFrame != null)
			{
				try
				{
					if (throwable is Exception)
					{
						frame.debuggerFrame.OnExit(cx, true, throwable);
					}
					else
					{
						object result;
						Interpreter.ContinuationJump cjump = (Interpreter.ContinuationJump)throwable;
						if (cjump == null)
						{
							result = frame.result;
						}
						else
						{
							result = cjump.result;
						}
						if (result == UniqueTag.DOUBLE_MARK)
						{
							double resultDbl;
							if (cjump == null)
							{
								resultDbl = frame.resultDbl;
							}
							else
							{
								resultDbl = cjump.resultDbl;
							}
							result = ScriptRuntime.WrapNumber(resultDbl);
						}
						frame.debuggerFrame.OnExit(cx, false, result);
					}
				}
				catch (Exception ex)
				{
					System.Console.Error.WriteLine("RHINO USAGE WARNING: onExit terminated with exception");
					System.Console.Error.WriteLine(ex);
				}
			}
		}

		private static void SetCallResult(Interpreter.CallFrame frame, object callResult, double callResultDbl)
		{
			if (frame.savedCallOp == Token.CALL)
			{
				frame.stack[frame.savedStackTop] = callResult;
				frame.sDbl[frame.savedStackTop] = callResultDbl;
			}
			else
			{
				if (frame.savedCallOp == Token.NEW)
				{
					// If construct returns scriptable,
					// then it replaces on stack top saved original instance
					// of the object.
					if (callResult is Scriptable)
					{
						frame.stack[frame.savedStackTop] = callResult;
					}
				}
				else
				{
					Kit.CodeBug();
				}
			}
			frame.savedCallOp = 0;
		}

		public static NativeContinuation CaptureContinuation(Context cx)
		{
			if (cx.lastInterpreterFrame == null || !(cx.lastInterpreterFrame is Interpreter.CallFrame))
			{
				throw new InvalidOperationException("Interpreter frames not found");
			}
			return CaptureContinuation(cx, (Interpreter.CallFrame)cx.lastInterpreterFrame, true);
		}

		private static NativeContinuation CaptureContinuation(Context cx, Interpreter.CallFrame frame, bool requireContinuationsTopFrame)
		{
			NativeContinuation c = new NativeContinuation();
			ScriptRuntime.SetObjectProtoAndParent(c, ScriptRuntime.GetTopCallScope(cx));
			// Make sure that all frames are frozen
			Interpreter.CallFrame x = frame;
			Interpreter.CallFrame outermost = frame;
			while (x != null && !x.frozen)
			{
				x.frozen = true;
				// Allow to GC unused stack space
				for (int i = x.savedStackTop + 1; i != x.stack.Length; ++i)
				{
					// Allow to GC unused stack space
					x.stack[i] = null;
					x.stackAttributes[i] = PropertyAttributes.EMPTY;
				}
				if (x.savedCallOp == Token.CALL)
				{
					// the call will always overwrite the stack top with the result
					x.stack[x.savedStackTop] = null;
				}
				else
				{
					if (x.savedCallOp != Token.NEW)
					{
						Kit.CodeBug();
					}
				}
				// the new operator uses stack top to store the constructed
				// object so it shall not be cleared: see comments in
				// setCallResult
				outermost = x;
				x = x.parentFrame;
			}
			if (requireContinuationsTopFrame)
			{
				while (outermost.parentFrame != null)
				{
					outermost = outermost.parentFrame;
				}
				if (!outermost.isContinuationsTopFrame)
				{
					throw new InvalidOperationException("Cannot capture continuation " + "from JavaScript code not called directly by " + "executeScriptWithContinuations or " + "callFunctionWithContinuations");
				}
			}
			c.InitImplementation(frame);
			return c;
		}

		private static int Stack_int32(Interpreter.CallFrame frame, int i)
		{
			object x = frame.stack[i];
			if (x == UniqueTag.DOUBLE_MARK)
			{
				return ScriptRuntime.ToInt32(frame.sDbl[i]);
			}
			else
			{
				return ScriptRuntime.ToInt32(x);
			}
		}

		private static double Stack_double(Interpreter.CallFrame frame, int i)
		{
			object x = frame.stack[i];
			if (x != UniqueTag.DOUBLE_MARK)
			{
				return ScriptRuntime.ToNumber(x);
			}
			else
			{
				return frame.sDbl[i];
			}
		}

		private static bool Stack_boolean(Interpreter.CallFrame frame, int i)
		{
			object x = frame.stack[i];
			if (x is bool)
			{
				return (bool) x;
			}
			else
			{
				if (x == UniqueTag.DOUBLE_MARK)
				{
					double d = frame.sDbl[i];
					return !Double.IsNaN(d) && d != 0.0;
				}
				else
				{
					if (x == null || x == Undefined.instance)
					{
						return false;
					}
					else
					{
						if (x.IsNumber())
						{
							double d = System.Convert.ToDouble(x);
							return (!Double.IsNaN(d) && d != 0.0);
						}
						else
						{
							return ScriptRuntime.ToBoolean(x);
						}
					}
				}
			}
		}

		private static void DoAdd(object[] stack, double[] sDbl, int stackTop, Context cx)
		{
			object rhs = stack[stackTop + 1];
			object lhs = stack[stackTop];
			double d;
			bool leftRightOrder;
			if (rhs == UniqueTag.DOUBLE_MARK)
			{
				d = sDbl[stackTop + 1];
				if (lhs == UniqueTag.DOUBLE_MARK)
				{
					sDbl[stackTop] += d;
					return;
				}
				leftRightOrder = true;
			}
			else
			{
				// fallthrough to object + number code
				if (lhs == UniqueTag.DOUBLE_MARK)
				{
					d = sDbl[stackTop];
					lhs = rhs;
					leftRightOrder = false;
				}
				else
				{
					// fallthrough to object + number code
					if (lhs is Scriptable || rhs is Scriptable)
					{
						stack[stackTop] = ScriptRuntime.Add(lhs, rhs, cx);
					}
					else
					{
						if (lhs is string || rhs is string)
						{
							string lstr = ScriptRuntime.ToCharSequence(lhs);
							string rstr = ScriptRuntime.ToCharSequence(rhs);
							stack[stackTop] = lstr + rstr;
						}
						else
						{
							double lDbl = (lhs.IsNumber()) ? System.Convert.ToDouble(lhs) : ScriptRuntime.ToNumber(lhs);
							double rDbl = (rhs.IsNumber()) ? System.Convert.ToDouble(rhs) : ScriptRuntime.ToNumber(rhs);
							stack[stackTop] = UniqueTag.DOUBLE_MARK;
							sDbl[stackTop] = lDbl + rDbl;
						}
					}
					return;
				}
			}
			// handle object(lhs) + number(d) code
			if (lhs is Scriptable)
			{
				rhs = ScriptRuntime.WrapNumber(d);
				if (!leftRightOrder)
				{
					object tmp = lhs;
					lhs = rhs;
					rhs = tmp;
				}
				stack[stackTop] = ScriptRuntime.Add(lhs, rhs, cx);
			}
			else
			{
				if (lhs is string)
				{
					string lstr = (string)lhs;
					string rstr = ScriptRuntime.ToCharSequence(d);
					if (leftRightOrder)
					{
						stack[stackTop] = lstr + rstr;
					}
					else
					{
						stack[stackTop] = rstr + lstr;
					}
				}
				else
				{
					double lDbl = (lhs.IsNumber()) ? System.Convert.ToDouble(lhs) : ScriptRuntime.ToNumber(lhs);
					stack[stackTop] = UniqueTag.DOUBLE_MARK;
					sDbl[stackTop] = lDbl + d;
				}
			}
		}

		private static int DoArithmetic(Interpreter.CallFrame frame, int op, object[] stack, double[] sDbl, int stackTop)
		{
			double rDbl = Stack_double(frame, stackTop);
			--stackTop;
			double lDbl = Stack_double(frame, stackTop);
			stack[stackTop] = UniqueTag.DOUBLE_MARK;
			switch (op)
			{
				case Token.SUB:
				{
					lDbl -= rDbl;
					break;
				}

				case Token.MUL:
				{
					lDbl *= rDbl;
					break;
				}

				case Token.DIV:
				{
					lDbl /= rDbl;
					break;
				}

				case Token.MOD:
				{
					lDbl %= rDbl;
					break;
				}
			}
			sDbl[stackTop] = lDbl;
			return stackTop;
		}

		private static object[] GetArgsArray(object[] stack, double[] sDbl, int shift, int count)
		{
			if (count == 0)
			{
				return ScriptRuntime.emptyArgs;
			}
			object[] args = new object[count];
			for (int i = 0; i != count; ++i, ++shift)
			{
				object val = stack[shift];
				if (val == UniqueTag.DOUBLE_MARK)
				{
					val = ScriptRuntime.WrapNumber(sDbl[shift]);
				}
				args[i] = val;
			}
			return args;
		}

		private static void AddInstructionCount(Context cx, Interpreter.CallFrame frame, int extra)
		{
			cx.instructionCount += frame.pc - frame.pcPrevBranch + extra;
			if (cx.instructionCount > cx.instructionThreshold)
			{
				cx.ObserveInstructionCount(cx.instructionCount);
				cx.instructionCount = 0;
			}
		}
	}
}
