/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// <p>This class represents a string composed of two components, each of which
	/// may be a <code>java.lang.String</code> or another ConsString.</p>
	/// <p>This string representation is optimized for concatenation using the "+"
	/// operator.
	/// </summary>
	/// <remarks>
	/// <p>This class represents a string composed of two components, each of which
	/// may be a <code>java.lang.String</code> or another ConsString.</p>
	/// <p>This string representation is optimized for concatenation using the "+"
	/// operator. Instead of immediately copying both components to a new character
	/// array, ConsString keeps references to the original components and only
	/// converts them to a String if either toString() is called or a certain depth
	/// level is reached.</p>
	/// <p>Note that instances of this class are only immutable if both parts are
	/// immutable, i.e. either Strings or ConsStrings that are ultimately composed
	/// of Strings.</p>
	/// <p>Both the name and the concept are borrowed from V8.</p>
	/// </remarks>
	[System.Serializable]
	public class ConsString : CharSequence
	{
		private const long serialVersionUID = -8432806714471372570L;

		private CharSequence s1;

		private CharSequence s2;

		private readonly int length;

		private int depth;

		public ConsString(CharSequence str1, CharSequence str2)
		{
			s1 = str1;
			s2 = str2;
			length = str1.Length + str2.Length;
			depth = 1;
			if (str1 is Rhino.ConsString)
			{
				depth += ((Rhino.ConsString)str1).depth;
			}
			if (str2 is Rhino.ConsString)
			{
				depth += ((Rhino.ConsString)str2).depth;
			}
			// Don't let it grow too deep, can cause stack overflows
			if (depth > 2000)
			{
				Flatten();
			}
		}

		// Replace with string representation when serializing
		private object WriteReplace()
		{
			return this.ToString();
		}

		public override string ToString()
		{
			return depth == 0 ? (string)s1 : Flatten();
		}

		private string Flatten()
		{
			lock (this)
			{
				if (depth > 0)
				{
					StringBuilder b = new StringBuilder(length);
					AppendTo(b);
					s1 = b.ToString();
					s2 = string.Empty;
					depth = 0;
				}
				return (string)s1;
			}
		}

		private void AppendTo(StringBuilder b)
		{
			lock (this)
			{
				AppendFragment(s1, b);
				AppendFragment(s2, b);
			}
		}

		private static void AppendFragment(CharSequence s, StringBuilder b)
		{
			if (s is Rhino.ConsString)
			{
				((Rhino.ConsString)s).AppendTo(b);
			}
			else
			{
				b.Append(s);
			}
		}

		public virtual int Length
		{
			get
			{
				return length;
			}
		}

		public virtual char CharAt(int index)
		{
			string str = depth == 0 ? (string)s1 : Flatten();
			return str[index];
		}

		public virtual CharSequence SubSequence(int start, int end)
		{
			string str = depth == 0 ? (string)s1 : Flatten();
			return Sharpen.Runtime.Substring(str, start, end);
		}
	}
}
