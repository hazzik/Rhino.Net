/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tests;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ClassShutterExceptionTest
	{
		private static Context.ClassShutterSetter classShutterSetter;

		/// <summary>Define a ClassShutter that prevents access to all Java classes.</summary>
		/// <remarks>Define a ClassShutter that prevents access to all Java classes.</remarks>
		internal class OpaqueShutter : ClassShutter
		{
			public virtual bool VisibleToScripts(string name)
			{
				return false;
			}
		}

		public virtual void Helper(string source)
		{
			Context cx = Context.Enter();
			Context.ClassShutterSetter setter = cx.GetClassShutterSetter();
			try
			{
				Scriptable globalScope = cx.InitStandardObjects();
				if (setter == null)
				{
					setter = classShutterSetter;
				}
				else
				{
					classShutterSetter = setter;
				}
				setter.SetClassShutter(new ClassShutterExceptionTest.OpaqueShutter());
				cx.EvaluateString(globalScope, source, "test source", 1, null);
			}
			finally
			{
				setter.SetClassShutter(null);
				Context.Exit();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestClassShutterException()
		{
			try
			{
				Helper("java.lang.System.out.println('hi');");
				Fail();
			}
			catch (RhinoException)
			{
				// OpaqueShutter should prevent access to java.lang...
				return;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestThrowingException()
		{
			// JavaScript exceptions with no reference to Java
			// should not be affected by the ClassShutter
			Helper("try { throw 3; } catch (e) { }");
		}

		[NUnit.Framework.Test]
		public virtual void TestThrowingEcmaError()
		{
			try
			{
				// JavaScript exceptions with no reference to Java
				// should not be affected by the ClassShutter
				Helper("friggin' syntax error!");
				Fail("Should have thrown an exception");
			}
			catch (EvaluatorException)
			{
			}
		}

		// should have thrown an exception for syntax error
		[NUnit.Framework.Test]
		public virtual void TestThrowingEvaluatorException()
		{
			// JavaScript exceptions with no reference to Java
			// should not be affected by the ClassShutter
			Helper("try { eval('for;if;else'); } catch (e) { }");
		}
	}
}
