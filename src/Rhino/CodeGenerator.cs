/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Rhino;
using Rhino.Ast;
using Rhino.Utils;
using Sharpen;
using Label = System.Reflection.Emit.Label;

namespace Rhino
{
	/// <summary>Generates bytecode for the Interpreter.</summary>
	/// <remarks>Generates bytecode for the Interpreter.</remarks>
	internal class CodeGenerator : Icode
	{
		private const int MIN_LABEL_TABLE_SIZE = 32;

		private const int MIN_FIXUP_TABLE_SIZE = 40;

		private CompilerEnvirons compilerEnv;

		private bool itsInFunctionFlag;

		private bool itsInTryFlag;

		private InterpreterData itsData;

		private ScriptNode scriptOrFn;

		private int iCodeTop;

		private int stackDepth;

		private int lineNumber;

		private int doubleTableTop;

		private readonly IDictionary<string, int> strings = new Dictionary<string, int>(20);

		private int localTop;

		private int[] labelTable;

		private int labelTableTop;

		private long[] fixupTable;

		private int fixupTableTop;

		private ArrayList literalIds = new ArrayList();

		private int exceptionTableTop;

		private const int ECF_TAIL = 1 << 0;

		// fixupTable[i] = (label_index << 32) | fixup_site
		// ECF_ or Expression Context Flags constants: for now only TAIL
		public virtual InterpreterData Compile(CompilerEnvirons compilerEnv, ScriptNode tree, string encodedSource, bool returnFunction)
		{
			this.compilerEnv = compilerEnv;
			new NodeTransformer().Transform(tree);
			if (returnFunction)
			{
				scriptOrFn = tree.GetFunctionNode(0);
			}
			else
			{
				scriptOrFn = tree;
			}
			itsData = new InterpreterData(compilerEnv.LanguageVersion, scriptOrFn.GetSourceName(), encodedSource, ((AstRoot)tree).IsInStrictMode());
			itsData.topLevel = true;
			if (returnFunction)
			{
				GenerateFunctionICode();
			}
			else
			{
				GenerateICodeFromTree(scriptOrFn);
			}
			return itsData;
		}

		private void GenerateFunctionICode()
		{
			itsInFunctionFlag = true;
			FunctionNode theFunction = (FunctionNode)scriptOrFn;
			itsData.itsFunctionType = theFunction.GetFunctionType();
			itsData.itsNeedsActivation = theFunction.RequiresActivation();
			if (theFunction.GetFunctionName() != null)
			{
				itsData.itsName = theFunction.GetName();
			}
			if (theFunction.IsGenerator())
			{
				AddIcode(Icode_GENERATOR);
				AddUint16(theFunction.GetBaseLineno() & 0xffff);
			}
			GenerateICodeFromTree(theFunction.GetLastChild());
		}

		private void GenerateICodeFromTree(Node tree)
		{
			GenerateNestedFunctions();
			GenerateRegExpLiterals();
			VisitStatement(tree, 0);
			FixLabelGotos();
			// add RETURN_RESULT only to scripts as function always ends with RETURN
			if (itsData.itsFunctionType == 0)
			{
				AddToken(Token.RETURN_RESULT);
			}
			if (itsData.itsICode.Length != iCodeTop)
			{
				// Make itsData.itsICode length exactly iCodeTop to save memory
				// and catch bugs with jumps beyond icode as early as possible
				sbyte[] tmp = new sbyte[iCodeTop];
				Array.Copy(itsData.itsICode, 0, tmp, 0, iCodeTop);
				itsData.itsICode = tmp;
			}
			if (strings.Count == 0)
			{
				itsData.itsStringTable = null;
			}
			else
			{
				itsData.itsStringTable = new string[strings.Count];
				foreach (var iter in strings)
				{
					string str = iter.Key;
					int index = iter.Value;
					if (itsData.itsStringTable[index] != null)
					{
						Kit.CodeBug();
					}
					itsData.itsStringTable[index] = str;
				}
			}
			if (doubleTableTop == 0)
			{
				itsData.itsDoubleTable = null;
			}
			else
			{
				if (itsData.itsDoubleTable.Length != doubleTableTop)
				{
					double[] tmp = new double[doubleTableTop];
					Array.Copy(itsData.itsDoubleTable, 0, tmp, 0, doubleTableTop);
					itsData.itsDoubleTable = tmp;
				}
			}
			if (exceptionTableTop != 0 && itsData.itsExceptionTable.Length != exceptionTableTop)
			{
				int[] tmp = new int[exceptionTableTop];
				Array.Copy(itsData.itsExceptionTable, 0, tmp, 0, exceptionTableTop);
				itsData.itsExceptionTable = tmp;
			}
			itsData.itsMaxVars = scriptOrFn.GetParamAndVarCount();
			// itsMaxFrameArray: interpret method needs this amount for its
			// stack and sDbl arrays
			itsData.itsMaxFrameArray = itsData.itsMaxVars + itsData.itsMaxLocals + itsData.itsMaxStack;
			itsData.argNames = scriptOrFn.GetParamAndVarNames();
			itsData.argIsConst = scriptOrFn.GetParamAndVarConst();
			itsData.argCount = scriptOrFn.GetParamCount();
			itsData.encodedSourceStart = scriptOrFn.GetEncodedSourceStart();
			itsData.encodedSourceEnd = scriptOrFn.GetEncodedSourceEnd();
			if (literalIds.Count != 0)
			{
				itsData.literalIds = literalIds.ToArray();
			}
		}

		private void GenerateNestedFunctions()
		{
			int functionCount = scriptOrFn.GetFunctionCount();
			if (functionCount == 0)
			{
				return;
			}
			InterpreterData[] array = new InterpreterData[functionCount];
			for (int i = 0; i != functionCount; i++)
			{
				FunctionNode fn = scriptOrFn.GetFunctionNode(i);
				CodeGenerator gen = new CodeGenerator();
				gen.compilerEnv = compilerEnv;
				gen.scriptOrFn = fn;
				gen.itsData = new InterpreterData(itsData);
				gen.GenerateFunctionICode();
				array[i] = gen.itsData;
			}
			itsData.itsNestedFunctions = array;
		}

