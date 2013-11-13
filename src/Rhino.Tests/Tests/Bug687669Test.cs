/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tests;
using Rhino.Tests.Ast;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	/// <author>AndrГ© Bargull</author>
	[NUnit.Framework.TestFixture]
	public class Bug687669Test
	{
		private Context cx;

		private ScriptableObject scope;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
			cx.SetLanguageVersion(Context.VERSION_1_8);
			scope = cx.InitStandardObjects();
		}

		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		private object Eval(CharSequence cs)
		{
			return cx.EvaluateString(scope, cs.ToString(), "<eval>", 1, null);
		}

		private AstRoot Parse(CharSequence cs)
		{
			CompilerEnvirons compilerEnv = new CompilerEnvirons();
			compilerEnv.InitFromContext(cx);
			ErrorReporter compilationErrorReporter = compilerEnv.GetErrorReporter();
			Parser p = new Parser(compilerEnv, compilationErrorReporter);
			return p.Parse(cs.ToString(), "<eval>", 1);
		}

		private string ToSource(CharSequence cs)
		{
			return Parse(cs).ToSource();
		}

		[NUnit.Framework.Test]
		public virtual void TestEval()
		{
			// test EmptyStatement node doesn't infer with return values (in
			// contrast to wrapping EmptyExpression into an ExpressionStatement)
			NUnit.Framework.Assert.AreEqual(1d, Eval("1;;;;"));
			NUnit.Framework.Assert.AreEqual(Undefined.instance, Eval("(function(){1;;;;})()"));
			NUnit.Framework.Assert.AreEqual(1d, Eval("(function(){return 1;;;;})()"));
		}

		[NUnit.Framework.Test]
		public virtual void TestToSource()
		{
			NUnit.Framework.Assert.AreEqual("L1:\n  ;\n", ToSource("L1:;"));
			NUnit.Framework.Assert.AreEqual("L1:\n  ;\na = 1;\n", ToSource("L1:; a=1;"));
			NUnit.Framework.Assert.AreEqual("if (1) \n  ;\n", ToSource("if(1);"));
			NUnit.Framework.Assert.AreEqual("if (1) \n  ;\na = 1;\n", ToSource("if(1); a=1;"));
			NUnit.Framework.Assert.AreEqual("if (1) \n  a = 1;\n", ToSource("if(1)a=1;"));
			NUnit.Framework.Assert.AreEqual("if (1) \n  a = 1;\na = 1;\n", ToSource("if(1)a=1; a=1;"));
			NUnit.Framework.Assert.AreEqual("if (1) \n  ;\n;\n;\n;\n", ToSource("if(1);;;;"));
		}
	}
}
