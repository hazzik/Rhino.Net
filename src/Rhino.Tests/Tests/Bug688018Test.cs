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
	public class Bug688018Test
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
		public virtual void TestToSource()
		{
			NUnit.Framework.Assert.AreEqual("void 0;\n", ToSource("void 0;"));
			NUnit.Framework.Assert.AreEqual("void 1;\n", ToSource("void 1;"));
			NUnit.Framework.Assert.AreEqual("void 'hello';\n", ToSource("void 'hello';"));
			NUnit.Framework.Assert.AreEqual("void fn();\n", ToSource("void fn();"));
		}
	}
}
