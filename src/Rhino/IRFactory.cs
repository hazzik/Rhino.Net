/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino
{
	/// <summary>This class rewrites the parse tree into an IR suitable for codegen.</summary>
	/// <remarks>This class rewrites the parse tree into an IR suitable for codegen.</remarks>
	/// <seealso cref="Node">Node</seealso>
	/// <author>Mike McCabe</author>
	/// <author>Norris Boyd</author>
	public sealed class IRFactory : Parser
	{
		private const int LOOP_DO_WHILE = 0;

		private const int LOOP_WHILE = 1;

		private const int LOOP_FOR = 2;

		private const int ALWAYS_TRUE_BOOLEAN = 1;

		private const int ALWAYS_FALSE_BOOLEAN = -1;

		private Decompiler decompiler = new Decompiler();

		public IRFactory() : base()
		{
		}

		public IRFactory(CompilerEnvirons env) : this(env, env.GetErrorReporter())
		{
		}

		public IRFactory(CompilerEnvirons env, ErrorReporter errorReporter) : base(env, errorReporter)
		{
		}

		/// <summary>Transforms the tree into a lower-level IR suitable for codegen.</summary>
		/// <remarks>
		/// Transforms the tree into a lower-level IR suitable for codegen.
		/// Optionally generates the encoded source.
		/// </remarks>
		public ScriptNode TransformTree(AstRoot root)
		{
			currentScriptOrFn = root;
			this.inUseStrictDirective = root.IsInStrictMode();
			int sourceStartOffset = decompiler.GetCurrentOffset();
			ScriptNode script = (ScriptNode)Transform(root);
			int sourceEndOffset = decompiler.GetCurrentOffset();
			script.SetEncodedSourceBounds(sourceStartOffset, sourceEndOffset);
			if (compilerEnv.IsGeneratingSource())
			{
				script.SetEncodedSource(decompiler.GetEncodedSource());
			}
			decompiler = null;
			return script;
		}

		// Might want to convert this to polymorphism - move transform*
		// functions into the AstNode subclasses.  OTOH that would make
		// IR transformation part of the public AST API - desirable?
		// Another possibility:  create AstTransformer interface and adapter.
		public Node Transform(AstNode node)
		{
			switch (node.GetType())
			{
				case Token.ARRAYCOMP:
				{
					return TransformArrayComp((ArrayComprehension)node);
				}

				case Token.ARRAYLIT:
				{
					return TransformArrayLiteral((ArrayLiteral)node);
				}

				case Token.BLOCK:
				{
					return TransformBlock(node);
				}

				case Token.BREAK:
				{
					return TransformBreak((BreakStatement)node);
				}

				case Token.CALL:
				{
					return TransformFunctionCall((FunctionCall)node);
				}

				case Token.CONTINUE:
				{
					return TransformContinue((ContinueStatement)node);
				}

				case Token.DO:
				{
					return TransformDoLoop((DoLoop)node);
				}

				case Token.EMPTY:
				{
					return node;
				}

				case Token.FOR:
				{
					if (node is ForInLoop)
					{
						return TransformForInLoop((ForInLoop)node);
					}
					else
					{
						return TransformForLoop((ForLoop)node);
					}
					goto case Token.FUNCTION;
				}

				case Token.FUNCTION:
				{
					return TransformFunction((FunctionNode)node);
				}

				case Token.GENEXPR:
				{
					return TransformGenExpr((GeneratorExpression)node);
				}

				case Token.GETELEM:
				{
					return TransformElementGet((ElementGet)node);
				}

				case Token.GETPROP:
				{
					return TransformPropertyGet((PropertyGet)node);
				}

				case Token.HOOK:
				{
					return TransformCondExpr((ConditionalExpression)node);
				}

				case Token.IF:
				{
					return TransformIf((IfStatement)node);
				}

				case Token.TRUE:
				case Token.FALSE:
				case Token.THIS:
				case Token.NULL:
				case Token.DEBUGGER:
				{
					return TransformLiteral(node);
				}

				case Token.NAME:
				{
					return TransformName((Name)node);
				}

				case Token.NUMBER:
				{
					return TransformNumber((NumberLiteral)node);
				}

				case Token.NEW:
				{
					return TransformNewExpr((NewExpression)node);
				}

				case Token.OBJECTLIT:
				{
					return TransformObjectLiteral((ObjectLiteral)node);
				}

				case Token.REGEXP:
				{
					return TransformRegExp((RegExpLiteral)node);
				}

				case Token.RETURN:
				{
					return TransformReturn((ReturnStatement)node);
				}

				case Token.SCRIPT:
				{
					return TransformScript((ScriptNode)node);
				}

				case Token.STRING:
				{
					return TransformString((StringLiteral)node);
				}

				case Token.SWITCH:
				{
					return TransformSwitch((SwitchStatement)node);
				}

				case Token.THROW:
				{
					return TransformThrow((ThrowStatement)node);
				}

				case Token.TRY:
				{
					return TransformTry((TryStatement)node);
				}

				case Token.WHILE:
				{
					return TransformWhileLoop((WhileLoop)node);
				}

				case Token.WITH:
				{
					return TransformWith((WithStatement)node);
				}

				case Token.YIELD:
				{
					return TransformYield((Yield)node);
				}

				default:
				{
					if (node is ExpressionStatement)
					{
						return TransformExprStmt((ExpressionStatement)node);
					}
					if (node is Assignment)
					{
						return TransformAssignment((Assignment)node);
					}
					if (node is UnaryExpression)
					{
						return TransformUnary((UnaryExpression)node);
					}
					if (node is XmlMemberGet)
					{
						return TransformXmlMemberGet((XmlMemberGet)node);
					}
					if (node is InfixExpression)
					{
						return TransformInfix((InfixExpression)node);
					}
					if (node is VariableDeclaration)
					{
						return TransformVariables((VariableDeclaration)node);
					}
					if (node is ParenthesizedExpression)
					{
						return TransformParenExpr((ParenthesizedExpression)node);
					}
					if (node is LabeledStatement)
					{
						return TransformLabeledStatement((LabeledStatement)node);
					}
					if (node is LetNode)
					{
						return TransformLetNode((LetNode)node);
					}
					if (node is XmlRef)
					{
						return TransformXmlRef((XmlRef)node);
					}
					if (node is XmlLiteral)
					{
						return TransformXmlLiteral((XmlLiteral)node);
					}
					throw new ArgumentException("Can't transform: " + node);
				}
			}
		}

		private Node TransformArrayComp(ArrayComprehension node)
		{
			// An array comprehension expression such as
			//
			//   [expr for (x in foo) for each ([y, z] in bar) if (cond)]
			//
			// is rewritten approximately as
			//
			// new Scope(ARRAYCOMP) {
			//   new Node(BLOCK) {
			//     let tmp1 = new Array;
			//     for (let x in foo) {
			//       for each (let tmp2 in bar) {
			//         if (cond) {
			//           tmp1.push([y, z] = tmp2, expr);
			//         }
			//       }
			//     }
			//   }
			//   createName(tmp1)
			// }
			int lineno = node.GetLineno();
			Scope scopeNode = CreateScopeNode(Token.ARRAYCOMP, lineno);
			string arrayName = currentScriptOrFn.GetNextTempName();
			PushScope(scopeNode);
			try
			{
				DefineSymbol(Token.LET, arrayName, false);
				Node block = new Node(Token.BLOCK, lineno);
				Node newArray = CreateCallOrNew(Token.NEW, CreateName("Array"));
				Node init = new Node(Token.EXPR_VOID, CreateAssignment(Token.ASSIGN, CreateName(arrayName), newArray), lineno);
				block.AddChildToBack(init);
				block.AddChildToBack(ArrayCompTransformHelper(node, arrayName));
				scopeNode.AddChildToBack(block);
				scopeNode.AddChildToBack(CreateName(arrayName));
				return scopeNode;
			}
			finally
			{
				PopScope();
			}
		}

		private Node ArrayCompTransformHelper(ArrayComprehension node, string arrayName)
		{
			decompiler.AddToken(Token.LB);
			int lineno = node.GetLineno();
			Node expr = Transform(node.GetResult());
			IList<ArrayComprehensionLoop> loops = node.GetLoops();
			int numLoops = loops.Count;
			// Walk through loops, collecting and defining their iterator symbols.
			Node[] iterators = new Node[numLoops];
			Node[] iteratedObjs = new Node[numLoops];
			for (int i = 0; i < numLoops; i++)
			{
				ArrayComprehensionLoop acl = loops[i];
				decompiler.AddName(" ");
				decompiler.AddToken(Token.FOR);
				if (acl.IsForEach())
				{
					decompiler.AddName("each ");
				}
				decompiler.AddToken(Token.LP);
				AstNode iter = acl.GetIterator();
				string name = null;
				if (iter.GetType() == Token.NAME)
				{
					name = iter.GetString();
					decompiler.AddName(name);
				}
				else
				{
					// destructuring assignment
					Decompile(iter);
					name = currentScriptOrFn.GetNextTempName();
					DefineSymbol(Token.LP, name, false);
					expr = CreateBinary(Token.COMMA, CreateAssignment(Token.ASSIGN, iter, CreateName(name)), expr);
				}
				Node init = CreateName(name);
				// Define as a let since we want the scope of the variable to
				// be restricted to the array comprehension
				DefineSymbol(Token.LET, name, false);
				iterators[i] = init;
				decompiler.AddToken(Token.IN);
				iteratedObjs[i] = Transform(acl.GetIteratedObject());
				decompiler.AddToken(Token.RP);
			}
			// generate code for tmpArray.push(body)
			Node call = CreateCallOrNew(Token.CALL, CreatePropertyGet(CreateName(arrayName), null, "push", 0));
			Node body = new Node(Token.EXPR_VOID, call, lineno);
			if (node.GetFilter() != null)
			{
				decompiler.AddName(" ");
				decompiler.AddToken(Token.IF);
				decompiler.AddToken(Token.LP);
				body = CreateIf(Transform(node.GetFilter()), body, null, lineno);
				decompiler.AddToken(Token.RP);
			}
			// Now walk loops in reverse to build up the body statement.
			int pushed = 0;
			try
			{
				for (int i_1 = numLoops - 1; i_1 >= 0; i_1--)
				{
					ArrayComprehensionLoop acl = loops[i_1];
					Scope loop = CreateLoopNode(null, acl.GetLineno());
					// no label
					PushScope(loop);
					pushed++;
					body = CreateForIn(Token.LET, loop, iterators[i_1], iteratedObjs[i_1], body, acl.IsForEach());
				}
			}
			finally
			{
				for (int i_1 = 0; i_1 < pushed; i_1++)
				{
					PopScope();
				}
			}
			decompiler.AddToken(Token.RB);
			// Now that we've accumulated any destructuring forms,
			// add expr to the call node; it's pushed on each iteration.
			call.AddChildToBack(expr);
			return body;
		}

		private Node TransformArrayLiteral(ArrayLiteral node)
		{
			if (node.IsDestructuring())
			{
				return node;
			}
			decompiler.AddToken(Token.LB);
			IList<AstNode> elems = node.GetElements();
			Node array = new Node(Token.ARRAYLIT);
			IList<int> skipIndexes = null;
			for (int i = 0; i < elems.Count; ++i)
			{
				AstNode elem = elems[i];
				if (elem.GetType() != Token.EMPTY)
				{
					array.AddChildToBack(Transform(elem));
				}
				else
				{
					if (skipIndexes == null)
					{
						skipIndexes = new AList<int>();
					}
					skipIndexes.AddItem(i);
				}
				if (i < elems.Count - 1)
				{
					decompiler.AddToken(Token.COMMA);
				}
			}
			decompiler.AddToken(Token.RB);
			array.PutIntProp(Node.DESTRUCTURING_ARRAY_LENGTH, node.GetDestructuringLength());
			if (skipIndexes != null)
			{
				int[] skips = new int[skipIndexes.Count];
				for (int i_1 = 0; i_1 < skipIndexes.Count; i_1++)
				{
					skips[i_1] = skipIndexes[i_1];
				}
				array.PutProp(Node.SKIP_INDEXES_PROP, skips);
			}
			return array;
		}

		private Node TransformAssignment(Assignment node)
		{
			AstNode left = RemoveParens(node.GetLeft());
			Node target = null;
			if (IsDestructuring(left))
			{
				Decompile(left);
				target = left;
			}
			else
			{
				target = Transform(left);
			}
			decompiler.AddToken(node.GetType());
			return CreateAssignment(node.GetType(), target, Transform(node.GetRight()));
		}

		private Node TransformBlock(AstNode node)
		{
			if (node is Scope)
			{
				PushScope((Scope)node);
			}
			try
			{
				IList<Node> kids = new AList<Node>();
				foreach (Node kid in node)
				{
					kids.AddItem(Transform((AstNode)kid));
				}
				node.RemoveChildren();
				foreach (Node kid_1 in kids)
				{
					node.AddChildToBack(kid_1);
				}
				return node;
			}
			finally
			{
				if (node is Scope)
				{
					PopScope();
				}
			}
		}

		private Node TransformBreak(BreakStatement node)
		{
			decompiler.AddToken(Token.BREAK);
			if (node.GetBreakLabel() != null)
			{
				decompiler.AddName(node.GetBreakLabel().GetIdentifier());
			}
			decompiler.AddEOL(Token.SEMI);
			return node;
		}

		private Node TransformCondExpr(ConditionalExpression node)
		{
			Node test = Transform(node.GetTestExpression());
			decompiler.AddToken(Token.HOOK);
			Node ifTrue = Transform(node.GetTrueExpression());
			decompiler.AddToken(Token.COLON);
			Node ifFalse = Transform(node.GetFalseExpression());
			return CreateCondExpr(test, ifTrue, ifFalse);
		}

		private Node TransformContinue(ContinueStatement node)
		{
			decompiler.AddToken(Token.CONTINUE);
			if (node.GetLabel() != null)
			{
				decompiler.AddName(node.GetLabel().GetIdentifier());
			}
			decompiler.AddEOL(Token.SEMI);
			return node;
		}

		private Node TransformDoLoop(DoLoop loop)
		{
			loop.SetType(Token.LOOP);
			PushScope(loop);
			try
			{
				decompiler.AddToken(Token.DO);
				decompiler.AddEOL(Token.LC);
				Node body = Transform(loop.GetBody());
				decompiler.AddToken(Token.RC);
				decompiler.AddToken(Token.WHILE);
				decompiler.AddToken(Token.LP);
				Node cond = Transform(loop.GetCondition());
				decompiler.AddToken(Token.RP);
				decompiler.AddEOL(Token.SEMI);
				return CreateLoop(loop, LOOP_DO_WHILE, body, cond, null, null);
			}
			finally
			{
				PopScope();
			}
		}

		private Node TransformElementGet(ElementGet node)
		{
			// OPT: could optimize to createPropertyGet
			// iff elem is string that can not be number
			Node target = Transform(node.GetTarget());
			decompiler.AddToken(Token.LB);
			Node element = Transform(node.GetElement());
			decompiler.AddToken(Token.RB);
			return new Node(Token.GETELEM, target, element);
		}

		private Node TransformExprStmt(ExpressionStatement node)
		{
			Node expr = Transform(node.GetExpression());
			decompiler.AddEOL(Token.SEMI);
			return new Node(node.GetType(), expr, node.GetLineno());
		}

		private Node TransformForInLoop(ForInLoop loop)
		{
			decompiler.AddToken(Token.FOR);
			if (loop.IsForEach())
			{
				decompiler.AddName("each ");
			}
			decompiler.AddToken(Token.LP);
			loop.SetType(Token.LOOP);
			PushScope(loop);
			try
			{
				int declType = -1;
				AstNode iter = loop.GetIterator();
				if (iter is VariableDeclaration)
				{
					declType = ((VariableDeclaration)iter).GetType();
				}
				Node lhs = Transform(iter);
				decompiler.AddToken(Token.IN);
				Node obj = Transform(loop.GetIteratedObject());
				decompiler.AddToken(Token.RP);
				decompiler.AddEOL(Token.LC);
				Node body = Transform(loop.GetBody());
				decompiler.AddEOL(Token.RC);
				return CreateForIn(declType, loop, lhs, obj, body, loop.IsForEach());
			}
			finally
			{
				PopScope();
			}
		}

		private Node TransformForLoop(ForLoop loop)
		{
			decompiler.AddToken(Token.FOR);
			decompiler.AddToken(Token.LP);
			loop.SetType(Token.LOOP);
			// XXX: Can't use pushScope/popScope here since 'createFor' may split
			// the scope
			Scope savedScope = currentScope;
			currentScope = loop;
			try
			{
				Node init = Transform(loop.GetInitializer());
				decompiler.AddToken(Token.SEMI);
				Node test = Transform(loop.GetCondition());
				decompiler.AddToken(Token.SEMI);
				Node incr = Transform(loop.GetIncrement());
				decompiler.AddToken(Token.RP);
				decompiler.AddEOL(Token.LC);
				Node body = Transform(loop.GetBody());
				decompiler.AddEOL(Token.RC);
				return CreateFor(loop, init, test, incr, body);
			}
			finally
			{
				currentScope = savedScope;
			}
		}

		private Node TransformFunction(FunctionNode fn)
		{
			int functionType = fn.GetFunctionType();
			int start = decompiler.MarkFunctionStart(functionType);
			Node mexpr = DecompileFunctionHeader(fn);
			int index = currentScriptOrFn.AddFunction(fn);
			Parser.PerFunctionVariables savedVars = new Parser.PerFunctionVariables(this, fn);
			try
			{
				// If we start needing to record much more codegen metadata during
				// function parsing, we should lump it all into a helper class.
				Node destructuring = (Node)fn.GetProp(Node.DESTRUCTURING_PARAMS);
				fn.RemoveProp(Node.DESTRUCTURING_PARAMS);
				int lineno = fn.GetBody().GetLineno();
				++nestingOfFunction;
				// only for body, not params
				Node body = Transform(fn.GetBody());
				if (!fn.IsExpressionClosure())
				{
					decompiler.AddToken(Token.RC);
				}
				fn.SetEncodedSourceBounds(start, decompiler.MarkFunctionEnd(start));
				if (functionType != FunctionNode.FUNCTION_EXPRESSION && !fn.IsExpressionClosure())
				{
					// Add EOL only if function is not part of expression
					// since it gets SEMI + EOL from Statement in that case
					decompiler.AddToken(Token.EOL);
				}
				if (destructuring != null)
				{
					body.AddChildToFront(new Node(Token.EXPR_VOID, destructuring, lineno));
				}
				int syntheticType = fn.GetFunctionType();
				Node pn = InitFunction(fn, index, body, syntheticType);
				if (mexpr != null)
				{
					pn = CreateAssignment(Token.ASSIGN, mexpr, pn);
					if (syntheticType != FunctionNode.FUNCTION_EXPRESSION)
					{
						pn = CreateExprStatementNoReturn(pn, fn.GetLineno());
					}
				}
				return pn;
			}
			finally
			{
				--nestingOfFunction;
				savedVars.Restore();
			}
		}

		private Node TransformFunctionCall(FunctionCall node)
		{
			Node call = CreateCallOrNew(Token.CALL, Transform(node.GetTarget()));
			call.SetLineno(node.GetLineno());
			decompiler.AddToken(Token.LP);
			IList<AstNode> args = node.GetArguments();
			for (int i = 0; i < args.Count; i++)
			{
				AstNode arg = args[i];
				call.AddChildToBack(Transform(arg));
				if (i < args.Count - 1)
				{
					decompiler.AddToken(Token.COMMA);
				}
			}
			decompiler.AddToken(Token.RP);
			return call;
		}

		private Node TransformGenExpr(GeneratorExpression node)
		{
			Node pn;
			FunctionNode fn = new FunctionNode();
			fn.SetSourceName(currentScriptOrFn.GetNextTempName());
			fn.SetIsGenerator();
			fn.SetFunctionType(FunctionNode.FUNCTION_EXPRESSION);
			fn.SetRequiresActivation();
			int functionType = fn.GetFunctionType();
			int start = decompiler.MarkFunctionStart(functionType);
			Node mexpr = DecompileFunctionHeader(fn);
			int index = currentScriptOrFn.AddFunction(fn);
			Parser.PerFunctionVariables savedVars = new Parser.PerFunctionVariables(this, fn);
			try
			{
				// If we start needing to record much more codegen metadata during
				// function parsing, we should lump it all into a helper class.
				Node destructuring = (Node)fn.GetProp(Node.DESTRUCTURING_PARAMS);
				fn.RemoveProp(Node.DESTRUCTURING_PARAMS);
				int lineno = node.lineno;
				++nestingOfFunction;
				// only for body, not params
				Node body = GenExprTransformHelper(node);
				if (!fn.IsExpressionClosure())
				{
					decompiler.AddToken(Token.RC);
				}
				fn.SetEncodedSourceBounds(start, decompiler.MarkFunctionEnd(start));
				if (functionType != FunctionNode.FUNCTION_EXPRESSION && !fn.IsExpressionClosure())
				{
					// Add EOL only if function is not part of expression
					// since it gets SEMI + EOL from Statement in that case
					decompiler.AddToken(Token.EOL);
				}
				if (destructuring != null)
				{
					body.AddChildToFront(new Node(Token.EXPR_VOID, destructuring, lineno));
				}
				int syntheticType = fn.GetFunctionType();
				pn = InitFunction(fn, index, body, syntheticType);
				if (mexpr != null)
				{
					pn = CreateAssignment(Token.ASSIGN, mexpr, pn);
					if (syntheticType != FunctionNode.FUNCTION_EXPRESSION)
					{
						pn = CreateExprStatementNoReturn(pn, fn.GetLineno());
					}
				}
			}
			finally
			{
				--nestingOfFunction;
				savedVars.Restore();
			}
			Node call = CreateCallOrNew(Token.CALL, pn);
			call.SetLineno(node.GetLineno());
			decompiler.AddToken(Token.LP);
			decompiler.AddToken(Token.RP);
			return call;
		}

		private Node GenExprTransformHelper(GeneratorExpression node)
		{
			decompiler.AddToken(Token.LP);
			int lineno = node.GetLineno();
			Node expr = Transform(node.GetResult());
			IList<GeneratorExpressionLoop> loops = node.GetLoops();
			int numLoops = loops.Count;
			// Walk through loops, collecting and defining their iterator symbols.
			Node[] iterators = new Node[numLoops];
			Node[] iteratedObjs = new Node[numLoops];
			for (int i = 0; i < numLoops; i++)
			{
				GeneratorExpressionLoop acl = loops[i];
				decompiler.AddName(" ");
				decompiler.AddToken(Token.FOR);
				decompiler.AddToken(Token.LP);
				AstNode iter = acl.GetIterator();
				string name = null;
				if (iter.GetType() == Token.NAME)
				{
					name = iter.GetString();
					decompiler.AddName(name);
				}
				else
				{
					// destructuring assignment
					Decompile(iter);
					name = currentScriptOrFn.GetNextTempName();
					DefineSymbol(Token.LP, name, false);
					expr = CreateBinary(Token.COMMA, CreateAssignment(Token.ASSIGN, iter, CreateName(name)), expr);
				}
				Node init = CreateName(name);
				// Define as a let since we want the scope of the variable to
				// be restricted to the array comprehension
				DefineSymbol(Token.LET, name, false);
				iterators[i] = init;
				decompiler.AddToken(Token.IN);
				iteratedObjs[i] = Transform(acl.GetIteratedObject());
				decompiler.AddToken(Token.RP);
			}
			// generate code for tmpArray.push(body)
			Node yield = new Node(Token.YIELD, expr, node.GetLineno());
			Node body = new Node(Token.EXPR_VOID, yield, lineno);
			if (node.GetFilter() != null)
			{
				decompiler.AddName(" ");
				decompiler.AddToken(Token.IF);
				decompiler.AddToken(Token.LP);
				body = CreateIf(Transform(node.GetFilter()), body, null, lineno);
				decompiler.AddToken(Token.RP);
			}
			// Now walk loops in reverse to build up the body statement.
			int pushed = 0;
			try
			{
				for (int i_1 = numLoops - 1; i_1 >= 0; i_1--)
				{
					GeneratorExpressionLoop acl = loops[i_1];
					Scope loop = CreateLoopNode(null, acl.GetLineno());
					// no label
					PushScope(loop);
					pushed++;
					body = CreateForIn(Token.LET, loop, iterators[i_1], iteratedObjs[i_1], body, acl.IsForEach());
				}
			}
			finally
			{
				for (int i_1 = 0; i_1 < pushed; i_1++)
				{
					PopScope();
				}
			}
			decompiler.AddToken(Token.RP);
			return body;
		}

		private Node TransformIf(IfStatement n)
		{
			decompiler.AddToken(Token.IF);
			decompiler.AddToken(Token.LP);
			Node cond = Transform(n.GetCondition());
			decompiler.AddToken(Token.RP);
			decompiler.AddEOL(Token.LC);
			Node ifTrue = Transform(n.GetThenPart());
			Node ifFalse = null;
			if (n.GetElsePart() != null)
			{
				decompiler.AddToken(Token.RC);
				decompiler.AddToken(Token.ELSE);
				decompiler.AddEOL(Token.LC);
				ifFalse = Transform(n.GetElsePart());
			}
			decompiler.AddEOL(Token.RC);
			return CreateIf(cond, ifTrue, ifFalse, n.GetLineno());
		}

		private Node TransformInfix(InfixExpression node)
		{
			Node left = Transform(node.GetLeft());
			decompiler.AddToken(node.GetType());
			Node right = Transform(node.GetRight());
			if (node is XmlDotQuery)
			{
				decompiler.AddToken(Token.RP);
			}
			return CreateBinary(node.GetType(), left, right);
		}

		private Node TransformLabeledStatement(LabeledStatement ls)
		{
			Label label = ls.GetFirstLabel();
			IList<Label> labels = ls.GetLabels();
			decompiler.AddName(label.GetName());
			if (labels.Count > 1)
			{
				// more than one label
				foreach (Label lb in labels.SubList(1, labels.Count))
				{
					decompiler.AddEOL(Token.COLON);
					decompiler.AddName(lb.GetName());
				}
			}
			if (ls.GetStatement().GetType() == Token.BLOCK)
			{
				// reuse OBJECTLIT for ':' workaround, cf. transformObjectLiteral()
				decompiler.AddToken(Token.OBJECTLIT);
				decompiler.AddEOL(Token.LC);
			}
			else
			{
				decompiler.AddEOL(Token.COLON);
			}
			Node statement = Transform(ls.GetStatement());
			if (ls.GetStatement().GetType() == Token.BLOCK)
			{
				decompiler.AddEOL(Token.RC);
			}
			// Make a target and put it _after_ the statement node.  Add in the
			// LABEL node, so breaks get the right target.
			Node breakTarget = Node.NewTarget();
			Node block = new Node(Token.BLOCK, label, statement, breakTarget);
			label.target = breakTarget;
			return block;
		}

		private Node TransformLetNode(LetNode node)
		{
			PushScope(node);
			try
			{
				decompiler.AddToken(Token.LET);
				decompiler.AddToken(Token.LP);
				Node vars = TransformVariableInitializers(node.GetVariables());
				decompiler.AddToken(Token.RP);
				node.AddChildToBack(vars);
				bool letExpr = node.GetType() == Token.LETEXPR;
				if (node.GetBody() != null)
				{
					if (letExpr)
					{
						decompiler.AddName(" ");
					}
					else
					{
						decompiler.AddEOL(Token.LC);
					}
					node.AddChildToBack(Transform(node.GetBody()));
					if (!letExpr)
					{
						decompiler.AddEOL(Token.RC);
					}
				}
				return node;
			}
			finally
			{
				PopScope();
			}
		}

		private Node TransformLiteral(AstNode node)
		{
			decompiler.AddToken(node.GetType());
			return node;
		}

		private Node TransformName(Name node)
		{
			decompiler.AddName(node.GetIdentifier());
			return node;
		}

		private Node TransformNewExpr(NewExpression node)
		{
			decompiler.AddToken(Token.NEW);
			Node nx = CreateCallOrNew(Token.NEW, Transform(node.GetTarget()));
			nx.SetLineno(node.GetLineno());
			IList<AstNode> args = node.GetArguments();
			decompiler.AddToken(Token.LP);
			for (int i = 0; i < args.Count; i++)
			{
				AstNode arg = args[i];
				nx.AddChildToBack(Transform(arg));
				if (i < args.Count - 1)
				{
					decompiler.AddToken(Token.COMMA);
				}
			}
			decompiler.AddToken(Token.RP);
			if (node.GetInitializer() != null)
			{
				nx.AddChildToBack(TransformObjectLiteral(node.GetInitializer()));
			}
			return nx;
		}

		private Node TransformNumber(NumberLiteral node)
		{
			decompiler.AddNumber(node.GetNumber());
			return node;
		}

		private Node TransformObjectLiteral(ObjectLiteral node)
		{
			if (node.IsDestructuring())
			{
				return node;
			}
			// createObjectLiteral rewrites its argument as object
			// creation plus object property entries, so later compiler
			// stages don't need to know about object literals.
			decompiler.AddToken(Token.LC);
			IList<ObjectProperty> elems = node.GetElements();
			Node @object = new Node(Token.OBJECTLIT);
			object[] properties;
			if (elems.IsEmpty())
			{
				properties = ScriptRuntime.emptyArgs;
			}
			else
			{
				int size = elems.Count;
				int i = 0;
				properties = new object[size];
				foreach (ObjectProperty prop in elems)
				{
					if (prop.IsGetter())
					{
						decompiler.AddToken(Token.GET);
					}
					else
					{
						if (prop.IsSetter())
						{
							decompiler.AddToken(Token.SET);
						}
					}
					properties[i++] = GetPropKey(prop.GetLeft());
					// OBJECTLIT is used as ':' in object literal for
					// decompilation to solve spacing ambiguity.
					if (!(prop.IsGetter() || prop.IsSetter()))
					{
						decompiler.AddToken(Token.OBJECTLIT);
					}
					Node right = Transform(prop.GetRight());
					if (prop.IsGetter())
					{
						right = CreateUnary(Token.GET, right);
					}
					else
					{
						if (prop.IsSetter())
						{
							right = CreateUnary(Token.SET, right);
						}
					}
					@object.AddChildToBack(right);
					if (i < size)
					{
						decompiler.AddToken(Token.COMMA);
					}
				}
			}
			decompiler.AddToken(Token.RC);
			@object.PutProp(Node.OBJECT_IDS_PROP, properties);
			return @object;
		}

		private object GetPropKey(Node id)
		{
			object key;
			if (id is Name)
			{
				string s = ((Name)id).GetIdentifier();
				decompiler.AddName(s);
				key = ScriptRuntime.GetIndexObject(s);
			}
			else
			{
				if (id is StringLiteral)
				{
					string s = ((StringLiteral)id).GetValue();
					decompiler.AddString(s);
					key = ScriptRuntime.GetIndexObject(s);
				}
				else
				{
					if (id is NumberLiteral)
					{
						double n = ((NumberLiteral)id).GetNumber();
						decompiler.AddNumber(n);
						key = ScriptRuntime.GetIndexObject(n);
					}
					else
					{
						throw Kit.CodeBug();
					}
				}
			}
			return key;
		}

		private Node TransformParenExpr(ParenthesizedExpression node)
		{
			AstNode expr = node.GetExpression();
			decompiler.AddToken(Token.LP);
			int count = 1;
			while (expr is ParenthesizedExpression)
			{
				decompiler.AddToken(Token.LP);
				count++;
				expr = ((ParenthesizedExpression)expr).GetExpression();
			}
			Node result = Transform(expr);
			for (int i = 0; i < count; i++)
			{
				decompiler.AddToken(Token.RP);
			}
			result.PutProp(Node.PARENTHESIZED_PROP, true);
			return result;
		}

		private Node TransformPropertyGet(PropertyGet node)
		{
			Node target = Transform(node.GetTarget());
			string name = node.GetProperty().GetIdentifier();
			decompiler.AddToken(Token.DOT);
			decompiler.AddName(name);
			return CreatePropertyGet(target, null, name, 0);
		}

		private Node TransformRegExp(RegExpLiteral node)
		{
			decompiler.AddRegexp(node.GetValue(), node.GetFlags());
			currentScriptOrFn.AddRegExp(node);
			return node;
		}

		private Node TransformReturn(ReturnStatement node)
		{
			bool expClosure = true.Equals(node.GetProp(Node.EXPRESSION_CLOSURE_PROP));
			if (expClosure)
			{
				decompiler.AddName(" ");
			}
			else
			{
				decompiler.AddToken(Token.RETURN);
			}
			AstNode rv = node.GetReturnValue();
			Node value = rv == null ? null : Transform(rv);
			if (!expClosure)
			{
				decompiler.AddEOL(Token.SEMI);
			}
			return rv == null ? new Node(Token.RETURN, node.GetLineno()) : new Node(Token.RETURN, value, node.GetLineno());
		}

		private Node TransformScript(ScriptNode node)
		{
			decompiler.AddToken(Token.SCRIPT);
			if (currentScope != null)
			{
				Kit.CodeBug();
			}
			currentScope = node;
			Node body = new Node(Token.BLOCK);
			foreach (Node kid in node)
			{
				body.AddChildToBack(Transform((AstNode)kid));
			}
			node.RemoveChildren();
			Node children = body.GetFirstChild();
			if (children != null)
			{
				node.AddChildrenToBack(children);
			}
			return node;
		}

		private Node TransformString(StringLiteral node)
		{
			decompiler.AddString(node.GetValue());
			return Node.NewString(node.GetValue());
		}

		private Node TransformSwitch(SwitchStatement node)
		{
			// The switch will be rewritten from:
			//
			// switch (expr) {
			//   case test1: statements1;
			//   ...
			//   default: statementsDefault;
			//   ...
			//   case testN: statementsN;
			// }
			//
			// to:
			//
			// {
			//     switch (expr) {
			//       case test1: goto label1;
			//       ...
			//       case testN: goto labelN;
			//     }
			//     goto labelDefault;
			//   label1:
			//     statements1;
			//   ...
			//   labelDefault:
			//     statementsDefault;
			//   ...
			//   labelN:
			//     statementsN;
			//   breakLabel:
			// }
			//
			// where inside switch each "break;" without label will be replaced
			// by "goto breakLabel".
			//
			// If the original switch does not have the default label, then
			// after the switch he transformed code would contain this goto:
			//     goto breakLabel;
			// instead of:
			//     goto labelDefault;
			decompiler.AddToken(Token.SWITCH);
			decompiler.AddToken(Token.LP);
			Node switchExpr = Transform(node.GetExpression());
			decompiler.AddToken(Token.RP);
			node.AddChildToBack(switchExpr);
			Node block = new Node(Token.BLOCK, node, node.GetLineno());
			decompiler.AddEOL(Token.LC);
			foreach (SwitchCase sc in node.GetCases())
			{
				AstNode expr = sc.GetExpression();
				Node caseExpr = null;
				if (expr != null)
				{
					decompiler.AddToken(Token.CASE);
					caseExpr = Transform(expr);
				}
				else
				{
					decompiler.AddToken(Token.DEFAULT);
				}
				decompiler.AddEOL(Token.COLON);
				IList<AstNode> stmts = sc.GetStatements();
				Node body = new Block();
				if (stmts != null)
				{
					foreach (AstNode kid in stmts)
					{
						body.AddChildToBack(Transform(kid));
					}
				}
				AddSwitchCase(block, caseExpr, body);
			}
			decompiler.AddEOL(Token.RC);
			CloseSwitch(block);
			return block;
		}

		private Node TransformThrow(ThrowStatement node)
		{
			decompiler.AddToken(Token.THROW);
			Node value = Transform(node.GetExpression());
			decompiler.AddEOL(Token.SEMI);
			return new Node(Token.THROW, value, node.GetLineno());
		}

		private Node TransformTry(TryStatement node)
		{
			decompiler.AddToken(Token.TRY);
			decompiler.AddEOL(Token.LC);
			Node tryBlock = Transform(node.GetTryBlock());
			decompiler.AddEOL(Token.RC);
			Node catchBlocks = new Block();
			foreach (CatchClause cc in node.GetCatchClauses())
			{
				decompiler.AddToken(Token.CATCH);
				decompiler.AddToken(Token.LP);
				string varName = cc.GetVarName().GetIdentifier();
				decompiler.AddName(varName);
				Node catchCond = null;
				AstNode ccc = cc.GetCatchCondition();
				if (ccc != null)
				{
					decompiler.AddName(" ");
					decompiler.AddToken(Token.IF);
					catchCond = Transform(ccc);
				}
				else
				{
					catchCond = new EmptyExpression();
				}
				decompiler.AddToken(Token.RP);
				decompiler.AddEOL(Token.LC);
				Node body = Transform(cc.GetBody());
				decompiler.AddEOL(Token.RC);
				catchBlocks.AddChildToBack(CreateCatch(varName, catchCond, body, cc.GetLineno()));
			}
			Node finallyBlock = null;
			if (node.GetFinallyBlock() != null)
			{
				decompiler.AddToken(Token.FINALLY);
				decompiler.AddEOL(Token.LC);
				finallyBlock = Transform(node.GetFinallyBlock());
				decompiler.AddEOL(Token.RC);
			}
			return CreateTryCatchFinally(tryBlock, catchBlocks, finallyBlock, node.GetLineno());
		}

		private Node TransformUnary(UnaryExpression node)
		{
			int type = node.GetType();
			if (type == Token.DEFAULTNAMESPACE)
			{
				return TransformDefaultXmlNamepace(node);
			}
			if (node.IsPrefix())
			{
				decompiler.AddToken(type);
			}
			Node child = Transform(node.GetOperand());
			if (node.IsPostfix())
			{
				decompiler.AddToken(type);
			}
			if (type == Token.INC || type == Token.DEC)
			{
				return CreateIncDec(type, node.IsPostfix(), child);
			}
			return CreateUnary(type, child);
		}

		private Node TransformVariables(VariableDeclaration node)
		{
			decompiler.AddToken(node.GetType());
			TransformVariableInitializers(node);
			// Might be most robust to have parser record whether it was
			// a variable declaration statement, possibly as a node property.
			AstNode parent = node.GetParent();
			if (!(parent is Loop) && !(parent is LetNode))
			{
				decompiler.AddEOL(Token.SEMI);
			}
			return node;
		}

		private Node TransformVariableInitializers(VariableDeclaration node)
		{
			IList<VariableInitializer> vars = node.GetVariables();
			int size = vars.Count;
			int i = 0;
			foreach (VariableInitializer var in vars)
			{
				AstNode target = var.GetTarget();
				AstNode init = var.GetInitializer();
				Node left = null;
				if (var.IsDestructuring())
				{
					Decompile(target);
					// decompile but don't transform
					left = target;
				}
				else
				{
					left = Transform(target);
				}
				Node right = null;
				if (init != null)
				{
					decompiler.AddToken(Token.ASSIGN);
					right = Transform(init);
				}
				if (var.IsDestructuring())
				{
					if (right == null)
					{
						// TODO:  should this ever happen?
						node.AddChildToBack(left);
					}
					else
					{
						Node d = CreateDestructuringAssignment(node.GetType(), left, right);
						node.AddChildToBack(d);
					}
				}
				else
				{
					if (right != null)
					{
						left.AddChildToBack(right);
					}
					node.AddChildToBack(left);
				}
				if (i++ < size - 1)
				{
					decompiler.AddToken(Token.COMMA);
				}
			}
			return node;
		}

		private Node TransformWhileLoop(WhileLoop loop)
		{
			decompiler.AddToken(Token.WHILE);
			loop.SetType(Token.LOOP);
			PushScope(loop);
			try
			{
				decompiler.AddToken(Token.LP);
				Node cond = Transform(loop.GetCondition());
				decompiler.AddToken(Token.RP);
				decompiler.AddEOL(Token.LC);
				Node body = Transform(loop.GetBody());
				decompiler.AddEOL(Token.RC);
				return CreateLoop(loop, LOOP_WHILE, body, cond, null, null);
			}
			finally
			{
				PopScope();
			}
		}

		private Node TransformWith(WithStatement node)
		{
			decompiler.AddToken(Token.WITH);
			decompiler.AddToken(Token.LP);
			Node expr = Transform(node.GetExpression());
			decompiler.AddToken(Token.RP);
			decompiler.AddEOL(Token.LC);
			Node stmt = Transform(node.GetStatement());
			decompiler.AddEOL(Token.RC);
			return CreateWith(expr, stmt, node.GetLineno());
		}

		private Node TransformYield(Yield node)
		{
			decompiler.AddToken(Token.YIELD);
			Node kid = node.GetValue() == null ? null : Transform(node.GetValue());
			if (kid != null)
			{
				return new Node(Token.YIELD, kid, node.GetLineno());
			}
			else
			{
				return new Node(Token.YIELD, node.GetLineno());
			}
		}

		private Node TransformXmlLiteral(XmlLiteral node)
		{
			// a literal like <foo>{bar}</foo> is rewritten as
			//   new XML("<foo>" + bar + "</foo>");
			Node pnXML = new Node(Token.NEW, node.GetLineno());
			IList<XmlFragment> frags = node.GetFragments();
			XmlString first = (XmlString)frags[0];
			bool anon = first.GetXml().Trim().StartsWith("<>");
			pnXML.AddChildToBack(CreateName(anon ? "XMLList" : "XML"));
			Node pn = null;
			foreach (XmlFragment frag in frags)
			{
				if (frag is XmlString)
				{
					string xml = ((XmlString)frag).GetXml();
					decompiler.AddName(xml);
					if (pn == null)
					{
						pn = CreateString(xml);
					}
					else
					{
						pn = CreateBinary(Token.ADD, pn, CreateString(xml));
					}
				}
				else
				{
					XmlExpression xexpr = (XmlExpression)frag;
					bool isXmlAttr = xexpr.IsXmlAttribute();
					Node expr;
					decompiler.AddToken(Token.LC);
					if (xexpr.GetExpression() is EmptyExpression)
					{
						expr = CreateString(string.Empty);
					}
					else
					{
						expr = Transform(xexpr.GetExpression());
					}
					decompiler.AddToken(Token.RC);
					if (isXmlAttr)
					{
						// Need to put the result in double quotes
						expr = CreateUnary(Token.ESCXMLATTR, expr);
						Node prepend = CreateBinary(Token.ADD, CreateString("\""), expr);
						expr = CreateBinary(Token.ADD, prepend, CreateString("\""));
					}
					else
					{
						expr = CreateUnary(Token.ESCXMLTEXT, expr);
					}
					pn = CreateBinary(Token.ADD, pn, expr);
				}
			}
			pnXML.AddChildToBack(pn);
			return pnXML;
		}

		private Node TransformXmlMemberGet(XmlMemberGet node)
		{
			XmlRef @ref = node.GetMemberRef();
			Node pn = Transform(node.GetLeft());
			int flags = @ref.IsAttributeAccess() ? Node.ATTRIBUTE_FLAG : 0;
			if (node.GetType() == Token.DOTDOT)
			{
				flags |= Node.DESCENDANTS_FLAG;
				decompiler.AddToken(Token.DOTDOT);
			}
			else
			{
				decompiler.AddToken(Token.DOT);
			}
			return TransformXmlRef(pn, @ref, flags);
		}

		// We get here if we weren't a child of a . or .. infix node
		private Node TransformXmlRef(XmlRef node)
		{
			int memberTypeFlags = node.IsAttributeAccess() ? Node.ATTRIBUTE_FLAG : 0;
			return TransformXmlRef(null, node, memberTypeFlags);
		}

		private Node TransformXmlRef(Node pn, XmlRef node, int memberTypeFlags)
		{
			if ((memberTypeFlags & Node.ATTRIBUTE_FLAG) != 0)
			{
				decompiler.AddToken(Token.XMLATTR);
			}
			Name @namespace = node.GetNamespace();
			string ns = @namespace != null ? @namespace.GetIdentifier() : null;
			if (ns != null)
			{
				decompiler.AddName(ns);
				decompiler.AddToken(Token.COLONCOLON);
			}
			if (node is XmlPropRef)
			{
				string name = ((XmlPropRef)node).GetPropName().GetIdentifier();
				decompiler.AddName(name);
				return CreatePropertyGet(pn, ns, name, memberTypeFlags);
			}
			else
			{
				decompiler.AddToken(Token.LB);
				Node expr = Transform(((XmlElemRef)node).GetExpression());
				decompiler.AddToken(Token.RB);
				return CreateElementGet(pn, ns, expr, memberTypeFlags);
			}
		}

		private Node TransformDefaultXmlNamepace(UnaryExpression node)
		{
			decompiler.AddToken(Token.DEFAULT);
			decompiler.AddName(" xml");
			decompiler.AddName(" namespace");
			decompiler.AddToken(Token.ASSIGN);
			Node child = Transform(node.GetOperand());
			return CreateUnary(Token.DEFAULTNAMESPACE, child);
		}

		/// <summary>If caseExpression argument is null it indicates a default label.</summary>
		/// <remarks>If caseExpression argument is null it indicates a default label.</remarks>
		private void AddSwitchCase(Node switchBlock, Node caseExpression, Node statements)
		{
			if (switchBlock.GetType() != Token.BLOCK)
			{
				throw Kit.CodeBug();
			}
			Jump switchNode = (Jump)switchBlock.GetFirstChild();
			if (switchNode.GetType() != Token.SWITCH)
			{
				throw Kit.CodeBug();
			}
			Node gotoTarget = Node.NewTarget();
			if (caseExpression != null)
			{
				Jump caseNode = new Jump(Token.CASE, caseExpression);
				caseNode.target = gotoTarget;
				switchNode.AddChildToBack(caseNode);
			}
			else
			{
				switchNode.SetDefault(gotoTarget);
			}
			switchBlock.AddChildToBack(gotoTarget);
			switchBlock.AddChildToBack(statements);
		}

		private void CloseSwitch(Node switchBlock)
		{
			if (switchBlock.GetType() != Token.BLOCK)
			{
				throw Kit.CodeBug();
			}
			Jump switchNode = (Jump)switchBlock.GetFirstChild();
			if (switchNode.GetType() != Token.SWITCH)
			{
				throw Kit.CodeBug();
			}
			Node switchBreakTarget = Node.NewTarget();
			// switchNode.target is only used by NodeTransformer
			// to detect switch end
			switchNode.target = switchBreakTarget;
			Node defaultTarget = switchNode.GetDefault();
			if (defaultTarget == null)
			{
				defaultTarget = switchBreakTarget;
			}
			switchBlock.AddChildAfter(MakeJump(Token.GOTO, defaultTarget), switchNode);
			switchBlock.AddChildToBack(switchBreakTarget);
		}

		private Node CreateExprStatementNoReturn(Node expr, int lineno)
		{
			return new Node(Token.EXPR_VOID, expr, lineno);
		}

		private Node CreateString(string @string)
		{
			return Node.NewString(@string);
		}

		/// <summary>Catch clause of try/catch/finally</summary>
		/// <param name="varName">the name of the variable to bind to the exception</param>
		/// <param name="catchCond">
		/// the condition under which to catch the exception.
		/// May be null if no condition is given.
		/// </param>
		/// <param name="stmts">the statements in the catch clause</param>
		/// <param name="lineno">the starting line number of the catch clause</param>
		private Node CreateCatch(string varName, Node catchCond, Node stmts, int lineno)
		{
			if (catchCond == null)
			{
				catchCond = new Node(Token.EMPTY);
			}
			return new Node(Token.CATCH, CreateName(varName), catchCond, stmts, lineno);
		}

		private Node InitFunction(FunctionNode fnNode, int functionIndex, Node statements, int functionType)
		{
			fnNode.SetFunctionType(functionType);
			fnNode.AddChildToBack(statements);
			int functionCount = fnNode.GetFunctionCount();
			if (functionCount != 0)
			{
				// Functions containing other functions require activation objects
				fnNode.SetRequiresActivation();
			}
			if (functionType == FunctionNode.FUNCTION_EXPRESSION)
			{
				Name name = fnNode.GetFunctionName();
				if (name != null && name.Length() != 0 && fnNode.GetSymbol(name.GetIdentifier()) == null)
				{
					// A function expression needs to have its name as a
					// variable (if it isn't already allocated as a variable).
					// See ECMA Ch. 13.  We add code to the beginning of the
					// function to initialize a local variable of the
					// function's name to the function value, but only if the
					// function doesn't already define a formal parameter, var,
					// or nested function with the same name.
					fnNode.PutSymbol(new Symbol(Token.FUNCTION, name.GetIdentifier()));
					Node setFn = new Node(Token.EXPR_VOID, new Node(Token.SETNAME, Node.NewString(Token.BINDNAME, name.GetIdentifier()), new Node(Token.THISFN)));
					statements.AddChildrenToFront(setFn);
				}
			}
			// Add return to end if needed.
			Node lastStmt = statements.GetLastChild();
			if (lastStmt == null || lastStmt.GetType() != Token.RETURN)
			{
				statements.AddChildToBack(new Node(Token.RETURN));
			}
			Node result = Node.NewString(Token.FUNCTION, fnNode.GetName());
			result.PutIntProp(Node.FUNCTION_PROP, functionIndex);
			return result;
		}

		/// <summary>Create loop node.</summary>
		/// <remarks>
		/// Create loop node. The code generator will later call
		/// createWhile|createDoWhile|createFor|createForIn
		/// to finish loop generation.
		/// </remarks>
		private Scope CreateLoopNode(Node loopLabel, int lineno)
		{
			Scope result = CreateScopeNode(Token.LOOP, lineno);
			if (loopLabel != null)
			{
				((Jump)loopLabel).SetLoop(result);
			}
			return result;
		}

		private Node CreateFor(Scope loop, Node init, Node test, Node incr, Node body)
		{
			if (init.GetType() == Token.LET)
			{
				// rewrite "for (let i=s; i < N; i++)..." as
				// "let (i=s) { for (; i < N; i++)..." so that "s" is evaluated
				// outside the scope of the for.
				Scope let = Scope.SplitScope(loop);
				let.SetType(Token.LET);
				let.AddChildrenToBack(init);
				let.AddChildToBack(CreateLoop(loop, LOOP_FOR, body, test, new Node(Token.EMPTY), incr));
				return let;
			}
			return CreateLoop(loop, LOOP_FOR, body, test, init, incr);
		}

		private Node CreateLoop(Jump loop, int loopType, Node body, Node cond, Node init, Node incr)
		{
			Node bodyTarget = Node.NewTarget();
			Node condTarget = Node.NewTarget();
			if (loopType == LOOP_FOR && cond.GetType() == Token.EMPTY)
			{
				cond = new Node(Token.TRUE);
			}
			Jump IFEQ = new Jump(Token.IFEQ, cond);
			IFEQ.target = bodyTarget;
			Node breakTarget = Node.NewTarget();
			loop.AddChildToBack(bodyTarget);
			loop.AddChildrenToBack(body);
			if (loopType == LOOP_WHILE || loopType == LOOP_FOR)
			{
				// propagate lineno to condition
				loop.AddChildrenToBack(new Node(Token.EMPTY, loop.GetLineno()));
			}
			loop.AddChildToBack(condTarget);
			loop.AddChildToBack(IFEQ);
			loop.AddChildToBack(breakTarget);
			loop.target = breakTarget;
			Node continueTarget = condTarget;
			if (loopType == LOOP_WHILE || loopType == LOOP_FOR)
			{
				// Just add a GOTO to the condition in the do..while
				loop.AddChildToFront(MakeJump(Token.GOTO, condTarget));
				if (loopType == LOOP_FOR)
				{
					int initType = init.GetType();
					if (initType != Token.EMPTY)
					{
						if (initType != Token.VAR && initType != Token.LET)
						{
							init = new Node(Token.EXPR_VOID, init);
						}
						loop.AddChildToFront(init);
					}
					Node incrTarget = Node.NewTarget();
					loop.AddChildAfter(incrTarget, body);
					if (incr.GetType() != Token.EMPTY)
					{
						incr = new Node(Token.EXPR_VOID, incr);
						loop.AddChildAfter(incr, incrTarget);
					}
					continueTarget = incrTarget;
				}
			}
			loop.SetContinue(continueTarget);
			return loop;
		}

		/// <summary>Generate IR for a for..in loop.</summary>
		/// <remarks>Generate IR for a for..in loop.</remarks>
		private Node CreateForIn(int declType, Node loop, Node lhs, Node obj, Node body, bool isForEach)
		{
			int destructuring = -1;
			int destructuringLen = 0;
			Node lvalue;
			int type = lhs.GetType();
			if (type == Token.VAR || type == Token.LET)
			{
				Node kid = lhs.GetLastChild();
				int kidType = kid.GetType();
				if (kidType == Token.ARRAYLIT || kidType == Token.OBJECTLIT)
				{
					type = destructuring = kidType;
					lvalue = kid;
					destructuringLen = 0;
					if (kid is ArrayLiteral)
					{
						destructuringLen = ((ArrayLiteral)kid).GetDestructuringLength();
					}
				}
				else
				{
					if (kidType == Token.NAME)
					{
						lvalue = Node.NewString(Token.NAME, kid.GetString());
					}
					else
					{
						ReportError("msg.bad.for.in.lhs");
						return null;
					}
				}
			}
			else
			{
				if (type == Token.ARRAYLIT || type == Token.OBJECTLIT)
				{
					destructuring = type;
					lvalue = lhs;
					destructuringLen = 0;
					if (lhs is ArrayLiteral)
					{
						destructuringLen = ((ArrayLiteral)lhs).GetDestructuringLength();
					}
				}
				else
				{
					lvalue = MakeReference(lhs);
					if (lvalue == null)
					{
						ReportError("msg.bad.for.in.lhs");
						return null;
					}
				}
			}
			Node localBlock = new Node(Token.LOCAL_BLOCK);
			int initType = isForEach ? Token.ENUM_INIT_VALUES : (destructuring != -1 ? Token.ENUM_INIT_ARRAY : Token.ENUM_INIT_KEYS);
			Node init = new Node(initType, obj);
			init.PutProp(Node.LOCAL_BLOCK_PROP, localBlock);
			Node cond = new Node(Token.ENUM_NEXT);
			cond.PutProp(Node.LOCAL_BLOCK_PROP, localBlock);
			Node id = new Node(Token.ENUM_ID);
			id.PutProp(Node.LOCAL_BLOCK_PROP, localBlock);
			Node newBody = new Node(Token.BLOCK);
			Node assign;
			if (destructuring != -1)
			{
				assign = CreateDestructuringAssignment(declType, lvalue, id);
				if (!isForEach && (destructuring == Token.OBJECTLIT || destructuringLen != 2))
				{
					// destructuring assignment is only allowed in for..each or
					// with an array type of length 2 (to hold key and value)
					ReportError("msg.bad.for.in.destruct");
				}
			}
			else
			{
				assign = SimpleAssignment(lvalue, id);
			}
			newBody.AddChildToBack(new Node(Token.EXPR_VOID, assign));
			newBody.AddChildToBack(body);
			loop = CreateLoop((Jump)loop, LOOP_WHILE, newBody, cond, null, null);
			loop.AddChildToFront(init);
			if (type == Token.VAR || type == Token.LET)
			{
				loop.AddChildToFront(lhs);
			}
			localBlock.AddChildToBack(loop);
			return localBlock;
		}

		/// <summary>
		/// Try/Catch/Finally
		/// The IRFactory tries to express as much as possible in the tree;
		/// the responsibilities remaining for Codegen are to add the Java
		/// handlers: (Either (but not both) of TARGET and FINALLY might not
		/// be defined)
		/// - a catch handler for javascript exceptions that unwraps the
		/// exception onto the stack and GOTOes to the catch target
		/// - a finally handler
		/// ...
		/// </summary>
		/// <remarks>
		/// Try/Catch/Finally
		/// The IRFactory tries to express as much as possible in the tree;
		/// the responsibilities remaining for Codegen are to add the Java
		/// handlers: (Either (but not both) of TARGET and FINALLY might not
		/// be defined)
		/// - a catch handler for javascript exceptions that unwraps the
		/// exception onto the stack and GOTOes to the catch target
		/// - a finally handler
		/// ... and a goto to GOTO around these handlers.
		/// </remarks>
		private Node CreateTryCatchFinally(Node tryBlock, Node catchBlocks, Node finallyBlock, int lineno)
		{
			bool hasFinally = (finallyBlock != null) && (finallyBlock.GetType() != Token.BLOCK || finallyBlock.HasChildren());
			// short circuit
			if (tryBlock.GetType() == Token.BLOCK && !tryBlock.HasChildren() && !hasFinally)
			{
				return tryBlock;
			}
			bool hasCatch = catchBlocks.HasChildren();
			// short circuit
			if (!hasFinally && !hasCatch)
			{
				// bc finally might be an empty block...
				return tryBlock;
			}
			Node handlerBlock = new Node(Token.LOCAL_BLOCK);
			Jump pn = new Jump(Token.TRY, tryBlock, lineno);
			pn.PutProp(Node.LOCAL_BLOCK_PROP, handlerBlock);
			if (hasCatch)
			{
				// jump around catch code
				Node endCatch = Node.NewTarget();
				pn.AddChildToBack(MakeJump(Token.GOTO, endCatch));
				// make a TARGET for the catch that the tcf node knows about
				Node catchTarget = Node.NewTarget();
				pn.target = catchTarget;
				// mark it
				pn.AddChildToBack(catchTarget);
				//
				//  Given
				//
				//   try {
				//       tryBlock;
				//   } catch (e if condition1) {
				//       something1;
				//   ...
				//
				//   } catch (e if conditionN) {
				//       somethingN;
				//   } catch (e) {
				//       somethingDefault;
				//   }
				//
				//  rewrite as
				//
				//   try {
				//       tryBlock;
				//       goto after_catch:
				//   } catch (x) {
				//       with (newCatchScope(e, x)) {
				//           if (condition1) {
				//               something1;
				//               goto after_catch;
				//           }
				//       }
				//   ...
				//       with (newCatchScope(e, x)) {
				//           if (conditionN) {
				//               somethingN;
				//               goto after_catch;
				//           }
				//       }
				//       with (newCatchScope(e, x)) {
				//           somethingDefault;
				//           goto after_catch;
				//       }
				//   }
				// after_catch:
				//
				// If there is no default catch, then the last with block
				// arround  "somethingDefault;" is replaced by "rethrow;"
				// It is assumed that catch handler generation will store
				// exeception object in handlerBlock register
				// Block with local for exception scope objects
				Node catchScopeBlock = new Node(Token.LOCAL_BLOCK);
				// expects catchblocks children to be (cond block) pairs.
				Node cb = catchBlocks.GetFirstChild();
				bool hasDefault = false;
				int scopeIndex = 0;
				while (cb != null)
				{
					int catchLineNo = cb.GetLineno();
					Node name = cb.GetFirstChild();
					Node cond = name.GetNext();
					Node catchStatement = cond.GetNext();
					cb.RemoveChild(name);
					cb.RemoveChild(cond);
					cb.RemoveChild(catchStatement);
					// Add goto to the catch statement to jump out of catch
					// but prefix it with LEAVEWITH since try..catch produces
					// "with"code in order to limit the scope of the exception
					// object.
					catchStatement.AddChildToBack(new Node(Token.LEAVEWITH));
					catchStatement.AddChildToBack(MakeJump(Token.GOTO, endCatch));
					// Create condition "if" when present
					Node condStmt;
					if (cond.GetType() == Token.EMPTY)
					{
						condStmt = catchStatement;
						hasDefault = true;
					}
					else
					{
						condStmt = CreateIf(cond, catchStatement, null, catchLineNo);
					}
					// Generate code to create the scope object and store
					// it in catchScopeBlock register
					Node catchScope = new Node(Token.CATCH_SCOPE, name, CreateUseLocal(handlerBlock));
					catchScope.PutProp(Node.LOCAL_BLOCK_PROP, catchScopeBlock);
					catchScope.PutIntProp(Node.CATCH_SCOPE_PROP, scopeIndex);
					catchScopeBlock.AddChildToBack(catchScope);
					// Add with statement based on catch scope object
					catchScopeBlock.AddChildToBack(CreateWith(CreateUseLocal(catchScopeBlock), condStmt, catchLineNo));
					// move to next cb
					cb = cb.GetNext();
					++scopeIndex;
				}
				pn.AddChildToBack(catchScopeBlock);
				if (!hasDefault)
				{
					// Generate code to rethrow if no catch clause was executed
					Node rethrow = new Node(Token.RETHROW);
					rethrow.PutProp(Node.LOCAL_BLOCK_PROP, handlerBlock);
					pn.AddChildToBack(rethrow);
				}
				pn.AddChildToBack(endCatch);
			}
			if (hasFinally)
			{
				Node finallyTarget = Node.NewTarget();
				pn.SetFinally(finallyTarget);
				// add jsr finally to the try block
				pn.AddChildToBack(MakeJump(Token.JSR, finallyTarget));
				// jump around finally code
				Node finallyEnd = Node.NewTarget();
				pn.AddChildToBack(MakeJump(Token.GOTO, finallyEnd));
				pn.AddChildToBack(finallyTarget);
				Node fBlock = new Node(Token.FINALLY, finallyBlock);
				fBlock.PutProp(Node.LOCAL_BLOCK_PROP, handlerBlock);
				pn.AddChildToBack(fBlock);
				pn.AddChildToBack(finallyEnd);
			}
			handlerBlock.AddChildToBack(pn);
			return handlerBlock;
		}

		private Node CreateWith(Node obj, Node body, int lineno)
		{
			SetRequiresActivation();
			Node result = new Node(Token.BLOCK, lineno);
			result.AddChildToBack(new Node(Token.ENTERWITH, obj));
			Node bodyNode = new Node(Token.WITH, body, lineno);
			result.AddChildrenToBack(bodyNode);
			result.AddChildToBack(new Node(Token.LEAVEWITH));
			return result;
		}

		private Node CreateIf(Node cond, Node ifTrue, Node ifFalse, int lineno)
		{
			int condStatus = IsAlwaysDefinedBoolean(cond);
			if (condStatus == ALWAYS_TRUE_BOOLEAN)
			{
				return ifTrue;
			}
			else
			{
				if (condStatus == ALWAYS_FALSE_BOOLEAN)
				{
					if (ifFalse != null)
					{
						return ifFalse;
					}
					// Replace if (false) xxx by empty block
					return new Node(Token.BLOCK, lineno);
				}
			}
			Node result = new Node(Token.BLOCK, lineno);
			Node ifNotTarget = Node.NewTarget();
			Jump IFNE = new Jump(Token.IFNE, cond);
			IFNE.target = ifNotTarget;
			result.AddChildToBack(IFNE);
			result.AddChildrenToBack(ifTrue);
			if (ifFalse != null)
			{
				Node endTarget = Node.NewTarget();
				result.AddChildToBack(MakeJump(Token.GOTO, endTarget));
				result.AddChildToBack(ifNotTarget);
				result.AddChildrenToBack(ifFalse);
				result.AddChildToBack(endTarget);
			}
			else
			{
				result.AddChildToBack(ifNotTarget);
			}
			return result;
		}

		private Node CreateCondExpr(Node cond, Node ifTrue, Node ifFalse)
		{
			int condStatus = IsAlwaysDefinedBoolean(cond);
			if (condStatus == ALWAYS_TRUE_BOOLEAN)
			{
				return ifTrue;
			}
			else
			{
				if (condStatus == ALWAYS_FALSE_BOOLEAN)
				{
					return ifFalse;
				}
			}
			return new Node(Token.HOOK, cond, ifTrue, ifFalse);
		}

		private Node CreateUnary(int nodeType, Node child)
		{
			int childType = child.GetType();
			switch (nodeType)
			{
				case Token.DELPROP:
				{
					Node n;
					if (childType == Token.NAME)
					{
						// Transform Delete(Name "a")
						//  to Delete(Bind("a"), String("a"))
						child.SetType(Token.BINDNAME);
						Node left = child;
						Node right = Node.NewString(child.GetString());
						n = new Node(nodeType, left, right);
					}
					else
					{
						if (childType == Token.GETPROP || childType == Token.GETELEM)
						{
							Node left = child.GetFirstChild();
							Node right = child.GetLastChild();
							child.RemoveChild(left);
							child.RemoveChild(right);
							n = new Node(nodeType, left, right);
						}
						else
						{
							if (childType == Token.GET_REF)
							{
								Node @ref = child.GetFirstChild();
								child.RemoveChild(@ref);
								n = new Node(Token.DEL_REF, @ref);
							}
							else
							{
								// Always evaluate delete operand, see ES5 11.4.1 & bug #726121
								n = new Node(nodeType, new Node(Token.TRUE), child);
							}
						}
					}
					return n;
				}

				case Token.TYPEOF:
				{
					if (childType == Token.NAME)
					{
						child.SetType(Token.TYPEOFNAME);
						return child;
					}
					break;
				}

				case Token.BITNOT:
				{
					if (childType == Token.NUMBER)
					{
						int value = ScriptRuntime.ToInt32(child.GetDouble());
						child.SetDouble(~value);
						return child;
					}
					break;
				}

				case Token.NEG:
				{
					if (childType == Token.NUMBER)
					{
						child.SetDouble(-child.GetDouble());
						return child;
					}
					break;
				}

				case Token.NOT:
				{
					int status = IsAlwaysDefinedBoolean(child);
					if (status != 0)
					{
						int type;
						if (status == ALWAYS_TRUE_BOOLEAN)
						{
							type = Token.FALSE;
						}
						else
						{
							type = Token.TRUE;
						}
						if (childType == Token.TRUE || childType == Token.FALSE)
						{
							child.SetType(type);
							return child;
						}
						return new Node(type);
					}
					break;
				}
			}
			return new Node(nodeType, child);
		}

		private Node CreateCallOrNew(int nodeType, Node child)
		{
			int type = Node.NON_SPECIALCALL;
			if (child.GetType() == Token.NAME)
			{
				string name = child.GetString();
				if (name.Equals("eval"))
				{
					type = Node.SPECIALCALL_EVAL;
				}
				else
				{
					if (name.Equals("With"))
					{
						type = Node.SPECIALCALL_WITH;
					}
				}
			}
			else
			{
				if (child.GetType() == Token.GETPROP)
				{
					string name = child.GetLastChild().GetString();
					if (name.Equals("eval"))
					{
						type = Node.SPECIALCALL_EVAL;
					}
				}
			}
			Node node = new Node(nodeType, child);
			if (type != Node.NON_SPECIALCALL)
			{
				// Calls to these functions require activation objects.
				SetRequiresActivation();
				node.PutIntProp(Node.SPECIALCALL_PROP, type);
			}
			return node;
		}

		private Node CreateIncDec(int nodeType, bool post, Node child)
		{
			child = MakeReference(child);
			int childType = child.GetType();
			switch (childType)
			{
				case Token.NAME:
				case Token.GETPROP:
				case Token.GETELEM:
				case Token.GET_REF:
				{
					Node n = new Node(nodeType, child);
					int incrDecrMask = 0;
					if (nodeType == Token.DEC)
					{
						incrDecrMask |= Node.DECR_FLAG;
					}
					if (post)
					{
						incrDecrMask |= Node.POST_FLAG;
					}
					n.PutIntProp(Node.INCRDECR_PROP, incrDecrMask);
					return n;
				}
			}
			throw Kit.CodeBug();
		}

		private Node CreatePropertyGet(Node target, string @namespace, string name, int memberTypeFlags)
		{
			if (@namespace == null && memberTypeFlags == 0)
			{
				if (target == null)
				{
					return CreateName(name);
				}
				CheckActivationName(name, Token.GETPROP);
				if (ScriptRuntime.IsSpecialProperty(name))
				{
					Node @ref = new Node(Token.REF_SPECIAL, target);
					@ref.PutProp(Node.NAME_PROP, name);
					return new Node(Token.GET_REF, @ref);
				}
				return new Node(Token.GETPROP, target, Node.NewString(name));
			}
			Node elem = Node.NewString(name);
			memberTypeFlags |= Node.PROPERTY_FLAG;
			return CreateMemberRefGet(target, @namespace, elem, memberTypeFlags);
		}

		/// <param name="target">the node before the LB</param>
		/// <param name="namespace">optional namespace</param>
		/// <param name="elem">the node in the brackets</param>
		/// <param name="memberTypeFlags">E4X flags</param>
		private Node CreateElementGet(Node target, string @namespace, Node elem, int memberTypeFlags)
		{
			// OPT: could optimize to createPropertyGet
			// iff elem is string that can not be number
			if (@namespace == null && memberTypeFlags == 0)
			{
				// stand-alone [aaa] as primary expression is array literal
				// declaration and should not come here!
				if (target == null)
				{
					throw Kit.CodeBug();
				}
				return new Node(Token.GETELEM, target, elem);
			}
			return CreateMemberRefGet(target, @namespace, elem, memberTypeFlags);
		}

		private Node CreateMemberRefGet(Node target, string @namespace, Node elem, int memberTypeFlags)
		{
			Node nsNode = null;
			if (@namespace != null)
			{
				// See 11.1.2 in ECMA 357
				if (@namespace.Equals("*"))
				{
					nsNode = new Node(Token.NULL);
				}
				else
				{
					nsNode = CreateName(@namespace);
				}
			}
			Node @ref;
			if (target == null)
			{
				if (@namespace == null)
				{
					@ref = new Node(Token.REF_NAME, elem);
				}
				else
				{
					@ref = new Node(Token.REF_NS_NAME, nsNode, elem);
				}
			}
			else
			{
				if (@namespace == null)
				{
					@ref = new Node(Token.REF_MEMBER, target, elem);
				}
				else
				{
					@ref = new Node(Token.REF_NS_MEMBER, target, nsNode, elem);
				}
			}
			if (memberTypeFlags != 0)
			{
				@ref.PutIntProp(Node.MEMBER_TYPE_PROP, memberTypeFlags);
			}
			return new Node(Token.GET_REF, @ref);
		}

		private Node CreateBinary(int nodeType, Node left, Node right)
		{
			switch (nodeType)
			{
				case Token.ADD:
				{
					// numerical addition and string concatenation
					if (left.type == Token.STRING)
					{
						string s2;
						if (right.type == Token.STRING)
						{
							s2 = right.GetString();
						}
						else
						{
							if (right.type == Token.NUMBER)
							{
								s2 = ScriptRuntime.NumberToString(right.GetDouble(), 10);
							}
							else
							{
								break;
							}
						}
						string s1 = left.GetString();
						left.SetString(System.String.Concat(s1, s2));
						return left;
					}
					else
					{
						if (left.type == Token.NUMBER)
						{
							if (right.type == Token.NUMBER)
							{
								left.SetDouble(left.GetDouble() + right.GetDouble());
								return left;
							}
							else
							{
								if (right.type == Token.STRING)
								{
									string s1;
									string s2;
									s1 = ScriptRuntime.NumberToString(left.GetDouble(), 10);
									s2 = right.GetString();
									right.SetString(System.String.Concat(s1, s2));
									return right;
								}
							}
						}
					}
					// can't do anything if we don't know  both types - since
					// 0 + object is supposed to call toString on the object and do
					// string concantenation rather than addition
					break;
				}

				case Token.SUB:
				{
					// numerical subtraction
					if (left.type == Token.NUMBER)
					{
						double ld = left.GetDouble();
						if (right.type == Token.NUMBER)
						{
							//both numbers
							left.SetDouble(ld - right.GetDouble());
							return left;
						}
						else
						{
							if (ld == 0.0)
							{
								// first 0: 0-x -> -x
								return new Node(Token.NEG, right);
							}
						}
					}
					else
					{
						if (right.type == Token.NUMBER)
						{
							if (right.GetDouble() == 0.0)
							{
								//second 0: x - 0 -> +x
								// can not make simply x because x - 0 must be number
								return new Node(Token.POS, left);
							}
						}
					}
					break;
				}

				case Token.MUL:
				{
					// numerical multiplication
					if (left.type == Token.NUMBER)
					{
						double ld = left.GetDouble();
						if (right.type == Token.NUMBER)
						{
							//both numbers
							left.SetDouble(ld * right.GetDouble());
							return left;
						}
						else
						{
							if (ld == 1.0)
							{
								// first 1: 1 *  x -> +x
								return new Node(Token.POS, right);
							}
						}
					}
					else
					{
						if (right.type == Token.NUMBER)
						{
							if (right.GetDouble() == 1.0)
							{
								//second 1: x * 1 -> +x
								// can not make simply x because x - 0 must be number
								return new Node(Token.POS, left);
							}
						}
					}
					// can't do x*0: Infinity * 0 gives NaN, not 0
					break;
				}

				case Token.DIV:
				{
					// number division
					if (right.type == Token.NUMBER)
					{
						double rd = right.GetDouble();
						if (left.type == Token.NUMBER)
						{
							// both constants -- just divide, trust Java to handle x/0
							left.SetDouble(left.GetDouble() / rd);
							return left;
						}
						else
						{
							if (rd == 1.0)
							{
								// second 1: x/1 -> +x
								// not simply x to force number convertion
								return new Node(Token.POS, left);
							}
						}
					}
					break;
				}

				case Token.AND:
				{
					// Since x && y gives x, not false, when Boolean(x) is false,
					// and y, not Boolean(y), when Boolean(x) is true, x && y
					// can only be simplified if x is defined. See bug 309957.
					int leftStatus = IsAlwaysDefinedBoolean(left);
					if (leftStatus == ALWAYS_FALSE_BOOLEAN)
					{
						// if the first one is false, just return it
						return left;
					}
					else
					{
						if (leftStatus == ALWAYS_TRUE_BOOLEAN)
						{
							// if first is true, set to second
							return right;
						}
					}
					break;
				}

				case Token.OR:
				{
					// Since x || y gives x, not true, when Boolean(x) is true,
					// and y, not Boolean(y), when Boolean(x) is false, x || y
					// can only be simplified if x is defined. See bug 309957.
					int leftStatus = IsAlwaysDefinedBoolean(left);
					if (leftStatus == ALWAYS_TRUE_BOOLEAN)
					{
						// if the first one is true, just return it
						return left;
					}
					else
					{
						if (leftStatus == ALWAYS_FALSE_BOOLEAN)
						{
							// if first is false, set to second
							return right;
						}
					}
					break;
				}
			}
			return new Node(nodeType, left, right);
		}

		private Node CreateAssignment(int assignType, Node left, Node right)
		{
			Node @ref = MakeReference(left);
			if (@ref == null)
			{
				if (left.GetType() == Token.ARRAYLIT || left.GetType() == Token.OBJECTLIT)
				{
					if (assignType != Token.ASSIGN)
					{
						ReportError("msg.bad.destruct.op");
						return right;
					}
					return CreateDestructuringAssignment(-1, left, right);
				}
				ReportError("msg.bad.assign.left");
				return right;
			}
			left = @ref;
			int assignOp;
			switch (assignType)
			{
				case Token.ASSIGN:
				{
					return SimpleAssignment(left, right);
				}

				case Token.ASSIGN_BITOR:
				{
					assignOp = Token.BITOR;
					break;
				}

				case Token.ASSIGN_BITXOR:
				{
					assignOp = Token.BITXOR;
					break;
				}

				case Token.ASSIGN_BITAND:
				{
					assignOp = Token.BITAND;
					break;
				}

				case Token.ASSIGN_LSH:
				{
					assignOp = Token.LSH;
					break;
				}

				case Token.ASSIGN_RSH:
				{
					assignOp = Token.RSH;
					break;
				}

				case Token.ASSIGN_URSH:
				{
					assignOp = Token.URSH;
					break;
				}

				case Token.ASSIGN_ADD:
				{
					assignOp = Token.ADD;
					break;
				}

				case Token.ASSIGN_SUB:
				{
					assignOp = Token.SUB;
					break;
				}

				case Token.ASSIGN_MUL:
				{
					assignOp = Token.MUL;
					break;
				}

				case Token.ASSIGN_DIV:
				{
					assignOp = Token.DIV;
					break;
				}

				case Token.ASSIGN_MOD:
				{
					assignOp = Token.MOD;
					break;
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
			int nodeType = left.GetType();
			switch (nodeType)
			{
				case Token.NAME:
				{
					Node op = new Node(assignOp, left, right);
					Node lvalueLeft = Node.NewString(Token.BINDNAME, left.GetString());
					return new Node(Token.SETNAME, lvalueLeft, op);
				}

				case Token.GETPROP:
				case Token.GETELEM:
				{
					Node obj = left.GetFirstChild();
					Node id = left.GetLastChild();
					int type = nodeType == Token.GETPROP ? Token.SETPROP_OP : Token.SETELEM_OP;
					Node opLeft = new Node(Token.USE_STACK);
					Node op = new Node(assignOp, opLeft, right);
					return new Node(type, obj, id, op);
				}

				case Token.GET_REF:
				{
					@ref = left.GetFirstChild();
					CheckMutableReference(@ref);
					Node opLeft = new Node(Token.USE_STACK);
					Node op = new Node(assignOp, opLeft, right);
					return new Node(Token.SET_REF_OP, @ref, op);
				}
			}
			throw Kit.CodeBug();
		}

		private Node CreateUseLocal(Node localBlock)
		{
			if (Token.LOCAL_BLOCK != localBlock.GetType())
			{
				throw Kit.CodeBug();
			}
			Node result = new Node(Token.LOCAL_LOAD);
			result.PutProp(Node.LOCAL_BLOCK_PROP, localBlock);
			return result;
		}

		private Jump MakeJump(int type, Node target)
		{
			Jump n = new Jump(type);
			n.target = target;
			return n;
		}

		private Node MakeReference(Node node)
		{
			int type = node.GetType();
			switch (type)
			{
				case Token.NAME:
				case Token.GETPROP:
				case Token.GETELEM:
				case Token.GET_REF:
				{
					return node;
				}

				case Token.CALL:
				{
					node.SetType(Token.REF_CALL);
					return new Node(Token.GET_REF, node);
				}
			}
			// Signal caller to report error
			return null;
		}

		// Check if Node always mean true or false in boolean context
		private static int IsAlwaysDefinedBoolean(Node node)
		{
			switch (node.GetType())
			{
				case Token.FALSE:
				case Token.NULL:
				{
					return ALWAYS_FALSE_BOOLEAN;
				}

				case Token.TRUE:
				{
					return ALWAYS_TRUE_BOOLEAN;
				}

				case Token.NUMBER:
				{
					double num = node.GetDouble();
					if (num == num && num != 0.0)
					{
						return ALWAYS_TRUE_BOOLEAN;
					}
					else
					{
						return ALWAYS_FALSE_BOOLEAN;
					}
				}
			}
			return 0;
		}

		// Check if node is the target of a destructuring bind.
		internal bool IsDestructuring(Node n)
		{
			return n is DestructuringForm && ((DestructuringForm)n).IsDestructuring();
		}

		internal Node DecompileFunctionHeader(FunctionNode fn)
		{
			Node mexpr = null;
			if (fn.GetFunctionName() != null)
			{
				decompiler.AddName(fn.GetName());
			}
			else
			{
				if (fn.GetMemberExprNode() != null)
				{
					mexpr = Transform(fn.GetMemberExprNode());
				}
			}
			decompiler.AddToken(Token.LP);
			IList<AstNode> @params = fn.GetParams();
			for (int i = 0; i < @params.Count; i++)
			{
				Decompile(@params[i]);
				if (i < @params.Count - 1)
				{
					decompiler.AddToken(Token.COMMA);
				}
			}
			decompiler.AddToken(Token.RP);
			if (!fn.IsExpressionClosure())
			{
				decompiler.AddEOL(Token.LC);
			}
			return mexpr;
		}

		internal void Decompile(AstNode node)
		{
			switch (node.GetType())
			{
				case Token.ARRAYLIT:
				{
					DecompileArrayLiteral((ArrayLiteral)node);
					break;
				}

				case Token.OBJECTLIT:
				{
					DecompileObjectLiteral((ObjectLiteral)node);
					break;
				}

				case Token.STRING:
				{
					decompiler.AddString(((StringLiteral)node).GetValue());
					break;
				}

				case Token.NAME:
				{
					decompiler.AddName(((Name)node).GetIdentifier());
					break;
				}

				case Token.NUMBER:
				{
					decompiler.AddNumber(((NumberLiteral)node).GetNumber());
					break;
				}

				case Token.GETPROP:
				{
					DecompilePropertyGet((PropertyGet)node);
					break;
				}

				case Token.EMPTY:
				{
					break;
				}

				case Token.GETELEM:
				{
					DecompileElementGet((ElementGet)node);
					break;
				}

				case Token.THIS:
				{
					decompiler.AddToken(node.GetType());
					break;
				}

				default:
				{
					Kit.CodeBug("unexpected token: " + Token.TypeToName(node.GetType()));
					break;
				}
			}
		}

		// used for destructuring forms, since we don't transform() them
		internal void DecompileArrayLiteral(ArrayLiteral node)
		{
			decompiler.AddToken(Token.LB);
			IList<AstNode> elems = node.GetElements();
			int size = elems.Count;
			for (int i = 0; i < size; i++)
			{
				AstNode elem = elems[i];
				Decompile(elem);
				if (i < size - 1)
				{
					decompiler.AddToken(Token.COMMA);
				}
			}
			decompiler.AddToken(Token.RB);
		}

		// only used for destructuring forms
		internal void DecompileObjectLiteral(ObjectLiteral node)
		{
			decompiler.AddToken(Token.LC);
			IList<ObjectProperty> props = node.GetElements();
			int size = props.Count;
			for (int i = 0; i < size; i++)
			{
				ObjectProperty prop = props[i];
				bool destructuringShorthand = true.Equals(prop.GetProp(Node.DESTRUCTURING_SHORTHAND));
				Decompile(prop.GetLeft());
				if (!destructuringShorthand)
				{
					decompiler.AddToken(Token.COLON);
					Decompile(prop.GetRight());
				}
				if (i < size - 1)
				{
					decompiler.AddToken(Token.COMMA);
				}
			}
			decompiler.AddToken(Token.RC);
		}

		// only used for destructuring forms
		internal void DecompilePropertyGet(PropertyGet node)
		{
			Decompile(node.GetTarget());
			decompiler.AddToken(Token.DOT);
			Decompile(node.GetProperty());
		}

		// only used for destructuring forms
		internal void DecompileElementGet(ElementGet node)
		{
			Decompile(node.GetTarget());
			decompiler.AddToken(Token.LB);
			Decompile(node.GetElement());
			decompiler.AddToken(Token.RB);
		}
	}
}
