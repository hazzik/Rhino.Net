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
using Rhino.CommonJS.Module;
using Rhino.CommonJS.Module.Provider;
using Rhino.Drivers;
using Rhino.Tests.CommonJS.Module;
using Sharpen;

namespace Rhino.Tests.CommonJS.Module
{
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ComplianceTest.java,v 1.1 2011/04/07 22:24:37 hannes%helma.at Exp $</version>
	[TestFixture(Description = "InteroperableJS tests")]
	public class ComplianceTest
	{
//		/// <exception cref="System.Exception"></exception>
//		public static TestSuite Suite()
//		{
//			TestSuite suite = new TestSuite("InteroperableJS tests");
//			Uri url = typeof(ComplianceTest).GetResource("1.0");
//			string path = URLDecoder.Decode(url.GetFile(), Runtime.GetProperty("file.encoding"));
//			DirectoryInfo testsDir = new DirectoryInfo(path);
//			AddTests(suite, testsDir, string.Empty);
//			return suite;
//		}

		private static void AddTests(TestSuite suite, DirectoryInfo testDir, string name)
		{
			FileInfo programFile = new FileInfo(testDir.FullName + "/" + "program.js");
			if (programFile.Exists)
			{
				suite.AddTest(new ComplianceTestCase(testDir, name));
			}
			else
			{
				DirectoryInfo[] files = testDir.GetDirectories();
				foreach (DirectoryInfo directory in files)
				{
					AddTests(suite, directory, name + "/" + directory.Name);
				}
			}
		}

		private sealed class ComplianceTestCase : TestCase
		{
			public ComplianceTestCase(DirectoryInfo testDir, string baseArg1) : base(baseArg1)
			{
				this.testDir = testDir;
			}

			/// <exception cref="System.Exception"></exception>
			public void RunBare()
			{
				Context cx = Context.Enter();
				try
				{
					cx.SetOptimizationLevel(-1);
					Scriptable scope = cx.InitStandardObjects();
					ScriptableObject.PutProperty(scope, "print", new Print(scope));
					CreateRequire(testDir, cx, scope).RequireMain(cx, "program");
				}
				finally
				{
					Context.Exit();
				}
			}

			private readonly DirectoryInfo testDir;
		}

		/// <exception cref="Sharpen.URISyntaxException"></exception>
		private static Require CreateRequire(DirectoryInfo dir, Context cx, Scriptable scope)
		{
			return new Require(cx,
				scope,
				new StrongCachingModuleScriptProvider(new UrlModuleSourceProvider(new List<Uri>(1) { new Uri(dir.FullName) },
					new List<Uri>(1) { new Uri(typeof (ComplianceTest).GetResource(".") + "/") })),
				null,
				null,
				false);
		}

		[Serializable]
		private sealed class Print : ScriptableObject, Function
		{
			internal Print(Scriptable scope)
			{
				Prototype = GetFunctionPrototype(scope);
			}

			public object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
			{
				if (args.Length > 1 && "fail".Equals(args[1]))
				{
					throw new AssertionException(args[0].ToString());
				}
				return null;
			}

			public Scriptable Construct(Context cx, Scriptable scope, object[] args)
			{
				throw new AssertionException("Shouldn't be invoked as constructor");
			}

			public override string GetClassName()
			{
				return "Function";
			}
		}
	}
}
