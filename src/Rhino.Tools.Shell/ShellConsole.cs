/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using System.Text;
using Sharpen;

namespace Rhino.Tools.Shell
{
	/// <author>AndrÃ© Bargull</author>
	public abstract class ShellConsole
	{
		protected internal ShellConsole()
		{
		}

		/// <summary>
		/// Returns the underlying
		/// <see cref="InputStream">System.IO.InputStream</see>
		/// </summary>
		public abstract TextReader GetIn();

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

		private class SimpleShellConsole : ShellConsole
		{
			private readonly TextReader @in;

			private readonly TextWriter @out;

			private readonly TextReader reader;

			internal SimpleShellConsole(TextReader @in, TextWriter ps, Encoding cs)
			{
				this.@in = @in;
				this.@out = ps;
				this.reader = @in;
			}

			public override TextReader GetIn()
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
		/// <see cref="InputStream">System.IO.InputStream</see>
		/// and
		/// <see cref="System.IO.TextWriter">System.IO.TextWriter</see>
		/// for its input/output
		/// </summary>
		public static ShellConsole GetConsole(TextReader @in, TextWriter ps, Encoding cs)
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
			return null;
		}
	}
}
