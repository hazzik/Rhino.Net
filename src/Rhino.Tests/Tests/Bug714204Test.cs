/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>AndrÃ© Bargull</author>
	[NUnit.Framework.TestFixture]
	public class Bug714204Test
	{
		private Context cx;

		private ScriptableObject scope;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
			scope = cx.InitStandardObjects();
			cx.SetLanguageVersion(170);
		}

		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		[NUnit.Framework.Test]
		public virtual void Test_assign_this()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("function F() {\n");
			sb.Append("  [this.x] = arguments;\n");
			sb.Append("}\n");
			sb.Append("var f = new F('a');\n");
			sb.Append("(f.x == 'a')\n");
			Script script = cx.CompileString(sb.ToString(), "<eval>", 1, null);
			object result = script.Exec(cx, scope);
			NUnit.Framework.Assert.AreEqual(true, result);
		}

		public virtual void Test_var_this()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("function F() {\n");
			sb.Append("  var [this.x] = arguments;\n");
			sb.Append("}\n");
			cx.CompileString(sb.ToString(), "<eval>", 1, null);
		}

		public virtual void Test_let_this()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("function F() {\n");
			sb.Append("  let [this.x] = arguments;\n");
			sb.Append("}\n");
			cx.CompileString(sb.ToString(), "<eval>", 1, null);
		}

		public virtual void Test_const_this()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("function F() {\n");
			sb.Append("  const [this.x] = arguments;\n");
			sb.Append("}\n");
			cx.CompileString(sb.ToString(), "<eval>", 1, null);
		}

		public virtual void Test_args_this()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("function F([this.x]) {\n");
			sb.Append("}\n");
			cx.CompileString(sb.ToString(), "<eval>", 1, null);
		}
	}
}
