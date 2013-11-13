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
using NUnit.Framework.Runners;
using Rhino;
using Rhino.Drivers;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>Run doctests in folder testsrc/doctests.</summary>
	/// <remarks>
	/// Run doctests in folder testsrc/doctests.
	/// A doctest is a test in the form of an interactive shell session; Rhino
	/// collects and runs the inputs to the shell prompt and compares them to the
	/// expected outputs.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	public class DoctestsTest
	{
		internal static readonly string baseDirectory = "testsrc" + FilePath.separator + "doctests";

		internal const string doctestsExtension = ".doctest";

		internal string name;

		internal string source;

		internal int optimizationLevel;

		public DoctestsTest(string name, string source, int optimizationLevel)
		{
			this.name = name;
			this.source = source;
			this.optimizationLevel = optimizationLevel;
		}

		public static FilePath[] GetDoctestFiles()
		{
			return TestUtils.RecursiveListFiles(new FilePath(baseDirectory), new _FileFilter_49());
		}

		private sealed class _FileFilter_49 : FileFilter
		{
			public _FileFilter_49()
			{
			}

			public bool Accept(FilePath f)
			{
				return f.GetName().EndsWith(Rhino.Tests.DoctestsTest.doctestsExtension);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static string LoadFile(FilePath f)
		{
			int length = (int)f.Length();
			// don't worry about very long files
			char[] buf = new char[length];
			new FileReader(f).Read(buf, 0, length);
			return new string(buf);
		}

		/// <exception cref="System.IO.IOException"></exception>
		[Parameterized.Parameters]
		public static ICollection<object[]> DoctestValues()
		{
			FilePath[] doctests = GetDoctestFiles();
			IList<object[]> result = new AList<object[]>();
			foreach (FilePath f in doctests)
			{
				string contents = LoadFile(f);
				result.AddItem(new object[] { f.GetName(), contents, -1 });
				result.AddItem(new object[] { f.GetName(), contents, 0 });
				result.AddItem(new object[] { f.GetName(), contents, 9 });
			}
			return result;
		}

		// move "@Parameters" to this method to test a single doctest
		/// <exception cref="System.IO.IOException"></exception>
		public static ICollection<object[]> SingleDoctest()
		{
			IList<object[]> result = new AList<object[]>();
			FilePath f = new FilePath(baseDirectory, "Counter.doctest");
			string contents = LoadFile(f);
			result.AddItem(new object[] { f.GetName(), contents, -1 });
			return result;
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void RunDoctest()
		{
			ContextFactory factory = ContextFactory.GetGlobal();
			Context cx = factory.EnterContext();
			try
			{
				cx.SetOptimizationLevel(optimizationLevel);
				Global global = new Global(cx);
				// global.runDoctest throws an exception on any failure
				int testsPassed = global.RunDoctest(cx, global, source, name, 1);
				System.Console.Out.WriteLine(name + "(" + optimizationLevel + "): " + testsPassed + " passed.");
				NUnit.Framework.Assert.IsTrue(testsPassed > 0);
			}
			catch (Exception ex)
			{
				System.Console.Out.WriteLine(name + "(" + optimizationLevel + "): FAILED due to " + ex);
				throw;
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
