/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using Rhino.Utils;
using Sharpen;

namespace Rhino
{
	/// <summary>Collection of utilities</summary>
	public class Kit
	{
		// Assume any exceptions means the method does not exist.
		public static Type ClassOrNull(string className)
		{
			try
			{
				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
				{
					Type t = a.GetType(className, false);
					if (t != null)
						return t;
				}
			}
			catch (TypeLoadException)
			{
			}
			catch (SecurityException)
			{
			}
			catch (LinkageError)
			{
			}
			catch (ArgumentException)
			{
			}
			// Can be thrown if name has characters that a class name
			// can not contain
			return null;
		}

		/// <summary>Attempt to load the class of the given name.</summary>
		/// <remarks>
		/// Attempt to load the class of the given name. Note that the type parameter
		/// isn't checked.
		/// </remarks>
		public static Type ClassOrNull(ClassLoader loader, string className)
		{
			try
			{
				return loader.LoadClass(className);
			}
			catch (TypeLoadException)
			{
			}
			catch (SecurityException)
			{
			}
			catch (LinkageError)
			{
			}
			catch (ArgumentException)
			{
			}
			// Can be thrown if name has characters that a class name
			// can not contain
			return null;
		}

		internal static object NewInstanceOrNull(Type cl)
		{
			try
			{
				return Activator.CreateInstance(cl);
			}
			catch (SecurityException)
			{
			}
			catch (LinkageError)
			{
			}
			catch (InstantiationException)
			{
			}
			catch (MemberAccessException)
			{
			}
			return null;
		}

		/// <summary>Check that testClass is accessible from the given loader.</summary>
		/// <remarks>Check that testClass is accessible from the given loader.</remarks>
		internal static bool TestIfCanLoadRhinoClasses(ClassLoader loader)
		{
			Type testClass = ScriptRuntime.ContextFactoryClass;
			Type x = ClassOrNull(loader, testClass.FullName);
			if (x != testClass)
			{
				// The check covers the case when x == null =>
				// loader does not know about testClass or the case
				// when x != null && x != testClass =>
				// loader loads a class unrelated to testClass
				return false;
			}
			return true;
		}

		/// <summary>
		/// If character <tt>c</tt> is a hexadecimal digit, return
		/// <tt>accumulator</tt> * 16 plus corresponding
		/// number.
		/// </summary>
		/// <remarks>
		/// If character <tt>c</tt> is a hexadecimal digit, return
		/// <tt>accumulator</tt> * 16 plus corresponding
		/// number. Otherise return -1.
		/// </remarks>
		public static int XDigitToInt(int c, int accumulator)
		{
			// Use 0..9 < A..Z < a..z
			if (c <= '9')
			{
				c -= '0';
				if (0 <= c)
				{
					goto check_break;
				}
			}
			else
			{
				if (c <= 'F')
				{
					if ('A' <= c)
					{
						c -= ('A' - 10);
						goto check_break;
					}
				}
				else
				{
					if (c <= 'f')
					{
						if ('a' <= c)
						{
							c -= ('a' - 10);
							goto check_break;
						}
					}
				}
			}
			return -1;
check_break: ;
			return (accumulator << 4) | c;
		}

