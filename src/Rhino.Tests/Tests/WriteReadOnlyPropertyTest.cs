/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using NUnit.Framework;

namespace Rhino.Tests
{
	/// <summary>Test that read-only properties can be...</summary>
	/// <remarks>
	/// Test that read-only properties can be... set when needed.
	/// This was the standard behavior in Rhino until 1.7R2 but has changed then.
	/// It is needed by HtmlUnit to simulate IE as well as FF2 (but not FF3).
	/// </remarks>
	/// <seealso><a href="https://bugzilla.mozilla.org/show_bug.cgi?id=519933">Rhino bug 519933</a></seealso>
	/// <author>Marc Guillemot</author>
	[TestFixture]
	public class WriteReadOnlyPropertyTest
	{
		[Test]
		public void TestWriteReadOnly_accepted()
		{
			TestWriteReadOnly(true);
		}

		[Test]
		public void TestWriteReadOnly_throws()
		{
			try
			{
				TestWriteReadOnly(false);
				Assert.Fail();
			}
			catch (EcmaError e)
			{
				Assert.IsTrue(e.Message.Contains("Cannot set property myProp that has only a getter"), e.Message);
			}
		}

		private static void TestWriteReadOnly(bool acceptWriteReadOnly)
		{
			MethodInfo readMethod = typeof(Foo).GetMethod("GetMyProp", Type.EmptyTypes);
			Foo foo = new Foo("hello");
			foo.DefineProperty("myProp", null, readMethod, null, ScriptableObject.EMPTY);
			string script = "foo.myProp = 123; foo.myProp";
			ContextFactory contextFactory = new TestContextFactory(acceptWriteReadOnly);
			contextFactory.Call(cx =>
			{
				ScriptableObject top = cx.InitStandardObjects();
				ScriptableObject.PutProperty(top, "foo", foo);
				cx.EvaluateString(top, script, "script", 0, null);
				return null;
			});
		}

		private sealed class TestContextFactory : ContextFactory
		{
			public TestContextFactory(bool acceptWriteReadOnly)
			{
				this.acceptWriteReadOnly = acceptWriteReadOnly;
			}

			protected override bool HasFeature(Context cx, int featureIndex)
			{
				if (Context.FEATURE_STRICT_MODE == featureIndex)
				{
					return !acceptWriteReadOnly;
				}
				return base.HasFeature(cx, featureIndex);
			}

			private readonly bool acceptWriteReadOnly;
		}

		/// <summary>Simple utility allowing to better see the concerned scope while debugging</summary>
		[Serializable]
		private sealed class Foo : ScriptableObject
		{
			private readonly string _prop;

			internal Foo(string label)
			{
				_prop = label;
			}

			public override string GetClassName()
			{
				return "Foo";
			}

			public string GetMyProp()
			{
				return _prop;
			}
		}
	}
}
