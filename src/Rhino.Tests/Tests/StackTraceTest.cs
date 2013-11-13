/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Rhino.Tests;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
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

		private void RunWithExpectedStackTrace(string _source, string _expectedStackTrace)
		{
			ContextAction action = new _ContextAction_42(_source, _expectedStackTrace);
			Utils.RunWithOptimizationLevel(action, -1);
		}

		private sealed class _ContextAction_42 : ContextAction
		{
			public _ContextAction_42(string _source, string _expectedStackTrace)
			{
				this._source = _source;
				this._expectedStackTrace = _expectedStackTrace;
			}

			public object Run(Context cx)
			{
				Scriptable scope = cx.InitStandardObjects();
				try
				{
					cx.EvaluateString(scope, _source, "test.js", 0, null);
				}
				catch (JavaScriptException e)
				{
					NUnit.Framework.Assert.AreEqual(_expectedStackTrace, e.GetScriptStackTrace());
					return null;
				}
				throw new Exception("Exception expected!");
			}

			private readonly string _source;

			private readonly string _expectedStackTrace;
		}
	}
}
