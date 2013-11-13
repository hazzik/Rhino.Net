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
	/// <summary>
	/// Test for
	/// <see cref="Rhino.Tests.Context.DecompileScript(Rhino.Tests.Script, int)">Rhino.Tests.Context.DecompileScript(Rhino.Tests.Script, int)</see>
	/// .
	/// </summary>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class DecompileTest
	{
		/// <summary>As of head of trunk on 30.09.09, decompile of "new Date()" returns "new Date" without parentheses.</summary>
		/// <remarks>As of head of trunk on 30.09.09, decompile of "new Date()" returns "new Date" without parentheses.</remarks>
		/// <seealso><a href="https://bugzilla.mozilla.org/show_bug.cgi?id=519692">Bug 519692</a></seealso>
		[NUnit.Framework.Test]
		public virtual void NewObject0Arg()
		{
			string source = "var x = new Date().getTime();";
			ContextAction action = new _ContextAction_28(source);
			Utils.RunWithAllOptimizationLevels(action);
		}

		private sealed class _ContextAction_28 : ContextAction
		{
			public _ContextAction_28(string source)
			{
				this.source = source;
			}

			public object Run(Context cx)
			{
				Script script = cx.CompileString(source, "my script", 0, null);
				NUnit.Framework.Assert.AreEqual(source, cx.DecompileScript(script, 4).Trim());
				return null;
			}

			private readonly string source;
		}
	}
}
