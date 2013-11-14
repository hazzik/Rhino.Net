/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections;
using System.Collections.Generic;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=448816</summary>
	/// <author>Hannes Wallnoefer</author>
	[NUnit.Framework.TestFixture]
	public class Bug448816Test
	{
		internal IDictionary<object, object> map;

		internal IDictionary<object, object> reference;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			// set up a reference map
			reference = new LinkedHashMap<object, object>();
			reference.Put("a", "a");
			reference.Put("b", true);
			reference.Put("c", new Dictionary<object, object>());
			reference.Put(1, 42);
			// get a js object as map
			Context context = Context.Enter();
			ScriptableObject scope = context.InitStandardObjects();
			map = (IDictionary<object, object>)context.EvaluateString(scope, "({ a: 'a', b: true, c: new java.util.HashMap(), 1: 42});", "testsrc", 1, null);
			Context.Exit();
		}

		[NUnit.Framework.Test]
		public virtual void TestEqual()
		{
			// FIXME we do not override equals() and hashCode() in ScriptableObject
			// so calling this with swapped argument fails. This breaks symmetry
			// of equals(), but overriding these methods might be risky.
			NUnit.Framework.Assert.AreEqual(reference, map);
		}

		[NUnit.Framework.Test]
		public virtual void TestBasicAccess()
		{
			NUnit.Framework.Assert.IsTrue(map.Count == 4);
			NUnit.Framework.Assert.AreEqual(map.Get("a"), reference.Get("a"));
			NUnit.Framework.Assert.AreEqual(map.Get("b"), reference.Get("b"));
			NUnit.Framework.Assert.AreEqual(map.Get("c"), reference.Get("c"));
			NUnit.Framework.Assert.AreEqual(map.Get(1), reference.Get(1));
			NUnit.Framework.Assert.AreEqual(map.Get("notfound"), reference.Get("notfound"));
			NUnit.Framework.Assert.IsTrue(map.ContainsKey("b"));
			NUnit.Framework.Assert.IsTrue(map.ContainsValue(true));
			NUnit.Framework.Assert.IsFalse(map.ContainsKey("x"));
			NUnit.Framework.Assert.IsFalse(map.ContainsValue(false));
			NUnit.Framework.Assert.IsFalse(map.ContainsValue(null));
		}

		[NUnit.Framework.Test]
		public virtual void TestCollections()
		{
			NUnit.Framework.Assert.AreEqual(map.Keys, reference.Keys);
			NUnit.Framework.Assert.AreEqual(map.EntrySet(), reference.EntrySet());
			// java.util.Collection does not imply overriding equals(), so:
			NUnit.Framework.Assert.IsTrue(map.Values.ContainsAll(reference.Values));
			NUnit.Framework.Assert.IsTrue(reference.Values.ContainsAll(map.Values));
		}

		[NUnit.Framework.Test]
		public virtual void TestRemoval()
		{
			// the only update we implement is removal
			NUnit.Framework.Assert.IsTrue(map.Count == 4);
			NUnit.Framework.Assert.AreEqual(Sharpen.Collections.Remove(map, "b"), true);
			Sharpen.Collections.Remove(reference, "b");
			NUnit.Framework.Assert.IsTrue(map.Count == 3);
			NUnit.Framework.Assert.AreEqual(reference, map);
			TestCollections();
		}

		[NUnit.Framework.Test]
		public virtual void TestKeyIterator()
		{
			CompareIterators(map.Keys.GetEnumerator(), reference.Keys.GetEnumerator());
		}

		[NUnit.Framework.Test]
		public virtual void TestEntryIterator()
		{
			CompareIterators(map.EntrySet().GetEnumerator(), reference.EntrySet().GetEnumerator());
		}

		[NUnit.Framework.Test]
		public virtual void TestValueIterator()
		{
			CompareIterators(map.Values.GetEnumerator(), reference.Values.GetEnumerator());
		}

		private void CompareIterators(IEnumerator it1, IEnumerator it2)
		{
			NUnit.Framework.Assert.IsTrue(map.Count == 4);
			while (it1.HasNext())
			{
				NUnit.Framework.Assert.AreEqual(it1.Next(), it2.Next());
				it1.Remove();
				it2.Remove();
			}
			NUnit.Framework.Assert.IsTrue(map.IsEmpty());
		}
	}
}
