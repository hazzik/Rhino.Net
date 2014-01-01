/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
		// Rhino.Tests.MozillaSuiteTest
		/// <exception cref="System.Exception"></exception>
		public static TestSuite Suite()
		{
			TestSuite suite = new TestSuite("Standard JavaScript tests");
			return suite;
		}

		private static void AddSuites(TestSuite topLevel, DirectoryInfo testDir, string[] excludes, int optimizationLevel)
		{
			DirectoryInfo[] subdirs = testDir.GetDirectories().Where(ShellTest.DIRECTORY_FILTER).ToArray();
			Array.Sort(subdirs);
			foreach (DirectoryInfo subdir in subdirs)
			{
				string name = subdir.Name;
				if (TestUtils.Matches(excludes, name))
				{
					continue;
				}
				TestSuite testSuite = new TestSuite(name);
				AddCategories(testSuite, subdir, name + "/", excludes, optimizationLevel);
				topLevel.AddTest(testSuite);
			}
		}

		private static void AddCategories(TestSuite suite, DirectoryInfo suiteDir, string prefix, string[] excludes, int optimizationLevel)
		{
			DirectoryInfo[] subdirs = suiteDir.GetDirectories().Where(ShellTest.DIRECTORY_FILTER).ToArray();
			Array.Sort(subdirs);
			foreach (DirectoryInfo subdir in subdirs)
			{
				string name = subdir.Name;
				TestSuite testCategory = new TestSuite(name);
				AddTests(testCategory, subdir, prefix + name + "/", excludes, optimizationLevel);
				suite.AddTest(testCategory);
			}
		}

		private static void AddTests(TestSuite suite, DirectoryInfo suiteDir, string prefix, string[] excludes, int optimizationLevel)
		{
			FileInfo[] jsFiles = suiteDir.GetFiles().Where(ShellTest.TEST_FILTER).ToArray();
			Array.Sort(jsFiles);
			foreach (FileInfo jsFile in jsFiles)
			{
				string name = jsFile.Name;
				if (!TestUtils.Matches(excludes, prefix + name))
				{
					throw new NotImplementedException();
//		            suite.AddTest(new JsTestCase(jsFile, optimizationLevel));
				}
			}
		}

		public class JunitStatus : ShellTest.Status
		{
			public sealed override void Running(FileInfo jsFile)
			{
			}

			//    do nothing
			public sealed override void Failed(string s)
			{
				Assert.Fail(s);
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

		[TestFixture]
		public sealed class JsTestCase
		{
			private readonly FileInfo jsFile;

			private readonly int optimizationLevel;

			internal JsTestCase(FileInfo jsFile, int optimizationLevel) 
				//: base(jsFile.Name + (optimizationLevel == 1 ? "-compiled" : "-interpreted"))
			{
				this.jsFile = jsFile;
				this.optimizationLevel = optimizationLevel;
			}

			public class ShellTestParameters : ShellTest.Parameters
			{
				public override int GetTimeoutMilliseconds()
				{
					var timeout = Runtime.GetProperty("mozilla.js.tests.timeout");
					if (timeout != null)
					{
						return Convert.ToInt32(timeout);
					}
					return 60000;
				}
			}

			/// <exception cref="System.Exception"></exception>
			//[TestCaseSource("")]
			public void RunBare()
			{
				ShellContextFactory shellContextFactory = new ShellContextFactory();
				shellContextFactory.SetOptimizationLevel(optimizationLevel);
				ShellTestParameters @params = new ShellTestParameters();
				ShellTest.Run(shellContextFactory, jsFile, @params, new JunitStatus());
			}
		}
	}
}
