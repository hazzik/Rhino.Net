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
	/// <author>AndrÃ© Bargull</author>
	[NUnit.Framework.TestFixture]
	public class Bug685403Test
	{
		private Context cx;

		private ScriptableObject scope;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
			cx.SetOptimizationLevel(-1);
			scope = cx.InitStandardObjects();
		}

		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		public static object Continuation(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			ContinuationPending pending = cx.CaptureContinuation();
			throw pending;
		}

		[NUnit.Framework.Test]
		public virtual void Test()
		{
			string source = "var state = '';";
			source += "function A(){state += 'A'}";
			source += "function B(){state += 'B'}";
			source += "function C(){state += 'C'}";
			source += "try { A(); continuation(); B() } finally { C() }";
			source += "state";
			string[] functions = new string[] { "continuation" };
			scope.DefineFunctionProperties(functions, typeof(Bug685403Test), ScriptableObject.DONTENUM);
			object state = null;
			Script script = cx.CompileString(source, string.Empty, 1, null);
			try
			{
				cx.ExecuteScriptWithContinuations(script, scope);
				NUnit.Framework.Assert.Fail("expected ContinuationPending exception");
			}
			catch (ContinuationPending pending)
			{
				state = cx.ResumeContinuation(pending.GetContinuation(), scope, string.Empty);
			}
			NUnit.Framework.Assert.AreEqual("ABC", state);
		}
	}
}
