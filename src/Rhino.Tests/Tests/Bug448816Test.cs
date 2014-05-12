/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Rhino;
using Rhino.Tests;
using Rhino.Utils;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=448816</summary>
	/// <author>Hannes Wallnoefer</author>
	[TestFixture]
	public class Bug448816Test
	{
		internal IDictionary<object, object> map;

		internal IDictionary<object, object> reference;

		[SetUp]
		protected virtual void SetUp()
		{
			// set up a reference map
			reference = new LinkedHashMap<object, object>();
			reference["a"] = "a";
			reference["b"] = true;
			reference["c"] = new Dictionary<object, object>();
			reference[1] = 42;
			// get a js object as map
			Context context = Context.Enter();
			ScriptableObject scope = context.InitStandardObjects();
			map = (IDictionary<object, object>)context.EvaluateString(scope, "({ a: 'a', b: true, c: new System.Collections.Hashtable(), 1: 42});", "testsrc", 1, null);
			Context.Exit();
		}

		[Test]
		public virtual void TestEqual()
		{
			// FIXME we do not override equals() and hashCode() in ScriptableObject
			// so calling this with swapped argument fails. This breaks symmetry
			// of equals(), but overriding these methods might be risky.
			Assert.AreEqual(reference, map);
		}

		[Test]
		public virtual void TestBasicAccess()
		{
			Assert.IsTrue(map.Count == 4);
			Assert.AreEqual(map.GetValueOrDefault("a"), reference.GetValueOrDefault("a"));
			Assert.AreEqual(map.GetValueOrDefault("b"), reference.GetValueOrDefault("b"));
			Assert.AreEqual(map.GetValueOrDefault("c"), reference.GetValueOrDefault("c"));
			Assert.AreEqual(map.GetValueOrDefault(1), reference.GetValueOrDefault(1));
			Assert.AreEqual(map.GetValueOrDefault("notfound"), reference.GetValueOrDefault("notfound"));
			Assert.IsTrue(map.ContainsKey("b"));
			Assert.IsTrue(map.Values.Contains(true));
			Assert.IsFalse(map.ContainsKey("x"));
			Assert.IsFalse(map.Values.Contains(false));
			Assert.IsFalse(map.Values.Contains(null));
		}

		[Test]
		public virtual void TestCollections()
		{
			Assert.AreEqual(map.Keys, reference.Keys);
			Assert.AreEqual(map, reference);
			// java.util.Collection does not imply overriding equals(), so:
			Assert.IsTrue(map.Values.ContainsAll(reference.Values));
			Assert.IsTrue(reference.Values.ContainsAll(map.Values));
		}

		[Test]
		public virtual void TestRemoval()
		{
			// the only update we implement is removal
			Assert.IsTrue(map.Count == 4);
			object ret;
			object local;
			if (map.TryGetValue ("b", out local))
			{
				map.Remove ("b");
				ret = local;
			}
			else
				ret = default(object);
			Assert.AreEqual(ret, true);
			reference.Remove("b");
			Assert.IsTrue(map.Count == 3);
			Assert.AreEqual(reference, map);
			TestCollections();
		}

		[Test]
		public virtual void TestKeyIterator()
		{
			CompareIterators(map.Keys.GetEnumerator(), reference.Keys.GetEnumerator());
		}

		[Test]
		public virtual void TestEntryIterator()
		{
			CompareIterators(map.GetEnumerator(), reference.GetEnumerator());
		}

		[Test]
		public virtual void TestValueIterator()
		{
			CompareIterators(map.Values.GetEnumerator(), reference.Values.GetEnumerator());
		}

		private void CompareIterators(IEnumerator it1, IEnumerator it2)
		{
			Assert.IsTrue(map.Count == 4);
			while (it1.MoveNext() && it2.MoveNext())
			{
				Assert.AreEqual(it1.Current, it2.Current);
			}
			//NUnit.Framework.Assert.IsTrue(map.IsEmpty());
		}
	}
}
