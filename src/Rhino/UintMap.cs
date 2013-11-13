/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>Map to associate non-negative integers to objects or integers.</summary>
	/// <remarks>
	/// Map to associate non-negative integers to objects or integers.
	/// The map does not synchronize any of its operation, so either use
	/// it from a single thread or do own synchronization or perform all mutation
	/// operations on one thread before passing the map to others.
	/// </remarks>
	/// <author>Igor Bukanov</author>
	[System.Serializable]
	public class UintMap
	{
		internal const long serialVersionUID = 4242698212885848444L;

		public UintMap() : this(4)
		{
		}

		public UintMap(int initialCapacity)
		{
			// Map implementation via hashtable,
			// follows "The Art of Computer Programming" by Donald E. Knuth
			if (initialCapacity < 0)
			{
				Kit.CodeBug();
			}
			// Table grow when number of stored keys >= 3/4 of max capacity
			int minimalCapacity = initialCapacity * 4 / 3;
			int i;
			for (i = 2; (1 << i) < minimalCapacity; ++i)
			{
			}
			power = i;
			if (check && power < 2)
			{
				Kit.CodeBug();
			}
		}

		public virtual bool IsEmpty()
		{
			return keyCount == 0;
		}

		public virtual int Size()
		{
			return keyCount;
		}

		public virtual bool Has(int key)
		{
			if (key < 0)
			{
				Kit.CodeBug();
			}
			return 0 <= FindIndex(key);
		}

		/// <summary>Get object value assigned with key.</summary>
		/// <remarks>Get object value assigned with key.</remarks>
		/// <returns>key object value or null if key is absent</returns>
		public virtual object GetObject(int key)
		{
			if (key < 0)
			{
				Kit.CodeBug();
			}
			if (values != null)
			{
				int index = FindIndex(key);
				if (0 <= index)
				{
					return values[index];
				}
			}
			return null;
		}

		/// <summary>Get integer value assigned with key.</summary>
		/// <remarks>Get integer value assigned with key.</remarks>
		/// <returns>key integer value or defaultValue if key is absent</returns>
		public virtual int GetInt(int key, int defaultValue)
		{
			if (key < 0)
			{
				Kit.CodeBug();
			}
			int index = FindIndex(key);
			if (0 <= index)
			{
				if (ivaluesShift != 0)
				{
					return keys[ivaluesShift + index];
				}
				return 0;
			}
			return defaultValue;
		}

		/// <summary>Get integer value assigned with key.</summary>
		/// <remarks>Get integer value assigned with key.</remarks>
		/// <returns>
		/// key integer value or defaultValue if key does not exist or does
		/// not have int value
		/// </returns>
		/// <exception cref="System.Exception">if key does not exist</exception>
		public virtual int GetExistingInt(int key)
		{
			if (key < 0)
			{
				Kit.CodeBug();
			}
			int index = FindIndex(key);
			if (0 <= index)
			{
				if (ivaluesShift != 0)
				{
					return keys[ivaluesShift + index];
				}
				return 0;
			}
			// Key must exist
			Kit.CodeBug();
			return 0;
		}

		/// <summary>Set object value of the key.</summary>
		/// <remarks>
		/// Set object value of the key.
		/// If key does not exist, also set its int value to 0.
		/// </remarks>
		public virtual void Put(int key, object value)
		{
			if (key < 0)
			{
				Kit.CodeBug();
			}
			int index = EnsureIndex(key, false);
			if (values == null)
			{
				values = new object[1 << power];
			}
			values[index] = value;
		}

		/// <summary>Set int value of the key.</summary>
		/// <remarks>
		/// Set int value of the key.
		/// If key does not exist, also set its object value to null.
		/// </remarks>
		public virtual void Put(int key, int value)
		{
			if (key < 0)
			{
				Kit.CodeBug();
			}
			int index = EnsureIndex(key, true);
			if (ivaluesShift == 0)
			{
				int N = 1 << power;
				// keys.length can be N * 2 after clear which set ivaluesShift to 0
				if (keys.Length != N * 2)
				{
					int[] tmp = new int[N * 2];
					System.Array.Copy(keys, 0, tmp, 0, N);
					keys = tmp;
				}
				ivaluesShift = N;
			}
			keys[ivaluesShift + index] = value;
		}

		public virtual void Remove(int key)
		{
			if (key < 0)
			{
				Kit.CodeBug();
			}
			int index = FindIndex(key);
			if (0 <= index)
			{
				keys[index] = DELETED;
				--keyCount;
				// Allow to GC value and make sure that new key with the deleted
				// slot shall get proper default values
				if (values != null)
				{
					values[index] = null;
				}
				if (ivaluesShift != 0)
				{
					keys[ivaluesShift + index] = 0;
				}
			}
		}

		public virtual void Clear()
		{
			int N = 1 << power;
			if (keys != null)
			{
				for (int i = 0; i != N; ++i)
				{
					keys[i] = EMPTY;
				}
				if (values != null)
				{
					for (int i_1 = 0; i_1 != N; ++i_1)
					{
						values[i_1] = null;
					}
				}
			}
			ivaluesShift = 0;
			keyCount = 0;
			occupiedCount = 0;
		}

		/// <summary>Return array of present keys</summary>
		public virtual int[] GetKeys()
		{
			int[] keys = this.keys;
			int n = keyCount;
			int[] result = new int[n];
			for (int i = 0; n != 0; ++i)
			{
				int entry = keys[i];
				if (entry != EMPTY && entry != DELETED)
				{
					result[--n] = entry;
				}
			}
			return result;
		}

		private static int TableLookupStep(int fraction, int mask, int power)
		{
			int shift = 32 - 2 * power;
			if (shift >= 0)
			{
				return (((int)(((uint)fraction) >> shift)) & mask) | 1;
			}
			else
			{
				return (fraction & ((int)(((uint)mask) >> -shift))) | 1;
			}
		}

		private int FindIndex(int key)
		{
			int[] keys = this.keys;
			if (keys != null)
			{
				int fraction = key * A;
				int index = (int)(((uint)fraction) >> (32 - power));
				int entry = keys[index];
				if (entry == key)
				{
					return index;
				}
				if (entry != EMPTY)
				{
					// Search in table after first failed attempt
					int mask = (1 << power) - 1;
					int step = TableLookupStep(fraction, mask, power);
					int n = 0;
					do
					{
						index = (index + step) & mask;
						entry = keys[index];
						if (entry == key)
						{
							return index;
						}
					}
					while (entry != EMPTY);
				}
			}
			return -1;
		}

		// Insert key that is not present to table without deleted entries
		// and enough free space
		private int InsertNewKey(int key)
		{
			if (check && occupiedCount != keyCount)
			{
				Kit.CodeBug();
			}
			if (check && keyCount == 1 << power)
			{
				Kit.CodeBug();
			}
			int[] keys = this.keys;
			int fraction = key * A;
			int index = (int)(((uint)fraction) >> (32 - power));
			if (keys[index] != EMPTY)
			{
				int mask = (1 << power) - 1;
				int step = TableLookupStep(fraction, mask, power);
				int firstIndex = index;
				do
				{
					if (check && keys[index] == DELETED)
					{
						Kit.CodeBug();
					}
					index = (index + step) & mask;
					if (check && firstIndex == index)
					{
						Kit.CodeBug();
					}
				}
				while (keys[index] != EMPTY);
			}
			keys[index] = key;
			++occupiedCount;
			++keyCount;
			return index;
		}

		private void RehashTable(bool ensureIntSpace)
		{
			if (keys != null)
			{
				// Check if removing deleted entries would free enough space
				if (keyCount * 2 >= occupiedCount)
				{
					// Need to grow: less then half of deleted entries
					++power;
				}
			}
			int N = 1 << power;
			int[] old = keys;
			int oldShift = ivaluesShift;
			if (oldShift == 0 && !ensureIntSpace)
			{
				keys = new int[N];
			}
			else
			{
				ivaluesShift = N;
				keys = new int[N * 2];
			}
			for (int i = 0; i != N; ++i)
			{
				keys[i] = EMPTY;
			}
			object[] oldValues = values;
			if (oldValues != null)
			{
				values = new object[N];
			}
			int oldCount = keyCount;
			occupiedCount = 0;
			if (oldCount != 0)
			{
				keyCount = 0;
				for (int i_1 = 0, remaining = oldCount; remaining != 0; ++i_1)
				{
					int key = old[i_1];
					if (key != EMPTY && key != DELETED)
					{
						int index = InsertNewKey(key);
						if (oldValues != null)
						{
							values[index] = oldValues[i_1];
						}
						if (oldShift != 0)
						{
							keys[ivaluesShift + index] = old[oldShift + i_1];
						}
						--remaining;
					}
				}
			}
		}

		// Ensure key index creating one if necessary
		private int EnsureIndex(int key, bool intType)
		{
			int index = -1;
			int firstDeleted = -1;
			int[] keys = this.keys;
			if (keys != null)
			{
				int fraction = key * A;
				index = (int)(((uint)fraction) >> (32 - power));
				int entry = keys[index];
				if (entry == key)
				{
					return index;
				}
				if (entry != EMPTY)
				{
					if (entry == DELETED)
					{
						firstDeleted = index;
					}
					// Search in table after first failed attempt
					int mask = (1 << power) - 1;
					int step = TableLookupStep(fraction, mask, power);
					int n = 0;
					do
					{
						index = (index + step) & mask;
						entry = keys[index];
						if (entry == key)
						{
							return index;
						}
						if (entry == DELETED && firstDeleted < 0)
						{
							firstDeleted = index;
						}
					}
					while (entry != EMPTY);
				}
			}
			// Inserting of new key
			if (check && keys != null && keys[index] != EMPTY)
			{
				Kit.CodeBug();
			}
			if (firstDeleted >= 0)
			{
				index = firstDeleted;
			}
			else
			{
				// Need to consume empty entry: check occupation level
				if (keys == null || occupiedCount * 4 >= (1 << power) * 3)
				{
					// Too litle unused entries: rehash
					RehashTable(intType);
					return InsertNewKey(key);
				}
				++occupiedCount;
			}
			keys[index] = key;
			++keyCount;
			return index;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			@out.DefaultWriteObject();
			int count = keyCount;
			if (count != 0)
			{
				bool hasIntValues = (ivaluesShift != 0);
				bool hasObjectValues = (values != null);
				@out.WriteBoolean(hasIntValues);
				@out.WriteBoolean(hasObjectValues);
				for (int i = 0; count != 0; ++i)
				{
					int key = keys[i];
					if (key != EMPTY && key != DELETED)
					{
						--count;
						@out.WriteInt(key);
						if (hasIntValues)
						{
							@out.WriteInt(keys[ivaluesShift + i]);
						}
						if (hasObjectValues)
						{
							@out.WriteObject(values[i]);
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			int writtenKeyCount = keyCount;
			if (writtenKeyCount != 0)
			{
				keyCount = 0;
				bool hasIntValues = @in.ReadBoolean();
				bool hasObjectValues = @in.ReadBoolean();
				int N = 1 << power;
				if (hasIntValues)
				{
					keys = new int[2 * N];
					ivaluesShift = N;
				}
				else
				{
					keys = new int[N];
				}
				for (int i = 0; i != N; ++i)
				{
					keys[i] = EMPTY;
				}
				if (hasObjectValues)
				{
					values = new object[N];
				}
				for (int i_1 = 0; i_1 != writtenKeyCount; ++i_1)
				{
					int key = @in.ReadInt();
					int index = InsertNewKey(key);
					if (hasIntValues)
					{
						int ivalue = @in.ReadInt();
						keys[ivaluesShift + index] = ivalue;
					}
					if (hasObjectValues)
					{
						values[index] = @in.ReadObject();
					}
				}
			}
		}

		private const int A = unchecked((int)(0x9e3779b9));

		private const int EMPTY = -1;

		private const int DELETED = -2;

		[System.NonSerialized]
		private int[] keys;

		[System.NonSerialized]
		private object[] values;

		private int power;

		private int keyCount;

		[System.NonSerialized]
		private int occupiedCount;

		[System.NonSerialized]
		private int ivaluesShift;

		private const bool check = false;
		// A == golden_ratio * (1 << 32) = ((sqrt(5) - 1) / 2) * (1 << 32)
		// See Knuth etc.
		// Structure of kyes and values arrays (N == 1 << power):
		// keys[0 <= i < N]: key value or EMPTY or DELETED mark
		// values[0 <= i < N]: value of key at keys[i]
		// keys[N <= i < 2N]: int values of keys at keys[i - N]
		// == keyCount + deleted_count
		// If ivaluesShift != 0, keys[ivaluesShift + index] contains integer
		// values associated with keys
		// If true, enables consitency checks
	}
}