		/// <summary>Add <i>listener</i> to <i>bag</i> of listeners.</summary>
		/// <remarks>
		/// Add <i>listener</i> to <i>bag</i> of listeners.
		/// The function does not modify <i>bag</i> and return a new collection
		/// containing <i>listener</i> and all listeners from <i>bag</i>.
		/// Bag without listeners always represented as the null value.
		/// <p>
		/// Usage example:
		/// <pre>
		/// private volatile Object changeListeners;
		/// public void addMyListener(PropertyChangeListener l)
		/// {
		/// synchronized (this) {
		/// changeListeners = Kit.addListener(changeListeners, l);
		/// }
		/// }
		/// public void removeTextListener(PropertyChangeListener l)
		/// {
		/// synchronized (this) {
		/// changeListeners = Kit.removeListener(changeListeners, l);
		/// }
		/// }
		/// public void fireChangeEvent(Object oldValue, Object newValue)
		/// {
		/// // Get immune local copy
		/// Object listeners = changeListeners;
		/// if (listeners != null) {
		/// PropertyChangeEvent e = new PropertyChangeEvent(
		/// this, "someProperty" oldValue, newValue);
		/// for (int i = 0; ; ++i) {
		/// Object l = Kit.getListener(listeners, i);
		/// if (l == null)
		/// break;
		/// ((PropertyChangeListener)l).propertyChange(e);
		/// }
		/// }
		/// }
		/// </pre>
		/// </remarks>
		/// <param name="listener">Listener to add to <i>bag</i></param>
		/// <param name="bag">Current collection of listeners.</param>
		/// <returns>
		/// A new bag containing all listeners from <i>bag</i> and
		/// <i>listener</i>.
		/// </returns>
		/// <seealso cref="RemoveListener(object, object)">RemoveListener(object, object)</seealso>
		/// <seealso cref="GetListener(object, int)">GetListener(object, int)</seealso>
		public static object AddListener(object bag, object listener)
		{
			if (listener == null)
			{
				throw new ArgumentException();
			}
			if (listener is object[])
			{
				throw new ArgumentException();
			}
			if (bag == null)
			{
				bag = listener;
			}
			else
			{
				if (!(bag is object[]))
				{
					bag = new object[] { bag, listener };
				}
				else
				{
					object[] array = (object[])bag;
					int L = array.Length;
					// bag has at least 2 elements if it is array
					if (L < 2)
					{
						throw new ArgumentException();
					}
					object[] tmp = new object[L + 1];
					Array.Copy(array, 0, tmp, 0, L);
					tmp[L] = listener;
					bag = tmp;
				}
			}
			return bag;
		}

		/// <summary>Remove <i>listener</i> from <i>bag</i> of listeners.</summary>
		/// <remarks>
		/// Remove <i>listener</i> from <i>bag</i> of listeners.
		/// The function does not modify <i>bag</i> and return a new collection
		/// containing all listeners from <i>bag</i> except <i>listener</i>.
		/// If <i>bag</i> does not contain <i>listener</i>, the function returns
		/// <i>bag</i>.
		/// <p>
		/// For usage example, see
		/// <see cref="AddListener(object, object)">AddListener(object, object)</see>
		/// .
		/// </remarks>
		/// <param name="listener">Listener to remove from <i>bag</i></param>
		/// <param name="bag">Current collection of listeners.</param>
		/// <returns>
		/// A new bag containing all listeners from <i>bag</i> except
		/// <i>listener</i>.
		/// </returns>
		/// <seealso cref="AddListener(object, object)">AddListener(object, object)</seealso>
		/// <seealso cref="GetListener(object, int)">GetListener(object, int)</seealso>
		public static object RemoveListener(object bag, object listener)
		{
			if (listener == null)
			{
				throw new ArgumentException();
			}
			if (listener is object[])
			{
				throw new ArgumentException();
			}
			if (bag == listener)
			{
				bag = null;
			}
			else
			{
				if (bag is object[])
				{
					object[] array = (object[])bag;
					int L = array.Length;
					// bag has at least 2 elements if it is array
					if (L < 2)
					{
						throw new ArgumentException();
					}
					if (L == 2)
					{
						if (array[1] == listener)
						{
							bag = array[0];
						}
						else
						{
							if (array[0] == listener)
							{
								bag = array[1];
							}
						}
					}
					else
					{
						int i = L;
						do
						{
							--i;
							if (array[i] == listener)
							{
								object[] tmp = new object[L - 1];
								Array.Copy(array, 0, tmp, 0, i);
								Array.Copy(array, i + 1, tmp, i, L - (i + 1));
								bag = tmp;
								break;
							}
						}
						while (i != 0);
					}
				}
			}
			return bag;
		}

