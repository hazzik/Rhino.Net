/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using Rhino.Drivers;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	public class JsTestsTest : JsTestsBase
	{
		internal static readonly string baseDirectory = "testsrc" + FilePath.separator + "jstests";

		internal const string jstestsExtension = ".jstest";

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void RunJsTests()
		{
			FilePath[] tests = TestUtils.RecursiveListFiles(new FilePath(baseDirectory), new _FileFilter_21());
			RunJsTests(tests);
		}

		private sealed class _FileFilter_21 : FileFilter
		{
			public _FileFilter_21()
			{
			}

			public bool Accept(FilePath f)
			{
				return f.GetName().EndsWith(JsTestsTest.jstestsExtension);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestJsTestsInterpreted()
		{
			SetOptimizationLevel(-1);
			RunJsTests();
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestJsTestsCompiled()
		{
			SetOptimizationLevel(0);
			RunJsTests();
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestJsTestsOptimized()
		{
			SetOptimizationLevel(9);
			RunJsTests();
		}
	}
}
