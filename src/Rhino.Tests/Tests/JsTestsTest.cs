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

namespace Rhino.Tests
{
	[TestFixture]
	public sealed class JsTestsTest
	{
		private const string BaseDirectory = "../../jstests";

		public int OptimizationLevel { private get; set; }

		public IEnumerable<FileInfo> JsTests
		{
			get { return new DirectoryInfo(BaseDirectory).EnumerateFiles("*.jstest", SearchOption.AllDirectories); }
		}

		[TestCaseSource("JsTests")]
		public void TestJsTestsInterpreted(FileInfo file)
		{
			OptimizationLevel = -1;
			RunJsTests(file);
		}

		[TestCaseSource("JsTests")]
		public void TestJsTestsCompiled(FileInfo file)
		{
			OptimizationLevel = 0;
			RunJsTests(file);
		}

		[TestCaseSource("JsTests")]
		public void TestJsTestsOptimized(FileInfo file)
		{
			OptimizationLevel = 9;
			RunJsTests(file);
		}

		private static void RunJsTest(Context cx, Scriptable shared, string name, string source)
		{
			// create a lightweight top-level scope
			Scriptable scope = cx.NewObject(shared);
			scope.SetPrototype(shared);
			Console.Out.Write(name + ": ");
			object result;
			try
			{
				result = cx.EvaluateString(scope, source, "jstest input", 1, null);
			}
			catch (Exception)
			{
				Console.Out.WriteLine("FAILED");
				throw;
			}
			Assert.IsTrue(result != null);
			Assert.IsTrue("success".Equals(result));
			Console.Out.WriteLine("passed");
		}

		private void RunJsTests(FileInfo test)
		{
			ContextFactory factory = ContextFactory.GetGlobal();
			Context cx = factory.EnterContext();
			try
			{
				cx.SetOptimizationLevel(OptimizationLevel);
				Scriptable shared = cx.InitStandardObjects();
				using (var reader = test.OpenText())
				{
					RunJsTest(cx, shared, test.Name, reader.ReadToEnd());
				}
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
