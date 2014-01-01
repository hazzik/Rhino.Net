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
using NUnit.Framework;
using Rhino.Tools.Shell;

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
	[TestFixture]
	public sealed class DoctestsTest
	{
		private const string BaseDirectory = "../../doctests";

		/// <exception cref="System.IO.IOException"></exception>
		private static string LoadFile(FileInfo f)
		{
			using (var reader = f.OpenText())
			{
				return reader.ReadToEnd();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static IEnumerable<object[]> DoctestValues()
		{
			var directory = new DirectoryInfo(BaseDirectory);
			var doctests = directory.EnumerateFiles("*.doctest", SearchOption.AllDirectories);
			var result = new List<object[]>();
			foreach (FileInfo f in doctests)
			{
				result.Add(new object[] { f, -1 });
				result.Add(new object[] { f, 0 });
				result.Add(new object[] { f, 9 });
			}
			return result;
		}

		// move "@Parameters" to this method to test a single doctest
		/// <exception cref="System.IO.IOException"></exception>
		public static IEnumerable<object[]> SingleDoctest()
		{
			FileInfo f = new FileInfo(BaseDirectory + "/" + "Counter.doctest");
			return new[] { new object[] { f, -1 } };
		}

		/// <exception cref="System.Exception"></exception>
		[TestCaseSource("DoctestValues")]
		//[TestCaseSource("SingleDoctest")] // uncomment this to test a single doctest
		public void RunDoctest(FileInfo file, int optimizationLevel)
		{
			var source =  LoadFile(file);
			ContextFactory factory = ContextFactory.GetGlobal();
			Context cx = factory.EnterContext();
			try
			{
				cx.SetOptimizationLevel(optimizationLevel);
				Global global = new Global(cx);
				// global.runDoctest throws an exception on any failure
				int testsPassed = global.RunDoctest(cx, global, source, file.Name, 1);
				Console.Out.WriteLine(file.Name + "(" + optimizationLevel + "): " + testsPassed + " passed.");
				Assert.IsTrue(testsPassed > 0);
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine(file.Name + "(" + optimizationLevel + "): FAILED due to " + ex);
				throw;
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
