/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Tests;
using Rhino.Tests.Es5;
using Sharpen;

namespace Rhino.Tests.Es5
{
	[NUnit.Framework.TestFixture]
	public class ObjectKeysTest
	{
		[NUnit.Framework.Test]
		public virtual void ShouldReturnOnlyEnumerablePropertiesOfArg()
		{
			NativeObject @object = new NativeObject();
			@object.DefineProperty("a", "1", PropertyAttributes.EMPTY);
			@object.DefineProperty("b", "2", PropertyAttributes.EMPTY);
			@object.DefineProperty("c", "3", PropertyAttributes.DONTENUM);
			object result = Evaluator.Eval("Object.keys(obj)", "obj", @object);
			NativeArray keys = (NativeArray)result;
			NUnit.Framework.Assert.AreEqual(2, keys.GetLength());
			NUnit.Framework.Assert.AreEqual("a", keys.Get(0, keys));
			NUnit.Framework.Assert.AreEqual("b", keys.Get(1, keys));
		}
	}
}
