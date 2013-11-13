/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Sharpen;

namespace Rhino
{
	/// <summary>Implementation of resizable array with focus on minimizing memory usage by storing few initial array elements in object fields.</summary>
	/// <remarks>Implementation of resizable array with focus on minimizing memory usage by storing few initial array elements in object fields. Can also be used as a stack.</remarks>
	[System.Serializable]
	public class ObjArray
	{
		internal const long serialVersionUID = 4174889037736658296L;

		public ObjArray()
		{
		}

		public bool IsSealed()
		{
			return @sealed;
		}

		public void Seal()
		{
			@sealed = true;
		}

		public bool IsEmpty()
		{
			return size == 0;
		}

		public int Size()
		{
			return size;
		}

		public void SetSize(int newSize)
		{
			if (newSize < 0)
			{
				throw new ArgumentException();
			}
			if (@sealed)
			{
				throw OnSeledMutation();
			}
			int N = size;
			if (newSize < N)
			{
				for (int i = newSize; i != N; ++i)
				{
					SetImpl(i, null);
				}
			}
			else
			{
				if (newSize > N)
				{
					if (newSize > FIELDS_STORE_SIZE)
					{
						EnsureCapacity(newSize);
					}
				}
			}
			size = newSize;
		}

		public object Get(int index)
		{
			if (!(0 <= index && index < size))
			{
				throw OnInvalidIndex(index, size);
			}
			return GetImpl(index);
		}

		public void Set(int index, object value)
		{
			if (!(0 <= index && index < size))
			{
				throw OnInvalidIndex(index, size);
			}
			if (@sealed)
			{
				throw OnSeledMutation();
			}
			SetImpl(index, value);
		}

		private object GetImpl(int index)
		{
			switch (index)
			{
				case 0:
				{
					return f0;
				}

				case 1:
				{
					return f1;
				}

				case 2:
				{
					return f2;
				}

				case 3:
				{
					return f3;
				}

				case 4:
				{
					return f4;
				}
			}
			return data[index - FIELDS_STORE_SIZE];
		}

		private void SetImpl(int index, object value)
		{
			switch (index)
			{
				case 0:
				{
					f0 = value;
					break;
				}

				case 1:
				{
					f1 = value;
					break;
				}

				case 2:
				{
					f2 = value;
					break;
				}

				case 3:
				{
					f3 = value;
					break;
				}

				case 4:
				{
					f4 = value;
					break;
				}

				default:
				{
					data[index - FIELDS_STORE_SIZE] = value;
					break;
				}
			}
		}

		public virtual int IndexOf(object obj)
		{
			int N = size;
			for (int i = 0; i != N; ++i)
			{
				object current = GetImpl(i);
				if (current == obj || (current != null && current.Equals(obj)))
				{
					return i;
				}
			}
			return -1;
		}

		public virtual int LastIndexOf(object obj)
		{
			for (int i = size; i != 0; )
			{
				--i;
				object current = GetImpl(i);
				if (current == obj || (current != null && current.Equals(obj)))
				{
					return i;
				}
			}
			return -1;
		}

		public object Peek()
		{
			int N = size;
			if (N == 0)
			{
				throw OnEmptyStackTopRead();
			}
			return GetImpl(N - 1);
		}

		public object Pop()
		{
			if (@sealed)
			{
				throw OnSeledMutation();
			}
			int N = size;
			--N;
			object top;
			switch (N)
			{
				case -1:
				{
					throw OnEmptyStackTopRead();
				}

				case 0:
				{
					top = f0;
					f0 = null;
					break;
				}

				case 1:
				{
					top = f1;
					f1 = null;
					break;
				}

				case 2:
				{
					top = f2;
					f2 = null;
					break;
				}

				case 3:
				{
					top = f3;
					f3 = null;
					break;
				}

				case 4:
				{
					top = f4;
					f4 = null;
					break;
				}

				default:
				{
					top = data[N - FIELDS_STORE_SIZE];
					data[N - FIELDS_STORE_SIZE] = null;
					break;
				}
			}
			size = N;
			return top;
		}

		public void Push(object value)
		{
			Add(value);
		}

		public void Add(object value)
		{
			if (@sealed)
			{
				throw OnSeledMutation();
			}
			int N = size;
			if (N >= FIELDS_STORE_SIZE)
			{
				EnsureCapacity(N + 1);
			}
			size = N + 1;
			SetImpl(N, value);
		}

		public void Add(int index, object value)
		{
			int N = size;
			if (!(0 <= index && index <= N))
			{
				throw OnInvalidIndex(index, N + 1);
			}
			if (@sealed)
			{
				throw OnSeledMutation();
			}
			object tmp;
			switch (index)
			{
				case 0:
				{
					if (N == 0)
					{
						f0 = value;
						break;
					}
					tmp = f0;
					f0 = value;
					value = tmp;
					goto case 1;
				}

				case 1:
				{
					if (N == 1)
					{
						f1 = value;
						break;
					}
					tmp = f1;
					f1 = value;
					value = tmp;
					goto case 2;
				}

				case 2:
				{
					if (N == 2)
					{
						f2 = value;
						break;
					}
					tmp = f2;
					f2 = value;
					value = tmp;
					goto case 3;
				}

				case 3:
				{
					if (N == 3)
					{
						f3 = value;
						break;
					}
					tmp = f3;
					f3 = value;
					value = tmp;
					goto case 4;
				}

				case 4:
				{
					if (N == 4)
					{
						f4 = value;
						break;
					}
					tmp = f4;
					f4 = value;
					value = tmp;
					index = FIELDS_STORE_SIZE;
					goto default;
				}

				default:
				{
					EnsureCapacity(N + 1);
					if (index != N)
					{
						System.Array.Copy(data, index - FIELDS_STORE_SIZE, data, index - FIELDS_STORE_SIZE + 1, N - index);
					}
					data[index - FIELDS_STORE_SIZE] = value;
					break;
				}
			}
			size = N + 1;
		}