		private void GenerateRegExpLiterals()
		{
			int N = scriptOrFn.GetRegExpCount();
			if (N == 0)
			{
				return;
			}
			Context cx = Context.GetContext();
			RegExpProxy rep = ScriptRuntime.CheckRegExpProxy(cx);
			object[] array = new object[N];
			for (int i = 0; i != N; i++)
			{
				string @string = scriptOrFn.GetRegExpString(i);
				string flags = scriptOrFn.GetRegExpFlags(i);
				array[i] = rep.CompileRegExp(cx, @string, flags);
			}
			itsData.itsRegExpLiterals = array;
		}

		private void UpdateLineNumber(Node node)
		{
			int lineno = node.GetLineno();
			if (lineno != lineNumber && lineno >= 0)
			{
				if (itsData.firstLinePC < 0)
				{
					itsData.firstLinePC = lineno;
				}
				lineNumber = lineno;
				AddIcode(Icode_LINE);
				AddUint16(lineno & 0xffff);
			}
		}

		private Exception BadTree(Node node)
		{
			throw new Exception(node.ToString());
		}

		private void VisitStatement(Node node, int initialStackDepth)
		{
			int type = node.GetType();
			Node child = node.GetFirstChild();
			switch (type)
			{
				case Token.FUNCTION:
				{
					int fnIndex = node.GetExistingIntProp(Node.FUNCTION_PROP);
					int fnType = scriptOrFn.GetFunctionNode(fnIndex).GetFunctionType();
					// Only function expressions or function expression
					// statements need closure code creating new function
					// object on stack as function statements are initialized
					// at script/function start.
					// In addition, function expressions can not be present here
					// at statement level, they must only be present as expressions.
					if (fnType == FunctionNode.FUNCTION_EXPRESSION_STATEMENT)
					{
						AddIndexOp(Icode_CLOSURE_STMT, fnIndex);
					}
					else
					{
						if (fnType != FunctionNode.FUNCTION_STATEMENT)
						{
							throw Kit.CodeBug();
						}
					}
					// For function statements or function expression statements
					// in scripts, we need to ensure that the result of the script
					// is the function if it is the last statement in the script.
					// For example, eval("function () {}") should return a
					// function, not undefined.
					if (!itsInFunctionFlag)
					{
						AddIndexOp(Icode_CLOSURE_EXPR, fnIndex);
						StackChange(1);
						AddIcode(Icode_POP_RESULT);
						StackChange(-1);
					}
					break;
				}

				case Token.LABEL:
				case Token.LOOP:
				case Token.BLOCK:
				case Token.EMPTY:
				case Token.WITH:
				{
					UpdateLineNumber(node);
					goto case Token.SCRIPT;
				}

				case Token.SCRIPT:
				{
					// fall through
					while (child != null)
					{
						VisitStatement(child, initialStackDepth);
						child = child.GetNext();
					}
					break;
				}

				case Token.ENTERWITH:
				{
					VisitExpression(child, 0);
					AddToken(Token.ENTERWITH);
					StackChange(-1);
					break;
				}

				case Token.LEAVEWITH:
				{
					AddToken(Token.LEAVEWITH);
					break;
				}

				case Token.LOCAL_BLOCK:
				{
					int local = AllocLocal();
					node.PutIntProp(Node.LOCAL_PROP, local);
					UpdateLineNumber(node);
					while (child != null)
					{
						VisitStatement(child, initialStackDepth);
						child = child.GetNext();
					}
					AddIndexOp(Icode_LOCAL_CLEAR, local);
					ReleaseLocal(local);
					break;
				}

				case Token.DEBUGGER:
				{
					AddIcode(Icode_DEBUGGER);
					break;
				}

				case Token.SWITCH:
				{
					UpdateLineNumber(node);
					// See comments in IRFactory.createSwitch() for description
					// of SWITCH node
					VisitExpression(child, 0);
					for (Jump caseNode = (Jump)child.GetNext(); caseNode != null; caseNode = (Jump)caseNode.GetNext())
					{
						if (caseNode.GetType() != Token.CASE)
						{
							throw BadTree(caseNode);
						}
						Node test = caseNode.GetFirstChild();
						AddIcode(Icode_DUP);
						StackChange(1);
						VisitExpression(test, 0);
						AddToken(Token.SHEQ);
						StackChange(-1);
						// If true, Icode_IFEQ_POP will jump and remove case
						// value from stack
						AddGoto(caseNode.target, Icode_IFEQ_POP);
						StackChange(-1);
					}
					AddIcode(Icode_POP);
					StackChange(-1);
					break;
				}

				case Token.TARGET:
				{
					MarkTargetLabel(node);
					break;
				}

				case Token.IFEQ:
				case Token.IFNE:
				{
					Node target = ((Jump)node).target;
					VisitExpression(child, 0);
					AddGoto(target, type);
					StackChange(-1);
					break;
				}

				case Token.GOTO:
				{
					Node target = ((Jump)node).target;
					AddGoto(target, type);
					break;
				}

				case Token.JSR:
				{
					Node target = ((Jump)node).target;
					AddGoto(target, Icode_GOSUB);
					break;
				}

				case Token.FINALLY:
				{
					// Account for incomming GOTOSUB address
					StackChange(1);
					int finallyRegister = GetLocalBlockRef(node);
					AddIndexOp(Icode_STARTSUB, finallyRegister);
					StackChange(-1);
					while (child != null)
					{
						VisitStatement(child, initialStackDepth);
						child = child.GetNext();
					}
					AddIndexOp(Icode_RETSUB, finallyRegister);
					break;
				}

				case Token.EXPR_VOID:
				case Token.EXPR_RESULT:
				{
					UpdateLineNumber(node);
					VisitExpression(child, 0);
					AddIcode((type == Token.EXPR_VOID) ? Icode_POP : Icode_POP_RESULT);
					StackChange(-1);
					break;
				}

				case Token.TRY:
				{
					Jump tryNode = (Jump)node;
					int exceptionObjectLocal = GetLocalBlockRef(tryNode);
					int scopeLocal = AllocLocal();
					AddIndexOp(Icode_SCOPE_SAVE, scopeLocal);
					int tryStart = iCodeTop;
					bool savedFlag = itsInTryFlag;
					itsInTryFlag = true;
					while (child != null)
					{
						VisitStatement(child, initialStackDepth);
						child = child.GetNext();
					}
					itsInTryFlag = savedFlag;
					Node catchTarget = tryNode.target;
					if (catchTarget != null)
					{
						int catchStartPC = labelTable[GetTargetLabel(catchTarget)];
						AddExceptionHandler(tryStart, catchStartPC, catchStartPC, false, exceptionObjectLocal, scopeLocal);
					}
					Node finallyTarget = tryNode.GetFinally();
					if (finallyTarget != null)
					{
						int finallyStartPC = labelTable[GetTargetLabel(finallyTarget)];
						AddExceptionHandler(tryStart, finallyStartPC, finallyStartPC, true, exceptionObjectLocal, scopeLocal);
					}
					AddIndexOp(Icode_LOCAL_CLEAR, scopeLocal);
					ReleaseLocal(scopeLocal);
					break;
				}

				case Token.CATCH_SCOPE:
				{
					int localIndex = GetLocalBlockRef(node);
					int scopeIndex = node.GetExistingIntProp(Node.CATCH_SCOPE_PROP);
					string name = child.GetString();
					child = child.GetNext();
					VisitExpression(child, 0);
					// load expression object
					AddStringPrefix(name);
					AddIndexPrefix(localIndex);
					AddToken(Token.CATCH_SCOPE);
					AddUint8(scopeIndex != 0 ? 1 : 0);
					StackChange(-1);
					break;
				}

				case Token.THROW:
				{
					UpdateLineNumber(node);
					VisitExpression(child, 0);
					AddToken(Token.THROW);
					AddUint16(lineNumber & 0xffff);
					StackChange(-1);
					break;
				}

				case Token.RETHROW:
				{
					UpdateLineNumber(node);
					AddIndexOp(Token.RETHROW, GetLocalBlockRef(node));
					break;
				}

				case Token.RETURN:
				{
					UpdateLineNumber(node);
					if (node.GetIntProp(Node.GENERATOR_END_PROP, 0) != 0)
					{
						// We're in a generator, so change RETURN to GENERATOR_END
						AddIcode(Icode_GENERATOR_END);
						AddUint16(lineNumber & 0xffff);
					}
					else
					{
						if (child != null)
						{
							VisitExpression(child, ECF_TAIL);
							AddToken(Token.RETURN);
							StackChange(-1);
						}
						else
						{
							AddIcode(Icode_RETUNDEF);
						}
					}
					break;
				}

				case Token.RETURN_RESULT:
				{
					UpdateLineNumber(node);
					AddToken(Token.RETURN_RESULT);
					break;
				}

				case Token.ENUM_INIT_KEYS:
				case Token.ENUM_INIT_VALUES:
				case Token.ENUM_INIT_ARRAY:
				{
					VisitExpression(child, 0);
					AddIndexOp(type, GetLocalBlockRef(node));
					StackChange(-1);
					break;
				}

				case Icode_GENERATOR:
				{
					break;
				}

				default:
				{
					throw BadTree(node);
				}
			}
			if (stackDepth != initialStackDepth)
			{
				throw Kit.CodeBug();
			}
		}

