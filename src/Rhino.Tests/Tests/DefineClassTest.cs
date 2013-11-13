/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;
using Rhino.Tests;
using Rhino.Tests.Annotations;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	[NUnit.Framework.TestFixture]
	public class DefineClassTest
	{
		internal Scriptable scope;

		[NUnit.Framework.Test]
		public virtual void TestAnnotatedHostObject()
		{
			Context cx = Context.Enter();
			try
			{
				object result = Evaluate(cx, "a = new AnnotatedHostObject(); a.initialized;");
				NUnit.Framework.Assert.AreEqual(result, true);
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "a.instanceFunction();"), "instanceFunction");
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "a.namedFunction();"), "namedFunction");
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "AnnotatedHostObject.staticFunction();"), "staticFunction");
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "AnnotatedHostObject.namedStaticFunction();"), "namedStaticFunction");
				NUnit.Framework.Assert.IsNull(Evaluate(cx, "a.foo;"));
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "a.foo = 'foo'; a.foo;"), "FOO");
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "a.bar;"), "bar");
				// Setting a property with no setting should be silently
				// ignored in non-strict mode.
				Evaluate(cx, "a.bar = 'new bar'");
				NUnit.Framework.Assert.AreEqual("bar", Evaluate(cx, "a.bar;"));
			}
			finally
			{
				Context.Exit();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestTraditionalHostObject()
		{
			Context cx = Context.Enter();
			try
			{
				object result = Evaluate(cx, "t = new TraditionalHostObject(); t.initialized;");
				NUnit.Framework.Assert.AreEqual(result, true);
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "t.instanceFunction();"), "instanceFunction");
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "TraditionalHostObject.staticFunction();"), "staticFunction");
				NUnit.Framework.Assert.IsNull(Evaluate(cx, "t.foo;"));
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "t.foo = 'foo'; t.foo;"), "FOO");
				NUnit.Framework.Assert.AreEqual(Evaluate(cx, "t.bar;"), "bar");
				// Setting a property with no setting should be silently
				// ignored in non-strict mode.
				Evaluate(cx, "t.bar = 'new bar'");
				NUnit.Framework.Assert.AreEqual("bar", Evaluate(cx, "t.bar;"));
			}
			finally
			{
				Context.Exit();
			}
		}

		private object Evaluate(Context cx, string str)
		{
			return cx.EvaluateString(scope, str, "<testsrc>", 0, null);
		}

		/// <exception cref="System.Exception"></exception>
		[SetUp]
		public virtual void Init()
		{
			Context cx = Context.Enter();
			try
			{
				scope = cx.InitStandardObjects();
				ScriptableObject.DefineClass<DefineClassTest.AnnotatedHostObject>(scope);
				ScriptableObject.DefineClass<DefineClassTest.TraditionalHostObject>(scope);
			}
			finally
			{
				Context.Exit();
			}
		}

		[System.Serializable]
		public class AnnotatedHostObject : ScriptableObject
		{
			internal string foo;

			internal string bar = "bar";

			public AnnotatedHostObject()
			{
			}

			public override string GetClassName()
			{
				return "AnnotatedHostObject";
			}

			[JSConstructor]
			public virtual void JsConstructorMethod()
			{
				Put("initialized", this, true);
			}

			[JSFunction]
			public virtual object InstanceFunction()
			{
				return "instanceFunction";
			}

			public virtual object SomeFunctionName()
			{
				return "namedFunction";
			}

			[JSStaticFunction]
			public static object StaticFunction()
			{
				return "staticFunction";
			}

			public static object SomeStaticFunctionName()
			{
				return "namedStaticFunction";
			}

			[JSGetter]
			public virtual string GetFoo()
			{
				return foo;
			}

			[JSSetter]
			public virtual void SetFoo(string foo)
			{
				this.foo = foo.ToUpper();
			}

			public virtual string GetMyBar()
			{
				return bar;
			}

			public virtual void SetBar(string bar)
			{
				this.bar = bar.ToUpper();
			}
		}

		[System.Serializable]
		public class TraditionalHostObject : ScriptableObject
		{
			internal string foo;

			internal string bar = "bar";

			public TraditionalHostObject()
			{
			}

			public override string GetClassName()
			{
				return "TraditionalHostObject";
			}

			public virtual void JsConstructor()
			{
				Put("initialized", this, true);
			}

			public virtual object JsFunction_instanceFunction()
			{
				return "instanceFunction";
			}

			public static object JsStaticFunction_staticFunction()
			{
				return "staticFunction";
			}

			public virtual string JsGet_foo()
			{
				return foo;
			}

			public virtual void JsSet_foo(string foo)
			{
				this.foo = foo.ToUpper();
			}

			public virtual string JsGet_bar()
			{
				return bar;
			}

			// not a JS setter
			public virtual void SetBar(string bar)
			{
				this.bar = bar.ToUpper();
			}
		}
	}
}
