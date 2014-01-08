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
	public class Bug637811Test
	{
		private Context cx;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = new _ContextFactory_22().EnterContext();
		}

		private sealed class _ContextFactory_22 : ContextFactory
		{
			public _ContextFactory_22()
			{
			}

			protected override bool HasFeature(Context cx, int featureIndex)
			{
				switch (featureIndex)
				{
					case Context.FEATURE_STRICT_MODE:
					case Context.FEATURE_WARNING_AS_ERROR:
					{
						return true;
					}
				}
				return base.HasFeature(cx, featureIndex);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		[NUnit.Framework.Test]
		public virtual void Test()
		{
			string source = string.Empty;
			source += "var x = 0;";
			source += "bar: while (x < 0) { x = x + 1; }";
			cx.CompileString(source, string.Empty, 1, null);
		}
	}
}