		/// <summary>
		/// Get listener at <i>index</i> position in <i>bag</i> or null if
		/// <i>index</i> equals to number of listeners in <i>bag</i>.
		/// </summary>
		/// <remarks>
		/// Get listener at <i>index</i> position in <i>bag</i> or null if
		/// <i>index</i> equals to number of listeners in <i>bag</i>.
		/// <p>
		/// For usage example, see
		/// <see cref="AddListener(object, object)">AddListener(object, object)</see>
		/// .
		/// </remarks>
		/// <param name="bag">Current collection of listeners.</param>
		/// <param name="index">Index of the listener to access.</param>
		/// <returns>Listener at the given index or null.</returns>
		/// <seealso cref="AddListener(object, object)">AddListener(object, object)</seealso>
		/// <seealso cref="RemoveListener(object, object)">RemoveListener(object, object)</seealso>
		public static object GetListener(object bag, int index)
		{
			if (index == 0)
			{
				if (bag == null)
				{
					return null;
				}
				if (!(bag is object[]))
				{
					return bag;
				}
				object[] array = (object[])bag;
				// bag has at least 2 elements if it is array
				if (array.Length < 2)
				{
					throw new ArgumentException();
				}
				return array[0];
			}
			else
			{
				if (index == 1)
				{
					if (!(bag is object[]))
					{
						if (bag == null)
						{
							throw new ArgumentException();
						}
						return null;
					}
					object[] array = (object[])bag;
					// the array access will check for index on its own
					return array[1];
				}
				else
				{
					// bag has to array
					object[] array = (object[])bag;
					int L = array.Length;
					if (L < 2)
					{
						throw new ArgumentException();
					}
					if (index == L)
					{
						return null;
					}
					return array[index];
				}
			}
		}

		internal static object InitHash(IDictionary<object, object> h, object key, object initialValue)
		{
			lock (h)
			{
				object current = h.GetValueOrDefault(key);
				if (current == null)
				{
					h[key] = initialValue;
				}
				else
				{
					initialValue = current;
				}
			}
			return initialValue;
		}

		private sealed class ComplexKey
		{
			private object key1;

			private object key2;

			private int hash;

			internal ComplexKey(object key1, object key2)
			{
				this.key1 = key1;
				this.key2 = key2;
			}

			public override bool Equals(object anotherObj)
			{
				var another = anotherObj as ComplexKey;
				if (another == null)
				{
					return false;
				}
				return key1.Equals(another.key1) && key2.Equals(another.key2);
			}

			public override int GetHashCode()
			{
				if (hash == 0)
				{
					hash = key1.GetHashCode() ^ key2.GetHashCode();
				}
				return hash;
			}
		}

		public static object MakeHashKeyFromPair(object key1, object key2)
		{
			if (key1 == null)
			{
				throw new ArgumentException();
			}
			if (key2 == null)
			{
				throw new ArgumentException();
			}
			return new ComplexKey(key1, key2);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static byte[] ReadStream(Stream @is, int initialBufferCapacity)
		{
			if (initialBufferCapacity <= 0)
			{
				throw new ArgumentException("Bad initialBufferCapacity: " + initialBufferCapacity);
			}
			byte[] buffer = new byte[initialBufferCapacity];
			int cursor = 0;
			for (; ; )
			{
				int n = @is.Read(buffer, cursor, buffer.Length - cursor);
				if (n <= 0)
				{
					break;
				}
				cursor += n;
				if (cursor == buffer.Length)
				{
					byte[] tmp = new byte[buffer.Length * 2];
					Array.Copy(buffer, 0, tmp, 0, cursor);
					buffer = tmp;
				}
			}
			if (cursor != buffer.Length)
			{
				byte[] tmp = new byte[cursor];
				Array.Copy(buffer, 0, tmp, 0, cursor);
				buffer = tmp;
			}
			return buffer;
		}

		/// <summary>Throws RuntimeException to indicate failed assertion.</summary>
		/// <remarks>
		/// Throws RuntimeException to indicate failed assertion.
		/// The function never returns and its return type is RuntimeException
		/// only to be able to write <tt>throw Kit.codeBug()</tt> if plain
		/// <tt>Kit.codeBug()</tt> triggers unreachable code error.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public static Exception CodeBug()
		{
			Exception ex = new InvalidOperationException("FAILED ASSERTION");
			// Print stack trace ASAP
			Console.Error.WriteLine(ex);
			throw ex;
		}

		/// <summary>Throws RuntimeException to indicate failed assertion.</summary>
		/// <remarks>
		/// Throws RuntimeException to indicate failed assertion.
		/// The function never returns and its return type is RuntimeException
		/// only to be able to write <tt>throw Kit.codeBug()</tt> if plain
		/// <tt>Kit.codeBug()</tt> triggers unreachable code error.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public static Exception CodeBug(string msg)
		{
			msg = "FAILED ASSERTION: " + msg;
			Exception ex = new InvalidOperationException(msg);
			// Print stack trace ASAP
			Console.Error.WriteLine(ex);
			throw ex;
		}
	}
}
