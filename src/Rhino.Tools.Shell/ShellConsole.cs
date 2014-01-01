/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Rhino;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	/// <author>AndrÃ© Bargull</author>
	public abstract class ShellConsole
	{
		private static readonly Type[] NO_ARG = new Type[] {  };

		private static readonly Type[] BOOLEAN_ARG = new Type[] { typeof(bool) };

		private static readonly Type[] STRING_ARG = new Type[] { typeof(string) };

		private static readonly Type[] CHARSEQ_ARG = new Type[] { typeof(CharSequence) };

		protected internal ShellConsole()
		{
		}

		/// <summary>
		/// Returns the underlying
		/// <see cref="System.IO.Stream">System.IO.Stream</see>
		/// </summary>
		public abstract Stream GetIn();

		/// <summary>Reads a single line from the console</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract string ReadLine();

		/// <summary>
		/// Reads a single line from the console and sets the console's prompt to
		/// <code>prompt</code>
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract string ReadLine(string prompt);

		/// <summary>Flushes the console's output</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Flush();

		/// <summary>Prints a single string to the console</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Print(string s);

		/// <summary>Prints the newline character-sequence to the console</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Println();

		/// <summary>Prints a string and the newline character-sequence to the console</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Println(string s);

		private static object TryInvoke(object obj, string method, Type[] paramTypes, params object[] args)
		{
			try
			{
				MethodInfo m = Sharpen.Runtime.GetDeclaredMethod(obj.GetType(), method, paramTypes);
				if (m != null)
				{
					return m.Invoke(obj, args);
				}
			}
			catch (MissingMethodException)
			{
			}
			catch (ArgumentException)
			{
			}
			catch (MemberAccessException)
			{
			}
			catch (TargetInvocationException)
			{
			}
			return null;
		}

		/// <summary>
		/// <see cref="ShellConsole">ShellConsole</see>
		/// implementation for JLine v1
		/// </summary>
		private class JLineShellConsoleV1 : ShellConsole
		{
			private readonly object reader;

			private readonly Stream @in;

			internal JLineShellConsoleV1(object reader, Encoding cs)
			{
				this.reader = reader;
				this.@in = new ShellConsole.ConsoleInputStream(this, cs);
			}

			public override Stream GetIn()
			{
				return @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override string ReadLine()
			{
				return (string)TryInvoke(reader, "readLine", NO_ARG);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override string ReadLine(string prompt)
			{
				return (string)TryInvoke(reader, "readLine", STRING_ARG, prompt);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
				TryInvoke(reader, "flushConsole", NO_ARG);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Print(string s)
			{
				TryInvoke(reader, "printString", STRING_ARG, s);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Println()
			{
				TryInvoke(reader, "printNewline", NO_ARG);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Println(string s)
			{
				TryInvoke(reader, "printString", STRING_ARG, s);
				TryInvoke(reader, "printNewline", NO_ARG);
			}
		}

		/// <summary>
		/// <see cref="ShellConsole">ShellConsole</see>
		/// implementation for JLine v2
		/// </summary>
		private class JLineShellConsoleV2 : ShellConsole
		{
			private readonly object reader;

			private readonly Stream @in;

			internal JLineShellConsoleV2(object reader, Encoding cs)
			{
				this.reader = reader;
				this.@in = new ShellConsole.ConsoleInputStream(this, cs);
			}

			public override Stream GetIn()
			{
				return @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override string ReadLine()
			{
				return (string)TryInvoke(reader, "readLine", NO_ARG);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override string ReadLine(string prompt)
			{
				return (string)TryInvoke(reader, "readLine", STRING_ARG, prompt);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
				TryInvoke(reader, "flush", NO_ARG);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Print(string s)
			{
				TryInvoke(reader, "print", CHARSEQ_ARG, s);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Println()
			{
				TryInvoke(reader, "println", NO_ARG);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Println(string s)
			{
				TryInvoke(reader, "println", CHARSEQ_ARG, s);
			}
		}

		/// <summary>
		/// JLine's ConsoleReaderInputStream is no longer public, therefore we need
		/// to use our own implementation
		/// </summary>
		private class ConsoleInputStream : Stream
		{
			private static readonly byte[] EMPTY = new byte[] {  };

			private readonly ShellConsole console;

			private readonly Encoding cs;

			private byte[] buffer = EMPTY;

			private int cursor = -1;

			private bool atEOF = false;

			public ConsoleInputStream(ShellConsole console, Encoding cs)
			{
				this.console = console;
				this.cs = cs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] b, int off, int len)
			{
				lock (this)
				{
					if (b == null)
					{
						throw new ArgumentNullException();
					}
					else
					{
						if (off < 0 || len < 0 || len > b.Length - off)
						{
							throw new IndexOutOfRangeException();
						}
						else
						{
							if (len == 0)
							{
								return 0;
							}
						}
					}
					if (!EnsureInput())
					{
						return -1;
					}
					int n = Math.Min(len, buffer.Length - cursor);
					for (int i = 0; i < n; ++i)
					{
						b[off + i] = buffer[cursor + i];
					}
					if (n < len)
					{
						b[off + n++] = (byte)('\n');
					}
					cursor += n;
					return n;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read()
			{
				lock (this)
				{
					if (!EnsureInput())
					{
						return -1;
					}
					if (cursor == buffer.Length)
					{
						cursor++;
						return '\n';
					}
					return buffer[cursor++];
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			private bool EnsureInput()
			{
				if (atEOF)
				{
					return false;
				}
				if (cursor < 0 || cursor > buffer.Length)
				{
					if (ReadNextLine() == -1)
					{
						atEOF = true;
						return false;
					}
					cursor = 0;
				}
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private int ReadNextLine()
			{
				string line = console.ReadLine(null);
				if (line != null)
				{
					buffer = Sharpen.Runtime.GetBytesForString(line, cs);
					return buffer.Length;
				}
				else
				{
					buffer = EMPTY;
					return -1;
				}
			}
		}

		private class SimpleShellConsole : ShellConsole
		{
			private readonly Stream @in;

			private readonly PrintWriter @out;

			private readonly BufferedReader reader;

			internal SimpleShellConsole(Stream @in, TextWriter ps, Encoding cs)
			{
				this.@in = @in;
				this.@out = new PrintWriter(ps);
				this.reader = new BufferedReader(new StreamReader(@in, cs));
			}

			public override Stream GetIn()
			{
				return @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override string ReadLine()
			{
				return reader.ReadLine();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override string ReadLine(string prompt)
			{
				if (prompt != null)
				{
					@out.Write(prompt);
					@out.Flush();
				}
				return reader.ReadLine();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
				@out.Flush();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Print(string s)
			{
				@out.Write(s);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Println()
			{
				@out.WriteLine();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Println(string s)
			{
				@out.WriteLine(s);
			}
		}

		/// <summary>
		/// Returns a new
		/// <see cref="ShellConsole">ShellConsole</see>
		/// which uses the supplied
		/// <see cref="System.IO.Stream">System.IO.Stream</see>
		/// and
		/// <see cref="System.IO.TextWriter">System.IO.TextWriter</see>
		/// for its input/output
		/// </summary>
		public static ShellConsole GetConsole(Stream @in, TextWriter ps, Encoding cs)
		{
			return new ShellConsole.SimpleShellConsole(@in, ps, cs);
		}

		/// <summary>
		/// Provides a specialized
		/// <see cref="ShellConsole">ShellConsole</see>
		/// to handle line editing,
		/// history and completion. Relies on the JLine library (see
		/// <http://jline.sourceforge.net>).
		/// </summary>
		public static ShellConsole GetConsole(Scriptable scope, Encoding cs)
		{
			// We don't want a compile-time dependency on the JLine jar, so use
			// reflection to load and reference the JLine classes.
			ClassLoader classLoader = typeof(ShellConsole).GetClassLoader();
			if (classLoader == null)
			{
				// If the attempt to get a class specific class loader above failed
				// then fallback to the system class loader.
				classLoader = ClassLoader.GetSystemClassLoader();
			}
			if (classLoader == null)
			{
				// If for some reason we still don't have a handle to a class
				// loader then give up (avoid a NullPointerException).
				return null;
			}
			try
			{
				// first try to load JLine v2...
				Type readerClass = Kit.ClassOrNull(classLoader, "jline.console.ConsoleReader");
				if (readerClass != null)
				{
					return GetJLineShellConsoleV2(classLoader, readerClass, scope, cs);
				}
				// ...if that fails, try to load JLine v1
				readerClass = Kit.ClassOrNull(classLoader, "jline.ConsoleReader");
				if (readerClass != null)
				{
					return GetJLineShellConsoleV1(classLoader, readerClass, scope, cs);
				}
			}
			catch (MissingMethodException)
			{
			}
			catch (MemberAccessException)
			{
			}
			catch (InstantiationException)
			{
			}
			catch (TargetInvocationException)
			{
			}
			return null;
		}

		/// <exception cref="System.MissingMethodException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		private static ShellConsole.JLineShellConsoleV1 GetJLineShellConsoleV1(ClassLoader classLoader, Type readerClass, Scriptable scope, Encoding cs)
		{
			// ConsoleReader reader = new ConsoleReader();
			ConstructorInfo c = readerClass.GetConstructor();
			object reader = c.NewInstance();
			// reader.setBellEnabled(false);
			TryInvoke(reader, "setBellEnabled", BOOLEAN_ARG, false);
			// reader.addCompletor(new FlexibleCompletor(prefixes));
			Type completorClass = Kit.ClassOrNull(classLoader, "jline.Completor");
			object completor = Proxy.NewProxyInstance(classLoader, new Type[] { completorClass }, new FlexibleCompletor(completorClass, scope));
			TryInvoke(reader, "addCompletor", new Type[] { completorClass }, completor);
			return new ShellConsole.JLineShellConsoleV1(reader, cs);
		}

		/// <exception cref="System.MissingMethodException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		private static ShellConsole.JLineShellConsoleV2 GetJLineShellConsoleV2(ClassLoader classLoader, Type readerClass, Scriptable scope, Encoding cs)
		{
			// ConsoleReader reader = new ConsoleReader();
			ConstructorInfo c = readerClass.GetConstructor();
			object reader = c.NewInstance();
			// reader.setBellEnabled(false);
			TryInvoke(reader, "setBellEnabled", BOOLEAN_ARG, false);
			// reader.addCompleter(new FlexibleCompletor(prefixes));
			Type completorClass = Kit.ClassOrNull(classLoader, "jline.console.completer.Completer");
			object completor = Proxy.NewProxyInstance(classLoader, new Type[] { completorClass }, new FlexibleCompletor(completorClass, scope));
			TryInvoke(reader, "addCompleter", new Type[] { completorClass }, completor);
			return new ShellConsole.JLineShellConsoleV2(reader, cs);
		}
	}

	/// <summary>
	/// The completors provided with JLine are pretty uptight, they only
	/// complete on a line that it can fully recognize (only composed of
	/// completed strings).
	/// </summary>
	/// <remarks>
	/// The completors provided with JLine are pretty uptight, they only
	/// complete on a line that it can fully recognize (only composed of
	/// completed strings). This one completes whatever came before.
	/// </remarks>
	internal class FlexibleCompletor : InvocationHandler
	{
		private MethodInfo completeMethod;

		private Scriptable global;

		/// <exception cref="System.MissingMethodException"></exception>
		internal FlexibleCompletor(Type completorClass, Scriptable global)
		{
			this.global = global;
			this.completeMethod = completorClass.GetMethod("complete", typeof(string), typeof(int), typeof(IList));
		}

		public virtual object Invoke(object proxy, MethodInfo method, object[] args)
		{
			if (method.Equals(this.completeMethod))
			{
				int result = Complete((string)args[0], System.Convert.ToInt32(((int)args[1])), (IList<string>)args[2]);
				return result;
			}
			throw new MissingMethodException(method.ToString());
		}

		public virtual int Complete(string buffer, int cursor, IList<string> candidates)
		{
			// Starting from "cursor" at the end of the buffer, look backward
			// and collect a list of identifiers separated by (possibly zero)
			// dots. Then look up each identifier in turn until getting to the
			// last, presumably incomplete fragment. Then enumerate all the
			// properties of the last object and find any that have the
			// fragment as a prefix and return those for autocompletion.
			int m = cursor - 1;
			while (m >= 0)
			{
				char c = buffer[m];
				if (!char.IsJavaIdentifierPart(c) && c != '.')
				{
					break;
				}
				m--;
			}
			string namesAndDots = Sharpen.Runtime.Substring(buffer, m + 1, cursor);
			string[] names = namesAndDots.Split("\\.", -1);
			Scriptable obj = this.global;
			for (int i = 0; i < names.Length - 1; i++)
			{
				object val = obj.Get(names[i], global);
				if (val is Scriptable)
				{
					obj = (Scriptable)val;
				}
				else
				{
					return buffer.Length;
				}
			}
			// no matches
			object[] ids = (obj is ScriptableObject) ? ((ScriptableObject)obj).GetAllIds() : obj.GetIds();
			string lastPart = names[names.Length - 1];
			for (int i_1 = 0; i_1 < ids.Length; i_1++)
			{
				if (!(ids[i_1] is string))
				{
					continue;
				}
				string id = (string)ids[i_1];
				if (id.StartsWith(lastPart))
				{
					if (obj.Get(id, obj) is Function)
					{
						id += "(";
					}
					candidates.Add(id);
				}
			}
			return buffer.Length - lastPart.Length;
		}
	}
}
