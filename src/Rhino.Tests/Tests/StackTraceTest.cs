/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class StackTraceTest
	{
		internal static readonly string LS = Runtime.GetProperty("line.separator");

		/// <summary>As of CVS head on May, 11.</summary>
		/// <remarks>
		/// As of CVS head on May, 11. 2009, stacktrace information is lost when a call to some
		/// native function has been made.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestFailureStackTrace()
		{
			RhinoException.UseMozillaStackStyle(false);
			string source1 = "function f2() { throw 'hello'; }; f2();";
			string source2 = "function f2() { 'H'.toLowerCase(); throw 'hello'; }; f2();";
			string source3 = "function f2() { new java.lang.String('H').toLowerCase(); throw 'hello'; }; f2();";
			string result = "\tat test.js (f2)" + LS + "\tat test.js" + LS;
			RunWithExpectedStackTrace(source1, result);
			RunWithExpectedStackTrace(source2, result);
			RunWithExpectedStackTrace(source3, result);
		}

		private static void RunWithExpectedStackTrace(string source, string expectedStackTrace)
		{
			Utils.RunWithOptimizationLevel(cx =>

			{
				Scriptable scope = cx.InitStandardObjects();
				try
				{
					cx.EvaluateString(scope, source, "test.js", 0, null);
				}
				catch (JavaScriptException e)
				{
					Assert.AreEqual(expectedStackTrace, e.GetScriptStackTrace());
					return null;
				}
				throw new Exception("Exception expected!");
			}, -1);
		}
	}
}
