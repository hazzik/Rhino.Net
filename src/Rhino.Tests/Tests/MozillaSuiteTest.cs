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
using System.Linq;
using NUnit.Framework;
using Rhino.Drivers;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>
	/// This JUnit suite runs the Mozilla test suite (in mozilla.org CVS 
	/// at /mozilla/js/tests).
	/// </summary>
	/// <remarks>
	/// This JUnit suite runs the Mozilla test suite (in mozilla.org CVS
	/// at /mozilla/js/tests).
	/// Not all tests in the suite are run. Since the mozilla.org tests are
	/// designed and maintained for the SpiderMonkey engine, tests in the
	/// suite may not pass due to feature set differences and known bugs.
	/// To make sure that this unit test is stable in the midst of changes
	/// to the mozilla.org suite, we maintain a list of passing tests in
	/// files opt-1.tests, opt0.tests, and opt9.tests. This class also
	/// implements the ability to run skipped tests, see if any pass, and
	/// print out a script to modify the *.tests files.
	/// (This approach doesn't handle breaking changes to existing passing
	/// tests, but in practice that has been very rare.)
	/// </remarks>
	/// <author>Norris Boyd</author>
	/// <author>Attila Szegedi</author>
	[TestFixture]
	public class MozillaSuiteTest
	{
		private static readonly int[] OPT_LEVELS = { -1, 0, 9 };

		/// <exception cref="System.IO.IOException"></exception>
		public static DirectoryInfo GetTestDir()
		{
			DirectoryInfo testDir;
			var property = Runtime.GetProperty("mozilla.js.tests");
			if (property != null)
			{
				testDir = new DirectoryInfo(property);
			}
			else
			{
				Uri url = typeof (StandardTests).GetResource(".");
				string path = null;//url.GetFile();
				int jsIndex = path.LastIndexOf("/js");
				if (jsIndex == -1)
				{
					throw new InvalidOperationException("You aren't running the tests from within the standard mozilla/js directory structure");
				}
				path = path.Substring(0, jsIndex + 3).Replace('/', Path.DirectorySeparatorChar);
				path = path.Replace("%20", " ");
				testDir = new DirectoryInfo(path + "/tests");
			}
			if (!testDir.Exists)
			{
				throw new FileNotFoundException(testDir + " is not a directory");
			}
			return testDir;
		}

		public static string GetTestFilename(int optimizationLevel)
		{
			return string.Format("opt{0}.tests", optimizationLevel);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static FileInfo[] GetTestFiles(int optimizationLevel)
		{
			DirectoryInfo testDir = GetTestDir();
			string[] tests = TestUtils.LoadTestsFromResource("/" + GetTestFilename(optimizationLevel));
			Array.Sort(tests, string.CompareOrdinal);
			FileInfo[] files = new FileInfo[tests.Length];
			for (int i = 0; i < files.Length; i++)
			{
				files[i] = new FileInfo(testDir.FullName + "/" + tests[i]);
			}
			return files;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static string LoadFile(FileInfo f)
		{
			using (StreamReader reader = f.OpenText())
			{
				return reader.ReadToEnd();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static IEnumerable<object[]> MozillaSuiteValues()
		{
			return OPT_LEVELS.SelectMany(GetTestFiles, (i, file) => new object[] {file, i});
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static IEnumerable<object[]> SingleDoctest()
		{
			const int singleTestOptimizationLevel = -1;
			var f = new FileInfo(GetTestDir().FullName + "/" + "e4x/Expressions/11.1.1.js");
			return new[]
			{
				new object[] {f, singleTestOptimizationLevel}
			};
		}

		private class ShellTestParameters : ShellTest.Parameters
		{
			public override int GetTimeoutMilliseconds()
			{
				var timeout = Runtime.GetProperty("mozilla.js.tests.timeout");
				if (timeout != null)
				{
					return Convert.ToInt32(timeout);
				}
				return 10000;
			}
		}

		private class JunitStatus : ShellTest.Status
		{
			internal FileInfo file;

			public sealed override void Running(FileInfo jsFile)
			{
				// remember file in case we fail
				file = jsFile;
			}

			public sealed override void Failed(string s)
			{
				// Include test source in message, this is the only way
				// to locate the test in a Parameterized JUnit test
				string msg = "In \"" + file + "\":" + Runtime.GetProperty("line.separator") + s;
				Console.Out.WriteLine(msg);
				Assert.Fail(msg);
			}

			public sealed override void ExitCodesWere(int expected, int actual)
			{
				Assert.AreEqual(expected, actual, "Unexpected exit code");
			}

			public sealed override void OutputWas(string s)
			{
			}

			// Do nothing; we don't want to see the output when running JUnit
			// tests.
			public sealed override void Threw(Exception t)
			{
				Assert.Fail(ShellTest.GetStackTrace(t));
			}

			public sealed override void TimedOut()
			{
				Failed("Timed out.");
			}
		}

		/// <exception cref="System.Exception"></exception>
		[TestCaseSource("MozillaSuiteValues")]
		//[TestCaseSource("SingleDoctest")] // uncomment this to test a single Mozilla test
		public void RunMozillaTest(FileInfo file, int level)
		{
			//Console.WriteLine("Test \"{0}\" running under optimization level {1}", file, level);
			var factory = new ShellContextFactory();
			factory.SetOptimizationLevel(level);
			var @params = new ShellTestParameters();
			var status = new JunitStatus();
			ShellTest.Run(factory, file, @params, status);
		}

		/// <summary>
		/// The main class will run all the test files that are *not* covered in
		/// the *.tests files, and print out a list of all the tests that pass.
		/// </summary>
		/// <remarks>
		/// The main class will run all the test files that are *not* covered in
		/// the *.tests files, and print out a list of all the tests that pass.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void Main(string[] args)
		{
			TextWriter @out = new StreamWriter("fix-tests-files.sh");
			try
			{
				foreach (int optLevel in OPT_LEVELS)
				{
					DirectoryInfo testDir = GetTestDir();
					FileInfo[] allTests = TestUtils.RecursiveListFiles(testDir, path => ShellTest.DIRECTORY_FILTER(path) || ShellTest.TEST_FILTER(path));
					HashSet<FileInfo> diff = new HashSet<FileInfo>(allTests);
					FileInfo[] testFiles = GetTestFiles(optLevel);
					diff.RemoveAll(testFiles);
					List<string> skippedPassed = new List<string>();
					int absolutePathLength = testDir.FullName.Length + 1;
					foreach (FileInfo testFile in diff)
					{
						try
						{
							new MozillaSuiteTest().RunMozillaTest(testFile, optLevel);
							// strip off testDir
							string canonicalized = testFile.FullName.Substring(absolutePathLength);
							canonicalized = canonicalized.Replace('\\', '/');
							skippedPassed.Add(canonicalized);
						}
						catch
						{
						}
					}
					// failed, so skip
					// "skippedPassed" now contains all the tests that are currently
					// skipped but now pass. Print out shell commands to update the
					// appropriate *.tests file.
					if (skippedPassed.Count > 0)
					{
						@out.WriteLine("cat >> " + GetTestFilename(optLevel) + " <<EOF");
						string[] sorted = skippedPassed.ToArray();
						Array.Sort(sorted, string.CompareOrdinal);
						foreach (string t in sorted)
						{
							@out.WriteLine(t);
						}
						@out.WriteLine("EOF");
					}
				}
				Console.Out.WriteLine("Done.");
			}
			finally
			{
				@out.Close();
			}
		}
	}
}
