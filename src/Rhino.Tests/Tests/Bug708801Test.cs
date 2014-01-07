/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Ast;
using Rhino.Optimizer;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>AndrÃ© Bargull</author>
	[TestFixture]
	public class Bug708801Test
	{
		private sealed class MyContextFactory : ContextFactory
		{
			private const int COMPILER_MODE = 9;

			protected override Context MakeContext()
			{
				Context cx = base.MakeContext();
				cx.SetLanguageVersion(LanguageVersion.VERSION_1_8);
				cx.SetOptimizationLevel(COMPILER_MODE);
				return cx;
			}
		}

		private static readonly ContextFactory factory = new MyContextFactory();

		[Test]
		public virtual void TestIncDec()
		{
			factory.Call(cx =>
		{
				cx.InitStandardObjects(null, true);
				// pre-inc
				AssertNumberVars(cx, "var b; ++b");
				AssertNumberVars(cx, "var b=0; ++b", "b");
				AssertNumberVars(cx, "var b; ++[1][++b]");
				AssertNumberVars(cx, "var b=0; ++[1][++b]", "b");
				AssertNumberVars(cx, "var b; ++[1][b=0,++b]", "b");
				// pre-dec
				AssertNumberVars(cx, "var b; --b");
				AssertNumberVars(cx, "var b=0; --b", "b");
				AssertNumberVars(cx, "var b; --[1][--b]");
				AssertNumberVars(cx, "var b=0; --[1][--b]", "b");
				AssertNumberVars(cx, "var b; --[1][b=0,--b]", "b");
				// post-inc
				AssertNumberVars(cx, "var b; b++");
				AssertNumberVars(cx, "var b=0; b++", "b");
				AssertNumberVars(cx, "var b; [1][b++]++");
				AssertNumberVars(cx, "var b=0; [1][b++]++", "b");
				AssertNumberVars(cx, "var b; [1][b=0,b++]++", "b");
				// post-dec
				AssertNumberVars(cx, "var b; b--");
				AssertNumberVars(cx, "var b=0; b--", "b");
				AssertNumberVars(cx, "var b; [1][b--]--");
				AssertNumberVars(cx, "var b=0; [1][b--]--", "b");
				AssertNumberVars(cx, "var b; [1][b=0,b--]--", "b");
				return null;
			});
			}

		[Test]
		public virtual void TestTypeofName()
		{
			factory.Call(cx =>
		{
				cx.InitStandardObjects(null, true);
				// Token.TYPEOFNAME
				AssertNumberVars(cx, "var b; typeof b");
				AssertNumberVars(cx, "var b=0; typeof b", "b");
				AssertNumberVars(cx, "var b; if(new Date()<0){b=0} typeof b");
				// Token.TYPEOF
				AssertNumberVars(cx, "var b; typeof (b,b)");
				AssertNumberVars(cx, "var b=0; typeof (b,b)", "b");
				AssertNumberVars(cx, "var b; if(new Date()<0){b=0} typeof (b,b)");
				return null;
			});
			}

		[Test]
		public virtual void TestEval()
		{
			factory.Call(cx =>
		{
				cx.InitStandardObjects(null, true);
				// direct eval => requires activation
				AssertNumberVars(cx, "var b; eval('typeof b')");
				AssertNumberVars(cx, "var b=0; eval('typeof b')");
				AssertNumberVars(cx, "var b; if(new Date()<0){b=0} eval('typeof b')");
				// indirect eval => requires no activation
				AssertNumberVars(cx, "var b; (1,eval)('typeof b');");
				AssertNumberVars(cx, "var b=0; (1,eval)('typeof b')", "b");
				AssertNumberVars(cx, "var b; if(new Date()<0){b=0} (1,eval)('typeof b')", "b");
				return null;
			});
			}

		[Test]
		public virtual void TestRelOp()
		{
			factory.Call(cx =>
		{
				cx.InitStandardObjects(null, true);
				// relational operators: <, <=, >, >=
				AssertNumberVars(cx, "var b = 1 < 1");
				AssertNumberVars(cx, "var b = 1 <= 1");
				AssertNumberVars(cx, "var b = 1 > 1");
				AssertNumberVars(cx, "var b = 1 >= 1");
				// equality operators: ==, !=, ===, !==
				AssertNumberVars(cx, "var b = 1 == 1");
				AssertNumberVars(cx, "var b = 1 != 1");
				AssertNumberVars(cx, "var b = 1 === 1");
				AssertNumberVars(cx, "var b = 1 !== 1");
				return null;
			});
			}

		[Test]
		public virtual void TestMore()
		{
			factory.Call(cx =>
		{
				cx.InitStandardObjects(null, true);
				// simple assignment:
				AssertNumberVars(cx, "var b");
				AssertNumberVars(cx, "var b = 1", "b");
				AssertNumberVars(cx, "var b = 'a'");
				AssertNumberVars(cx, "var b = true");
				AssertNumberVars(cx, "var b = /(?:)/");
				AssertNumberVars(cx, "var b = o");
				AssertNumberVars(cx, "var b = fn");
				// use before assignment
				AssertNumberVars(cx, "b; var b = 1");
				AssertNumberVars(cx, "b || c; var b = 1, c = 2");
				AssertNumberVars(cx, "if(b) var b = 1");
				AssertNumberVars(cx, "typeof b; var b=1");
				AssertNumberVars(cx, "typeof (b,b); var b=1");
				// relational operators: in, instanceof
				AssertNumberVars(cx, "var b = 1 instanceof 1");
				AssertNumberVars(cx, "var b = 1 in 1");
				// other operators with nested comma expression:
				AssertNumberVars(cx, "var b = !(1,1)");
				AssertNumberVars(cx, "var b = typeof(1,1)");
				AssertNumberVars(cx, "var b = void(1,1)");
				// nested assignment
				AssertNumberVars(cx, "var b = 1; var f = (b = 'a')");
				// let expression:
				AssertNumberVars(cx, "var b = let(x=1) x", "b", "x");
				AssertNumberVars(cx, "var b = let(x=1,y=1) x,y", "b", "x", "y");
				// conditional operator:
				AssertNumberVars(cx, "var b = 0 ? 1 : 2", "b");
				AssertNumberVars(cx, "var b = 'a' ? 1 : 2", "b");
				// comma expression:
				AssertNumberVars(cx, "var b = (0,1)", "b");
				AssertNumberVars(cx, "var b = ('a',1)", "b");
				// assignment operations:
				AssertNumberVars(cx, "var b; var c=0; b=c", "b", "c");
				AssertNumberVars(cx, "var b; var c=0; b=(c=1)", "b", "c");
				AssertNumberVars(cx, "var b; var c=0; b=(c='a')");
				AssertNumberVars(cx, "var b; var c=0; b=(c+=1)", "b", "c");
				AssertNumberVars(cx, "var b; var c=0; b=(c*=1)", "b", "c");
				AssertNumberVars(cx, "var b; var c=0; b=(c%=1)", "b", "c");
				AssertNumberVars(cx, "var b; var c=0; b=(c/=1)", "b", "c");
				// property access:
				AssertNumberVars(cx, "var b; b=(o.p)");
				AssertNumberVars(cx, "var b; b=(o.p=1)", "b");
				AssertNumberVars(cx, "var b; b=(o.p+=1)");
				AssertNumberVars(cx, "var b; b=(o['p']=1)", "b");
				AssertNumberVars(cx, "var b; b=(o['p']+=1)");
				AssertNumberVars(cx, "var b; var o = {p:0}; b=(o.p=1)", "b");
				AssertNumberVars(cx, "var b; var o = {p:0}; b=(o.p+=1)");
				AssertNumberVars(cx, "var b; var o = {p:0}; b=(o['p']=1)", "b");
				AssertNumberVars(cx, "var b; var o = {p:0}; b=(o['p']+=1)");
				AssertNumberVars(cx, "var b = 1; b.p = true", "b");
				AssertNumberVars(cx, "var b = 1; b.p", "b");
				AssertNumberVars(cx, "var b = 1; b.p()", "b");
				AssertNumberVars(cx, "var b = 1; b[0] = true", "b");
				AssertNumberVars(cx, "var b = 1; b[0]", "b");
				AssertNumberVars(cx, "var b = 1; b[0]()", "b");
				// assignment (global)
				AssertNumberVars(cx, "var b = foo");
				AssertNumberVars(cx, "var b = foo1=1", "b");
				AssertNumberVars(cx, "var b = foo2+=1");
				// boolean operators:
				AssertNumberVars(cx, "var b = 1||2", "b");
				AssertNumberVars(cx, "var b; var c=1; b=c||2", "b", "c");
				AssertNumberVars(cx, "var b; var c=1; b=c||c||2", "b", "c");
				AssertNumberVars(cx, "var b = 1&&2", "b");
				AssertNumberVars(cx, "var b; var c=1; b=c&&2", "b", "c");
				AssertNumberVars(cx, "var b; var c=1; b=c&&c&&2", "b", "c");
				// bit not:
				AssertNumberVars(cx, "var b = ~0", "b");
				AssertNumberVars(cx, "var b = ~o", "b");
				AssertNumberVars(cx, "var b; var c=1; b=~c", "b", "c");
				// increment, function call:
				AssertNumberVars(cx, "var b; var g; b = (g=0,g++)", "b", "g");
				AssertNumberVars(cx, "var b; var x = fn(b=1)", "b");
				AssertNumberVars(cx, "var b; var x = fn(b=1).p++", "b", "x");
				AssertNumberVars(cx, "var b; ({1:{}})[b=1].p++", "b");
				AssertNumberVars(cx, "var b; o[b=1]++", "b");
				// destructuring
				AssertNumberVars(cx, "var r,s; [r,s] = [1,1]");
				AssertNumberVars(cx, "var r=0, s=0; [r,s] = [1,1]");
				AssertNumberVars(cx, "var r,s; ({a: r, b: s}) = {a:1, b:1}");
				AssertNumberVars(cx, "var r=0, s=0; ({a: r, b: s}) = {a:1, b:1}");
				// array comprehension
				AssertNumberVars(cx, "var b=[i*i for each (i in [1,2,3])]");
				AssertNumberVars(cx, "var b=[j*j for each (j in [1,2,3]) if (j>1)]");
				return null;
			});
			}

		/// <summary>
		/// Compiles
		/// <code>source</code>
		/// and returns the transformed and optimized
		/// <see cref="Rhino.Ast.ScriptNode">Rhino.Ast.ScriptNode</see>
		/// </summary>
		private static ScriptNode Compile(Context context, string source)
		{
			string mainMethodClassName = "Main";
			string scriptClassName = "Main";
			CompilerEnvirons compilerEnv = new CompilerEnvirons();
			compilerEnv.InitFromContext(context);
			ErrorReporter compilationErrorReporter = compilerEnv.GetErrorReporter();
			Parser p = new Parser(compilerEnv, compilationErrorReporter);
			AstRoot ast = p.Parse(source, "<eval>", 1);
			IRFactory irf = new IRFactory(compilerEnv);
			ScriptNode tree = irf.TransformTree(ast);
#if COMPILATION
			Codegen codegen = new Codegen();
			codegen.SetMainMethodClass(mainMethodClassName);
			codegen.CompileToClassFile(compilerEnv, scriptClassName, tree, tree.GetEncodedSource(), false);
#endif
			return tree;
		}

		private static object Evaluate(ScriptableObject scope, Context cx, string s)
		{
			return cx.EvaluateString(scope, s, "<eval>", 1, null);
		}

		/// <summary>
		/// Checks every variable
		/// <code>v</code>
		/// in
		/// <code>source</code>
		/// is marked as a
		/// number-variable iff
		/// <code>numbers</code>
		/// contains
		/// <code>v</code>
		/// </summary>
		private static void AssertNumberVars(Context context, string source, params string[] numbers)
		{
			// wrap source in function
			ScriptNode tree = Compile(context, "function f(o, fn){" + source + "}");
			FunctionNode fnode = tree.GetFunctionNode(0);
			Assert.IsNotNull(fnode);
			OptFunctionNode opt = OptFunctionNode.Get(fnode);
			Assert.IsNotNull(opt);
			Assert.AreSame(fnode, opt.fnode);
			for (int i = 0, c = fnode.GetParamCount(); i < c; ++i)
			{
				Assert.IsTrue(opt.IsParameter(i));
				Assert.IsFalse(opt.IsNumberVar(i));
			}
			ICollection<string> set = new HashSet<string>(numbers);
			for (int i = fnode.GetParamCount(), count = fnode.GetParamAndVarCount(); i < count; ++i)
			{
				Assert.IsFalse(opt.IsParameter(i));
				string name = fnode.GetParamOrVarName(i);
				string msg = String.Format("{%s -> number? = %b}", name, opt.IsNumberVar(i));
				Assert.AreEqual(set.Contains(name), opt.IsNumberVar(i), msg);
			}
		}
	}
}
