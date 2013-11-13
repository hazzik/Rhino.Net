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
	/// <summary>Test for overloaded varargs/non-varargs methods.</summary>
	/// <remarks>
	/// Test for overloaded varargs/non-varargs methods.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=467396
	/// </remarks>
	/// <author>Hannes Wallnoefer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class Bug467396Test
	{
		[NUnit.Framework.Test]
		public virtual void TestOverloadedVarargs()
		{
			Context cx = ContextFactory.GetGlobal().EnterContext();
			try
			{
				Scriptable scope = cx.InitStandardObjects();
				object result = Unwrap(cx.EvaluateString(scope, "java.lang.reflect.Array.newInstance(java.lang.Object, 1)", "source", 1, null));
				NUnit.Framework.Assert.IsTrue(result is object[]);
				NUnit.Framework.Assert.AreEqual(1, ((object[])result).Length);
				result = Unwrap(cx.EvaluateString(scope, "java.lang.reflect.Array.newInstance(java.lang.Object, [1])", "source", 1, null));
				NUnit.Framework.Assert.IsTrue(result is object[]);
				NUnit.Framework.Assert.AreEqual(1, ((object[])result).Length);
				result = Unwrap(cx.EvaluateString(scope, "java.lang.reflect.Array.newInstance(java.lang.Object, [1, 1])", "source", 1, null));
				NUnit.Framework.Assert.IsTrue(result is object[][]);
				NUnit.Framework.Assert.AreEqual(1, ((object[][])result).Length);
				NUnit.Framework.Assert.AreEqual(1, ((object[][])result)[0].Length);
				result = Unwrap(cx.EvaluateString(scope, "java.lang.reflect.Array.newInstance(java.lang.Object, 1, 1)", "source", 1, null));
				NUnit.Framework.Assert.IsTrue(result is object[][]);
				NUnit.Framework.Assert.AreEqual(1, ((object[][])result).Length);
				NUnit.Framework.Assert.AreEqual(1, ((object[][])result)[0].Length);
			}
			finally
			{
				Context.Exit();
			}
		}

		private object Unwrap(object obj)
		{
			return obj is Wrapper ? ((Wrapper)obj).Unwrap() : obj;
		}
	}
}
