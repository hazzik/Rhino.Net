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
	public class ObjectGetOwnPropertyDescriptorTest
	{
		[NUnit.Framework.Test]
		public virtual void TestContentsOfPropertyDescriptorShouldReflectAttributesOfProperty()
		{
			NativeObject descriptor;
			NativeObject @object = new NativeObject();
			@object.DefineProperty("a", "1", PropertyAttributes.EMPTY);
			@object.DefineProperty("b", "2", PropertyAttributes.DONTENUM | PropertyAttributes.READONLY | PropertyAttributes.PERMANENT);
			descriptor = (NativeObject)Evaluator.Eval("Object.getOwnPropertyDescriptor(obj, 'a')", "obj", @object);
			NUnit.Framework.Assert.AreEqual("1", descriptor.Get("value"));
			NUnit.Framework.Assert.AreEqual(true, descriptor.Get("enumerable"));
			NUnit.Framework.Assert.AreEqual(true, descriptor.Get("writable"));
			NUnit.Framework.Assert.AreEqual(true, descriptor.Get("configurable"));
			descriptor = (NativeObject)Evaluator.Eval("Object.getOwnPropertyDescriptor(obj, 'b')", "obj", @object);
			NUnit.Framework.Assert.AreEqual("2", descriptor.Get("value"));
			NUnit.Framework.Assert.AreEqual(false, descriptor.Get("enumerable"));
			NUnit.Framework.Assert.AreEqual(false, descriptor.Get("writable"));
			NUnit.Framework.Assert.AreEqual(false, descriptor.Get("configurable"));
		}
	}
}
