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
using Rhino.Tests;
using Rhino.Tests.Ast;
using Rhino.Tests.Testing;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ParserTest
	{
		internal CompilerEnvirons environment;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			environment = new CompilerEnvirons();
		}

		[NUnit.Framework.Test]
		public virtual void TestAutoSemiColonBetweenNames()
		{
			AstRoot root = Parse("\nx\ny\nz\n");
			AstNode first = ((ExpressionStatement)root.GetFirstChild()).GetExpression();
			NUnit.Framework.Assert.AreEqual("x", first.GetString());
			AstNode second = ((ExpressionStatement)root.GetFirstChild().GetNext()).GetExpression();
			NUnit.Framework.Assert.AreEqual("y", second.GetString());
			AstNode third = ((ExpressionStatement)root.GetFirstChild().GetNext().GetNext()).GetExpression();
			NUnit.Framework.Assert.AreEqual("z", third.GetString());
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestParseAutoSemiColonBeforeNewlineAndComments()
		{
			AstRoot root = ParseAsReader("var s = 3\n" + "/* */var t = 1;");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual("var s = 3;\nvar t = 1;\n", root.ToSource());
		}

		[NUnit.Framework.Test]
		public virtual void TestAutoSemiBeforeComment1()
		{
			Parse("var a = 1\n/** a */ var b = 2");
		}

		[NUnit.Framework.Test]
		public virtual void TestAutoSemiBeforeComment2()
		{
			Parse("var a = 1\n/** a */\n var b = 2");
		}

		[NUnit.Framework.Test]
		public virtual void TestAutoSemiBeforeComment3()
		{
			Parse("var a = 1\n/** a */\n /** b */ var b = 2");
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoAssign()
		{
			AstRoot root = Parse("\n\na = b");
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			AstNode n = st.GetExpression();
			NUnit.Framework.Assert.IsTrue(n is Assignment);
			NUnit.Framework.Assert.AreEqual(Token.ASSIGN, n.GetType());
			NUnit.Framework.Assert.AreEqual(2, n.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoCall()
		{
			AstRoot root = Parse("\nfoo(123);");
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			AstNode n = st.GetExpression();
			NUnit.Framework.Assert.IsTrue(n is FunctionCall);
			NUnit.Framework.Assert.AreEqual(Token.CALL, n.GetType());
			NUnit.Framework.Assert.AreEqual(1, n.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoGetProp()
		{
			AstRoot root = Parse("\nfoo.bar");
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			AstNode n = st.GetExpression();
			NUnit.Framework.Assert.IsTrue(n is PropertyGet);
			NUnit.Framework.Assert.AreEqual(Token.GETPROP, n.GetType());
			NUnit.Framework.Assert.AreEqual(1, n.GetLineno());
			PropertyGet getprop = (PropertyGet)n;
			AstNode m = getprop.GetRight();
			NUnit.Framework.Assert.IsTrue(m is Name);
			NUnit.Framework.Assert.AreEqual(Token.NAME, m.GetType());
			// used to be Token.STRING!
			NUnit.Framework.Assert.AreEqual(1, m.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoGetElem()
		{
			AstRoot root = Parse("\nfoo[123]");
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			AstNode n = st.GetExpression();
			NUnit.Framework.Assert.IsTrue(n is ElementGet);
			NUnit.Framework.Assert.AreEqual(Token.GETELEM, n.GetType());
			NUnit.Framework.Assert.AreEqual(1, n.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoComment()
		{
			AstRoot root = Parse("\n/** a */");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoComment2()
		{
			AstRoot root = Parse("\n/**\n\n a */");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoComment3()
		{
			AstRoot root = Parse("\n  \n\n/**\n\n a */");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(3, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoComment4()
		{
			AstRoot root = Parse("\n  \n\n  /**\n\n a */");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(3, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLineComment5()
		{
			AstRoot root = Parse("  /**\n* a.\n* b.\n* c.*/\n");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(0, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLineComment6()
		{
			AstRoot root = Parse("  \n/**\n* a.\n* b.\n* c.*/\n");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoComment7()
		{
			AstRoot root = Parse("var x;\n/**\n\n a */");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoComment8()
		{
			AstRoot root = Parse("\nvar x;/**\n\n a */");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().First().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoLiteral()
		{
			AstRoot root = Parse("\nvar d =\n" + "    \"foo\";\n" + "var e =\n" + "    1;\n" + "var f = \n" + "    1.2;\n" + "var g = \n" + "    2e5;\n" + "var h = \n" + "    'bar';\n");
			VariableDeclaration stmt1 = (VariableDeclaration)root.GetFirstChild();
			IList<VariableInitializer> vars1 = stmt1.GetVariables();
			VariableInitializer firstVar = vars1[0];
			Name firstVarName = (Name)firstVar.GetTarget();
			AstNode firstVarLiteral = firstVar.GetInitializer();
			VariableDeclaration stmt2 = (VariableDeclaration)stmt1.GetNext();
			IList<VariableInitializer> vars2 = stmt2.GetVariables();
			VariableInitializer secondVar = vars2[0];
			Name secondVarName = (Name)secondVar.GetTarget();
			AstNode secondVarLiteral = secondVar.GetInitializer();
			VariableDeclaration stmt3 = (VariableDeclaration)stmt2.GetNext();
			IList<VariableInitializer> vars3 = stmt3.GetVariables();
			VariableInitializer thirdVar = vars3[0];
			Name thirdVarName = (Name)thirdVar.GetTarget();
			AstNode thirdVarLiteral = thirdVar.GetInitializer();
			VariableDeclaration stmt4 = (VariableDeclaration)stmt3.GetNext();
			IList<VariableInitializer> vars4 = stmt4.GetVariables();
			VariableInitializer fourthVar = vars4[0];
			Name fourthVarName = (Name)fourthVar.GetTarget();
			AstNode fourthVarLiteral = fourthVar.GetInitializer();
			VariableDeclaration stmt5 = (VariableDeclaration)stmt4.GetNext();
			IList<VariableInitializer> vars5 = stmt5.GetVariables();
			VariableInitializer fifthVar = vars5[0];
			Name fifthVarName = (Name)fifthVar.GetTarget();
			AstNode fifthVarLiteral = fifthVar.GetInitializer();
			NUnit.Framework.Assert.AreEqual(2, firstVarLiteral.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, secondVarLiteral.GetLineno());
			NUnit.Framework.Assert.AreEqual(6, thirdVarLiteral.GetLineno());
			NUnit.Framework.Assert.AreEqual(8, fourthVarLiteral.GetLineno());
			NUnit.Framework.Assert.AreEqual(10, fifthVarLiteral.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoSwitch()
		{
			AstRoot root = Parse("\nswitch (a) {\n" + "   case\n" + "     1:\n" + "     b++;\n" + "   case 2:\n" + "   default:\n" + "     b--;\n" + "  }\n");
			SwitchStatement switchStmt = (SwitchStatement)root.GetFirstChild();
			AstNode switchVar = switchStmt.GetExpression();
			IList<SwitchCase> cases = switchStmt.GetCases();
			SwitchCase firstCase = cases[0];
			AstNode caseArg = firstCase.GetExpression();
			IList<AstNode> caseBody = firstCase.GetStatements();
			ExpressionStatement exprStmt = (ExpressionStatement)caseBody[0];
			UnaryExpression incrExpr = (UnaryExpression)exprStmt.GetExpression();
			AstNode incrVar = incrExpr.GetOperand();
			SwitchCase secondCase = cases[1];
			AstNode defaultCase = cases[2];
			AstNode returnStmt = (AstNode)switchStmt.GetNext();
			NUnit.Framework.Assert.AreEqual(1, switchStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, switchVar.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, firstCase.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, caseArg.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, exprStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, incrExpr.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, incrVar.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, secondCase.GetLineno());
			NUnit.Framework.Assert.AreEqual(6, defaultCase.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoFunctionParams()
		{
			AstRoot root = Parse("\nfunction\n" + "    foo(\n" + "    a,\n" + "    b,\n" + "    c) {\n" + "}\n");
			FunctionNode function = (FunctionNode)root.GetFirstChild();
			Name functionName = function.GetFunctionName();
			AstNode body = function.GetBody();
			IList<AstNode> @params = function.GetParams();
			AstNode param1 = @params[0];
			AstNode param2 = @params[1];
			AstNode param3 = @params[2];
			NUnit.Framework.Assert.AreEqual(1, function.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, functionName.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, param1.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, param2.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, param3.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, body.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoVarDecl()
		{
			AstRoot root = Parse("\nvar\n" + "    a =\n" + "    3\n");
			VariableDeclaration decl = (VariableDeclaration)root.GetFirstChild();
			IList<VariableInitializer> vars = decl.GetVariables();
			VariableInitializer init = vars[0];
			AstNode declName = init.GetTarget();
			AstNode expr = init.GetInitializer();
			NUnit.Framework.Assert.AreEqual(1, decl.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, init.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, declName.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, expr.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoReturn()
		{
			AstRoot root = Parse("\nfunction\n" + "    foo(\n" + "    a,\n" + "    b,\n" + "    c) {\n" + "    return\n" + "    4;\n" + "}\n");
			FunctionNode function = (FunctionNode)root.GetFirstChild();
			Name functionName = function.GetFunctionName();
			AstNode body = function.GetBody();
			ReturnStatement returnStmt = (ReturnStatement)body.GetFirstChild();
			ExpressionStatement exprStmt = (ExpressionStatement)returnStmt.GetNext();
			AstNode returnVal = exprStmt.GetExpression();
			NUnit.Framework.Assert.AreEqual(6, returnStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, exprStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, returnVal.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoFor()
		{
			AstRoot root = Parse("\nfor(\n" + ";\n" + ";\n" + ") {\n" + "}\n");
			ForLoop forLoop = (ForLoop)root.GetFirstChild();
			AstNode initClause = forLoop.GetInitializer();
			AstNode condClause = forLoop.GetCondition();
			AstNode incrClause = forLoop.GetIncrement();
			NUnit.Framework.Assert.AreEqual(1, forLoop.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, initClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, condClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, incrClause.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoInfix()
		{
			AstRoot root = Parse("\nvar d = a\n" + "    + \n" + "    b;\n" + "var\n" + "    e =\n" + "    a +\n" + "    c;\n" + "var f = b\n" + "    / c;\n");
			VariableDeclaration stmt1 = (VariableDeclaration)root.GetFirstChild();
			IList<VariableInitializer> vars1 = stmt1.GetVariables();
			VariableInitializer var1 = vars1[0];
			Name firstVarName = (Name)var1.GetTarget();
			InfixExpression var1Add = (InfixExpression)var1.GetInitializer();
			VariableDeclaration stmt2 = (VariableDeclaration)stmt1.GetNext();
			IList<VariableInitializer> vars2 = stmt2.GetVariables();
			VariableInitializer var2 = vars2[0];
			Name secondVarName = (Name)var2.GetTarget();
			InfixExpression var2Add = (InfixExpression)var2.GetInitializer();
			VariableDeclaration stmt3 = (VariableDeclaration)stmt2.GetNext();
			IList<VariableInitializer> vars3 = stmt3.GetVariables();
			VariableInitializer var3 = vars3[0];
			Name thirdVarName = (Name)var3.GetTarget();
			InfixExpression thirdVarDiv = (InfixExpression)var3.GetInitializer();
			ReturnStatement returnStmt = (ReturnStatement)stmt3.GetNext();
			NUnit.Framework.Assert.AreEqual(1, var1.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, firstVarName.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, var1Add.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, var1Add.GetLeft().GetLineno());
			NUnit.Framework.Assert.AreEqual(3, var1Add.GetRight().GetLineno());
			// var directive with name on next line wrong --
			// should be 6.
			NUnit.Framework.Assert.AreEqual(5, var2.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, secondVarName.GetLineno());
			NUnit.Framework.Assert.AreEqual(6, var2Add.GetLineno());
			NUnit.Framework.Assert.AreEqual(6, var2Add.GetLeft().GetLineno());
			NUnit.Framework.Assert.AreEqual(7, var2Add.GetRight().GetLineno());
			NUnit.Framework.Assert.AreEqual(8, var3.GetLineno());
			NUnit.Framework.Assert.AreEqual(8, thirdVarName.GetLineno());
			NUnit.Framework.Assert.AreEqual(8, thirdVarDiv.GetLineno());
			NUnit.Framework.Assert.AreEqual(8, thirdVarDiv.GetLeft().GetLineno());
			NUnit.Framework.Assert.AreEqual(9, thirdVarDiv.GetRight().GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoPrefix()
		{
			AstRoot root = Parse("\na++;\n" + "   --\n" + "   b;\n");
			ExpressionStatement first = (ExpressionStatement)root.GetFirstChild();
			ExpressionStatement secondStmt = (ExpressionStatement)first.GetNext();
			UnaryExpression firstOp = (UnaryExpression)first.GetExpression();
			UnaryExpression secondOp = (UnaryExpression)secondStmt.GetExpression();
			AstNode firstVarRef = firstOp.GetOperand();
			AstNode secondVarRef = secondOp.GetOperand();
			NUnit.Framework.Assert.AreEqual(1, firstOp.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, secondOp.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, firstVarRef.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, secondVarRef.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoIf()
		{
			AstRoot root = Parse("\nif\n" + "   (a == 3)\n" + "   {\n" + "     b = 0;\n" + "   }\n" + "     else\n" + "   {\n" + "     c = 1;\n" + "   }\n");
			IfStatement ifStmt = (IfStatement)root.GetFirstChild();
			AstNode condClause = ifStmt.GetCondition();
			AstNode thenClause = ifStmt.GetThenPart();
			AstNode elseClause = ifStmt.GetElsePart();
			NUnit.Framework.Assert.AreEqual(1, ifStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, condClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, thenClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, elseClause.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoTry()
		{
			AstRoot root = Parse("\ntry {\n" + "    var x = 1;\n" + "} catch\n" + "    (err)\n" + "{\n" + "} finally {\n" + "    var y = 2;\n" + "}\n");
			TryStatement tryStmt = (TryStatement)root.GetFirstChild();
			AstNode tryBlock = tryStmt.GetTryBlock();
			IList<CatchClause> catchBlocks = tryStmt.GetCatchClauses();
			CatchClause catchClause = catchBlocks[0];
			Block catchVarBlock = catchClause.GetBody();
			Name catchVar = catchClause.GetVarName();
			AstNode finallyBlock = tryStmt.GetFinallyBlock();
			AstNode finallyStmt = (AstNode)finallyBlock.GetFirstChild();
			NUnit.Framework.Assert.AreEqual(1, tryStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, tryBlock.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, catchVarBlock.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, catchVar.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, catchClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(6, finallyBlock.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, finallyStmt.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoConditional()
		{
			AstRoot root = Parse("\na\n" + "    ?\n" + "    b\n" + "    :\n" + "    c\n" + "    ;\n");
			ExpressionStatement ex = (ExpressionStatement)root.GetFirstChild();
			ConditionalExpression hook = (ConditionalExpression)ex.GetExpression();
			AstNode condExpr = hook.GetTestExpression();
			AstNode thenExpr = hook.GetTrueExpression();
			AstNode elseExpr = hook.GetFalseExpression();
			NUnit.Framework.Assert.AreEqual(2, hook.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, condExpr.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, thenExpr.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, elseExpr.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoLabel()
		{
			AstRoot root = Parse("\nfoo:\n" + "a = 1;\n" + "bar:\n" + "b = 2;\n");
			LabeledStatement firstStmt = (LabeledStatement)root.GetFirstChild();
			LabeledStatement secondStmt = (LabeledStatement)firstStmt.GetNext();
			NUnit.Framework.Assert.AreEqual(1, firstStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, secondStmt.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoCompare()
		{
			AstRoot root = Parse("\na\n" + "<\n" + "b\n");
			ExpressionStatement expr = (ExpressionStatement)root.GetFirstChild();
			InfixExpression compare = (InfixExpression)expr.GetExpression();
			AstNode lhs = compare.GetLeft();
			AstNode rhs = compare.GetRight();
			NUnit.Framework.Assert.AreEqual(1, lhs.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, compare.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, rhs.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoEq()
		{
			AstRoot root = Parse("\na\n" + "==\n" + "b\n");
			ExpressionStatement expr = (ExpressionStatement)root.GetFirstChild();
			InfixExpression compare = (InfixExpression)expr.GetExpression();
			AstNode lhs = compare.GetLeft();
			AstNode rhs = compare.GetRight();
			NUnit.Framework.Assert.AreEqual(1, lhs.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, compare.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, rhs.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoPlusEq()
		{
			AstRoot root = Parse("\na\n" + "+=\n" + "b\n");
			ExpressionStatement expr = (ExpressionStatement)root.GetFirstChild();
			Assignment assign = (Assignment)expr.GetExpression();
			AstNode lhs = assign.GetLeft();
			AstNode rhs = assign.GetRight();
			NUnit.Framework.Assert.AreEqual(1, lhs.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, assign.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, rhs.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoComma()
		{
			AstRoot root = Parse("\na,\n" + "    b,\n" + "    c;\n");
			ExpressionStatement stmt = (ExpressionStatement)root.GetFirstChild();
			InfixExpression comma1 = (InfixExpression)stmt.GetExpression();
			InfixExpression comma2 = (InfixExpression)comma1.GetLeft();
			AstNode cRef = comma1.GetRight();
			AstNode aRef = comma2.GetLeft();
			AstNode bRef = comma2.GetRight();
			NUnit.Framework.Assert.AreEqual(1, comma1.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, comma2.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, aRef.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, bRef.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, cRef.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestRegexpLocation()
		{
			AstNode root = Parse("\nvar path =\n" + "      replace(\n" + "/a/g," + "'/');\n");
			VariableDeclaration firstVarDecl = (VariableDeclaration)root.GetFirstChild();
			IList<VariableInitializer> vars1 = firstVarDecl.GetVariables();
			VariableInitializer firstInitializer = vars1[0];
			Name firstVarName = (Name)firstInitializer.GetTarget();
			FunctionCall callNode = (FunctionCall)firstInitializer.GetInitializer();
			AstNode fnName = callNode.GetTarget();
			IList<AstNode> args = callNode.GetArguments();
			RegExpLiteral regexObject = (RegExpLiteral)args[0];
			AstNode aString = args[1];
			NUnit.Framework.Assert.AreEqual(1, firstVarDecl.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, firstVarName.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, callNode.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, fnName.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, regexObject.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, aString.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestNestedOr()
		{
			AstNode root = Parse("\nif (a && \n" + "    b() || \n" + "    /* comment */\n" + "    c) {\n" + "}\n");
			IfStatement ifStmt = (IfStatement)root.GetFirstChild();
			InfixExpression orClause = (InfixExpression)ifStmt.GetCondition();
			InfixExpression andClause = (InfixExpression)orClause.GetLeft();
			AstNode cName = orClause.GetRight();
			NUnit.Framework.Assert.AreEqual(1, ifStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, orClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, andClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, cName.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestObjectLitGetterAndSetter()
		{
			AstNode root = Parse("'use strict';\n" + "function App() {}\n" + "App.prototype = {\n" + "  get appData() { return this.appData_; },\n" + "  set appData(data) { this.appData_ = data; }\n" + "};");
			NUnit.Framework.Assert.IsNotNull(root);
		}

		[NUnit.Framework.Test]
		public virtual void TestObjectLitLocation()
		{
			AstNode root = Parse("\nvar foo =\n" + "{ \n" + "'A' : 'A', \n" + "'B' : 'B', \n" + "'C' : \n" + "      'C' \n" + "};\n");
			VariableDeclaration firstVarDecl = (VariableDeclaration)root.GetFirstChild();
			IList<VariableInitializer> vars1 = firstVarDecl.GetVariables();
			VariableInitializer firstInitializer = vars1[0];
			Name firstVarName = (Name)firstInitializer.GetTarget();
			ObjectLiteral objectLiteral = (ObjectLiteral)firstInitializer.GetInitializer();
			IList<ObjectProperty> props = objectLiteral.GetElements();
			ObjectProperty firstObjectLit = props[0];
			ObjectProperty secondObjectLit = props[1];
			ObjectProperty thirdObjectLit = props[2];
			AstNode firstKey = firstObjectLit.GetLeft();
			AstNode firstValue = firstObjectLit.GetRight();
			AstNode secondKey = secondObjectLit.GetLeft();
			AstNode secondValue = secondObjectLit.GetRight();
			AstNode thirdKey = thirdObjectLit.GetLeft();
			AstNode thirdValue = thirdObjectLit.GetRight();
			NUnit.Framework.Assert.AreEqual(1, firstVarName.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, objectLiteral.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, firstObjectLit.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, firstKey.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, firstValue.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, secondKey.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, secondValue.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, thirdKey.GetLineno());
			NUnit.Framework.Assert.AreEqual(6, thirdValue.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestTryWithoutCatchLocation()
		{
			AstNode root = Parse("\ntry {\n" + "  var x = 1;\n" + "} finally {\n" + "  var y = 2;\n" + "}\n");
			TryStatement tryStmt = (TryStatement)root.GetFirstChild();
			AstNode tryBlock = tryStmt.GetTryBlock();
			IList<CatchClause> catchBlocks = tryStmt.GetCatchClauses();
			Scope finallyBlock = (Scope)tryStmt.GetFinallyBlock();
			AstNode finallyStmt = (AstNode)finallyBlock.GetFirstChild();
			NUnit.Framework.Assert.AreEqual(1, tryStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, tryBlock.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, finallyBlock.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, finallyStmt.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestTryWithoutFinallyLocation()
		{
			AstNode root = Parse("\ntry {\n" + "  var x = 1;\n" + "} catch (ex) {\n" + "  var y = 2;\n" + "}\n");
			TryStatement tryStmt = (TryStatement)root.GetFirstChild();
			Scope tryBlock = (Scope)tryStmt.GetTryBlock();
			IList<CatchClause> catchBlocks = tryStmt.GetCatchClauses();
			CatchClause catchClause = catchBlocks[0];
			AstNode catchStmt = catchClause.GetBody();
			AstNode exceptionVar = catchClause.GetVarName();
			AstNode varDecl = (AstNode)catchStmt.GetFirstChild();
			NUnit.Framework.Assert.AreEqual(1, tryStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, tryBlock.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, catchClause.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, catchStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, exceptionVar.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, varDecl.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoMultilineEq()
		{
			AstRoot root = Parse("\nif\n" + "    (((a == \n" + "  3) && \n" + "  (b == 2)) || \n" + " (c == 1)) {\n" + "}\n");
			IfStatement ifStmt = (IfStatement)root.GetFirstChild();
			InfixExpression orTest = (InfixExpression)ifStmt.GetCondition();
			ParenthesizedExpression cTestParen = (ParenthesizedExpression)orTest.GetRight();
			InfixExpression cTest = (InfixExpression)cTestParen.GetExpression();
			ParenthesizedExpression andTestParen = (ParenthesizedExpression)orTest.GetLeft();
			InfixExpression andTest = (InfixExpression)andTestParen.GetExpression();
			AstNode aTest = andTest.GetLeft();
			AstNode bTest = andTest.GetRight();
			NUnit.Framework.Assert.AreEqual(1, ifStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, orTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, andTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, aTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(4, bTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, cTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, cTestParen.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, andTestParen.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoMultilineBitTest()
		{
			AstRoot root = Parse("\nif (\n" + "      ((a \n" + "        | 3 \n" + "       ) == \n" + "       (b \n" + "        & 2)) && \n" + "      ((a \n" + "         ^ 0xffff) \n" + "       != \n" + "       (c \n" + "        << 1))) {\n" + "}\n");
			IfStatement ifStmt = (IfStatement)root.GetFirstChild();
			InfixExpression andTest = (InfixExpression)ifStmt.GetCondition();
			ParenthesizedExpression bigLHSExpr = (ParenthesizedExpression)andTest.GetLeft();
			ParenthesizedExpression bigRHSExpr = (ParenthesizedExpression)andTest.GetRight();
			InfixExpression eqTest = (InfixExpression)bigLHSExpr.GetExpression();
			InfixExpression notEqTest = (InfixExpression)bigRHSExpr.GetExpression();
			ParenthesizedExpression test1Expr = (ParenthesizedExpression)eqTest.GetLeft();
			ParenthesizedExpression test2Expr = (ParenthesizedExpression)eqTest.GetRight();
			ParenthesizedExpression test3Expr = (ParenthesizedExpression)notEqTest.GetLeft();
			ParenthesizedExpression test4Expr = (ParenthesizedExpression)notEqTest.GetRight();
			InfixExpression bitOrTest = (InfixExpression)test1Expr.GetExpression();
			InfixExpression bitAndTest = (InfixExpression)test2Expr.GetExpression();
			InfixExpression bitXorTest = (InfixExpression)test3Expr.GetExpression();
			InfixExpression bitShiftTest = (InfixExpression)test4Expr.GetExpression();
			NUnit.Framework.Assert.AreEqual(1, ifStmt.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, bigLHSExpr.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, bigRHSExpr.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, eqTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, notEqTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, test1Expr.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, test2Expr.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, test3Expr.GetLineno());
			NUnit.Framework.Assert.AreEqual(10, test4Expr.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, bitOrTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(5, bitAndTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(7, bitXorTest.GetLineno());
			NUnit.Framework.Assert.AreEqual(10, bitShiftTest.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoFunctionCall()
		{
			AstNode root = Parse("\nfoo.\n" + "bar.\n" + "baz(1);");
			ExpressionStatement stmt = (ExpressionStatement)root.GetFirstChild();
			FunctionCall fc = (FunctionCall)stmt.GetExpression();
			// Line number should get closest to the actual paren.
			NUnit.Framework.Assert.AreEqual(3, fc.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoName()
		{
			AstNode root = Parse("\na;\n" + "b.\n" + "c;\n");
			ExpressionStatement exprStmt = (ExpressionStatement)root.GetFirstChild();
			AstNode aRef = exprStmt.GetExpression();
			ExpressionStatement bExprStmt = (ExpressionStatement)exprStmt.GetNext();
			AstNode bRef = bExprStmt.GetExpression();
			NUnit.Framework.Assert.AreEqual(1, aRef.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, bRef.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestLinenoDeclaration()
		{
			AstNode root = Parse("\na.\n" + "b=\n" + "function() {};\n");
			ExpressionStatement exprStmt = (ExpressionStatement)root.GetFirstChild();
			Assignment fnAssignment = (Assignment)exprStmt.GetExpression();
			PropertyGet aDotbName = (PropertyGet)fnAssignment.GetLeft();
			AstNode aName = aDotbName.GetLeft();
			AstNode bName = aDotbName.GetRight();
			FunctionNode fnNode = (FunctionNode)fnAssignment.GetRight();
			NUnit.Framework.Assert.AreEqual(1, fnAssignment.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, aDotbName.GetLineno());
			NUnit.Framework.Assert.AreEqual(1, aName.GetLineno());
			NUnit.Framework.Assert.AreEqual(2, bName.GetLineno());
			NUnit.Framework.Assert.AreEqual(3, fnNode.GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestInOperatorInForLoop1()
		{
			Parse("var a={};function b_(p){ return p;};" + "for(var i=b_(\"length\" in a);i<0;) {}");
		}

		[NUnit.Framework.Test]
		public virtual void TestInOperatorInForLoop2()
		{
			Parse("var a={}; for (;(\"length\" in a);) {}");
		}

		[NUnit.Framework.Test]
		public virtual void TestInOperatorInForLoop3()
		{
			Parse("for (x in y) {}");
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment1()
		{
			AstRoot root = Parse("/** @type number */var a;");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual("/** @type number */", root.GetComments().First().GetValue());
			NUnit.Framework.Assert.IsNotNull(root.GetFirstChild().GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment2()
		{
			AstRoot root = Parse("/** @type number */a.b;");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual("/** @type number */", root.GetComments().First().GetValue());
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			NUnit.Framework.Assert.IsNotNull(st.GetExpression().GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment3()
		{
			AstRoot root = Parse("var a = /** @type number */(x);");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual("/** @type number */", root.GetComments().First().GetValue());
			VariableDeclaration vd = (VariableDeclaration)root.GetFirstChild();
			VariableInitializer vi = vd.GetVariables()[0];
			NUnit.Framework.Assert.IsNotNull(vi.GetInitializer().GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment4()
		{
			AstRoot root = Parse("(function() {/** should not be attached */})()");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			FunctionCall fc = (FunctionCall)st.GetExpression();
			ParenthesizedExpression pe = (ParenthesizedExpression)fc.GetTarget();
			NUnit.Framework.Assert.IsNull(pe.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment5()
		{
			AstRoot root = Parse("({/** attach me */ 1: 2});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			NumberLiteral number = (NumberLiteral)lit.GetElements()[0].GetLeft();
			NUnit.Framework.Assert.IsNotNull(number.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment6()
		{
			AstRoot root = Parse("({1: /** don't attach me */ 2, 3: 4});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			foreach (ObjectProperty el in lit.GetElements())
			{
				NUnit.Framework.Assert.IsNull(el.GetLeft().GetJsDoc());
				NUnit.Framework.Assert.IsNull(el.GetRight().GetJsDoc());
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment7()
		{
			AstRoot root = Parse("({/** attach me */ '1': 2});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			StringLiteral stringLit = (StringLiteral)lit.GetElements()[0].GetLeft();
			NUnit.Framework.Assert.IsNotNull(stringLit.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment8()
		{
			AstRoot root = Parse("({'1': /** attach me */ (foo())});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			ParenthesizedExpression parens = (ParenthesizedExpression)lit.GetElements()[0].GetRight();
			NUnit.Framework.Assert.IsNotNull(parens.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment9()
		{
			AstRoot root = Parse("({/** attach me */ foo: 2});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			Name objLitKey = (Name)lit.GetElements()[0].GetLeft();
			NUnit.Framework.Assert.IsNotNull(objLitKey.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment10()
		{
			AstRoot root = Parse("({foo: /** attach me */ (bar)});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			ParenthesizedExpression parens = (ParenthesizedExpression)lit.GetElements()[0].GetRight();
			NUnit.Framework.Assert.IsNotNull(parens.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment11()
		{
			AstRoot root = Parse("({/** attach me */ get foo() {}});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			Name objLitKey = (Name)lit.GetElements()[0].GetLeft();
			NUnit.Framework.Assert.IsNotNull(objLitKey.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment12()
		{
			AstRoot root = Parse("({/** attach me */ get 1() {}});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			NumberLiteral number = (NumberLiteral)lit.GetElements()[0].GetLeft();
			NUnit.Framework.Assert.IsNotNull(number.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment13()
		{
			AstRoot root = Parse("({/** attach me */ get 'foo'() {}});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			ParenthesizedExpression pt = (ParenthesizedExpression)st.GetExpression();
			ObjectLiteral lit = (ObjectLiteral)pt.GetExpression();
			StringLiteral stringLit = (StringLiteral)lit.GetElements()[0].GetLeft();
			NUnit.Framework.Assert.IsNotNull(stringLit.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment14()
		{
			AstRoot root = Parse("var a = (/** @type {!Foo} */ {});");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual("/** @type {!Foo} */", root.GetComments().First().GetValue());
			VariableDeclaration vd = (VariableDeclaration)root.GetFirstChild();
			VariableInitializer vi = vd.GetVariables()[0];
			NUnit.Framework.Assert.IsNotNull(((ParenthesizedExpression)vi.GetInitializer()).GetExpression().GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment15()
		{
			AstRoot root = Parse("/** @private */ x(); function f() {}");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			NUnit.Framework.Assert.IsNotNull(st.GetExpression().GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestJSDocAttachment16()
		{
			AstRoot root = Parse("/** @suppress {with} */ with (context) {\n" + "  eval('[' + expr + ']');\n" + "}\n");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			WithStatement st = (WithStatement)root.GetFirstChild();
			NUnit.Framework.Assert.IsNotNull(st.GetJsDoc());
		}

		[NUnit.Framework.Test]
		public virtual void TestParsingWithoutJSDoc()
		{
			AstRoot root = Parse("var a = /** @type number */(x);", false);
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(1, root.GetComments().Count);
			NUnit.Framework.Assert.AreEqual("/** @type number */", root.GetComments().First().GetValue());
			VariableDeclaration vd = (VariableDeclaration)root.GetFirstChild();
			VariableInitializer vi = vd.GetVariables()[0];
			NUnit.Framework.Assert.IsTrue(vi.GetInitializer() is ParenthesizedExpression);
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestParseCommentsAsReader()
		{
			AstRoot root = ParseAsReader("/** a */var a;\n /** b */var b; /** c */var c;");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(3, root.GetComments().Count);
			Comment[] comments = new Comment[3];
			comments = Sharpen.Collections.ToArray(root.GetComments(), comments);
			NUnit.Framework.Assert.AreEqual("/** a */", comments[0].GetValue());
			NUnit.Framework.Assert.AreEqual("/** b */", comments[1].GetValue());
			NUnit.Framework.Assert.AreEqual("/** c */", comments[2].GetValue());
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestParseCommentsAsReader2()
		{
			string js = string.Empty;
			for (int i = 0; i < 100; i++)
			{
				string stri = Sharpen.Extensions.ToString(i);
				js += "/** Some comment for a" + stri + " */" + "var a" + stri + " = " + stri + ";\n";
			}
			AstRoot root = ParseAsReader(js);
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestLinenoCommentsWithJSDoc()
		{
			AstRoot root = ParseAsReader("/* foo \n" + " bar \n" + "*/\n" + "/** @param {string} x */\n" + "function a(x) {};\n");
			NUnit.Framework.Assert.IsNotNull(root.GetComments());
			NUnit.Framework.Assert.AreEqual(2, root.GetComments().Count);
			Comment[] comments = new Comment[2];
			comments = Sharpen.Collections.ToArray(root.GetComments(), comments);
			NUnit.Framework.Assert.AreEqual(0, comments[0].GetLineno());
			NUnit.Framework.Assert.AreEqual(3, comments[1].GetLineno());
		}

		[NUnit.Framework.Test]
		public virtual void TestParseUnicodeFormatStringLiteral()
		{
			AstRoot root = Parse("'A\u200DB'");
			ExpressionStatement st = (ExpressionStatement)root.GetFirstChild();
			StringLiteral stringLit = (StringLiteral)st.GetExpression();
			NUnit.Framework.Assert.AreEqual("A\u200DB", stringLit.GetValue());
		}

		[NUnit.Framework.Test]
		public virtual void TestParseUnicodeFormatName()
		{
			AstRoot root = Parse("A\u200DB");
			AstNode first = ((ExpressionStatement)root.GetFirstChild()).GetExpression();
			NUnit.Framework.Assert.AreEqual("AB", first.GetString());
		}

		[NUnit.Framework.Test]
		public virtual void TestParseUnicodeReservedKeywords1()
		{
			AstRoot root = Parse("\\u0069\\u0066");
			AstNode first = ((ExpressionStatement)root.GetFirstChild()).GetExpression();
			NUnit.Framework.Assert.AreEqual("i\\u0066", first.GetString());
		}

		[NUnit.Framework.Test]
		public virtual void TestParseUnicodeReservedKeywords2()
		{
			AstRoot root = Parse("v\\u0061\\u0072");
			AstNode first = ((ExpressionStatement)root.GetFirstChild()).GetExpression();
			NUnit.Framework.Assert.AreEqual("va\\u0072", first.GetString());
		}

		[NUnit.Framework.Test]
		public virtual void TestParseUnicodeReservedKeywords3()
		{
			// All are keyword "while"
			AstRoot root = Parse("w\\u0068\\u0069\\u006C\\u0065;" + "\\u0077\\u0068il\\u0065; \\u0077h\\u0069le;");
			AstNode first = ((ExpressionStatement)root.GetFirstChild()).GetExpression();
			AstNode second = ((ExpressionStatement)root.GetFirstChild().GetNext()).GetExpression();
			AstNode third = ((ExpressionStatement)root.GetFirstChild().GetNext().GetNext()).GetExpression();
			NUnit.Framework.Assert.AreEqual("whil\\u0065", first.GetString());
			NUnit.Framework.Assert.AreEqual("whil\\u0065", second.GetString());
			NUnit.Framework.Assert.AreEqual("whil\\u0065", third.GetString());
		}

		[NUnit.Framework.Test]
		public virtual void TestParseObjectLiteral1()
		{
			environment.SetReservedKeywordAsIdentifier(true);
			Parse("({a:1});");
			Parse("({'a':1});");
			Parse("({0:1});");
			// property getter and setter definitions accept string and number
			Parse("({get a() {return 1}});");
			Parse("({get 'a'() {return 1}});");
			Parse("({get 0() {return 1}});");
			Parse("({set a(a) {return 1}});");
			Parse("({set 'a'(a) {return 1}});");
			Parse("({set 0(a) {return 1}});");
			// keywords ok
			Parse("({function:1});");
			// reserved words ok
			Parse("({float:1});");
		}

		[NUnit.Framework.Test]
		public virtual void TestParseObjectLiteral2()
		{
			// keywords, fail
			environment.SetReservedKeywordAsIdentifier(false);
			ExpectParseErrors("({function:1});", new string[] { "invalid property id" });
			environment.SetReservedKeywordAsIdentifier(true);
			// keywords ok
			Parse("({function:1});");
		}

		[NUnit.Framework.Test]
		public virtual void TestParseObjectLiteral3()
		{
			environment.SetLanguageVersion(Context.VERSION_1_8);
			environment.SetReservedKeywordAsIdentifier(true);
			Parse("var {get} = {get:1};");
			environment.SetReservedKeywordAsIdentifier(false);
			Parse("var {get} = {get:1};");
			ExpectParseErrors("var {get} = {if:1};", new string[] { "invalid property id" });
		}

		[NUnit.Framework.Test]
		public virtual void TestParseKeywordPropertyAccess()
		{
			environment.SetReservedKeywordAsIdentifier(true);
			// keywords ok
			Parse("({function:1}).function;");
			// reserved words ok.
			Parse("({import:1}).import;");
		}

		private void ExpectParseErrors(string @string, string[] errors)
		{
			Parse(@string, errors, null, false);
		}

		private AstRoot Parse(string @string)
		{
			return Parse(@string, true);
		}

		private AstRoot Parse(string @string, bool jsdoc)
		{
			return Parse(@string, null, null, jsdoc);
		}

		private AstRoot Parse(string @string, string[] errors, string[] warnings, bool jsdoc)
		{
			TestErrorReporter testErrorReporter = new _TestErrorReporter_1202(errors, errors, warnings);
			environment.SetErrorReporter(testErrorReporter);
			environment.SetRecordingComments(true);
			environment.SetRecordingLocalJsDocComments(jsdoc);
			Parser p = new Parser(environment, testErrorReporter);
			AstRoot script = null;
			try
			{
				script = p.Parse(@string, null, 0);
			}
			catch (EvaluatorException e)
			{
				if (errors == null)
				{
					// EvaluationExceptions should not occur when we aren't expecting
					// errors.
					throw;
				}
			}
			NUnit.Framework.Assert.IsTrue(testErrorReporter.HasEncounteredAllErrors());
			NUnit.Framework.Assert.IsTrue(testErrorReporter.HasEncounteredAllWarnings());
			return script;
		}

		private sealed class _TestErrorReporter_1202 : TestErrorReporter
		{
			public _TestErrorReporter_1202(string[] errors, string[] baseArg1, string[] baseArg2) : base(baseArg1, baseArg2)
			{
				this.errors = errors;
			}

			public override EvaluatorException RuntimeError(string message, string sourceName, int line, string lineSource, int lineOffset)
			{
				if (errors == null)
				{
					throw new NotSupportedException();
				}
				return new EvaluatorException(message, sourceName, line, lineSource, lineOffset);
			}

			private readonly string[] errors;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private AstRoot ParseAsReader(string @string)
		{
			TestErrorReporter testErrorReporter = new TestErrorReporter(null, null);
			environment.SetErrorReporter(testErrorReporter);
			environment.SetRecordingComments(true);
			environment.SetRecordingLocalJsDocComments(true);
			Parser p = new Parser(environment, testErrorReporter);
			AstRoot script = p.Parse(new StringReader(@string), null, 0);
			NUnit.Framework.Assert.IsTrue(testErrorReporter.HasEncounteredAllErrors());
			NUnit.Framework.Assert.IsTrue(testErrorReporter.HasEncounteredAllWarnings());
			return script;
		}
	}
}
