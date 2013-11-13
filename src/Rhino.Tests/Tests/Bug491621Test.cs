/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;
using Rhino;
using Rhino.Ast;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>Hannes Wallnoefer</author>
	[NUnit.Framework.TestFixture]
	public class Bug491621Test
	{
		/// <summary>
		/// Asserts that the value returned by
		/// <see cref="Rhino.Ast.AstNode.ToSource()">Rhino.Ast.AstNode.ToSource()</see>
		/// after
		/// the given input source was parsed equals the specified expected output source.
		/// </summary>
		/// <param name="source">the JavaScript source to be parsed</param>
		/// <param name="expectedOutput">
		/// the JavaScript source that is expected to be
		/// returned by
		/// <see cref="Rhino.Ast.AstNode.ToSource()">Rhino.Ast.AstNode.ToSource()</see>
		/// </param>
		private void AssertSource(string source, string expectedOutput)
		{
			CompilerEnvirons env = new CompilerEnvirons();
			env.SetLanguageVersion(Context.VERSION_1_7);
			Parser parser = new Parser(env);
			AstRoot root = parser.Parse(source, null, 0);
			NUnit.Framework.Assert.AreEqual(expectedOutput, root.ToSource());
		}

		/// <summary>Tests that var declaration AST nodes is properly decompiled.</summary>
		/// <remarks>Tests that var declaration AST nodes is properly decompiled.</remarks>
		[NUnit.Framework.Test]
		public virtual void TestVarDeclarationToSource()
		{
			AssertSource("var x=0;x++;", "var x = 0;\nx++;\n");
			AssertSource("for(var i=0;i<10;i++)x[i]=i;a++;", "for (var i = 0; i < 10; i++) \n  x[i] = i;\na++;\n");
			AssertSource("var a;if(true)a=1;", "var a;\nif (true) \n  a = 1;\n");
			AssertSource("switch(x){case 1:var y;z++}", "switch (x) {\n  case 1:\n    var y;\n    z++;\n}\n");
			AssertSource("for(var p in o)s+=o[p]", "for (var p in o) \n  s += o[p];\n");
			AssertSource("if(c)var a=0;else a=1", "if (c) \n  var a = 0;\nelse \n  a = 1;\n");
			AssertSource("for(var i=0;i<10;i++)var x=i;x++;", "for (var i = 0; i < 10; i++) \n  var x = i;\nx++;\n");
			AssertSource("function f(){var i=2;for(var j=0;j<i;++j)print(j);}", "function f() {\n  var i = 2;\n  for (var j = 0; j < i; ++j) \n    print(j);\n}\n");
		}

		/// <summary>Tests that let declaration AST nodes are properly decompiled.</summary>
		/// <remarks>Tests that let declaration AST nodes are properly decompiled.</remarks>
		[NUnit.Framework.Test]
		public virtual void TestLetDeclarationToSource()
		{
			AssertSource("let x=0;x++;", "let x = 0;\nx++;\n");
			AssertSource("for(let i=0;i<10;i++)x[i]=i;a++;", "for (let i = 0; i < 10; i++) \n  x[i] = i;\na++;\n");
			AssertSource("let a;if(true)a=1;", "let a;\nif (true) \n  a = 1;\n");
			AssertSource("switch(x){case 1:let y;z++}", "switch (x) {\n  case 1:\n    let y;\n    z++;\n}\n");
			AssertSource("for(let p in o)s+=o[p]", "for (let p in o) \n  s += o[p];\n");
			AssertSource("if(c)let a=0;else a=1", "if (c) \n  let a = 0;\nelse \n  a = 1;\n");
			AssertSource("for(let i=0;i<10;i++){let x=i;}x++;", "for (let i = 0; i < 10; i++) {\n  let x = i;\n}\nx++;\n");
			AssertSource("function f(){let i=2;for(let j=0;j<i;++j)print(j);}", "function f() {\n  let i = 2;\n  for (let j = 0; j < i; ++j) \n    print(j);\n}\n");
		}

		/// <summary>Tests that const declaration AST nodes are properly decompiled.</summary>
		/// <remarks>Tests that const declaration AST nodes are properly decompiled.</remarks>
		[NUnit.Framework.Test]
		public virtual void TestConstDeclarationToSource()
		{
			AssertSource("const x=0;x++;", "const x = 0;\nx++;\n");
			AssertSource("const a;if(true)a=1;", "const a;\nif (true) \n  a = 1;\n");
			AssertSource("switch(x){case 1:const y;z++}", "switch (x) {\n  case 1:\n    const y;\n    z++;\n}\n");
			AssertSource("if(c)const a=0;else a=1", "if (c) \n  const a = 0;\nelse \n  a = 1;\n");
		}
	}
}
