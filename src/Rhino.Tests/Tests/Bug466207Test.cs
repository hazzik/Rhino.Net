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
	/// <summary>See https://bugzilla.mozilla.org/show_bug.cgi?id=466207</summary>
	/// <author>Hannes Wallnoefer</author>
	[NUnit.Framework.TestFixture]
	public class Bug466207Test
	{
		internal IList<object> list;

		internal IList<object> reference;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			// set up a reference map
			reference = new AList<object>();
			reference.AddItem("a");
			reference.AddItem(true);
			reference.AddItem(new Dictionary<object, object>());
			reference.AddItem(42);
			reference.AddItem("a");
			// get a js object as map
			Context context = Context.Enter();
			ScriptableObject scope = context.InitStandardObjects();
			list = (IList<object>)context.EvaluateString(scope, "(['a', true, new java.util.HashMap(), 42, 'a']);", "testsrc", 1, null);
			Context.Exit();
		}

		[NUnit.Framework.Test]
		public virtual void TestEqual()
		{
			// FIXME we do not override equals() and hashCode() in NativeArray
			// so calling this with swapped argument fails. This breaks symmetry
			// of equals(), but overriding these methods might be risky.
			NUnit.Framework.Assert.AreEqual(reference, list);
		}

		[NUnit.Framework.Test]
		public virtual void TestIndexedAccess()
		{
			NUnit.Framework.Assert.IsTrue(list.Count == 5);
			NUnit.Framework.Assert.AreEqual(list[0], reference[0]);
			NUnit.Framework.Assert.AreEqual(list[1], reference[1]);
			NUnit.Framework.Assert.AreEqual(list[2], reference[2]);
			NUnit.Framework.Assert.AreEqual(list[3], reference[3]);
			NUnit.Framework.Assert.AreEqual(list[4], reference[4]);
		}

		[NUnit.Framework.Test]
		public virtual void TestContains()
		{
			NUnit.Framework.Assert.IsTrue(list.Contains("a"));
			NUnit.Framework.Assert.IsTrue(list.Contains(true));
			NUnit.Framework.Assert.IsFalse(list.Contains("x"));
			NUnit.Framework.Assert.IsFalse(list.Contains(false));
			NUnit.Framework.Assert.IsFalse(list.Contains(null));
		}

		[NUnit.Framework.Test]
		public virtual void TestIndexOf()
		{
			NUnit.Framework.Assert.IsTrue(list.IndexOf("a") == 0);
			NUnit.Framework.Assert.IsTrue(list.IndexOf(true) == 1);
			NUnit.Framework.Assert.IsTrue(list.LastIndexOf("a") == 4);
			NUnit.Framework.Assert.IsTrue(list.LastIndexOf(true) == 1);
			NUnit.Framework.Assert.IsTrue(list.IndexOf("x") == -1);
			NUnit.Framework.Assert.IsTrue(list.LastIndexOf("x") == -1);
			NUnit.Framework.Assert.IsTrue(list.IndexOf(null) == -1);
			NUnit.Framework.Assert.IsTrue(list.LastIndexOf(null) == -1);
		}

		[NUnit.Framework.Test]
		public virtual void TestToArray()
		{
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(Sharpen.Collections.ToArray(list), Sharpen.Collections.ToArray(reference)));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(Sharpen.Collections.ToArray(list, new object[5]), Sharpen.Collections.ToArray(reference, new object[5])));
			NUnit.Framework.Assert.IsTrue(Arrays.Equals(Sharpen.Collections.ToArray(list, new object[6]), Sharpen.Collections.ToArray(reference, new object[6])));
		}

		[NUnit.Framework.Test]
		public virtual void TestIterator()
		{
			CompareIterators(list.GetEnumerator(), reference.GetEnumerator());
			CompareIterators(list.ListIterator(), reference.ListIterator());
			CompareIterators(list.ListIterator(2), reference.ListIterator(2));
			CompareIterators(list.ListIterator(3), reference.ListIterator(3));
			CompareIterators(list.ListIterator(5), reference.ListIterator(5));
			CompareListIterators(list.ListIterator(), reference.ListIterator());
			CompareListIterators(list.ListIterator(2), reference.ListIterator(2));
			CompareListIterators(list.ListIterator(3), reference.ListIterator(3));
			CompareListIterators(list.ListIterator(5), reference.ListIterator(5));
		}

		private void CompareIterators(IEnumerator it1, IEnumerator it2)
		{
			while (it1.HasNext())
			{
				NUnit.Framework.Assert.AreEqual(it1.Next(), it2.Next());
			}
			NUnit.Framework.Assert.IsFalse(it2.HasNext());
		}

		private void CompareListIterators(ListIterator it1, ListIterator it2)
		{
			while (it1.HasPrevious())
			{
				NUnit.Framework.Assert.AreEqual(it1.Previous(), it2.Previous());
			}
			NUnit.Framework.Assert.IsFalse(it2.HasPrevious());
			CompareIterators(it1, it2);
		}
	}
}
