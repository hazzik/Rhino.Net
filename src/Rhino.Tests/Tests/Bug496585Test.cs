/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;

namespace Rhino.Tests
{
	[TestFixture]
	public class Bug496585Test
	{
		public virtual string Method(string one, Function function)
		{
			return "string+function";
		}

		public virtual string Method(params string[] strings)
		{
			return "string[]";
		}

		[Test]
		public virtual void CallOverloadedFunction()
		{
			new ContextFactory().Call(cx =>
			{
				cx.GetWrapFactory().SetJavaPrimitiveWrap(false);
				Assert.AreEqual("string[]", cx.EvaluateString(cx.InitStandardObjects(), "new Rhino.Tests.Bug496585Test().method('one', 'two', 'three')", "<test>", 1, null));
				Assert.AreEqual("string+function", cx.EvaluateString(cx.InitStandardObjects(), "new Rhino.Tests.Bug496585Test().method('one', function() {})", "<test>", 1, null));
				return null;
			});
		}
	}
}
