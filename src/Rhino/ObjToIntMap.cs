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
	/// <summary>Map to associate objects to integers.</summary>
	/// <remarks>
	/// Map to associate objects to integers.
	/// The map does not synchronize any of its operation, so either use
	/// it from a single thread or do own synchronization or perform all mutation
	/// operations on one thread before passing the map to others
	/// </remarks>
	/// <author>Igor Bukanov</author>
	[System.Serializable]
	public class ObjToIntMap
	{
		internal const long serialVersionUID = -1542220580748809402L;

		public class Iterator
		{
			internal Iterator(ObjToIntMap master)
			{
				// Map implementation via hashtable,
				// follows "The Art of Computer Programming" by Donald E. Knuth
				// ObjToIntMap is a copy cat of ObjToIntMap with API adjusted to object keys
				this.master = master;
			}

			internal void Init(object[] keys, int[] values, int keyCount)
			{
				this.keys = keys;
				this.values = values;
				this.cursor = -1;
				this.remaining = keyCount;
			}

			public virtual void Start()
			{
				master.InitIterator(this);
				Next();
			}

			public virtual bool Done()
			{
				return remaining < 0;
			}

			public virtual void Next()
			{
				if (remaining == -1)
				{
					Kit.CodeBug();
				}
				if (remaining == 0)
				{
					remaining = -1;
					cursor = -1;
				}
				else
				{
					for (++cursor; ; ++cursor)
					{
						object key = keys[cursor];
						if (key != null && key != DELETED)
						{
							--remaining;
							break;
						}
					}
				}
			}

			public virtual object GetKey()
			{
				object key = keys[cursor];
				if (key == UniqueTag.NULL_VALUE)
				{
					key = null;
				}
				return key;
			}

			public virtual int GetValue()
			{
				return values[cursor];
			}

			public virtual void SetValue(int value)
			{
				values[cursor] = value;
			}

			internal ObjToIntMap master;

			private int cursor;

			private int remaining;

			private object[] keys;

			private int[] values;
		}

		public ObjToIntMap() : this(4)
		{
		}

		public ObjToIntMap(int keyCountHint)
		{
			if (keyCountHint < 0)
			{
				Kit.CodeBug();
			}
			// Table grow when number of stored keys >= 3/4 of max capacity
			int minimalCapacity = keyCountHint * 4 / 3;
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

		public virtual bool Has(object key)
		{
			if (key == null)
			{
				key = UniqueTag.NULL_VALUE;
			}
			return 0 <= FindIndex(key);
		}

		/// <summary>Get integer value assigned with key.</summary>
		/// <remarks>Get integer value assigned with key.</remarks>
		/// <returns>key integer value or defaultValue if key is absent</returns>
		public virtual int Get(object key, int defaultValue)
		{
			if (key == null)
			{
				key = UniqueTag.NULL_VALUE;
			}
			int index = FindIndex(key);
			if (0 <= index)
			{
				return values[index];
			}
			return defaultValue;
		}

		/// <summary>Get integer value assigned with key.</summary>
		/// <remarks>Get integer value assigned with key.</remarks>
		/// <returns>key integer value</returns>
		/// <exception cref="System.Exception">if key does not exist</exception>
		public virtual int GetExisting(object key)
		{
			if (key == null)
			{
				key = UniqueTag.NULL_VALUE;
			}
			int index = FindIndex(key);
			if (0 <= index)
			{
				return values[index];
			}
			// Key must exist
			Kit.CodeBug();
			return 0;
		}

		public virtual void Put(object key, int value)
		{
			if (key == null)
			{
				key = UniqueTag.NULL_VALUE;
			}
			int index = EnsureIndex(key);
			values[index] = value;
		}

		/// <summary>
		/// If table already contains a key that equals to keyArg, return that key
		/// while setting its value to zero, otherwise add keyArg with 0 value to
		/// the table and return it.
		/// </summary>
		/// <remarks>
		/// If table already contains a key that equals to keyArg, return that key
		/// while setting its value to zero, otherwise add keyArg with 0 value to
		/// the table and return it.
		/// </remarks>
		public virtual object Intern(object keyArg)
		{
			bool nullKey = false;
			if (keyArg == null)
			{
				nullKey = true;
				keyArg = UniqueTag.NULL_VALUE;
			}
			int index = EnsureIndex(keyArg);
			values[index] = 0;
			return (nullKey) ? null : keys[index];
		}

		public virtual void Remove(object key)
		{
			if (key == null)
			{
				key = UniqueTag.NULL_VALUE;
			}
			int index = FindIndex(key);
			if (0 <= index)
			{
				keys[index] = DELETED;
				--keyCount;
			}
		}

		public virtual void Clear()
		{
			int i = keys.Length;
			while (i != 0)
			{
				keys[--i] = null;
			}
			keyCount = 0;
			occupiedCount = 0;
		}

		public virtual ObjToIntMap.Iterator NewIterator()
		{
			return new ObjToIntMap.Iterator(this);
		}

		// The sole purpose of the method is to avoid accessing private fields
		// from the Iterator inner class to workaround JDK 1.1 compiler bug which
		// generates code triggering VerifierError on recent JVMs
		internal void InitIterator(ObjToIntMap.Iterator i)
		{
			i.Init(keys, values, keyCount);
		}

		/// <summary>Return array of present keys</summary>
		public virtual object[] GetKeys()
		{
			object[] array = new object[keyCount];
			GetKeys(array, 0);
			return array;
		}

		public virtual void GetKeys(object[] array, int offset)
		{
			int count = keyCount;
			for (int i = 0; count != 0; ++i)
			{
				object key = keys[i];
				if (key != null && key != DELETED)
				{
					if (key == UniqueTag.NULL_VALUE)
					{
						key = null;
					}
					array[offset] = key;
					++offset;
					--count;
				}
			}
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

		private int FindIndex(object key)
		{
			if (keys != null)
			{
				int hash = key.GetHashCode();
				int fraction = hash * A;
				int index = (int)(((uint)fraction) >> (32 - power));
				object test = keys[index];
				if (test != null)
				{
					int N = 1 << power;
					if (test == key || (values[N + index] == hash && test.Equals(key)))
					{
						return index;
					}
					// Search in table after first failed attempt
					int mask = N - 1;
					int step = TableLookupStep(fraction, mask, power);
					int n = 0;
					for (; ; )
					{
						index = (index + step) & mask;
						test = keys[index];
						if (test == null)
						{
							break;
						}
						if (test == key || (values[N + index] == hash && test.Equals(key)))
						{
							return index;
						}
					}
				}
			}
			return -1;
		}

		// Insert key that is not present to table without deleted entries
		// and enough free space
		private int InsertNewKey(object key, int hash)
		{
			if (check && occupiedCount != keyCount)
			{
				Kit.CodeBug();
			}
			if (check && keyCount == 1 << power)
			{
				Kit.CodeBug();
			}
			int fraction = hash * A;
			int index = (int)(((uint)fraction) >> (32 - power));
			int N = 1 << power;
			if (keys[index] != null)
			{
				int mask = N - 1;
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
				while (keys[index] != null);
			}
			keys[index] = key;
			values[N + index] = hash;
			++occupiedCount;
			++keyCount;
			return index;
		}

		private void RehashTable()
		{
			if (keys == null)
			{
				if (check && keyCount != 0)
				{
					Kit.CodeBug();
				}
				if (check && occupiedCount != 0)
				{
					Kit.CodeBug();
				}
				int N = 1 << power;
				keys = new object[N];
				values = new int[2 * N];
			}
			else
			{
				// Check if removing deleted entries would free enough space
				if (keyCount * 2 >= occupiedCount)
				{
					// Need to grow: less then half of deleted entries
					++power;
				}
				int N = 1 << power;
				object[] oldKeys = keys;
				int[] oldValues = values;
				int oldN = oldKeys.Length;
				keys = new object[N];
				values = new int[2 * N];
				int remaining = keyCount;
				occupiedCount = keyCount = 0;
				for (int i = 0; remaining != 0; ++i)
				{
					object key = oldKeys[i];
					if (key != null && key != DELETED)
					{
						int keyHash = oldValues[oldN + i];
						int index = InsertNewKey(key, keyHash);
						values[index] = oldValues[i];
						--remaining;
					}
				}
			}
		}

		// Ensure key index creating one if necessary
		private int EnsureIndex(object key)
		{
			int hash = key.GetHashCode();
			int index = -1;
			int firstDeleted = -1;
			if (keys != null)
			{
				int fraction = hash * A;
				index = (int)(((uint)fraction) >> (32 - power));
				object test = keys[index];
				if (test != null)
				{
					int N = 1 << power;
					if (test == key || (values[N + index] == hash && test.Equals(key)))
					{
						return index;
					}
					if (test == DELETED)
					{
						firstDeleted = index;
					}
					// Search in table after first failed attempt
					int mask = N - 1;
					int step = TableLookupStep(fraction, mask, power);
					int n = 0;
					for (; ; )
					{
						index = (index + step) & mask;
						test = keys[index];
						if (test == null)
						{
							break;
						}
						if (test == key || (values[N + index] == hash && test.Equals(key)))
						{
							return index;
						}
						if (test == DELETED && firstDeleted < 0)
						{
							firstDeleted = index;
						}
					}
				}
			}
			// Inserting of new key
			if (check && keys != null && keys[index] != null)
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
					RehashTable();
					return InsertNewKey(key, hash);
				}
				++occupiedCount;
			}
			keys[index] = key;
			values[(1 << power) + index] = hash;
			++keyCount;
			return index;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			@out.DefaultWriteObject();
			int count = keyCount;
			for (int i = 0; count != 0; ++i)
			{
				object key = keys[i];
				if (key != null && key != DELETED)
				{
					--count;
					@out.WriteObject(key);
					@out.WriteInt(values[i]);
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
				int N = 1 << power;
				keys = new object[N];
				values = new int[2 * N];
				for (int i = 0; i != writtenKeyCount; ++i)
				{
					object key = @in.ReadObject();
					int hash = key.GetHashCode();
					int index = InsertNewKey(key, hash);
					values[index] = @in.ReadInt();
				}
			}
		}

		private const int A = unchecked((int)(0x9e3779b9));

		private static readonly object DELETED = new object();

		[System.NonSerialized]
		private object[] keys;

		[System.NonSerialized]
		private int[] values;

		private int power;

		private int keyCount;

		[System.NonSerialized]
		private int occupiedCount;

		private const bool check = false;
		// A == golden_ratio * (1 << 32) = ((sqrt(5) - 1) / 2) * (1 << 32)
		// See Knuth etc.
		// Structure of kyes and values arrays (N == 1 << power):
		// keys[0 <= i < N]: key value or null or DELETED mark
		// values[0 <= i < N]: value of key at keys[i]
		// values[N <= i < 2*N]: hash code of key at keys[i-N]
		// == keyCount + deleted_count
		// If true, enables consitency checks
	}
}
