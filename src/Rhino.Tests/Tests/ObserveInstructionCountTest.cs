/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Drivers;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	public class ObserveInstructionCountTest
	{
		internal class MyContext : Context
		{
			internal MyContext(ContextFactory factory) : base(factory)
			{
			}

			internal int quota;
			// Custom Context to store execution time.
		}

		[System.Serializable]
		internal class QuotaExceeded : Exception
		{
			private const long serialVersionUID = -8018441873635071899L;
		}

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			TestUtils.SetGlobalContextFactory(new ObserveInstructionCountTest.MyFactory());
		}

		[NUnit.Framework.TearDown]
		protected virtual void TearDown()
		{
			TestUtils.SetGlobalContextFactory(null);
		}

		internal class MyFactory : ContextFactory
		{
			protected override Context MakeContext()
			{
				ObserveInstructionCountTest.MyContext cx = new ObserveInstructionCountTest.MyContext(this);
				// Make Rhino runtime call observeInstructionCount
				// each 500 bytecode instructions (if we're really enforcing
				// a quota of 2000, we could set this closer to 2000)
				cx.SetInstructionObserverThreshold(500);
				return cx;
			}

			protected override void ObserveInstructionCount(Context cx, int instructionCount)
			{
				ObserveInstructionCountTest.MyContext mcx = (ObserveInstructionCountTest.MyContext)cx;
				mcx.quota -= instructionCount;
				if (mcx.quota <= 0)
				{
					throw new ObserveInstructionCountTest.QuotaExceeded();
				}
			}

			protected override object DoTopCall(Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
			{
				ObserveInstructionCountTest.MyContext mcx = (ObserveInstructionCountTest.MyContext)cx;
				mcx.quota = 2000;
				return base.DoTopCall(callable, cx, scope, thisObj, args);
			}
		}

		private void BaseCase(int optimizationLevel, string source)
		{
			ContextFactory factory = new ObserveInstructionCountTest.MyFactory();
			Context cx = factory.EnterContext();
			cx.SetOptimizationLevel(optimizationLevel);
			NUnit.Framework.Assert.IsTrue(cx is ObserveInstructionCountTest.MyContext);
			try
			{
				Scriptable globalScope = cx.InitStandardObjects();
				cx.EvaluateString(globalScope, source, "test source", 1, null);
				Fail();
			}
			catch (ObserveInstructionCountTest.QuotaExceeded)
			{
			}
			catch (Exception e)
			{
				// expected
				Fail(e.ToString());
			}
			finally
			{
				Context.Exit();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestWhileTrueInGlobal()
		{
			string source = "var i=0; while (true) i++;";
			BaseCase(-1, source);
			// interpreted mode
			BaseCase(1, source);
		}

		// compiled mode
		[NUnit.Framework.Test]
		public virtual void TestWhileTrueNoCounterInGlobal()
		{
			string source = "while (true);";
			BaseCase(-1, source);
			// interpreted mode
			BaseCase(1, source);
		}

		// compiled mode
		[NUnit.Framework.Test]
		public virtual void TestWhileTrueInFunction()
		{
			string source = "var i=0; function f() { while (true) i++; } f();";
			BaseCase(-1, source);
			// interpreted mode
			BaseCase(1, source);
		}

		// compiled mode
		[NUnit.Framework.Test]
		public virtual void TestForever()
		{
			string source = "for(;;);";
			BaseCase(-1, source);
			// interpreted mode
			BaseCase(1, source);
		}
		// compiled mode
	}
}
