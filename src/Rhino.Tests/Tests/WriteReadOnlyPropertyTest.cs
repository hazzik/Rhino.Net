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
using Rhino;
using Rhino.Tests;
using Sharpen;

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
	[NUnit.Framework.TestFixture]
	public class WriteReadOnlyPropertyTest
	{
		/// <exception cref="System.Exception">if the test fails</exception>
		[NUnit.Framework.Test]
		public virtual void TestWriteReadOnly_accepted()
		{
			TestWriteReadOnly(true);
		}

		/// <exception cref="System.Exception">if the test fails</exception>
		[NUnit.Framework.Test]
		public virtual void TestWriteReadOnly_throws()
		{
			try
			{
				TestWriteReadOnly(false);
				NUnit.Framework.Assert.Fail();
			}
			catch (EcmaError e)
			{
				NUnit.Framework.Assert.IsTrue(e.Message.Contains("Cannot set property myProp that has only a getter"), e.Message);
			}
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void TestWriteReadOnly(bool acceptWriteReadOnly)
		{
			MethodInfo readMethod = typeof(WriteReadOnlyPropertyTest.Foo).GetMethod("getMyProp", (Type[])null);
			WriteReadOnlyPropertyTest.Foo foo = new WriteReadOnlyPropertyTest.Foo("hello");
			foo.DefineProperty("myProp", null, readMethod, null, ScriptableObject.EMPTY);
			string script = "foo.myProp = 123; foo.myProp";
			ContextFactory contextFactory = new _ContextFactory_66(acceptWriteReadOnly);
			contextFactory.Call(cx =>
			{
				ScriptableObject top = cx.InitStandardObjects();
				ScriptableObject.PutProperty(top, "foo", foo);
				cx.EvaluateString(top, script, "script", 0, null);
				return null;
			});
		}

		private sealed class _ContextFactory_66 : ContextFactory
		{
			public _ContextFactory_66(bool acceptWriteReadOnly)
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
		[System.Serializable]
		internal class Foo : ScriptableObject
		{
			internal readonly string prop_;

			internal Foo(string label)
			{
				prop_ = label;
			}

			public override string GetClassName()
			{
				return "Foo";
			}

			public virtual string GetMyProp()
			{
				return prop_;
			}
		}
	}
}
