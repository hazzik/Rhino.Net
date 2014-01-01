/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=466207 </summary>
	/// <author>Hannes Wallnoefer</author>
	[TestFixture]
	public class Bug466207Test
	{
		internal List<object> list;

		internal IList<object> reference;

		[SetUp]
		protected virtual void SetUp()
		{
			// set up a reference map
			reference = new List<object>();
			reference.Add("a");
			reference.Add(true);
			reference.Add(new Dictionary<object, object>());
			reference.Add(42);
			reference.Add("a");
			// get a js object as map
			Context context = Context.Enter();
			ScriptableObject scope = context.InitStandardObjects();
			list = ((IList<object>) context.EvaluateString(scope, "(['a', true, new System.Collections.Hashtable(), 42, 'a']);", "testsrc", 1, null)).ToList();
			Context.Exit();
		}

		[Test]
		public virtual void TestEqual()
		{
			// FIXME we do not override equals() and hashCode() in NativeArray
			// so calling this with swapped argument fails. This breaks symmetry
			// of equals(), but overriding these methods might be risky.
			Assert.AreEqual(reference, list);
		}

		[Test]
		public virtual void TestIndexedAccess()
		{
			Assert.IsTrue(list.Count == 5);
			Assert.AreEqual(list[0], reference[0]);
			Assert.AreEqual(list[1], reference[1]);
			Assert.AreEqual(list[2], reference[2]);
			Assert.AreEqual(list[3], reference[3]);
			Assert.AreEqual(list[4], reference[4]);
		}

		[Test]
		public virtual void TestContains()
		{
			Assert.IsTrue(list.Contains("a"));
			Assert.IsTrue(list.Contains(true));
			Assert.IsFalse(list.Contains("x"));
			Assert.IsFalse(list.Contains(false));
			Assert.IsFalse(list.Contains(null));
		}

		[Test]
		public virtual void TestIndexOf()
		{
			Assert.IsTrue(list.IndexOf("a") == 0);
			Assert.IsTrue(list.IndexOf(true) == 1);
			Assert.IsTrue(list.LastIndexOf("a") == 4);
			Assert.IsTrue(list.LastIndexOf(true) == 1);
			Assert.IsTrue(list.IndexOf("x") == -1);
			Assert.IsTrue(list.LastIndexOf("x") == -1);
			Assert.IsTrue(list.IndexOf(null) == -1);
			Assert.IsTrue(list.LastIndexOf(null) == -1);
		}

		[Test]
		public virtual void TestToArray()
		{
			Assert.IsTrue(Arrays.Equals(list.ToArray(), reference.ToArray()));
		}

		[Test]
		public virtual void TestIterator()
		{
			CollectionAssert.AreEqual(list, reference);
		}
	}
}
