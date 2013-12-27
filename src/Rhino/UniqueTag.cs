/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>Class instances represent serializable tags to mark special Object values.</summary>
	/// <remarks>
	/// Class instances represent serializable tags to mark special Object values.
	/// <p>
	/// Compatibility note: under jdk 1.1 use
	/// Rhino.Serialize.ScriptableInputStream to read serialized
	/// instances of UniqueTag as under this JDK version the default
	/// ObjectInputStream would not restore them correctly as it lacks support
	/// for readResolve method
	/// </remarks>
	[System.Serializable]
	public sealed class UniqueTag
	{
		internal const long serialVersionUID = -4320556826714577259L;

		private const int ID_NOT_FOUND = 1;

		private const int ID_NULL_VALUE = 2;

		private const int ID_DOUBLE_MARK = 3;

		/// <summary>Tag to mark non-existing values.</summary>
		/// <remarks>Tag to mark non-existing values.</remarks>
		public static readonly Rhino.UniqueTag NOT_FOUND = new Rhino.UniqueTag(ID_NOT_FOUND);

		/// <summary>Tag to distinguish between uninitialized and null values.</summary>
		/// <remarks>Tag to distinguish between uninitialized and null values.</remarks>
		public static readonly Rhino.UniqueTag NULL_VALUE = new Rhino.UniqueTag(ID_NULL_VALUE);

		/// <summary>
		/// Tag to indicate that a object represents "double" with the real value
		/// stored somewhere else.
		/// </summary>
		/// <remarks>
		/// Tag to indicate that a object represents "double" with the real value
		/// stored somewhere else.
		/// </remarks>
		public static readonly Rhino.UniqueTag DOUBLE_MARK = new Rhino.UniqueTag(ID_DOUBLE_MARK);

		private readonly int tagId;

		private UniqueTag(int tagId)
		{
			this.tagId = tagId;
		}

		public object ReadResolve()
		{
			switch (tagId)
			{
				case ID_NOT_FOUND:
				{
					return NOT_FOUND;
				}

				case ID_NULL_VALUE:
				{
					return NULL_VALUE;
				}

				case ID_DOUBLE_MARK:
				{
					return DOUBLE_MARK;
				}
			}
			throw new InvalidOperationException(tagId.ToString());
		}

		// Overridden for better debug printouts
		public override string ToString()
		{
			string name;
			switch (tagId)
			{
				case ID_NOT_FOUND:
				{
					name = "NOT_FOUND";
					break;
				}

				case ID_NULL_VALUE:
				{
					name = "NULL_VALUE";
					break;
				}

				case ID_DOUBLE_MARK:
				{
					name = "DOUBLE_MARK";
					break;
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
			return base.ToString() + ": " + name;
		}
	}
}
