/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;

namespace Rhino.Tests
{
	/// <summary>Test for overloaded array concat with non-dense arg.</summary>
	/// <remarks>
	/// Test for overloaded array concat with non-dense arg.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=477604
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[TestFixture]
	public class ArrayConcatTest
	{
		[Test]
		public virtual void TestArrayConcat()
		{
			const string script = "var a = ['a0', 'a1'];\n" + "a[3] = 'a3';\n" + "var b = ['b1', 'b2'];\n" + "b.concat(a)";
			Utils.RunWithAllOptimizationLevels(cx =>
			{
				ScriptableObject scope = cx.InitStandardObjects();
				object result = cx.EvaluateString(scope, script, "test script", 0, null);
				Assert.AreEqual("b1,b2,a0,a1,,a3", Context.ToString(result));
				return null;
			});
		}
	}
}