		private void VisitExpression(Node node, int contextFlags)
		{
			int type = node.GetType();
			Node child = node.GetFirstChild();
			int savedStackDepth = stackDepth;
			switch (type)
			{
				case Token.FUNCTION:
				{
					int fnIndex = node.GetExistingIntProp(Node.FUNCTION_PROP);
					FunctionNode fn = scriptOrFn.GetFunctionNode(fnIndex);
					// See comments in visitStatement for Token.FUNCTION case
					if (fn.GetFunctionType() != FunctionNode.FUNCTION_EXPRESSION)
					{
						throw Kit.CodeBug();
					}
					AddIndexOp(Icode_CLOSURE_EXPR, fnIndex);
					StackChange(1);
					break;
				}

				case Token.LOCAL_LOAD:
				{
					int localIndex = GetLocalBlockRef(node);
					AddIndexOp(Token.LOCAL_LOAD, localIndex);
					StackChange(1);
					break;
				}

				case Token.COMMA:
				{
					Node lastChild = node.GetLastChild();
					while (child != lastChild)
					{
						VisitExpression(child, 0);
						AddIcode(Icode_POP);
						StackChange(-1);
						child = child.GetNext();
					}
					// Preserve tail context flag if any
					VisitExpression(child, contextFlags & ECF_TAIL);
					break;
				}

				case Token.USE_STACK:
				{
					// Indicates that stack was modified externally,
					// like placed catch object
					StackChange(1);
					break;
				}

				case Token.REF_CALL:
				case Token.CALL:
				case Token.NEW:
				{
					if (type == Token.NEW)
					{
						VisitExpression(child, 0);
					}
					else
					{
						GenerateCallFunAndThis(child);
					}
					int argCount = 0;
					while ((child = child.GetNext()) != null)
					{
						VisitExpression(child, 0);
						++argCount;
					}
					int callType = node.GetIntProp(Node.SPECIALCALL_PROP, Node.NON_SPECIALCALL);
					if (type != Token.REF_CALL && callType != Node.NON_SPECIALCALL)
					{
						// embed line number and source filename
						AddIndexOp(Icode_CALLSPECIAL, argCount);
						AddUint8(callType);
						AddUint8(type == Token.NEW ? 1 : 0);
						AddUint16(lineNumber & 0xffff);
					}
					else
					{
						// Only use the tail call optimization if we're not in a try
						// or we're not generating debug info (since the
						// optimization will confuse the debugger)
						if (type == Token.CALL && (contextFlags & ECF_TAIL) != 0 && !compilerEnv.GenerateDebugInfo && !itsInTryFlag)
						{
							type = Icode_TAIL_CALL;
						}
						AddIndexOp(type, argCount);
					}
					// adjust stack
					if (type == Token.NEW)
					{
						// new: f, args -> result
						StackChange(-argCount);
					}
					else
					{
						// call: f, thisObj, args -> result
						// ref_call: f, thisObj, args -> ref
						StackChange(-1 - argCount);
					}
					if (argCount > itsData.itsMaxCalleeArgs)
					{
						itsData.itsMaxCalleeArgs = argCount;
					}
					break;
				}

				case Token.AND:
				case Token.OR:
				{
					VisitExpression(child, 0);
					AddIcode(Icode_DUP);
					StackChange(1);
					int afterSecondJumpStart = iCodeTop;
					int jump = (type == Token.AND) ? Token.IFNE : Token.IFEQ;
					AddGotoOp(jump);
					StackChange(-1);
					AddIcode(Icode_POP);
					StackChange(-1);
					child = child.GetNext();
					// Preserve tail context flag if any
					VisitExpression(child, contextFlags & ECF_TAIL);
					ResolveForwardGoto(afterSecondJumpStart);
					break;
				}

				case Token.HOOK:
				{
					Node ifThen = child.GetNext();
					Node ifElse = ifThen.GetNext();
					VisitExpression(child, 0);
					int elseJumpStart = iCodeTop;
					AddGotoOp(Token.IFNE);
					StackChange(-1);
					// Preserve tail context flag if any
					VisitExpression(ifThen, contextFlags & ECF_TAIL);
					int afterElseJumpStart = iCodeTop;
					AddGotoOp(Token.GOTO);
					ResolveForwardGoto(elseJumpStart);
					stackDepth = savedStackDepth;
					// Preserve tail context flag if any
					VisitExpression(ifElse, contextFlags & ECF_TAIL);
					ResolveForwardGoto(afterElseJumpStart);
					break;
				}

				case Token.GETPROP:
				case Token.GETPROPNOWARN:
				{
					VisitExpression(child, 0);
					child = child.GetNext();
					AddStringOp(type, child.GetString());
					break;
				}

				case Token.DELPROP:
				{
					bool isName = child.GetType() == Token.BINDNAME;
					VisitExpression(child, 0);
					child = child.GetNext();
					VisitExpression(child, 0);
					if (isName)
					{
						// special handling for delete name
						AddIcode(Icode_DELNAME);
					}
					else
					{
						AddToken(Token.DELPROP);
					}
					StackChange(-1);
					break;
				}

				case Token.GETELEM:
				case Token.BITAND:
				case Token.BITOR:
				case Token.BITXOR:
				case Token.LSH:
				case Token.RSH:
				case Token.URSH:
				case Token.ADD:
				case Token.SUB:
				case Token.MOD:
				case Token.DIV:
				case Token.MUL:
				case Token.EQ:
				case Token.NE:
				case Token.SHEQ:
				case Token.SHNE:
				case Token.IN:
				case Token.INSTANCEOF:
				case Token.LE:
				case Token.LT:
				case Token.GE:
				case Token.GT:
				{
					VisitExpression(child, 0);
					child = child.GetNext();
					VisitExpression(child, 0);
					AddToken(type);
					StackChange(-1);
					break;
				}

				case Token.POS:
				case Token.NEG:
				case Token.NOT:
				case Token.BITNOT:
				case Token.TYPEOF:
				case Token.VOID:
				{
					VisitExpression(child, 0);
					if (type == Token.VOID)
					{
						AddIcode(Icode_POP);
						AddIcode(Icode_UNDEF);
					}
					else
					{
						AddToken(type);
					}
					break;
				}

				case Token.GET_REF:
				case Token.DEL_REF:
				{
					VisitExpression(child, 0);
					AddToken(type);
					break;
				}

				case Token.SETPROP:
				case Token.SETPROP_OP:
				{
					VisitExpression(child, 0);
					child = child.GetNext();
					string property = child.GetString();
					child = child.GetNext();
					if (type == Token.SETPROP_OP)
					{
						AddIcode(Icode_DUP);
						StackChange(1);
						AddStringOp(Token.GETPROP, property);
						// Compensate for the following USE_STACK
						StackChange(-1);
					}
					VisitExpression(child, 0);
					AddStringOp(Token.SETPROP, property);
					StackChange(-1);
					break;
				}

				case Token.SETELEM:
				case Token.SETELEM_OP:
				{
					VisitExpression(child, 0);
					child = child.GetNext();
					VisitExpression(child, 0);
					child = child.GetNext();
					if (type == Token.SETELEM_OP)
					{
						AddIcode(Icode_DUP2);
						StackChange(2);
						AddToken(Token.GETELEM);
						StackChange(-1);
						// Compensate for the following USE_STACK
						StackChange(-1);
					}
					VisitExpression(child, 0);
					AddToken(Token.SETELEM);
					StackChange(-2);
					break;
				}

				case Token.SET_REF:
				case Token.SET_REF_OP:
				{
					VisitExpression(child, 0);
					child = child.GetNext();
					if (type == Token.SET_REF_OP)
					{
						AddIcode(Icode_DUP);
						StackChange(1);
						AddToken(Token.GET_REF);
						// Compensate for the following USE_STACK
						StackChange(-1);
					}
					VisitExpression(child, 0);
					AddToken(Token.SET_REF);
					StackChange(-1);
					break;
				}

				case Token.STRICT_SETNAME:
				case Token.SETNAME:
				{
					string name = child.GetString();
					VisitExpression(child, 0);
					child = child.GetNext();
					VisitExpression(child, 0);
					AddStringOp(type, name);
					StackChange(-1);
					break;
				}

				case Token.SETCONST:
				{
					string name = child.GetString();
					VisitExpression(child, 0);
					child = child.GetNext();
					VisitExpression(child, 0);
					AddStringOp(Icode_SETCONST, name);
					StackChange(-1);
					break;
				}

				case Token.TYPEOFNAME:
				{
					int index = -1;
					// use typeofname if an activation frame exists
					// since the vars all exist there instead of in jregs
					if (itsInFunctionFlag && !itsData.itsNeedsActivation)
					{
						index = scriptOrFn.GetIndexForNameNode(node);
					}
					if (index == -1)
					{
						AddStringOp(Icode_TYPEOFNAME, node.GetString());
						StackChange(1);
					}
					else
					{
						AddVarOp(Token.GETVAR, index);
						StackChange(1);
						AddToken(Token.TYPEOF);
					}
					break;
				}

				case Token.BINDNAME:
				case Token.NAME:
				case Token.STRING:
				{
					AddStringOp(type, node.GetString());
					StackChange(1);
					break;
				}

				case Token.INC:
				case Token.DEC:
				{
					VisitIncDec(node, child);
					break;
				}

				case Token.NUMBER:
				{
					double num = node.GetDouble();
					int inum = (int)num;
					if (inum == num)
					{
						if (inum == 0)
						{
							AddIcode(Icode_ZERO);
							// Check for negative zero
							if (1.0 / num < 0.0)
							{
								AddToken(Token.NEG);
							}
						}
						else
						{
							if (inum == 1)
							{
								AddIcode(Icode_ONE);
							}
							else
							{
								if ((short)inum == inum)
								{
									AddIcode(Icode_SHORTNUMBER);
									// write short as uin16 bit pattern
									AddUint16(inum & 0xffff);
								}
								else
								{
									AddIcode(Icode_INTNUMBER);
									AddInt(inum);
								}
							}
						}
					}
					else
					{
						int index = GetDoubleIndex(num);
						AddIndexOp(Token.NUMBER, index);
					}
					StackChange(1);
					break;
				}

				case Token.GETVAR:
				{
					if (itsData.itsNeedsActivation)
					{
						Kit.CodeBug();
					}
					int index = scriptOrFn.GetIndexForNameNode(node);
					AddVarOp(Token.GETVAR, index);
					StackChange(1);
					break;
				}

				case Token.SETVAR:
				{
					if (itsData.itsNeedsActivation)
					{
						Kit.CodeBug();
					}
					int index = scriptOrFn.GetIndexForNameNode(child);
					child = child.GetNext();
					VisitExpression(child, 0);
					AddVarOp(Token.SETVAR, index);
					break;
				}

				case Token.SETCONSTVAR:
				{
					if (itsData.itsNeedsActivation)
					{
						Kit.CodeBug();
					}
					int index = scriptOrFn.GetIndexForNameNode(child);
					child = child.GetNext();
					VisitExpression(child, 0);
					AddVarOp(Token.SETCONSTVAR, index);
					break;
				}

				case Token.NULL:
				case Token.THIS:
				case Token.THISFN:
				case Token.FALSE:
				case Token.TRUE:
				{
					AddToken(type);
					StackChange(1);
					break;
				}

				case Token.ENUM_NEXT:
				case Token.ENUM_ID:
				{
					AddIndexOp(type, GetLocalBlockRef(node));
					StackChange(1);
					break;
				}

				case Token.REGEXP:
				{
					int index = node.GetExistingIntProp(Node.REGEXP_PROP);
					AddIndexOp(Token.REGEXP, index);
					StackChange(1);
					break;
				}

				case Token.ARRAYLIT:
				case Token.OBJECTLIT:
				{
					VisitLiteral(node, child);
					break;
				}

				case Token.ARRAYCOMP:
				{
					VisitArrayComprehension(node, child, child.GetNext());
					break;
				}

				case Token.REF_SPECIAL:
				{
					VisitExpression(child, 0);
					AddStringOp(type, (string)node.GetProp(Node.NAME_PROP));
					break;
				}

				case Token.REF_MEMBER:
				case Token.REF_NS_MEMBER:
				case Token.REF_NAME:
				case Token.REF_NS_NAME:
				{
					int memberTypeFlags = node.GetIntProp(Node.MEMBER_TYPE_PROP, 0);
					// generate possible target, possible namespace and member
					int childCount = 0;
					do
					{
						VisitExpression(child, 0);
						++childCount;
						child = child.GetNext();
					}
					while (child != null);
					AddIndexOp(type, memberTypeFlags);
					StackChange(1 - childCount);
					break;
				}

				case Token.DOTQUERY:
				{
					int queryPC;
					UpdateLineNumber(node);
					VisitExpression(child, 0);
					AddIcode(Icode_ENTERDQ);
					StackChange(-1);
					queryPC = iCodeTop;
					VisitExpression(child.GetNext(), 0);
					AddBackwardGoto(Icode_LEAVEDQ, queryPC);
					break;
				}

				case Token.DEFAULTNAMESPACE:
				case Token.ESCXMLATTR:
				case Token.ESCXMLTEXT:
				{
					VisitExpression(child, 0);
					AddToken(type);
					break;
				}

				case Token.YIELD:
				{
					if (child != null)
					{
						VisitExpression(child, 0);
					}
					else
					{
						AddIcode(Icode_UNDEF);
						StackChange(1);
					}
					AddToken(Token.YIELD);
					AddUint16(node.GetLineno() & unchecked((int)(0xFFFF)));
					break;
				}

				case Token.WITHEXPR:
				{
					Node enterWith = node.GetFirstChild();
					Node with = enterWith.GetNext();
					VisitExpression(enterWith.GetFirstChild(), 0);
					AddToken(Token.ENTERWITH);
					StackChange(-1);
					VisitExpression(with.GetFirstChild(), 0);
					AddToken(Token.LEAVEWITH);
					break;
				}

				default:
				{
					throw BadTree(node);
				}
			}
			if (savedStackDepth + 1 != stackDepth)
			{
				Kit.CodeBug();
			}
		}

