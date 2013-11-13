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
	/// <summary>Primitive numbers are not wrapped before calling apply.</summary>
	/// <remarks>
	/// Primitive numbers are not wrapped before calling apply.
	/// Test for bug <a href="https://bugzilla.mozilla.org/show_bug.cgi?id=466661">466661</a>.
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ApplyOnPrimitiveNumberTest
	{
		[NUnit.Framework.Test]
		public virtual void TestIt()
		{
			string script = "var fn = function() { return this; }\n" + "fn.apply(1)";
			ContextAction action = new _ContextAction_27(script);
			Utils.RunWithAllOptimizationLevels(action);
		}

		private sealed class _ContextAction_27 : ContextAction
		{
			public _ContextAction_27(string script)
			{
				this.script = script;
			}

			public object Run(Context _cx)
			{
				ScriptableObject scope = _cx.InitStandardObjects();
				object result = _cx.EvaluateString(scope, script, "test script", 0, null);
				NUnit.Framework.Assert.AreEqual("object", ScriptRuntime.Typeof(result));
				NUnit.Framework.Assert.AreEqual("1", Context.ToString(result));
				return null;
			}

			private readonly string script;
		}
	}
}
