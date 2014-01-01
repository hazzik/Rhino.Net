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
	[NUnit.Framework.TestFixture]
	public class NativeArrayTest
	{
		private NativeArray array;

		[SetUp]
		public virtual void Init()
		{
			array = new NativeArray(1);
		}

		[NUnit.Framework.Test]
		public virtual void GetIdsShouldIncludeBothIndexAndNormalProperties()
		{
			array.Put(0, array, "index");
			array.Put("a", array, "normal");
			NUnit.Framework.Assert.That(array.GetIds(), Is.EqualTo(new object[] { 0, "a" }));
		}

		[NUnit.Framework.Test]
		public virtual void DeleteShouldRemoveIndexProperties()
		{
			array.Put(0, array, "a");
			array.Delete(0);
			NUnit.Framework.Assert.That(array.Has(0, array), Is.EqualTo(false));
		}

		[NUnit.Framework.Test]
		public virtual void DeleteShouldRemoveNormalProperties()
		{
			array.Put("p", array, "a");
			array.Delete("p");
			NUnit.Framework.Assert.That(array.Has("p", array), Is.EqualTo(false));
		}

		[NUnit.Framework.Test]
		public virtual void PutShouldAddIndexProperties()
		{
			array.Put(0, array, "a");
			NUnit.Framework.Assert.That(array.Has(0, array), Is.EqualTo(true));
		}

		[NUnit.Framework.Test]
		public virtual void PutShouldAddNormalProperties()
		{
			array.Put("p", array, "a");
			NUnit.Framework.Assert.That(array.Has("p", array), Is.EqualTo(true));
		}

		[NUnit.Framework.Test]
		public virtual void GetShouldReturnIndexProperties()
		{
			array.Put(0, array, "a");
			array.Put("p", array, "b");
			NUnit.Framework.Assert.That((string)array.Get(0, array), Is.EqualTo("a"));
		}

		[NUnit.Framework.Test]
		public virtual void GetShouldReturnNormalProperties()
		{
			array.Put("p", array, "a");
			NUnit.Framework.Assert.That((string)array.Get("p", array), Is.EqualTo("a"));
		}

		[NUnit.Framework.Test]
		public virtual void HasShouldBeFalseForANewArray()
		{
			NUnit.Framework.Assert.That(new NativeArray(0).Has(0, array), Is.EqualTo(false));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldBeEmptyForEmptyArray()
		{
			NUnit.Framework.Assert.That(new NativeArray(0).GetIndexIds(), Is.EqualTo(new int[] {  }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldBeAZeroForSimpleSingletonArray()
		{
			array.Put(0, array, "a");
			NUnit.Framework.Assert.That(array.GetIndexIds(), Is.EqualTo(new int[] { 0 }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldWorkWhenIndicesSetAsString()
		{
			array.Put("0", array, "a");
			NUnit.Framework.Assert.That(array.GetIndexIds(), Is.EqualTo(new int[] { 0 }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldNotIncludeNegativeIds()
		{
			array.Put(-1, array, "a");
			NUnit.Framework.Assert.That(array.GetIndexIds(), Is.EqualTo(new int[] {  }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldIncludeIdsLessThan2ToThe32()
		{
			int maxIndex = int.MaxValue;
			array.Put(maxIndex, array, "a");
			NUnit.Framework.Assert.That(array.GetIndexIds(), Is.EqualTo(new int[] { maxIndex }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldNotIncludeIdsGreaterThanOrEqualTo2ToThe32()
		{
			array.Put((1L << 31) + string.Empty, array, "a");
			NUnit.Framework.Assert.That(array.GetIndexIds(), Is.EqualTo(new int[] {  }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldNotReturnNonNumericIds()
		{
			array.Put("x", array, "a");
			NUnit.Framework.Assert.That(array.GetIndexIds(), Is.EqualTo(new int[] {  }));
		}
	}
}