		private void GenerateCallFunAndThis(Node left)
		{
			// Generate code to place on stack function and thisObj
			int type = left.GetType();
			switch (type)
			{
				case Token.NAME:
				{
					string name = left.GetString();
					// stack: ... -> ... function thisObj
					AddStringOp(Icode_NAME_AND_THIS, name);
					StackChange(2);
					break;
				}

				case Token.GETPROP:
				case Token.GETELEM:
				{
					Node target = left.GetFirstChild();
					VisitExpression(target, 0);
					Node id = target.GetNext();
					if (type == Token.GETPROP)
					{
						string property = id.GetString();
						// stack: ... target -> ... function thisObj
						AddStringOp(Icode_PROP_AND_THIS, property);
						StackChange(1);
					}
					else
					{
						VisitExpression(id, 0);
						// stack: ... target id -> ... function thisObj
						AddIcode(Icode_ELEM_AND_THIS);
					}
					break;
				}

				default:
				{
					// Including Token.GETVAR
					VisitExpression(left, 0);
					// stack: ... value -> ... function thisObj
					AddIcode(Icode_VALUE_AND_THIS);
					StackChange(1);
					break;
				}
			}
		}

		private void VisitIncDec(Node node, Node child)
		{
			int incrDecrMask = node.GetExistingIntProp(Node.INCRDECR_PROP);
			int childType = child.GetType();
			switch (childType)
			{
				case Token.GETVAR:
				{
					if (itsData.itsNeedsActivation)
					{
						Kit.CodeBug();
					}
					int i = scriptOrFn.GetIndexForNameNode(child);
					AddVarOp(Icode_VAR_INC_DEC, i);
					AddUint8(incrDecrMask);
					StackChange(1);
					break;
				}

				case Token.NAME:
				{
					string name = child.GetString();
					AddStringOp(Icode_NAME_INC_DEC, name);
					AddUint8(incrDecrMask);
					StackChange(1);
					break;
				}

				case Token.GETPROP:
				{
					Node @object = child.GetFirstChild();
					VisitExpression(@object, 0);
					string property = @object.GetNext().GetString();
					AddStringOp(Icode_PROP_INC_DEC, property);
					AddUint8(incrDecrMask);
					break;
				}

				case Token.GETELEM:
				{
					Node @object = child.GetFirstChild();
					VisitExpression(@object, 0);
					Node index = @object.GetNext();
					VisitExpression(index, 0);
					AddIcode(Icode_ELEM_INC_DEC);
					AddUint8(incrDecrMask);
					StackChange(-1);
					break;
				}

				case Token.GET_REF:
				{
					Node @ref = child.GetFirstChild();
					VisitExpression(@ref, 0);
					AddIcode(Icode_REF_INC_DEC);
					AddUint8(incrDecrMask);
					break;
				}

				default:
				{
					throw BadTree(node);
				}
			}
		}

