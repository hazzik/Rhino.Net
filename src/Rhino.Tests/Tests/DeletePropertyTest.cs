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
	/// <summary>Test for delete that should apply for properties defined in prototype chain.</summary>
	/// <remarks>
	/// Test for delete that should apply for properties defined in prototype chain.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=510504
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class DeletePropertyTest
	{
		/// <summary>delete should not delete anything in the prototype chain.</summary>
		/// <remarks>delete should not delete anything in the prototype chain.</remarks>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		[NUnit.Framework.Test]
		public virtual void TestDeletePropInPrototype()
		{
			string script = "Array.prototype.foo = function() {};\n" + "Array.prototype[1] = function() {};\n" + "var t = [];\n" + "[].foo();\n" + "for (var i in t) delete t[i];\n" + "[].foo();\n" + "[][1]();\n";
			ContextAction action = new _ContextAction_35(script);
			Utils.RunWithAllOptimizationLevels(action);
		}

		private sealed class _ContextAction_35 : ContextAction
		{
			public _ContextAction_35(string script)
			{
				this.script = script;
			}

			public object Run(Context _cx)
			{
				ScriptableObject scope = _cx.InitStandardObjects();
				object result = _cx.EvaluateString(scope, script, "test script", 0, null);
				return null;
			}

			private readonly string script;
		}
	}
}
