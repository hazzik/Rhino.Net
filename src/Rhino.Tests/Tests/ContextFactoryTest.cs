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
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	public class ContextFactoryTest
	{
		internal class MyFactory : ContextFactory
		{
			protected override bool HasFeature(Context cx, int featureIndex)
			{
				switch (featureIndex)
				{
					case Context.FEATURE_MEMBER_EXPR_AS_FUNCTION_NAME:
					{
						return true;
					}
				}
				return base.HasFeature(cx, featureIndex);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestCustomContextFactory()
		{
			ContextFactory factory = new ContextFactoryTest.MyFactory();
			Context cx = factory.EnterContext();
			try
			{
				Scriptable globalScope = cx.InitStandardObjects();
			}
			catch (RhinoException e)
			{
				// Test that FEATURE_MEMBER_EXPR_AS_FUNCTION_NAME is enabled
				Fail(e.ToString());
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
