/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>Takes care that the name of the method generated for a function "looks like" the original function name.</summary>
	/// <remarks>
	/// Takes care that the name of the method generated for a function "looks like" the original function name.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=460726
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class GeneratedMethodNameTest
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestStandardFunction()
		{
			string scriptCode = "function myFunc() {\n" + " var m = javaNameGetter.readCurrentFunctionJavaName();\n" + "  if (m != 'myFunc') throw 'got '  + m;" + "}\n" + "myFunc();";
			DoTest(scriptCode);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFunctionDollar()
		{
			string scriptCode = "function $() {\n" + " var m = javaNameGetter.readCurrentFunctionJavaName();\n" + "  if (m != '$') throw 'got '  + m;" + "}\n" + "$();";
			DoTest(scriptCode);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestScriptName()
		{
			string scriptCode = "var m = javaNameGetter.readCurrentFunctionJavaName();\n" + "if (m != 'script') throw 'got '  + m;";
			DoTest(scriptCode);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestConstructor()
		{
			string scriptCode = "function myFunc() {\n" + " var m = javaNameGetter.readCurrentFunctionJavaName();\n" + "  if (m != 'myFunc') throw 'got '  + m;" + "}\n" + "new myFunc();";
			DoTest(scriptCode);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestAnonymousFunction()
		{
			string scriptCode = "var myFunc = function() {\n" + " var m = javaNameGetter.readCurrentFunctionJavaName();\n" + "  if (m != 'anonymous') throw 'got '  + m;" + "}\n" + "myFunc();";
			DoTest(scriptCode);
		}

		public class JavaNameGetter
		{
			public virtual string ReadCurrentFunctionJavaName()
			{
				Exception t = new Exception();
				// remove prefix and suffix of method name
				return t.GetStackTrace()[8].GetMethodName().ReplaceFirst("_[^_]*_(.*)_[^_]*", "$1");
			}

			internal JavaNameGetter(GeneratedMethodNameTest _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly GeneratedMethodNameTest _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void DoTest(string scriptCode)
		{
			Context cx = ContextFactory.GetGlobal().EnterContext();
			try
			{
				Scriptable topScope = cx.InitStandardObjects();
				topScope.Put("javaNameGetter", topScope, new GeneratedMethodNameTest.JavaNameGetter(this));
				Script script = cx.CompileString(scriptCode, "myScript", 1, null);
				script.Exec(cx, topScope);
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
