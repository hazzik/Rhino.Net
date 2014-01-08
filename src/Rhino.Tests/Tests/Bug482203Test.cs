/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	[NUnit.Framework.TestFixture]
	public class Bug482203Test
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestJsApi()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				Script script = cx.CompileReader(new StreamReader(typeof(Bug482203Test).GetResourceAsStream("Bug482203.js")), string.Empty, 1, null);
				Scriptable scope = cx.InitStandardObjects();
				script.Exec(cx, scope);
				int counter = 0;
				for (; ; )
				{
					object cont = ScriptableObject.GetProperty(scope, "c");
					if (cont == null)
					{
						break;
					}
					counter++;
					((Callable)cont).Call(cx, scope, scope, new object[] { null });
				}
				NUnit.Framework.Assert.AreEqual(counter, 5);
				NUnit.Framework.Assert.AreEqual(Sharpen.Extensions.ValueOf(3), ScriptableObject.GetProperty(scope, "result"));
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestJavaApi()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				Script script = cx.CompileReader(new StreamReader(typeof(Bug482203Test).GetResourceAsStream("Bug482203.js")), string.Empty, 1, null);
				Scriptable scope = cx.InitStandardObjects();
				cx.ExecuteScriptWithContinuations(script, scope);
				int counter = 0;
				for (; ; )
				{
					object cont = ScriptableObject.GetProperty(scope, "c");
					if (cont == null)
					{
						break;
					}
					counter++;
					cx.ResumeContinuation(cont, scope, null);
				}
				NUnit.Framework.Assert.AreEqual(counter, 5);
				NUnit.Framework.Assert.AreEqual(Sharpen.Extensions.ValueOf(3), ScriptableObject.GetProperty(scope, "result"));
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
