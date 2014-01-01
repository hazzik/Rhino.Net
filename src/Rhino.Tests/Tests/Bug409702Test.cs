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
	/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=409702 </summary>
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	public class Bug409702Test
	{
		public abstract class Foo
		{
			public Foo()
			{
			}

			public abstract void A();

			public abstract int B();

			public abstract class Subclass : Bug409702Test.Foo
			{
				public sealed override void A()
				{
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestAdapter()
		{
			int value = 12;
			string source = "var instance = " + "  new JavaAdapter(" + typeof (Bug409702Test.Foo.Subclass).FullName.Replace("+", "$") + "," + "{ B: function () { return " + value + "; } });" + "instance.B();";
			Context cx = ContextFactory.GetGlobal().EnterContext();
			try
			{
				Scriptable scope = cx.InitStandardObjects();
				object result = cx.EvaluateString(scope, source, "source", 1, null);
				NUnit.Framework.Assert.AreEqual(value, result);
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