		private void VisitLiteral(Node node, Node child)
		{
			int type = node.GetType();
			int count;
			object[] propertyIds = null;
			if (type == Token.ARRAYLIT)
			{
				count = 0;
				for (Node n = child; n != null; n = n.GetNext())
				{
					++count;
				}
			}
			else
			{
				if (type == Token.OBJECTLIT)
				{
					propertyIds = (object[])node.GetProp(Node.OBJECT_IDS_PROP);
					count = propertyIds.Length;
				}
				else
				{
					throw BadTree(node);
				}
			}
			AddIndexOp(Icode_LITERAL_NEW, count);
			StackChange(2);
			while (child != null)
			{
				int childType = child.GetType();
				if (childType == Token.GET)
				{
					VisitExpression(child.GetFirstChild(), 0);
					AddIcode(Icode_LITERAL_GETTER);
				}
				else
				{
					if (childType == Token.SET)
					{
						VisitExpression(child.GetFirstChild(), 0);
						AddIcode(Icode_LITERAL_SETTER);
					}
					else
					{
						VisitExpression(child, 0);
						AddIcode(Icode_LITERAL_SET);
					}
				}
				StackChange(-1);
				child = child.GetNext();
			}
			if (type == Token.ARRAYLIT)
			{
				int[] skipIndexes = (int[])node.GetProp(Node.SKIP_INDEXES_PROP);
				if (skipIndexes == null)
				{
					AddToken(Token.ARRAYLIT);
				}
				else
				{
					int index = literalIds.Count;
					literalIds.Add(skipIndexes);
					AddIndexOp(Icode_SPARE_ARRAYLIT, index);
				}
			}
			else
			{
				int index = literalIds.Count;
				literalIds.Add(propertyIds);
				AddIndexOp(Token.OBJECTLIT, index);
			}
			StackChange(-1);
		}

