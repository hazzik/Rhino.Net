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
	[NUnit.Framework.TestFixture]
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

		[NUnit.Framework.Test]
		public virtual void CallOverloadedFunction()
		{
			new ContextFactory().Call(new _ContextAction_25());
		}

		private sealed class _ContextAction_25 : ContextAction
		{
			public _ContextAction_25()
			{
			}

			public object Run(Context cx)
			{
				cx.GetWrapFactory().SetJavaPrimitiveWrap(false);
				NUnit.Framework.Assert.AreEqual("string[]", cx.EvaluateString(cx.InitStandardObjects(), "new org.mozilla.javascript.tests.Bug496585Test().method('one', 'two', 'three')", "<test>", 1, null));
				NUnit.Framework.Assert.AreEqual("string+function", cx.EvaluateString(cx.InitStandardObjects(), "new org.mozilla.javascript.tests.Bug496585Test().method('one', function() {})", "<test>", 1, null));
				return null;
			}
		}
	}
}
