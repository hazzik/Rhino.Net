/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Ast;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>AndrÃ© Bargull</author>
	[NUnit.Framework.TestFixture]
	public class Bug689314Test
	{
		private Context cx;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
			cx.SetLanguageVersion(Context.VERSION_1_8);
		}

		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		private AstRoot Parse(string cs)
		{
			CompilerEnvirons compilerEnv = new CompilerEnvirons();
			compilerEnv.InitFromContext(cx);
			ErrorReporter compilationErrorReporter = compilerEnv.GetErrorReporter();
			Parser p = new Parser(compilerEnv, compilationErrorReporter);
			return p.Parse(cs.ToString(), "<eval>", 1);
		}

		private string ToSource(string cs)
		{
			return Parse(cs).ToSource();
		}

		[NUnit.Framework.Test]
		public virtual void TestToSourceFunctionStatement()
		{
			NUnit.Framework.Assert.AreEqual("function F() 1 + 2;\n", ToSource("function F() 1+2"));
			NUnit.Framework.Assert.AreEqual("function F() {\n  return 1 + 2;\n}\n", ToSource("function F() {return 1+2}"));
		}

		[NUnit.Framework.Test]
		public virtual void TestToSourceFunctionExpression()
		{
			NUnit.Framework.Assert.AreEqual("var x = function() 1 + 2;\n", ToSource("var x = function () 1+2"));
			NUnit.Framework.Assert.AreEqual("var x = function() {\n  return 1 + 2;\n};\n", ToSource("var x = function () {return 1+2}"));
			NUnit.Framework.Assert.AreEqual("var x = function F() 1 + 2;\n", ToSource("var x = function F() 1+2"));
			NUnit.Framework.Assert.AreEqual("var x = function F() {\n  return 1 + 2;\n};\n", ToSource("var x = function F() {return 1+2}"));
		}
	}
}
