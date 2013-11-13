/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;
using Rhino.Tests;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class NativeStringTest
	{
		/// <summary>
		/// Test for bug #492359
		/// https://bugzilla.mozilla.org/show_bug.cgi?id=492359
		/// Calling generic String or Array functions without arguments was causing ArrayIndexOutOfBoundsException
		/// in 1.7R2
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TesttoLowerCaseApply()
		{
			AssertEvaluates("hello", "var x = String.toLowerCase; x.apply('HELLO')");
			AssertEvaluates("hello", "String.toLowerCase('HELLO')");
		}

		// first patch proposed to #492359 was breaking this
		private void AssertEvaluates(object expected, string source)
		{
			ContextAction action = new _ContextAction_33(source, expected);
			Utils.RunWithAllOptimizationLevels(action);
		}

		private sealed class _ContextAction_33 : ContextAction
		{
			public _ContextAction_33(string source, object expected)
			{
				this.source = source;
				this.expected = expected;
			}

			public object Run(Context cx)
			{
				Scriptable scope = cx.InitStandardObjects();
				object rep = cx.EvaluateString(scope, source, "test.js", 0, null);
				NUnit.Framework.Assert.AreEqual(expected, rep);
				return null;
			}

			private readonly string source;

			private readonly object expected;
		}
	}
}
