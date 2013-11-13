/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Rhino;
using Rhino.Drivers;
using Sharpen;

namespace Rhino.Drivers
{
	[NUnit.Framework.TestFixture]
	public class JsTestsBase
	{
		private int optimizationLevel;

		public virtual void SetOptimizationLevel(int level)
		{
			this.optimizationLevel = level;
		}

		public virtual void RunJsTest(Context cx, Scriptable shared, string name, string source)
		{
			// create a lightweight top-level scope
			Scriptable scope = cx.NewObject(shared);
			scope.SetPrototype(shared);
			System.Console.Out.Write(name + ": ");
			object result;
			try
			{
				result = cx.EvaluateString(scope, source, "jstest input", 1, null);
			}
			catch (Exception e)
			{
				System.Console.Out.WriteLine("FAILED");
				throw;
			}
			NUnit.Framework.Assert.IsTrue(result != null);
			NUnit.Framework.Assert.IsTrue("success".Equals(result));
			System.Console.Out.WriteLine("passed");
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void RunJsTests(FilePath[] tests)
		{
			ContextFactory factory = ContextFactory.GetGlobal();
			Context cx = factory.EnterContext();
			try
			{
				cx.SetOptimizationLevel(this.optimizationLevel);
				Scriptable shared = cx.InitStandardObjects();
				foreach (FilePath f in tests)
				{
					int length = (int)f.Length();
					// don't worry about very long
					// files
					char[] buf = new char[length];
					new FileReader(f).Read(buf, 0, length);
					string session = new string(buf);
					RunJsTest(cx, shared, f.GetName(), session);
				}
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
