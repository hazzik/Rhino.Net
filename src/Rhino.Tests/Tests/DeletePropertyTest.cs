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
	/// <summary>Test for delete that should apply for properties defined in prototype chain.</summary>
	/// <remarks>
	/// Test for delete that should apply for properties defined in prototype chain.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=510504
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[TestFixture]
	public class DeletePropertyTest
	{
		/// <summary>delete should not delete anything in the prototype chain.</summary>
		/// <remarks>delete should not delete anything in the prototype chain.</remarks>
		/// <exception cref="System.Exception"></exception>
		[Test]
		public void TestDeletePropInPrototype()
		{
			const string script = "Array.prototype.foo = function() {};\n" +
								  "Array.prototype[1] = function() {};\n" +
								  "var t = [];\n" +
								  "[].foo();\n" +
								  "for (var i in t) delete t[i];\n" +
								  "[].foo();\n" +
								  "[][1]();\n";

			Utils.RunWithAllOptimizationLevels(cx =>
			{
				ScriptableObject scope = cx.InitStandardObjects();
				object result = cx.EvaluateString(scope, script, "test script", 0, null);
				return null;
			});
		}
	}
}
