/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

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
	[NUnit.Framework.TestFixture]
	public class PrimitiveTypeScopeResolutionTest
	{
		[NUnit.Framework.Test]
		public virtual void FunctionCall()
		{
			string str2 = "function f() {\n" + "String.prototype.foo = function() { return 'from 2' }; \n" + "var s2 = 's2';\n" + "var s2Foo = s2.foo();\n" + "if (s2Foo != 'from 2') throw 's2 got: ' + s2Foo;\n" + "}";
			// fails
			string str1 = "String.prototype.foo = function() { return 'from 1'};" + "scope2.f()";
			TestWithTwoScopes(str1, str2);
		}

		[NUnit.Framework.Test]
		public virtual void PropertyAccess()
		{
			string str2 = "function f() { String.prototype.foo = 'from 2'; \n" + "var s2 = 's2';\n" + "var s2Foo = s2.foo;\n" + "if (s2Foo != 'from 2') throw 's2 got: ' + s2Foo;\n" + "}";
			// fails
			string str1 = "String.prototype.foo = 'from 1'; scope2.f()";
			TestWithTwoScopes(str1, str2);
		}

		[NUnit.Framework.Test]
		public virtual void ElementAccess()
		{
			string str2 = "function f() { String.prototype.foo = 'from 2'; \n" + "var s2 = 's2';\n" + "var s2Foo = s2['foo'];\n" + "if (s2Foo != 'from 2') throw 's2 got: ' + s2Foo;\n" + "}";
			// fails
			string str1 = "String.prototype.foo = 'from 1'; scope2.f()";
			TestWithTwoScopes(str1, str2);
		}

		private void TestWithTwoScopes(string scriptScope1, string scriptScope2)
		{
			ContextAction action = new _ContextAction_68(scriptScope2, scriptScope1);
			Utils.RunWithAllOptimizationLevels(action);
		}

		private sealed class _ContextAction_68 : ContextAction
		{
			public _ContextAction_68(string scriptScope2, string scriptScope1)
			{
				this.scriptScope2 = scriptScope2;
				this.scriptScope1 = scriptScope1;
			}

			public object Run(Context cx)
			{
				Scriptable scope1 = cx.InitStandardObjects(new PrimitiveTypeScopeResolutionTest.MySimpleScriptableObject("scope1"));
				Scriptable scope2 = cx.InitStandardObjects(new PrimitiveTypeScopeResolutionTest.MySimpleScriptableObject("scope2"));
				cx.EvaluateString(scope2, scriptScope2, "source2", 1, null);
				scope1.Put("scope2", scope1, scope2);
				return cx.EvaluateString(scope1, scriptScope1, "source1", 1, null);
			}

			private readonly string scriptScope2;

			private readonly string scriptScope1;
		}

		/// <summary>Simple utility allowing to better see the concerned scope while debugging</summary>
		[System.Serializable]
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

		[System.Serializable]
		public class MyObject : ScriptableObject
		{
			private const long serialVersionUID = 1L;

			public override string GetClassName()
			{
				return "MyObject";
			}

			public virtual object ReadPropFoo(Scriptable s)
			{
				return ScriptableObject.GetProperty(s, "foo");
			}
		}

		/// <summary>
		/// Test that FunctionObject use the right top scope to convert a primitive
		/// to an object
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void FunctionObjectPrimitiveToObject()
		{
			string scriptScope2 = "function f() {\n" + "String.prototype.foo = 'from 2'; \n" + "var s2 = 's2';\n" + "var s2Foo = s2.foo;\n" + "var s2FooReadByFunction = myObject.readPropFoo(s2);\n" + "if (s2Foo != s2FooReadByFunction)\n" + "throw 's2 got: ' + s2FooReadByFunction;\n" + "}";
			// define object with custom method
			PrimitiveTypeScopeResolutionTest.MyObject myObject = new PrimitiveTypeScopeResolutionTest.MyObject();
			string[] functionNames = new string[] { "readPropFoo" };
			myObject.DefineFunctionProperties(functionNames, typeof(PrimitiveTypeScopeResolutionTest.MyObject), ScriptableObject.EMPTY);
			string scriptScope1 = "String.prototype.foo = 'from 1'; scope2.f()";
			ContextAction action = new _ContextAction_148(myObject, scriptScope2, scriptScope1);
			Utils.RunWithAllOptimizationLevels(action);
		}

		private sealed class _ContextAction_148 : ContextAction
		{
			public _ContextAction_148(PrimitiveTypeScopeResolutionTest.MyObject myObject, string scriptScope2, string scriptScope1)
			{
				this.myObject = myObject;
				this.scriptScope2 = scriptScope2;
				this.scriptScope1 = scriptScope1;
			}

			public object Run(Context cx)
			{
				Scriptable scope1 = cx.InitStandardObjects(new PrimitiveTypeScopeResolutionTest.MySimpleScriptableObject("scope1"));
				Scriptable scope2 = cx.InitStandardObjects(new PrimitiveTypeScopeResolutionTest.MySimpleScriptableObject("scope2"));
				scope2.Put("myObject", scope2, myObject);
				cx.EvaluateString(scope2, scriptScope2, "source2", 1, null);
				scope1.Put("scope2", scope1, scope2);
				return cx.EvaluateString(scope1, scriptScope1, "source1", 1, null);
			}

			private readonly PrimitiveTypeScopeResolutionTest.MyObject myObject;

			private readonly string scriptScope2;

			private readonly string scriptScope1;
		}
	}
}
