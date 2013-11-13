/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Rhino.Drivers;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Drivers
{
	/// <summary>Executes the tests in the js/tests directory, much like jsDriver.pl does.</summary>
	/// <remarks>
	/// Executes the tests in the js/tests directory, much like jsDriver.pl does.
	/// Excludes tests found in the js/tests/rhino-n.tests file.
	/// </remarks>
	/// <author>Attila Szegedi</author>
	/// <version>$Id: StandardTests.java,v 1.15 2009/07/21 17:39:05 nboyd%atg.com Exp $</version>
	public class StandardTests : TestSuite
	{
		private const bool DISABLE = true;

		// Disable this suite in favor of
		// org.mozilla.javascript.tests.MozillaSuiteTest
		/// <exception cref="System.Exception"></exception>
		public static TestSuite Suite()
		{
			TestSuite suite = new TestSuite("Standard JavaScript tests");
			return suite;
		}

		private static void AddSuites(TestSuite topLevel, FilePath testDir, string[] excludes, int optimizationLevel)
		{
			FilePath[] subdirs = testDir.ListFiles(ShellTest.DIRECTORY_FILTER);
			Arrays.Sort(subdirs);
			for (int i = 0; i < subdirs.Length; i++)
			{
				FilePath subdir = subdirs[i];
				string name = subdir.GetName();
				if (TestUtils.Matches(excludes, name))
				{
					continue;
				}
				TestSuite testSuite = new TestSuite(name);
				AddCategories(testSuite, subdir, name + "/", excludes, optimizationLevel);
				topLevel.AddTest(testSuite);
			}
		}

		private static void AddCategories(TestSuite suite, FilePath suiteDir, string prefix, string[] excludes, int optimizationLevel)
		{
			FilePath[] subdirs = suiteDir.ListFiles(ShellTest.DIRECTORY_FILTER);
			Arrays.Sort(subdirs);
			for (int i = 0; i < subdirs.Length; i++)
			{
				FilePath subdir = subdirs[i];
				string name = subdir.GetName();
				TestSuite testCategory = new TestSuite(name);
				AddTests(testCategory, subdir, prefix + name + "/", excludes, optimizationLevel);
				suite.AddTest(testCategory);
			}
		}

		private static void AddTests(TestSuite suite, FilePath suiteDir, string prefix, string[] excludes, int optimizationLevel)
		{
			FilePath[] jsFiles = suiteDir.ListFiles(ShellTest.TEST_FILTER);
			Arrays.Sort(jsFiles);
			for (int i = 0; i < jsFiles.Length; i++)
			{
				FilePath jsFile = jsFiles[i];
				string name = jsFile.GetName();
				if (!TestUtils.Matches(excludes, prefix + name))
				{
					suite.AddTest(new StandardTests.JsTestCase(jsFile, optimizationLevel));
				}
			}
		}

		public class JunitStatus : ShellTest.Status
		{
			public sealed override void Running(FilePath jsFile)
			{
			}

			//    do nothing
			public sealed override void Failed(string s)
			{
				Assert.Fail(s);
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
				Assert.Fail(ShellTest.GetStackTrace(t));
			}

			public sealed override void TimedOut()
			{
				Failed("Timed out.");
			}
		}

		[NUnit.Framework.TestFixture]
		public sealed class JsTestCase
		{
			private readonly FilePath jsFile;

			private readonly int optimizationLevel;

			internal JsTestCase(FilePath jsFile, int optimizationLevel) : base(jsFile.GetName() + (optimizationLevel == 1 ? "-compiled" : "-interpreted"))
			{
				this.jsFile = jsFile;
				this.optimizationLevel = optimizationLevel;
			}

			public override int CountTestCases()
			{
				return 1;
			}

			public class ShellTestParameters : ShellTest.Parameters
			{
				public override int GetTimeoutMilliseconds()
				{
					if (Runtime.GetProperty("mozilla.js.tests.timeout") != null)
					{
						return System.Convert.ToInt32(Runtime.GetProperty("mozilla.js.tests.timeout"));
					}
					return 60000;
				}
			}

			/// <exception cref="System.Exception"></exception>
			public override void RunBare()
			{
				ShellContextFactory shellContextFactory = new ShellContextFactory();
				shellContextFactory.SetOptimizationLevel(optimizationLevel);
				StandardTests.JsTestCase.ShellTestParameters @params = new StandardTests.JsTestCase.ShellTestParameters();
				ShellTest.Run(shellContextFactory, jsFile, @params, new StandardTests.JunitStatus());
			}
		}
	}
}
