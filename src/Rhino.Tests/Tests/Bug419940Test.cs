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
	/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=419940</summary>
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	public class Bug419940Test
	{
		internal const int value = 12;

		public abstract class BaseFoo
		{
			public abstract int DoSomething();
		}

		public class Foo : Bug419940Test.BaseFoo
		{
			public override int DoSomething()
			{
				return value;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestAdapter()
		{
			string source = "(new JavaAdapter(" + typeof(Bug419940Test.Foo).FullName + ", {})).doSomething();";
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
