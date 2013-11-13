/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tests;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	/// <summary>Takes care that it's possible to customize the result of the typeof operator.</summary>
	/// <remarks>
	/// Takes care that it's possible to customize the result of the typeof operator.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=463996
	/// Includes fix and test for https://bugzilla.mozilla.org/show_bug.cgi?id=453360
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TypeOfTest
	{
		[System.Serializable]
		public class Foo : ScriptableObject
		{
			private const long serialVersionUID = -8771045033217033529L;

			private readonly string typeOfValue_;

			public Foo(string _typeOfValue)
			{
				typeOfValue_ = _typeOfValue;
			}

			public override string GetTypeOf()
			{
				return typeOfValue_;
			}

			public override string GetClassName()
			{
				return "Foo";
			}
		}

		/// <summary>ECMA 11.4.3 says that typeof on host object is Implementation-dependent</summary>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestCustomizeTypeOf()
		{
			TestCustomizeTypeOf("object", new TypeOfTest.Foo("object"));
			TestCustomizeTypeOf("blabla", new TypeOfTest.Foo("blabla"));
		}

		/// <summary>ECMA 11.4.3 says that typeof on host object is Implementation-dependent</summary>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void Test0()
		{
			Function f = new _BaseFunction_62();
			ContextAction action = new _ContextAction_71(f);
			DoTest("function", action);
		}

		private sealed class _BaseFunction_62 : BaseFunction
		{
			public _BaseFunction_62()
			{
			}

			public override object Call(Context _cx, Scriptable _scope, Scriptable _thisObj, object[] _args)
			{
				return _args[0].GetType().FullName;
			}
		}

		private sealed class _ContextAction_71 : ContextAction
		{
			public _ContextAction_71(Function f)
			{
				this.f = f;
			}

			public object Run(Context context)
			{
				Scriptable scope = context.InitStandardObjects();
				scope.Put("myObj", scope, f);
				return context.EvaluateString(scope, "typeof myObj", "test script", 1, null);
			}

			private readonly Function f;
		}

		private void TestCustomizeTypeOf(string expected, Scriptable obj)
		{
			ContextAction action = new _ContextAction_85(obj);
			DoTest(expected, action);
		}

		private sealed class _ContextAction_85 : ContextAction
		{
			public _ContextAction_85(Scriptable obj)
			{
				this.obj = obj;
			}

			public object Run(Context context)
			{
				Scriptable scope = context.InitStandardObjects();
				scope.Put("myObj", scope, obj);
				return context.EvaluateString(scope, "typeof myObj", "test script", 1, null);
			}

			private readonly Scriptable obj;
		}

		/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=453360</summary>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestBug453360()
		{
			DoTest("object", "typeof new RegExp();");
			DoTest("object", "typeof /foo/;");
		}

		private void DoTest(string expected, string script)
		{
			ContextAction action = new _ContextAction_108(script);
			DoTest(expected, action);
		}

		private sealed class _ContextAction_108 : ContextAction
		{
			public _ContextAction_108(string script)
			{
				this.script = script;
			}

			public object Run(Context context)
			{
				Scriptable scope = context.InitStandardObjects();
				return context.EvaluateString(scope, script, "test script", 1, null);
			}

			private readonly string script;
		}

		private void DoTest(string expected, ContextAction action)
		{
			DoTest(-1, expected, action);
			DoTest(0, expected, action);
			DoTest(1, expected, action);
		}

		private void DoTest(int optimizationLevel, string expected, ContextAction action)
		{
			object o = new ContextFactory().Call(new _ContextAction_128(optimizationLevel, action));
			NUnit.Framework.Assert.AreEqual(expected, o);
		}

		private sealed class _ContextAction_128 : ContextAction
		{
			public _ContextAction_128(int optimizationLevel, ContextAction action)
			{
				this.optimizationLevel = optimizationLevel;
				this.action = action;
			}

			public object Run(Context context)
			{
				context.SetOptimizationLevel(optimizationLevel);
				return Context.ToString(action.Run(context));
			}

			private readonly int optimizationLevel;

			private readonly ContextAction action;
		}
	}
}
