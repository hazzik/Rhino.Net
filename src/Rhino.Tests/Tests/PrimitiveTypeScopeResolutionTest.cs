/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>
	/// Unit tests for <a href="https://bugzilla.mozilla.org/show_bug.cgi?id=374918">Bug 374918 -
	/// String primitive prototype wrongly resolved when used with many top scopes</a>
	/// </summary>
	/// <author>Marc Guillemot</author>
	[TestFixture]
	public class PrimitiveTypeScopeResolutionTest
	{
		[Test]
		public virtual void FunctionCall()
		{
			string str2 = "function f() {\n" + "String.prototype.foo = function() { return 'from 2' }; \n" + "var s2 = 's2';\n" + "var s2Foo = s2.foo();\n" + "if (s2Foo != 'from 2') throw 's2 got: ' + s2Foo;\n" + "}";
			// fails
			string str1 = "String.prototype.foo = function() { return 'from 1'};" + "scope2.f()";
			TestWithTwoScopes(str1, str2);
		}

		[Test]
		public virtual void PropertyAccess()
		{
			string str2 = "function f() { String.prototype.foo = 'from 2'; \n" + "var s2 = 's2';\n" + "var s2Foo = s2.foo;\n" + "if (s2Foo != 'from 2') throw 's2 got: ' + s2Foo;\n" + "}";
			// fails
			string str1 = "String.prototype.foo = 'from 1'; scope2.f()";
			TestWithTwoScopes(str1, str2);
		}

		[Test]
		public virtual void ElementAccess()
		{
			string str2 = "function f() { String.prototype.foo = 'from 2'; \n" + "var s2 = 's2';\n" + "var s2Foo = s2['foo'];\n" + "if (s2Foo != 'from 2') throw 's2 got: ' + s2Foo;\n" + "}";
			// fails
			string str1 = "String.prototype.foo = 'from 1'; scope2.f()";
			TestWithTwoScopes(str1, str2);
		}

		private void TestWithTwoScopes(string scriptScope1, string scriptScope2)
		{
			Utils.RunWithAllOptimizationLevels(cx =>
			{
				Scriptable scope1 = cx.InitStandardObjects(new MySimpleScriptableObject("scope1"));
				Scriptable scope2 = cx.InitStandardObjects(new MySimpleScriptableObject("scope2"));
				cx.EvaluateString(scope2, scriptScope2, "source2", 1, null);
				scope1.Put("scope2", scope1, scope2);
				return cx.EvaluateString(scope1, scriptScope1, "source1", 1, null);
			});
		}

		/// <summary>Simple utility allowing to better see the concerned scope while debugging</summary>
		[Serializable]
		internal class MySimpleScriptableObject : ScriptableObject
		{
			private const long serialVersionUID = 1L;

			private string label_;

			internal MySimpleScriptableObject(string label)
			{
				label_ = label;
			}

			public override string GetClassName()
			{
				return "MySimpleScriptableObject";
			}

			public override string ToString()
			{
				return label_;
			}
		}

		[Serializable]
		public class MyObject : ScriptableObject
		{
			public override string GetClassName()
			{
				return "MyObject";
			}

			public virtual object ReadPropFoo(Scriptable s)
			{
				return GetProperty(s, "foo");
			}
		}

		/// <summary>
		/// Test that FunctionObject use the right top scope to convert a primitive
		/// to an object
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		[Test]
		public virtual void FunctionObjectPrimitiveToObject()
		{
			string scriptScope2 = "function f() {\n" + "String.prototype.foo = 'from 2'; \n" + "var s2 = 's2';\n" + "var s2Foo = s2.foo;\n" + "var s2FooReadByFunction = myObject.ReadPropFoo(s2);\n" + "if (s2Foo != s2FooReadByFunction)\n" + "throw 's2 got: ' + s2FooReadByFunction;\n" + "}";
			// define object with custom method
			MyObject myObject = new MyObject();
			string[] functionNames = new string[] { "ReadPropFoo" };
			myObject.DefineFunctionProperties(functionNames, typeof(MyObject), PropertyAttributes.EMPTY);
			string scriptScope1 = "String.prototype.foo = 'from 1'; scope2.f()";
			Utils.RunWithAllOptimizationLevels(cx =>
			{
				Scriptable scope1 = cx.InitStandardObjects(new MySimpleScriptableObject("scope1"));
				Scriptable scope2 = cx.InitStandardObjects(new MySimpleScriptableObject("scope2"));
				scope2.Put("myObject", scope2, myObject);
				cx.EvaluateString(scope2, scriptScope2, "source2", 1, null);
				scope1.Put("scope2", scope1, scope2);
				return cx.EvaluateString(scope1, scriptScope1, "source1", 1, null);
			});
		}
	}
}
