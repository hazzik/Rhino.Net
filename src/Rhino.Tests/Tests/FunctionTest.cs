/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>Unit tests for Function.</summary>
	/// <remarks>Unit tests for Function.</remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class FunctionTest
	{
		/// <summary>
		/// Test for bug #600479
		/// https://bugzilla.mozilla.org/show_bug.cgi?id=600479
		/// Syntax of function built from Function's constructor string parameter was not correct
		/// when this string contained "//".
		/// </summary>
		/// <remarks>
		/// Test for bug #600479
		/// https://bugzilla.mozilla.org/show_bug.cgi?id=600479
		/// Syntax of function built from Function's constructor string parameter was not correct
		/// when this string contained "//".
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestFunctionWithSlashSlash()
		{
			AssertEvaluates(true, "new Function('return true//;').call()");
		}

		private static void AssertEvaluates(object expected, string source)
		{
			Utils.RunWithAllOptimizationLevels(cx =>
			{
				Scriptable scope = cx.InitStandardObjects();
				object rep = cx.EvaluateString(scope, source, "test.js", 0, null);
				NUnit.Framework.Assert.AreEqual(expected, rep);
				return null;
			});
		}
	}
}
