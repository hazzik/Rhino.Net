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
using NUnit.Framework.Runners;
using Rhino.Drivers;
using Rhino.Tests;
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
	[NUnit.Framework.TestFixture]
	public class MozillaSuiteTest
	{
		private readonly FilePath jsFile;

		private readonly int optimizationLevel;

		internal static readonly int[] OPT_LEVELS = new int[] { -1, 0, 9 };

		public MozillaSuiteTest(FilePath jsFile, int optimizationLevel)
		{
			this.jsFile = jsFile;
			this.optimizationLevel = optimizationLevel;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static FilePath GetTestDir()
		{
			FilePath testDir = null;
			if (Runtime.GetProperty("mozilla.js.tests") != null)
			{
				testDir = new FilePath(Runtime.GetProperty("mozilla.js.tests"));
			}
			else
			{
				Uri url = typeof(StandardTests).GetResource(".");
				string path = url.GetFile();
				int jsIndex = path.LastIndexOf("/js");
				if (jsIndex == -1)
				{
					throw new InvalidOperationException("You aren't running the tests " + "from within the standard mozilla/js directory structure");
				}
				path = Sharpen.Runtime.Substring(path, 0, jsIndex + 3).Replace('/', FilePath.separatorChar);
				path = path.Replace("%20", " ");
				testDir = new FilePath(path, "tests");
			}
			if (!testDir.IsDirectory())
			{
				throw new FileNotFoundException(testDir + " is not a directory");
			}
			return testDir;
		}

		public static string GetTestFilename(int optimizationLevel)
		{
			return "opt" + optimizationLevel + ".tests";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static FilePath[] GetTestFiles(int optimizationLevel)
		{
			FilePath testDir = GetTestDir();
			string[] tests = TestUtils.LoadTestsFromResource("/" + GetTestFilename(optimizationLevel), null);
			Arrays.Sort(tests);
			FilePath[] files = new FilePath[tests.Length];
			for (int i = 0; i < files.Length; i++)
			{
				files[i] = new FilePath(testDir, tests[i]);
			}
			return files;
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
		public static ICollection<object[]> MozillaSuiteValues()
		{
			IList<object[]> result = new List<object[]>();
			int[] optLevels = OPT_LEVELS;
			for (int i = 0; i < optLevels.Length; i++)
			{
				FilePath[] tests = GetTestFiles(optLevels[i]);
				foreach (FilePath f in tests)
				{
					result.AddItem(new object[] { f, optLevels[i] });
				}
			}
			return result;
		}

		// move "@Parameters" to this method to test a single Mozilla test
		/// <exception cref="System.IO.IOException"></exception>
		public static ICollection<object[]> SingleDoctest()
		{
			string SINGLE_TEST_FILE = "e4x/Expressions/11.1.1.js";
			int SINGLE_TEST_OPTIMIZATION_LEVEL = -1;
			IList<object[]> result = new List<object[]>();
			FilePath f = new FilePath(GetTestDir(), SINGLE_TEST_FILE);
			result.AddItem(new object[] { f, SINGLE_TEST_OPTIMIZATION_LEVEL });
			return result;
		}

		private class ShellTestParameters : ShellTest.Parameters
		{
			public override int GetTimeoutMilliseconds()
			{
				if (Runtime.GetProperty("mozilla.js.tests.timeout") != null)
				{
					return System.Convert.ToInt32(Runtime.GetProperty("mozilla.js.tests.timeout"));
				}
				return 10000;
			}
		}

		private class JunitStatus : ShellTest.Status
		{
			internal FilePath file;

			public sealed override void Running(FilePath jsFile)
			{
				// remember file in case we fail
				file = jsFile;
			}

			public sealed override void Failed(string s)
			{
				// Include test source in message, this is the only way
				// to locate the test in a Parameterized JUnit test
				string msg = "In \"" + file + "\":" + Runtime.GetProperty("line.separator") + s;
				System.Console.Out.WriteLine(msg);
				NUnit.Framework.Assert.Fail(msg);
			}

			public sealed override void ExitCodesWere(int expected, int actual)
			{
				NUnit.Framework.Assert.AreEqual(expected, actual, "Unexpected exit code");
			}

			public sealed override void OutputWas(string s)
			{
			}

			// Do nothing; we don't want to see the output when running JUnit
			// tests.
			public sealed override void Threw(Exception t)
			{
				NUnit.Framework.Assert.Fail(ShellTest.GetStackTrace(t));
			}

			public sealed override void TimedOut()
			{
				Failed("Timed out.");
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void RunMozillaTest()
		{
			//System.out.println("Test \"" + jsFile + "\" running under optimization level " + optimizationLevel);
			ShellContextFactory shellContextFactory = new ShellContextFactory();
			shellContextFactory.SetOptimizationLevel(optimizationLevel);
			MozillaSuiteTest.ShellTestParameters @params = new MozillaSuiteTest.ShellTestParameters();
			MozillaSuiteTest.JunitStatus status = new MozillaSuiteTest.JunitStatus();
			ShellTest.Run(shellContextFactory, jsFile, @params, status);
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
			TextWriter @out = new TextWriter("fix-tests-files.sh");
			try
			{
				for (int i = 0; i < OPT_LEVELS.Length; i++)
				{
					int optLevel = OPT_LEVELS[i];
					FilePath testDir = GetTestDir();
					FilePath[] allTests = TestUtils.RecursiveListFiles(testDir, new _FileFilter_204());
					HashSet<FilePath> diff = new HashSet<FilePath>(Arrays.AsList(allTests));
					FilePath[] testFiles = GetTestFiles(optLevel);
					diff.RemoveAll(Arrays.AsList(testFiles));
					List<string> skippedPassed = new List<string>();
					int absolutePathLength = testDir.GetAbsolutePath().Length + 1;
					foreach (FilePath testFile in diff)
					{
						try
						{
							(new MozillaSuiteTest(testFile, optLevel)).RunMozillaTest();
							// strip off testDir
							string canonicalized = Sharpen.Runtime.Substring(testFile.GetAbsolutePath(), absolutePathLength);
							canonicalized = canonicalized.Replace('\\', '/');
							skippedPassed.AddItem(canonicalized);
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
						string[] sorted = Sharpen.Collections.ToArray(skippedPassed, new string[0]);
						Arrays.Sort(sorted);
						for (int j = 0; j < sorted.Length; j++)
						{
							@out.WriteLine(sorted[j]);
						}
						@out.WriteLine("EOF");
					}
				}
				System.Console.Out.WriteLine("Done.");
			}
			finally
			{
				@out.Close();
			}
		}

		private sealed class _FileFilter_204 : FileFilter
		{
			public _FileFilter_204()
			{
			}

			public bool Accept(FilePath pathname)
			{
				return ShellTest.DIRECTORY_FILTER.Accept(pathname) || ShellTest.TEST_FILTER.Accept(pathname);
			}
		}
	}
}
