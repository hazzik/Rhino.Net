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
	/// <summary>Primitive numbers are not wrapped before calling apply.</summary>
	/// <remarks>
	/// Primitive numbers are not wrapped before calling apply.
	/// Test for bug <a href="https://bugzilla.mozilla.org/show_bug.cgi?id=466661">466661</a>.
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[TestFixture]
	public class ApplyOnPrimitiveNumberTest
	{
		[Test]
		public virtual void TestIt()
		{
			const string script = "var fn = function() { return this; }\n" + "fn.apply(1)";
			Utils.RunWithAllOptimizationLevels(cx =>
			{
				ScriptableObject scope = cx.InitStandardObjects();
				object result = cx.EvaluateString(scope, script, "test script", 0, null);
				Assert.AreEqual("object", ScriptRuntime.Typeof(result));
				Assert.AreEqual("1", Context.ToString(result));
				return null;
			});
		}
	}
}
