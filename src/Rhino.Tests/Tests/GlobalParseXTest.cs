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
	/// <summary>Tests for global functions parseFloat and parseInt.</summary>
	/// <remarks>Tests for global functions parseFloat and parseInt.</remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class GlobalParseXTest
	{
		/// <summary>
		/// Test for bug #501972
		/// https://bugzilla.mozilla.org/show_bug.cgi?id=501972
		/// Leading whitespaces should be ignored with following white space chars
		/// (see ECMA spec 15.1.2.3)
		/// <TAB>, <SP>, <NBSP>, <FF>, <VT>, <CR>, <LF>, <LS>, <PS>, <USP>
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestParseFloatAndIntWhiteSpaces()
		{
			TestParseFloatWhiteSpaces("\\u00A0 ");
			// <NBSP>
			TestParseFloatWhiteSpaces("\\t ");
			TestParseFloatWhiteSpaces("\\u00A0 ");
			// <NBSP>
			TestParseFloatWhiteSpaces("\\u000C ");
			// <FF>
			TestParseFloatWhiteSpaces("\\u000B ");
			// <VT>
			TestParseFloatWhiteSpaces("\\u000D ");
			// <CR>
			TestParseFloatWhiteSpaces("\\u000A ");
			// <LF>
			TestParseFloatWhiteSpaces("\\u2028 ");
			// <LS>
			TestParseFloatWhiteSpaces("\\u2029 ");
		}

		// <PS>
		private void TestParseFloatWhiteSpaces(string prefix)
		{
			AssertEvaluates("789", "String(parseInt('" + prefix + "789 '))");
			AssertEvaluates("7.89", "String(parseFloat('" + prefix + "7.89 '))");
		}

		/// <summary>
		/// Test for bug #531436
		/// https://bugzilla.mozilla.org/show_bug.cgi?id=531436
		/// Trailing noise should be ignored
		/// (see ECMA spec 15.1.2.3)
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestParseFloatTrailingNoise()
		{
			TestParseFloat("7890", "789e1");
			TestParseFloat("7890", "789E1");
			TestParseFloat("7890", "789E+1");
			TestParseFloat("7890", "789E+1e");
			TestParseFloat("789", "7890E-1");
			TestParseFloat("789", "7890E-1e");
			TestParseFloat("789", "789hello");
			TestParseFloat("789", "789e");
			TestParseFloat("789", "789E");
			TestParseFloat("789", "789e+");
			TestParseFloat("789", "789Efgh");
			TestParseFloat("789", "789efgh");
			TestParseFloat("789", "789e-");
			TestParseFloat("789", "789e-hello");
			TestParseFloat("789", "789e+hello");
			TestParseFloat("789", "789+++hello");
			TestParseFloat("789", "789-e-+hello");
			TestParseFloat("789", "789e+e++hello");
			TestParseFloat("789", "789e-e++hello");
		}

		private void TestParseFloat(string expected, string value)
		{
			AssertEvaluates(expected, "String(parseFloat('" + value + "'))");
		}

		private static void AssertEvaluates(object expected, string source)
		{
			Utils.RunWithAllOptimizationLevels(cx =>
			{
				Scriptable scope = cx.InitStandardObjects();
				object rep = cx.EvaluateString(scope, source, "test.js", 0, null);
				Assert.AreEqual(expected, rep);
				return null;
			});
		}
	}
}