		private void VisitArrayComprehension(Node node, Node initStmt, Node expr)
		{
			// A bit of a hack: array comprehensions are implemented using
			// statement nodes for the iteration, yet they appear in an
			// expression context. So we pass the current stack depth to
			// visitStatement so it can check that the depth is not altered
			// by statements.
			VisitStatement(initStmt, stackDepth);
			VisitExpression(expr, 0);
		}

		private int GetLocalBlockRef(Node node)
		{
			Node localBlock = (Node)node.GetProp(Node.LOCAL_BLOCK_PROP);
			return localBlock.GetExistingIntProp(Node.LOCAL_PROP);
		}

		private int GetTargetLabel(Node target)
		{
			int label = target.LabelId();
			if (label != -1)
			{
				return label;
			}
			label = labelTableTop;
			if (labelTable == null || label == labelTable.Length)
			{
				if (labelTable == null)
				{
					labelTable = new int[MIN_LABEL_TABLE_SIZE];
				}
				else
				{
					int[] tmp = new int[labelTable.Length * 2];
					Array.Copy(labelTable, 0, tmp, 0, label);
					labelTable = tmp;
				}
			}
			labelTableTop = label + 1;
			labelTable[label] = -1;
			target.LabelId(label);
			return label;
		}

		private void MarkTargetLabel(Node target)
		{
			int label = GetTargetLabel(target);
			if (labelTable[label] != -1)
			{
				// Can mark label only once
				Kit.CodeBug();
			}
			labelTable[label] = iCodeTop;
		}

