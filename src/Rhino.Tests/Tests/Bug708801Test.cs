/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using Rhino;
using Rhino.Ast;
using Rhino.Optimizer;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>AndrÃ© Bargull</author>
	[NUnit.Framework.TestFixture]
	public class Bug708801Test
	{
		private sealed class _ContextFactory_41 : ContextFactory
		{
			public _ContextFactory_41()
			{
				this.COMPILER_MODE = 9;
			}

			internal const int COMPILER_MODE;

			protected override Context MakeContext()
			{
				Context cx = base.MakeContext();
				cx.SetLanguageVersion(Context.VERSION_1_8);
				cx.SetOptimizationLevel(_T2143424677.COMPILER_MODE);
				return cx;
			}
		}

		private static readonly ContextFactory factory = new _ContextFactory_41();

		private abstract class Action : ContextAction
		{
			protected internal Context cx;

			protected internal ScriptableObject scope;

			protected internal virtual object Evaluate(string s)
			{
				return cx.EvaluateString(scope, s, "<eval>", 1, null);
			}

			/// <summary>
			/// Compiles
			/// <code>source</code>
			/// and returns the transformed and optimized
			/// <see cref="Rhino.Ast.ScriptNode">Rhino.Ast.ScriptNode</see>
			/// </summary>
			protected internal virtual ScriptNode Compile(CharSequence source)
			{
				string mainMethodClassName = "Main";
				string scriptClassName = "Main";
				CompilerEnvirons compilerEnv = new CompilerEnvirons();
				compilerEnv.InitFromContext(cx);
				ErrorReporter compilationErrorReporter = compilerEnv.GetErrorReporter();
				Parser p = new Parser(compilerEnv, compilationErrorReporter);
				AstRoot ast = p.Parse(source.ToString(), "<eval>", 1);
				IRFactory irf = new IRFactory(compilerEnv);
				ScriptNode tree = irf.TransformTree(ast);
				Codegen codegen = new Codegen();
				codegen.SetMainMethodClass(mainMethodClassName);
				codegen.CompileToClassFile(compilerEnv, scriptClassName, tree, tree.GetEncodedSource(), false);
				return tree;
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
			protected internal virtual void AssertNumberVars(CharSequence source, params string[] numbers)
			{
				// wrap source in function
				ScriptNode tree = Compile("function f(o, fn){" + source + "}");
				FunctionNode fnode = tree.GetFunctionNode(0);
				NUnit.Framework.Assert.IsNotNull(fnode);
				OptFunctionNode opt = OptFunctionNode.Get(fnode);
				NUnit.Framework.Assert.IsNotNull(opt);
				NUnit.Framework.Assert.AreSame(fnode, opt.fnode);
				for (int i = 0, c = fnode.GetParamCount(); i < c; ++i)
				{
					NUnit.Framework.Assert.IsTrue(opt.IsParameter(i));
					NUnit.Framework.Assert.IsFalse(opt.IsNumberVar(i));
				}
				ICollection<string> set = new HashSet<string>(Arrays.AsList(numbers));
				for (int i_1 = fnode.GetParamCount(), c_1 = fnode.GetParamAndVarCount(); i_1 < c_1; ++i_1)
				{
					NUnit.Framework.Assert.IsFalse(opt.IsParameter(i_1));
					string name = fnode.GetParamOrVarName(i_1);
					string msg = string.Format("{%s -> number? = %b}", name, opt.IsNumberVar(i_1));
					NUnit.Framework.Assert.AreEqual(set.Contains(name), opt.IsNumberVar(i_1), msg);
				}
			}

			public object Run(Context cx)
			{
				this.cx = cx;
				scope = cx.InitStandardObjects(null, true);
				return Run();
			}

			protected internal abstract object Run();
		}

		[NUnit.Framework.Test]
		public virtual void TestIncDec()
		{
			factory.Call(new _Action_126());
		}

		private sealed class _Action_126 : Bug708801Test.Action
		{
			public _Action_126()
			{
			}

			protected internal override object Run()
			{
				// pre-inc
				this.AssertNumberVars("var b; ++b");
				this.AssertNumberVars("var b=0; ++b", "b");
				this.AssertNumberVars("var b; ++[1][++b]");
				this.AssertNumberVars("var b=0; ++[1][++b]", "b");
				this.AssertNumberVars("var b; ++[1][b=0,++b]", "b");
				// pre-dec
				this.AssertNumberVars("var b; --b");
				this.AssertNumberVars("var b=0; --b", "b");
				this.AssertNumberVars("var b; --[1][--b]");
				this.AssertNumberVars("var b=0; --[1][--b]", "b");
				this.AssertNumberVars("var b; --[1][b=0,--b]", "b");
				// post-inc
				this.AssertNumberVars("var b; b++");
				this.AssertNumberVars("var b=0; b++", "b");
				this.AssertNumberVars("var b; [1][b++]++");
				this.AssertNumberVars("var b=0; [1][b++]++", "b");
				this.AssertNumberVars("var b; [1][b=0,b++]++", "b");
				// post-dec
				this.AssertNumberVars("var b; b--");
				this.AssertNumberVars("var b=0; b--", "b");
				this.AssertNumberVars("var b; [1][b--]--");
				this.AssertNumberVars("var b=0; [1][b--]--", "b");
				this.AssertNumberVars("var b; [1][b=0,b--]--", "b");
				return null;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestTypeofName()
		{
			factory.Call(new _Action_160());
		}

		private sealed class _Action_160 : Bug708801Test.Action
		{
			public _Action_160()
			{
			}

			protected internal override object Run()
			{
				// Token.TYPEOFNAME
				this.AssertNumberVars("var b; typeof b");
				this.AssertNumberVars("var b=0; typeof b", "b");
				this.AssertNumberVars("var b; if(new Date()<0){b=0} typeof b");
				// Token.TYPEOF
				this.AssertNumberVars("var b; typeof (b,b)");
				this.AssertNumberVars("var b=0; typeof (b,b)", "b");
				this.AssertNumberVars("var b; if(new Date()<0){b=0} typeof (b,b)");
				return null;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestEval()
		{
			factory.Call(new _Action_179());
		}

		private sealed class _Action_179 : Bug708801Test.Action
		{
			public _Action_179()
			{
			}

			protected internal override object Run()
			{
				// direct eval => requires activation
				this.AssertNumberVars("var b; eval('typeof b')");
				this.AssertNumberVars("var b=0; eval('typeof b')");
				this.AssertNumberVars("var b; if(new Date()<0){b=0} eval('typeof b')");
				// indirect eval => requires no activation
				this.AssertNumberVars("var b; (1,eval)('typeof b');");
				this.AssertNumberVars("var b=0; (1,eval)('typeof b')", "b");
				this.AssertNumberVars("var b; if(new Date()<0){b=0} (1,eval)('typeof b')", "b");
				return null;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestRelOp()
		{
			factory.Call(new _Action_201());
		}

		private sealed class _Action_201 : Bug708801Test.Action
		{
			public _Action_201()
			{
			}

			protected internal override object Run()
			{
				// relational operators: <, <=, >, >=
				this.AssertNumberVars("var b = 1 < 1");
				this.AssertNumberVars("var b = 1 <= 1");
				this.AssertNumberVars("var b = 1 > 1");
				this.AssertNumberVars("var b = 1 >= 1");
				// equality operators: ==, !=, ===, !==
				this.AssertNumberVars("var b = 1 == 1");
				this.AssertNumberVars("var b = 1 != 1");
				this.AssertNumberVars("var b = 1 === 1");
				this.AssertNumberVars("var b = 1 !== 1");
				return null;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestMore()
		{
			factory.Call(new _Action_222());
		}

		private sealed class _Action_222 : Bug708801Test.Action
		{
			public _Action_222()
			{
			}

			protected internal override object Run()
			{
				// simple assignment:
				this.AssertNumberVars("var b");
				this.AssertNumberVars("var b = 1", "b");
				this.AssertNumberVars("var b = 'a'");
				this.AssertNumberVars("var b = true");
				this.AssertNumberVars("var b = /(?:)/");
				this.AssertNumberVars("var b = o");
				this.AssertNumberVars("var b = fn");
				// use before assignment
				this.AssertNumberVars("b; var b = 1");
				this.AssertNumberVars("b || c; var b = 1, c = 2");
				this.AssertNumberVars("if(b) var b = 1");
				this.AssertNumberVars("typeof b; var b=1");
				this.AssertNumberVars("typeof (b,b); var b=1");
				// relational operators: in, instanceof
				this.AssertNumberVars("var b = 1 instanceof 1");
				this.AssertNumberVars("var b = 1 in 1");
				// other operators with nested comma expression:
				this.AssertNumberVars("var b = !(1,1)");
				this.AssertNumberVars("var b = typeof(1,1)");
				this.AssertNumberVars("var b = void(1,1)");
				// nested assignment
				this.AssertNumberVars("var b = 1; var f = (b = 'a')");
				// let expression:
				this.AssertNumberVars("var b = let(x=1) x", "b", "x");
				this.AssertNumberVars("var b = let(x=1,y=1) x,y", "b", "x", "y");
				// conditional operator:
				this.AssertNumberVars("var b = 0 ? 1 : 2", "b");
				this.AssertNumberVars("var b = 'a' ? 1 : 2", "b");
				// comma expression:
				this.AssertNumberVars("var b = (0,1)", "b");
				this.AssertNumberVars("var b = ('a',1)", "b");
				// assignment operations:
				this.AssertNumberVars("var b; var c=0; b=c", "b", "c");
				this.AssertNumberVars("var b; var c=0; b=(c=1)", "b", "c");
				this.AssertNumberVars("var b; var c=0; b=(c='a')");
				this.AssertNumberVars("var b; var c=0; b=(c+=1)", "b", "c");
				this.AssertNumberVars("var b; var c=0; b=(c*=1)", "b", "c");
				this.AssertNumberVars("var b; var c=0; b=(c%=1)", "b", "c");
				this.AssertNumberVars("var b; var c=0; b=(c/=1)", "b", "c");
				// property access:
				this.AssertNumberVars("var b; b=(o.p)");
				this.AssertNumberVars("var b; b=(o.p=1)", "b");
				this.AssertNumberVars("var b; b=(o.p+=1)");
				this.AssertNumberVars("var b; b=(o['p']=1)", "b");
				this.AssertNumberVars("var b; b=(o['p']+=1)");
				this.AssertNumberVars("var b; var o = {p:0}; b=(o.p=1)", "b");
				this.AssertNumberVars("var b; var o = {p:0}; b=(o.p+=1)");
				this.AssertNumberVars("var b; var o = {p:0}; b=(o['p']=1)", "b");
				this.AssertNumberVars("var b; var o = {p:0}; b=(o['p']+=1)");
				this.AssertNumberVars("var b = 1; b.p = true", "b");
				this.AssertNumberVars("var b = 1; b.p", "b");
				this.AssertNumberVars("var b = 1; b.p()", "b");
				this.AssertNumberVars("var b = 1; b[0] = true", "b");
				this.AssertNumberVars("var b = 1; b[0]", "b");
				this.AssertNumberVars("var b = 1; b[0]()", "b");
				// assignment (global)
				this.AssertNumberVars("var b = foo");
				this.AssertNumberVars("var b = foo1=1", "b");
				this.AssertNumberVars("var b = foo2+=1");
				// boolean operators:
				this.AssertNumberVars("var b = 1||2", "b");
				this.AssertNumberVars("var b; var c=1; b=c||2", "b", "c");
				this.AssertNumberVars("var b; var c=1; b=c||c||2", "b", "c");
				this.AssertNumberVars("var b = 1&&2", "b");
				this.AssertNumberVars("var b; var c=1; b=c&&2", "b", "c");
				this.AssertNumberVars("var b; var c=1; b=c&&c&&2", "b", "c");
				// bit not:
				this.AssertNumberVars("var b = ~0", "b");
				this.AssertNumberVars("var b = ~o", "b");
				this.AssertNumberVars("var b; var c=1; b=~c", "b", "c");
				// increment, function call:
				this.AssertNumberVars("var b; var g; b = (g=0,g++)", "b", "g");
				this.AssertNumberVars("var b; var x = fn(b=1)", "b");
				this.AssertNumberVars("var b; var x = fn(b=1).p++", "b", "x");
				this.AssertNumberVars("var b; ({1:{}})[b=1].p++", "b");
				this.AssertNumberVars("var b; o[b=1]++", "b");
				// destructuring
				this.AssertNumberVars("var r,s; [r,s] = [1,1]");
				this.AssertNumberVars("var r=0, s=0; [r,s] = [1,1]");
				this.AssertNumberVars("var r,s; ({a: r, b: s}) = {a:1, b:1}");
				this.AssertNumberVars("var r=0, s=0; ({a: r, b: s}) = {a:1, b:1}");
				// array comprehension
				this.AssertNumberVars("var b=[i*i for each (i in [1,2,3])]");
				this.AssertNumberVars("var b=[j*j for each (j in [1,2,3]) if (j>1)]");
				return null;
			}
		}
	}
}
