/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Rhino.Tests;
using Rhino.Tests.Commonjs.Module;
using Rhino.Tests.Commonjs.Module.Provider;
using Rhino.Tests.Tests.Commonjs.Module;
using Sharpen;

namespace Rhino.Tests.Tests.Commonjs.Module
{
	/// <author>Attila Szegedi</author>
	/// <version>$Id: ComplianceTest.java,v 1.1 2011/04/07 22:24:37 hannes%helma.at Exp $</version>
	[NUnit.Framework.TestFixture]
	public class ComplianceTest
	{
		/// <exception cref="System.Exception"></exception>
		public static TestSuite Suite()
		{
			TestSuite suite = new TestSuite("InteroperableJS tests");
			Uri url = typeof(ComplianceTest).GetResource("1.0");
			string path = URLDecoder.Decode(url.GetFile(), Runtime.GetProperty("file.encoding"));
			FilePath testsDir = new FilePath(path);
			AddTests(suite, testsDir, string.Empty);
			return suite;
		}

		private static void AddTests(TestSuite suite, FilePath testDir, string name)
		{
			FilePath programFile = new FilePath(testDir, "program.js");
			if (programFile.IsFile())
			{
				suite.AddTest(CreateTest(testDir, name));
			}
			else
			{
				FilePath[] files = testDir.ListFiles();
				foreach (FilePath file in files)
				{
					if (file.IsDirectory())
					{
						AddTests(suite, file, name + "/" + file.GetName());
					}
				}
			}
		}

		private static NUnit.Framework.Test CreateTest(FilePath testDir, string name)
		{
			return new _TestCase_54(testDir, name);
		}

		private sealed class _TestCase_54 : TestCase
		{
			public _TestCase_54(FilePath testDir, string baseArg1) : base(baseArg1)
			{
				this.testDir = testDir;
			}

			public override int CountTestCases()
			{
				return 1;
			}

			/// <exception cref="System.Exception"></exception>
			public override void RunBare()
			{
				Context cx = Context.Enter();
				try
				{
					cx.SetOptimizationLevel(-1);
					Scriptable scope = cx.InitStandardObjects();
					ScriptableObject.PutProperty(scope, "print", new ComplianceTest.Print(scope));
					ComplianceTest.CreateRequire(testDir, cx, scope).RequireMain(cx, "program");
				}
				finally
				{
					Context.Exit();
				}
			}

			private readonly FilePath testDir;
		}

		/// <exception cref="Sharpen.URISyntaxException"></exception>
		private static Require CreateRequire(FilePath dir, Context cx, Scriptable scope)
		{
			return new Require(cx, scope, new StrongCachingModuleScriptProvider(new UrlModuleSourceProvider(Collections.Singleton(dir.GetAbsoluteFile().ToURI()), Collections.Singleton(new Uri(typeof(ComplianceTest).GetResource(".").ToExternalForm() + "/")))), null, null, false);
		}

		[System.Serializable]
		private class Print : ScriptableObject, Function
		{
			internal Print(Scriptable scope)
			{
				SetPrototype(ScriptableObject.GetFunctionPrototype(scope));
			}

			public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
			{
				if (args.Length > 1 && "fail".Equals(args[1]))
				{
					throw new AssertionFailedError(args[0].ToString());
				}
				return null;
			}

			public virtual Scriptable Construct(Context cx, Scriptable scope, object[] args)
			{
				throw new AssertionFailedError("Shouldn't be invoked as constructor");
			}

			public override string GetClassName()
			{
				return "Function";
			}
		}
	}
}
