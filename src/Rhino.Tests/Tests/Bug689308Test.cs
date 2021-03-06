﻿/*
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
	public class Bug689308Test
	{
		private Context cx;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
			cx.SetLanguageVersion(LanguageVersion.VERSION_1_8);
		}

		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		private AstRoot Parse(string cs)
		{
            var compilerEnv = new CompilerEnvirons(cx);
			ErrorReporter compilationErrorReporter = compilerEnv.ErrorReporter;
			Parser p = new Parser(compilerEnv, compilationErrorReporter);
			return p.Parse(cs, "<eval>", 1);
		}

		private string ToSource(string cs)
		{
			return Parse(cs).ToSource();
		}

		[NUnit.Framework.Test]
		public virtual void TestToSourceArray()
		{
			NUnit.Framework.Assert.AreEqual("[];\n", ToSource("[]"));
			NUnit.Framework.Assert.AreEqual("[,];\n", ToSource("[,]"));
			NUnit.Framework.Assert.AreEqual("[, ,];\n", ToSource("[,,]"));
			NUnit.Framework.Assert.AreEqual("[, , ,];\n", ToSource("[,,,]"));
			NUnit.Framework.Assert.AreEqual("[1];\n", ToSource("[1]"));
			NUnit.Framework.Assert.AreEqual("[1];\n", ToSource("[1,]"));
			NUnit.Framework.Assert.AreEqual("[, 1];\n", ToSource("[,1]"));
			NUnit.Framework.Assert.AreEqual("[1, 1];\n", ToSource("[1,1]"));
		}
	}
}
