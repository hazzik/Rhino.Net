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
	[NUnit.Framework.TestFixture]
	public class Bug492525Test
	{
		[NUnit.Framework.Test]
		public virtual void GetAllIdsShouldIncludeArrayIndices()
		{
			NativeArray array = new NativeArray(new string[] { "a", "b" });
			object[] expectedIds = new object[] { 0, 1, "length" };
			object[] actualIds = array.GetAllIds();
			Assert.AssertArrayEquals(expectedIds, actualIds);
		}
	}
}
