/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;

namespace Rhino.Tests
{
	/// <summary>Takes care that it's possible to customize the result of the typeof operator.</summary>
	/// <remarks>
	/// Takes care that it's possible to customize the result of the typeof operator.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=463996
	/// Includes fix and test for https://bugzilla.mozilla.org/show_bug.cgi?id=453360
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[TestFixture]
	public sealed class TypeOfTest
	{
		[Serializable]
		public class Foo : ScriptableObject
		{
			private readonly string _typeOfValue;

			public Foo(string typeOfValue)
			{
				_typeOfValue = typeOfValue;
			}

			public override string GetTypeOf()
			{
				return _typeOfValue;
			}

			public override string GetClassName()
			{
				return "Foo";
			}
		}

		/// <summary>ECMA 11.4.3 says that typeof on host object is Implementation-dependent</summary>
		/// <exception cref="System.Exception"></exception>
		[Test]
		public void TestCustomizeTypeOf()
		{
			TestCustomizeTypeOf("object", new Foo("object"));
			TestCustomizeTypeOf("blabla", new Foo("blabla"));
		}

		/// <summary>ECMA 11.4.3 says that typeof on host object is Implementation-dependent</summary>
		/// <exception cref="System.Exception"></exception>
		[Test]
		public void Test0()
		{
			Function f = new Test0Function();
			DoTest("function", cx =>
			{
				Scriptable scope = cx.InitStandardObjects();
				scope.Put("myObj", scope, f);
				return cx.EvaluateString(scope, "typeof myObj", "test script", 1, null);
			});
		}

		private sealed class Test0Function : BaseFunction
		{
			public override object Call(Context _cx, Scriptable _scope, Scriptable _thisObj, object[] _args)
			{
				return _args[0].GetType().FullName;
			}
		}

		private void TestCustomizeTypeOf(string expected, Scriptable obj)
		{
			DoTest(expected, cx =>
			{
				Scriptable scope = cx.InitStandardObjects();
				scope.Put("myObj", scope, obj);
				return cx.EvaluateString(scope, "typeof myObj", "test script", 1, null);
			});
		}

		/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=453360</summary>
		/// <exception cref="System.Exception"></exception>
		[Test]
		public void TestBug453360()
		{
			DoTest("object", "typeof new RegExp();");
			DoTest("object", "typeof /foo/;");
		}

		private void DoTest(string expected, string script)
		{
			DoTest(expected, cx =>
			{
				Scriptable scope = cx.InitStandardObjects();
				return cx.EvaluateString(scope, script, "test script", 1, null);
			});
		}

		private void DoTest(string expected, ContextAction action)
		{
			DoTest(-1, expected, action);
			DoTest(0, expected, action);
			DoTest(1, expected, action);
		}

		private static void DoTest(int optimizationLevel, string expected, ContextAction action)
		{
			object o = new ContextFactory().Call(cx =>
			{
				cx.SetOptimizationLevel(optimizationLevel);
				return Context.ToString(action(cx));
			});
			Assert.AreEqual(expected, o);
		}
	}
}