		private void AddGoto(Node target, int gotoOp)
		{
			int label = GetTargetLabel(target);
			if (!(label < labelTableTop))
			{
				Kit.CodeBug();
			}
			int targetPC = labelTable[label];
			if (targetPC != -1)
			{
				AddBackwardGoto(gotoOp, targetPC);
			}
			else
			{
				int gotoPC = iCodeTop;
				AddGotoOp(gotoOp);
				int top = fixupTableTop;
				if (fixupTable == null || top == fixupTable.Length)
				{
					if (fixupTable == null)
					{
						fixupTable = new long[MIN_FIXUP_TABLE_SIZE];
					}
					else
					{
						long[] tmp = new long[fixupTable.Length * 2];
						Array.Copy(fixupTable, 0, tmp, 0, top);
						fixupTable = tmp;
					}
				}
				fixupTableTop = top + 1;
				fixupTable[top] = ((long)label << 32) | gotoPC;
			}
		}

		private void FixLabelGotos()
		{
			for (int i = 0; i < fixupTableTop; i++)
			{
				long fixup = fixupTable[i];
				int label = (int)(fixup >> 32);
				int jumpSource = (int)fixup;
				int pc = labelTable[label];
				if (pc == -1)
				{
					// Unlocated label
					throw Kit.CodeBug();
				}
				ResolveGoto(jumpSource, pc);
			}
			fixupTableTop = 0;
		}

		private void AddBackwardGoto(int gotoOp, int jumpPC)
		{
			int fromPC = iCodeTop;
			// Ensure that this is a jump backward
			if (fromPC <= jumpPC)
			{
				throw Kit.CodeBug();
			}
			AddGotoOp(gotoOp);
			ResolveGoto(fromPC, jumpPC);
		}

		private void ResolveForwardGoto(int fromPC)
		{
			// Ensure that forward jump skips at least self bytecode
			if (iCodeTop < fromPC + 3)
			{
				throw Kit.CodeBug();
			}
			ResolveGoto(fromPC, iCodeTop);
		}

		private void ResolveGoto(int fromPC, int jumpPC)
		{
			int offset = jumpPC - fromPC;
			// Ensure that jumps do not overlap
			if (0 <= offset && offset <= 2)
			{
				throw Kit.CodeBug();
			}
			int offsetSite = fromPC + 1;
			if (offset != (short)offset)
			{
				if (itsData.longJumps == null)
				{
					itsData.longJumps = new UintMap();
				}
				itsData.longJumps.Put(offsetSite, jumpPC);
				offset = 0;
			}
			sbyte[] array = itsData.itsICode;
			array[offsetSite] = unchecked((sbyte)(offset >> 8));
			array[offsetSite + 1] = unchecked((sbyte)offset);
		}

		private void AddToken(int token)
		{
			if (!ValidTokenCode(token))
			{
				throw Kit.CodeBug();
			}
			AddUint8(token);
		}

		private void AddIcode(int icode)
		{
			if (!ValidIcode(icode))
			{
				throw Kit.CodeBug();
			}
			// Write negative icode as uint8 bits
			AddUint8(icode & 0xff);
		}

		private void AddUint8(int value)
		{
			if ((value & ~0xFF) != 0)
			{
				throw Kit.CodeBug();
			}
			sbyte[] array = itsData.itsICode;
			int top = iCodeTop;
			if (top == array.Length)
			{
				array = IncreaseICodeCapacity(1);
			}
			array[top] = unchecked((sbyte)value);
			iCodeTop = top + 1;
		}

		private void AddUint16(int value)
		{
			if ((value & ~0xffff) != 0)
			{
				throw Kit.CodeBug();
			}
			sbyte[] array = itsData.itsICode;
			int top = iCodeTop;
			if (top + 2 > array.Length)
			{
				array = IncreaseICodeCapacity(2);
			}
			array[top] = unchecked((sbyte)((int)(((uint)value) >> 8)));
			array[top + 1] = unchecked((sbyte)value);
			iCodeTop = top + 2;
		}

		private void AddInt(int i)
		{
			sbyte[] array = itsData.itsICode;
			int top = iCodeTop;
			if (top + 4 > array.Length)
			{
				array = IncreaseICodeCapacity(4);
			}
			array[top] = unchecked((sbyte)((int)(((uint)i) >> 24)));
			array[top + 1] = unchecked((sbyte)((int)(((uint)i) >> 16)));
			array[top + 2] = unchecked((sbyte)((int)(((uint)i) >> 8)));
			array[top + 3] = unchecked((sbyte)i);
			iCodeTop = top + 4;
		}

