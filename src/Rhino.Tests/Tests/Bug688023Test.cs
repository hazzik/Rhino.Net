/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Rhino;
using Rhino.Ast;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>AndrÃ© Bargull</author>
	[NUnit.Framework.TestFixture]
	public class Bug688023Test
	{
		private Context cx;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
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

		private static string Lines(params string[] lines)
		{
			StringBuilder sb = new StringBuilder();
			foreach (string line in lines)
			{
				sb.Append(line).Append('\n');
			}
			return sb.ToString();
		}

		[NUnit.Framework.Test]
		public virtual void ToSourceForInLoop()
		{
			NUnit.Framework.Assert.AreEqual(Lines("for (x in y) ", "  b = 1;"), ToSource("for(x in y) b=1;"));
			NUnit.Framework.Assert.AreEqual(Lines("for (x in y) {", "  b = 1;", "}"), ToSource("for(x in y) {b=1;}"));
		}

		[NUnit.Framework.Test]
		public virtual void ToSourceForLoop()
		{
			NUnit.Framework.Assert.AreEqual(Lines("for (; ; ) ", "  b = 1;"), ToSource("for(;;) b=1;"));
			NUnit.Framework.Assert.AreEqual(Lines("for (; ; ) {", "  b = 1;", "}"), ToSource("for(;;) {b=1;}"));
		}

		[NUnit.Framework.Test]
		public virtual void ToSourceWhileLoop()
		{
			NUnit.Framework.Assert.AreEqual(Lines("while (a) ", "  b = 1;"), ToSource("while(a) b=1;"));
			NUnit.Framework.Assert.AreEqual(Lines("while (a) {", "  b = 1;", "}"), ToSource("while(a) {b=1;}"));
		}

		[NUnit.Framework.Test]
		public virtual void ToSourceWithStatement()
		{
			NUnit.Framework.Assert.AreEqual(Lines("with (a) ", "  b = 1;"), ToSource("with(a) b=1;"));
			NUnit.Framework.Assert.AreEqual(Lines("with (a) {", "  b = 1;", "}"), ToSource("with(a) {b=1;}"));
		}

		[NUnit.Framework.Test]
		public virtual void ToSourceIfStatement()
		{
			NUnit.Framework.Assert.AreEqual(Lines("if (a) ", "  b = 1;"), ToSource("if(a) b=1;"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) {", "  b = 1;", "}"), ToSource("if(a) {b=1;}"));
		}

		[NUnit.Framework.Test]
		public virtual void ToSourceIfElseStatement()
		{
			NUnit.Framework.Assert.AreEqual(Lines("if (a) ", "  b = 1;", "else ", "  b = 2;"), ToSource("if(a) b=1; else b=2;"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) {", "  b = 1;", "} else ", "  b = 2;"), ToSource("if(a) { b=1; } else b=2;"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) ", "  b = 1;", "else {", "  b = 2;", "}"), ToSource("if(a) b=1; else { b=2; }"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) {", "  b = 1;", "} else {", "  b = 2;", "}"), ToSource("if(a) { b=1; } else { b=2; }"));
		}

		[NUnit.Framework.Test]
		public virtual void ToSourceIfElseIfElseStatement()
		{
			NUnit.Framework.Assert.AreEqual(Lines("if (a) ", "  b = 1;", "else if (a) ", "  b = 2;", "else ", "  b = 3;"), ToSource("if(a) b=1; else if (a) b=2; else b=3;"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) {", "  b = 1;", "} else if (a) ", "  b = 2;", "else ", "  b = 3;"), ToSource("if(a) { b=1; } else if (a) b=2; else b=3;"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) ", "  b = 1;", "else if (a) {", "  b = 2;", "} else ", "  b = 3;"), ToSource("if(a) b=1; else if (a) { b=2; } else b=3;"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) {", "  b = 1;", "} else if (a) {", "  b = 2;", "} else ", "  b = 3;"), ToSource("if(a) { b=1; } else if (a) { b=2; } else b=3;"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) ", "  b = 1;", "else if (a) ", "  b = 2;", "else {", "  b = 3;", "}"), ToSource("if(a) b=1; else if (a) b=2; else {b=3;}"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) {", "  b = 1;", "} else if (a) ", "  b = 2;", "else {", "  b = 3;", "}"), ToSource("if(a) { b=1; } else if (a) b=2; else {b=3;}"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) ", "  b = 1;", "else if (a) {", "  b = 2;", "} else {", "  b = 3;", "}"), ToSource("if(a) b=1; else if (a) { b=2; } else {b=3;}"));
			NUnit.Framework.Assert.AreEqual(Lines("if (a) {", "  b = 1;", "} else if (a) {", "  b = 2;", "} else {", "  b = 3;", "}"), ToSource("if(a) { b=1; } else if (a) { b=2; } else {b=3;}"));
		}
	}
}
