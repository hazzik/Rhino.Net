/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>Test of strict mode APIs.</summary>
	/// <remarks>Test of strict mode APIs.</remarks>
	/// <author>Norris Boyd</author>
	[TestFixture]
	public class StrictModeApiTest
	{
		private ScriptableObject global;

		private ContextFactory contextFactory;

		internal class MyContextFactory : ContextFactory
		{
			protected override bool HasFeature(Context cx, int featureIndex)
			{
				switch (featureIndex)
				{
					case Context.FEATURE_STRICT_MODE:
					case Context.FEATURE_STRICT_VARS:
					case Context.FEATURE_STRICT_EVAL:
					case Context.FEATURE_WARNING_AS_ERROR:
					{
						return true;
					}
				}
				return base.HasFeature(cx, featureIndex);
			}
		}

		[Test]
		public virtual void TestStrictModeError()
		{
			contextFactory = new MyContextFactory();
			Context cx = contextFactory.EnterContext();
			try
			{
				global = cx.InitStandardObjects();
				try
				{
					RunScript("({}.nonexistent);");
					Assert.Fail();
				}
				catch (EvaluatorException e)
				{
					Assert.IsTrue(e.Message.StartsWith("Reference to undefined property"));
				}
			}
			finally
			{
				Context.Exit();
			}
		}

		private object RunScript(string scriptSourceText)
		{
			return contextFactory.Call(cx => cx.EvaluateString(global, scriptSourceText, "test source", 1, null));
		}
	}
}