		private int GetDoubleIndex(double num)
		{
			int index = doubleTableTop;
			if (index == 0)
			{
				itsData.itsDoubleTable = new double[64];
			}
			else
			{
				if (itsData.itsDoubleTable.Length == index)
				{
					double[] na = new double[index * 2];
					Array.Copy(itsData.itsDoubleTable, 0, na, 0, index);
					itsData.itsDoubleTable = na;
				}
			}
			itsData.itsDoubleTable[index] = num;
			doubleTableTop = index + 1;
			return index;
		}

		private void AddGotoOp(int gotoOp)
		{
			sbyte[] array = itsData.itsICode;
			int top = iCodeTop;
			if (top + 3 > array.Length)
			{
				array = IncreaseICodeCapacity(3);
			}
			array[top] = unchecked((sbyte)gotoOp);
			// Offset would written later
			iCodeTop = top + 1 + 2;
		}

		private void AddVarOp(int op, int varIndex)
		{
			switch (op)
			{
				case Token.SETCONSTVAR:
				{
					if (varIndex < 128)
					{
						AddIcode(Icode_SETCONSTVAR1);
						AddUint8(varIndex);
						return;
					}
					AddIndexOp(Icode_SETCONSTVAR, varIndex);
					return;
				}

				case Token.GETVAR:
				case Token.SETVAR:
				{
					if (varIndex < 128)
					{
						AddIcode(op == Token.GETVAR ? Icode_GETVAR1 : Icode_SETVAR1);
						AddUint8(varIndex);
						return;
					}
					goto case Icode_VAR_INC_DEC;
				}

				case Icode_VAR_INC_DEC:
				{
					// fallthrough
					AddIndexOp(op, varIndex);
					return;
				}
			}
			throw Kit.CodeBug();
		}

		private void AddStringOp(int op, string str)
		{
			AddStringPrefix(str);
			if (ValidIcode(op))
			{
				AddIcode(op);
			}
			else
			{
				AddToken(op);
			}
		}

		private void AddIndexOp(int op, int index)
		{
			AddIndexPrefix(index);
			if (ValidIcode(op))
			{
				AddIcode(op);
			}
			else
			{
				AddToken(op);
			}
		}

		private void AddStringPrefix(string str)
		{
			int index = strings.GetValueOrDefault(str, -1);
			if (index == -1)
			{
				index = strings.Count;
				strings[str] = index;
			}
			if (index < 4)
			{
				AddIcode(Icode_REG_STR_C0 - index);
			}
			else
			{
				if (index <= 0xff)
				{
					AddIcode(Icode_REG_STR1);
					AddUint8(index);
				}
				else
				{
					if (index <= 0xffff)
					{
						AddIcode(Icode_REG_STR2);
						AddUint16(index);
					}
					else
					{
						AddIcode(Icode_REG_STR4);
						AddInt(index);
					}
				}
			}
		}

		private void AddIndexPrefix(int index)
		{
			if (index < 0)
			{
				Kit.CodeBug();
			}
			if (index < 6)
			{
				AddIcode(Icode_REG_IND_C0 - index);
			}
			else
			{
				if (index <= 0xff)
				{
					AddIcode(Icode_REG_IND1);
					AddUint8(index);
				}
				else
				{
					if (index <= 0xffff)
					{
						AddIcode(Icode_REG_IND2);
						AddUint16(index);
					}
					else
					{
						AddIcode(Icode_REG_IND4);
						AddInt(index);
					}
				}
			}
		}

		private void AddExceptionHandler(int icodeStart, int icodeEnd, int handlerStart, bool isFinally, int exceptionObjectLocal, int scopeLocal)
		{
			int top = exceptionTableTop;
			int[] table = itsData.itsExceptionTable;
			if (table == null)
			{
				if (top != 0)
				{
					Kit.CodeBug();
				}
				table = new int[Interpreter.EXCEPTION_SLOT_SIZE * 2];
				itsData.itsExceptionTable = table;
			}
			else
			{
				if (table.Length == top)
				{
					table = new int[table.Length * 2];
					Array.Copy(itsData.itsExceptionTable, 0, table, 0, top);
					itsData.itsExceptionTable = table;
				}
			}
			table[top + Interpreter.EXCEPTION_TRY_START_SLOT] = icodeStart;
			table[top + Interpreter.EXCEPTION_TRY_END_SLOT] = icodeEnd;
			table[top + Interpreter.EXCEPTION_HANDLER_SLOT] = handlerStart;
			table[top + Interpreter.EXCEPTION_TYPE_SLOT] = isFinally ? 1 : 0;
			table[top + Interpreter.EXCEPTION_LOCAL_SLOT] = exceptionObjectLocal;
			table[top + Interpreter.EXCEPTION_SCOPE_SLOT] = scopeLocal;
			exceptionTableTop = top + Interpreter.EXCEPTION_SLOT_SIZE;
		}

		private sbyte[] IncreaseICodeCapacity(int extraSize)
		{
			int capacity = itsData.itsICode.Length;
			int top = iCodeTop;
			if (top + extraSize <= capacity)
			{
				throw Kit.CodeBug();
			}
			capacity *= 2;
			if (top + extraSize > capacity)
			{
				capacity = top + extraSize;
			}
			sbyte[] array = new sbyte[capacity];
			Array.Copy(itsData.itsICode, 0, array, 0, top);
			itsData.itsICode = array;
			return array;
		}

		private void StackChange(int change)
		{
			if (change <= 0)
			{
				stackDepth += change;
			}
			else
			{
				int newDepth = stackDepth + change;
				if (newDepth > itsData.itsMaxStack)
				{
					itsData.itsMaxStack = newDepth;
				}
				stackDepth = newDepth;
			}
		}

		private int AllocLocal()
		{
			int localSlot = localTop;
			++localTop;
			if (localTop > itsData.itsMaxLocals)
			{
				itsData.itsMaxLocals = localTop;
			}
			return localSlot;
		}

		private void ReleaseLocal(int localSlot)
		{
			--localTop;
			if (localSlot != localTop)
			{
				Kit.CodeBug();
			}
		}
	}
}
