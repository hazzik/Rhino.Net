/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tests;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=412433</summary>
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class Bug412433Test
	{
		[NUnit.Framework.Test]
		public virtual void TestMalformedJavascript2()
		{
			Context context = Context.Enter();
			try
			{
				ScriptableObject scope = context.InitStandardObjects();
				context.EvaluateString(scope, "\"\".split(/[/?,/&]/)", string.Empty, 0, null);
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
