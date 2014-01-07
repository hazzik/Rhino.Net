/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>Unit tests for <a href="https://bugzilla.mozilla.org/show_bug.cgi?id=549604">bug 549604</a>.</summary>
	/// <remarks>
	/// Unit tests for <a href="https://bugzilla.mozilla.org/show_bug.cgi?id=549604">bug 549604</a>.
	/// This tests verify the properties of a JS exception and ensures that they don't change with different optimization levels.
	/// </remarks>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class ErrorPropertiesTest
	{
		internal static readonly string LS = Runtime.GetProperty("line.separator");

		private void TestScriptStackTrace(string script, string expectedStackTrace)
		{
			TestScriptStackTrace(script, expectedStackTrace, -1);
			TestScriptStackTrace(script, expectedStackTrace, 0);
			TestScriptStackTrace(script, expectedStackTrace, 1);
		}

		private void TestScriptStackTrace(string script, string expectedStackTrace, int optimizationLevel)
		{
			try
			{
				Utils.ExecuteScript(script, optimizationLevel);
			}
			catch (RhinoException e)
			{
				NUnit.Framework.Assert.AreEqual(expectedStackTrace, e.GetScriptStackTrace());
			}
		}

		[NUnit.Framework.Test]
		public virtual void FileName()
		{
			TestIt("try { null.method() } catch (e) { e.fileName }", "myScript.js");
			TestIt("try { null.property } catch (e) { e.fileName }", "myScript.js");
		}

		[NUnit.Framework.Test]
		public virtual void LineNumber()
		{
			TestIt("try { null.method() } catch (e) { e.lineNumber }", 1);
			TestIt("try {\n null.method() \n} catch (e) { e.lineNumber }", 2);
			TestIt("\ntry \n{\n null.method() \n} catch (e) { e.lineNumber }", 4);
			TestIt("function f() {\n null.method(); \n}\n try { f() } catch (e) { e.lineNumber }", 2);
		}

		[NUnit.Framework.Test]
		public virtual void DefaultStack()
		{
			RhinoException.UseMozillaStackStyle(false);
			TestScriptStackTrace("null.method()", "\tat myScript.js:1" + LS);
			string script = "function f() \n{\n  null.method();\n}\nf();\n";
			TestScriptStackTrace(script, "\tat myScript.js:3 (f)" + LS + "\tat myScript.js:5" + LS);
			TestIt("try { null.method() } catch (e) { e.stack }", "\tat myScript.js:1" + LS);
			string expectedStack = "\tat myScript.js:2 (f)" + LS + "\tat myScript.js:4" + LS;
			TestIt("function f() {\n null.method(); \n}\n try { f() } catch (e) { e.stack }", expectedStack);
		}

		[NUnit.Framework.Test]
		public virtual void MozillaStack()
		{
			RhinoException.UseMozillaStackStyle(true);
			TestScriptStackTrace("null.method()", "@myScript.js:1" + LS);
			string script = "function f() \n{\n  null.method();\n}\nf();\n";
			TestScriptStackTrace(script, "f()@myScript.js:3" + LS + "@myScript.js:5" + LS);
			TestIt("try { null.method() } catch (e) { e.stack }", "@myScript.js:1" + LS);
			string expectedStack = "f()@myScript.js:2" + LS + "@myScript.js:4" + LS;
			TestIt("function f() {\n null.method(); \n}\n try { f() } catch (e) { e.stack }", expectedStack);
		}

		private static void TestIt(string script, object expected)
		{
			Utils.RunWithAllOptimizationLevels(cx =>
		{
				ScriptableObject scope = cx.InitStandardObjects();
				object o = cx.EvaluateString(scope, script, "myScript.js", 1, null);
				Assert.AreEqual(expected, o);
				return o;
			});
		}
	}
}