		public void Remove(int index)
		{
			int N = size;
			if (!(0 <= index && index < N))
			{
				throw OnInvalidIndex(index, N);
			}
			if (@sealed)
			{
				throw OnSeledMutation();
			}
			--N;
			switch (index)
			{
				case 0:
				{
					if (N == 0)
					{
						f0 = null;
						break;
					}
					f0 = f1;
					goto case 1;
				}

				case 1:
				{
					if (N == 1)
					{
						f1 = null;
						break;
					}
					f1 = f2;
					goto case 2;
				}

				case 2:
				{
					if (N == 2)
					{
						f2 = null;
						break;
					}
					f2 = f3;
					goto case 3;
				}

				case 3:
				{
					if (N == 3)
					{
						f3 = null;
						break;
					}
					f3 = f4;
					goto case 4;
				}

				case 4:
				{
					if (N == 4)
					{
						f4 = null;
						break;
					}
					f4 = data[0];
					index = FIELDS_STORE_SIZE;
					goto default;
				}

				default:
				{
					if (index != N)
					{
						System.Array.Copy(data, index - FIELDS_STORE_SIZE + 1, data, index - FIELDS_STORE_SIZE, N - index);
					}
					data[N - FIELDS_STORE_SIZE] = null;
					break;
				}
			}
			size = N;
		}

		public void Clear()
		{
			if (@sealed)
			{
				throw OnSeledMutation();
			}
			int N = size;
			for (int i = 0; i != N; ++i)
			{
				SetImpl(i, null);
			}
			size = 0;
		}

		public object[] ToArray()
		{
			object[] array = new object[size];
			ToArray(array, 0);
			return array;
		}

		public void ToArray(object[] array)
		{
			ToArray(array, 0);
		}

		public void ToArray(object[] array, int offset)
		{
			int N = size;
			switch (N)
			{
				default:
				{
					System.Array.Copy(data, 0, array, offset + FIELDS_STORE_SIZE, N - FIELDS_STORE_SIZE);
					goto case 5;
				}

				case 5:
				{
					array[offset + 4] = f4;
					goto case 4;
				}

				case 4:
				{
					array[offset + 3] = f3;
					goto case 3;
				}

				case 3:
				{
					array[offset + 2] = f2;
					goto case 2;
				}

				case 2:
				{
					array[offset + 1] = f1;
					goto case 1;
				}

				case 1:
				{
					array[offset + 0] = f0;
					goto case 0;
				}

				case 0:
				{
					break;
				}
			}
		}

		private void EnsureCapacity(int minimalCapacity)
		{
			int required = minimalCapacity - FIELDS_STORE_SIZE;
			if (required <= 0)
			{
				throw new ArgumentException();
			}
			if (data == null)
			{
				int alloc = FIELDS_STORE_SIZE * 2;
				if (alloc < required)
				{
					alloc = required;
				}
				data = new object[alloc];
			}
			else
			{
				int alloc = data.Length;
				if (alloc < required)
				{
					if (alloc <= FIELDS_STORE_SIZE)
					{
						alloc = FIELDS_STORE_SIZE * 2;
					}
					else
					{
						alloc *= 2;
					}
					if (alloc < required)
					{
						alloc = required;
					}
					object[] tmp = new object[alloc];
					if (size > FIELDS_STORE_SIZE)
					{
						System.Array.Copy(data, 0, tmp, 0, size - FIELDS_STORE_SIZE);
					}
					data = tmp;
				}
			}
		}

		private static Exception OnInvalidIndex(int index, int upperBound)
		{
			// \u2209 is "NOT ELEMENT OF"
			string msg = index + " \u2209 [0, " + upperBound + ')';
			throw new IndexOutOfRangeException(msg);
		}

		private static Exception OnEmptyStackTopRead()
		{
			throw new Exception("Empty stack");
		}

		private static Exception OnSeledMutation()
		{
			throw new InvalidOperationException("Attempt to modify sealed array");
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream os)
		{
			os.DefaultWriteObject();
			int N = size;
			for (int i = 0; i != N; ++i)
			{
				object obj = GetImpl(i);
				os.WriteObject(obj);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream @is)
		{
			@is.DefaultReadObject();
			// It reads size
			int N = size;
			if (N > FIELDS_STORE_SIZE)
			{
				data = new object[N - FIELDS_STORE_SIZE];
			}
			for (int i = 0; i != N; ++i)
			{
				object obj = @is.ReadObject();
				SetImpl(i, obj);
			}
		}

		private int size;

		private bool @sealed;

		private const int FIELDS_STORE_SIZE = 5;

		[System.NonSerialized]
		private object f0;

		[System.NonSerialized]
		private object f1;

		[System.NonSerialized]
		private object f2;

		[System.NonSerialized]
		private object f3;

		[System.NonSerialized]
		private object f4;

		[System.NonSerialized]
		private object[] data;
		// Number of data elements
	}
}
