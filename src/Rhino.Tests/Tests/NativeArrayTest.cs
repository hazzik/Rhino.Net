/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NUnit.Framework;
using Org.Hamcrest.Core;
using Rhino;
using Rhino.Tests;
using Sharpen;

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
			Assert.AssertThat(array.GetIds(), IS.Is(new object[] { 0, "a" }));
		}

		[NUnit.Framework.Test]
		public virtual void DeleteShouldRemoveIndexProperties()
		{
			array.Put(0, array, "a");
			array.Delete(0);
			Assert.AssertThat(array.Has(0, array), IS.Is(false));
		}

		[NUnit.Framework.Test]
		public virtual void DeleteShouldRemoveNormalProperties()
		{
			array.Put("p", array, "a");
			array.Delete("p");
			Assert.AssertThat(array.Has("p", array), IS.Is(false));
		}

		[NUnit.Framework.Test]
		public virtual void PutShouldAddIndexProperties()
		{
			array.Put(0, array, "a");
			Assert.AssertThat(array.Has(0, array), IS.Is(true));
		}

		[NUnit.Framework.Test]
		public virtual void PutShouldAddNormalProperties()
		{
			array.Put("p", array, "a");
			Assert.AssertThat(array.Has("p", array), IS.Is(true));
		}

		[NUnit.Framework.Test]
		public virtual void GetShouldReturnIndexProperties()
		{
			array.Put(0, array, "a");
			array.Put("p", array, "b");
			Assert.AssertThat((string)array.Get(0, array), IS.Is("a"));
		}

		[NUnit.Framework.Test]
		public virtual void GetShouldReturnNormalProperties()
		{
			array.Put("p", array, "a");
			Assert.AssertThat((string)array.Get("p", array), IS.Is("a"));
		}

		[NUnit.Framework.Test]
		public virtual void HasShouldBeFalseForANewArray()
		{
			Assert.AssertThat(new NativeArray(0).Has(0, array), IS.Is(false));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldBeEmptyForEmptyArray()
		{
			Assert.AssertThat(new NativeArray(0).GetIndexIds(), IS.Is(new int[] {  }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldBeAZeroForSimpleSingletonArray()
		{
			array.Put(0, array, "a");
			Assert.AssertThat(array.GetIndexIds(), IS.Is(new int[] { 0 }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldWorkWhenIndicesSetAsString()
		{
			array.Put("0", array, "a");
			Assert.AssertThat(array.GetIndexIds(), IS.Is(new int[] { 0 }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldNotIncludeNegativeIds()
		{
			array.Put(-1, array, "a");
			Assert.AssertThat(array.GetIndexIds(), IS.Is(new int[] {  }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldIncludeIdsLessThan2ToThe32()
		{
			int maxIndex = (int)(1L << 31) - 1;
			array.Put(maxIndex, array, "a");
			Assert.AssertThat(array.GetIndexIds(), IS.Is(new int[] { maxIndex }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldNotIncludeIdsGreaterThanOrEqualTo2ToThe32()
		{
			array.Put((1L << 31) + string.Empty, array, "a");
			Assert.AssertThat(array.GetIndexIds(), IS.Is(new int[] {  }));
		}

		[NUnit.Framework.Test]
		public virtual void GetIndexIdsShouldNotReturnNonNumericIds()
		{
			array.Put("x", array, "a");
			Assert.AssertThat(array.GetIndexIds(), IS.Is(new int[] {  }));
		}
	}
}
