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
	/// <summary>
	/// Takes care that the class name of the generated class "looks like"
	/// the provided script name.
	/// </summary>
	/// <remarks>
	/// Takes care that the class name of the generated class "looks like"
	/// the provided script name.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=460283
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class GeneratedClassNameTest
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestGeneratedClassName()
		{
			DoTest("myScript_js", "myScript.js");
			DoTest("foo", "foo");
			DoTest("c", string.Empty);
			DoTest("_1", "1");
			DoTest("_", "_");
			DoTest("unnamed_script", null);
			DoTest("some_dir_some_foo_js", "some/dir/some/foo.js");
			DoTest("some_dir_some_foo_js", "some\\dir\\some\\foo.js");
			DoTest("_12_foo_34_js", "12 foo 34.js");
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTest(string expectedName, string scriptName)
		{
			Script script = (Script)ContextFactory.GetGlobal().Call(new _ContextAction_38(scriptName));
			// remove serial number
			string name = script.GetType().Name;
			NUnit.Framework.Assert.AreEqual(expectedName, Sharpen.Runtime.Substring(name, 0, name.LastIndexOf('_')));
		}

		private sealed class _ContextAction_38 : ContextAction
		{
			public _ContextAction_38(string scriptName)
			{
				this.scriptName = scriptName;
			}

			public object Run(Context context)
			{
				return context.CompileString("var f = 1", scriptName, 1, null);
			}

			private readonly string scriptName;
		}
	}
}
