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
using Sharpen;

namespace Rhino.Tests.CommonJS.Module
{
	[TestFixture]
	public class RequireTest
	{
		[Test]
		public virtual void TestSandboxed()
		{
			Context cx = CreateContext();
			Require require = GetSandboxedRequire(cx);
			require.RequireMain(cx, "testSandboxed");
			// Also, test idempotent double-require of same main:
			require.RequireMain(cx, "testSandboxed");
			// Also, test failed require of different main:
			try
			{
				require.RequireMain(cx, "blah");
				Assert.Fail();
			}
			catch (InvalidOperationException)
			{
			}
		}

		// Expected, success
		private Context CreateContext()
		{
			Context cx = Context.Enter();
			cx.SetOptimizationLevel(-1);
			return cx;
		}

		/// <exception cref="System.Exception"></exception>
		[Test]
		public virtual void TestNonSandboxed()
		{
			Context cx = CreateContext();
			Scriptable scope = cx.InitStandardObjects();
			Require require = GetSandboxedRequire(cx, scope, false);
			string jsFile = GetType().GetResource("testNonSandboxed.js").ToString();
			ScriptableObject.PutProperty(scope, "moduleUri", jsFile);
			require.RequireMain(cx, "testNonSandboxed");
		}

		/// <exception cref="System.Exception"></exception>
		[Test]
		public virtual void TestVariousUsageErrors()
		{
			TestWithSandboxedRequire("testNoArgsRequire");
		}

		/// <exception cref="System.Exception"></exception>
		[Test]
		public virtual void TestRelativeId()
		{
			Context cx = CreateContext();
			Scriptable scope = cx.InitStandardObjects();
			Require require = GetSandboxedRequire(cx, scope, false);
			require.Install(scope);
			cx.EvaluateReader(scope, GetReader("testRelativeId.js"), "testRelativeId.js", 1, null);
		}

		/// <exception cref="System.Exception"></exception>
		[Test]
		public virtual void TestSetMainForAlreadyLoadedModule()
		{
			Context cx = CreateContext();
			Scriptable scope = cx.InitStandardObjects();
			Require require = GetSandboxedRequire(cx, scope, false);
			require.Install(scope);
			cx.EvaluateReader(scope, GetReader("testSetMainForAlreadyLoadedModule.js"), "testSetMainForAlreadyLoadedModule.js", 1, null);
			try
			{
				require.RequireMain(cx, "assert");
				Assert.Fail();
			}
			catch (InvalidOperationException e)
			{
				Assert.AreEqual(e.Message, "Attempt to set main module after it was loaded");
			}
		}

		private TextReader GetReader(string name)
		{
			return new StreamReader(GetType().GetResourceAsStream(name));
		}

		/// <exception cref="System.Exception"></exception>
		private void TestWithSandboxedRequire(string moduleId)
		{
			Context cx = CreateContext();
			GetSandboxedRequire(cx).RequireMain(cx, moduleId);
		}

		/// <exception cref="Sharpen.URISyntaxException"></exception>
		private Require GetSandboxedRequire(Context cx)
		{
			return GetSandboxedRequire(cx, cx.InitStandardObjects(), true);
		}

		/// <exception cref="Sharpen.URISyntaxException"></exception>
		private Require GetSandboxedRequire(Context cx, Scriptable scope, bool sandboxed)
		{
			return new Require(cx, cx.InitStandardObjects(), new StrongCachingModuleScriptProvider(new UrlModuleSourceProvider(new List<Uri> (1) { GetDirectory() }, null)), null, null, true);
		}

		/// <exception cref="Sharpen.URISyntaxException"></exception>
		private Uri GetDirectory()
		{
			string jsFile = GetType().GetResource("testSandboxed.js").ToString();
			return new Uri(jsFile.Substring(0, jsFile.LastIndexOf('/') + 1));
		}
	}
}
