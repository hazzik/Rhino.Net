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
using Rhino.Serialize;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>
	/// Test of new API functions for running and resuming scripts containing
	/// continuations, and for suspending a continuation from a Java method
	/// called from JavaScript.
	/// </summary>
	/// <remarks>
	/// Test of new API functions for running and resuming scripts containing
	/// continuations, and for suspending a continuation from a Java method
	/// called from JavaScript.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	public class ContinuationsApiTest
	{
		internal Scriptable globalScope;

		[System.Serializable]
		public class MyClass
		{
			private const long serialVersionUID = 4189002778806232070L;

			public virtual int F(int a)
			{
				Context cx = Context.Enter();
				try
				{
					ContinuationPending pending = cx.CaptureContinuation();
					pending.SetApplicationState(a);
					throw pending;
				}
				finally
				{
					Context.Exit();
				}
			}

			public virtual int G(int a)
			{
				Context cx = Context.Enter();
				try
				{
					ContinuationPending pending = cx.CaptureContinuation();
					pending.SetApplicationState(2 * a);
					throw pending;
				}
				finally
				{
					Context.Exit();
				}
			}

			public virtual string H()
			{
				Context cx = Context.Enter();
				try
				{
					ContinuationPending pending = cx.CaptureContinuation();
					pending.SetApplicationState("2*3");
					throw pending;
				}
				finally
				{
					Context.Exit();
				}
			}
		}

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			Context cx = Context.Enter();
			try
			{
				globalScope = cx.InitStandardObjects();
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				globalScope.Put("myObject", globalScope, Context.JavaToJS(new ContinuationsApiTest.MyClass(), globalScope));
			}
			finally
			{
				Context.Exit();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestScriptWithContinuations()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				Script script = cx.CompileString("myObject.f(3) + 1;", "test source", 1, null);
				cx.ExecuteScriptWithContinuations(script, globalScope);
				NUnit.Framework.Assert.Fail("Should throw ContinuationPending");
			}
			catch (ContinuationPending pending)
			{
				object applicationState = pending.GetApplicationState();
				NUnit.Framework.Assert.AreEqual(3, applicationState);
				int saved = (int)applicationState;
				object result = cx.ResumeContinuation(pending.GetContinuation(), globalScope, saved + 1);
				NUnit.Framework.Assert.AreEqual(5, System.Convert.ToInt32(result));
			}
			finally
			{
				Context.Exit();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestScriptWithMultipleContinuations()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				Script script = cx.CompileString("myObject.f(3) + myObject.g(3) + 2;", "test source", 1, null);
				cx.ExecuteScriptWithContinuations(script, globalScope);
				NUnit.Framework.Assert.Fail("Should throw ContinuationPending");
			}
			catch (ContinuationPending pending)
			{
				try
				{
					object applicationState = pending.GetApplicationState();
					NUnit.Framework.Assert.AreEqual(3, applicationState);
					int saved = (int)applicationState;
					cx.ResumeContinuation(pending.GetContinuation(), globalScope, saved + 1);
					NUnit.Framework.Assert.Fail("Should throw another ContinuationPending");
				}
				catch (ContinuationPending pending2)
				{
					object applicationState2 = pending2.GetApplicationState();
					NUnit.Framework.Assert.AreEqual(6, applicationState2);
					int saved2 = (int)applicationState2;
					object result2 = cx.ResumeContinuation(pending2.GetContinuation(), globalScope, saved2 + 1);
					NUnit.Framework.Assert.AreEqual(13, System.Convert.ToInt32(result2));
				}
			}
			finally
			{
				Context.Exit();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestScriptWithNestedContinuations()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				Script script = cx.CompileString("myObject.g( myObject.f(1) ) + 2;", "test source", 1, null);
				cx.ExecuteScriptWithContinuations(script, globalScope);
				NUnit.Framework.Assert.Fail("Should throw ContinuationPending");
			}
			catch (ContinuationPending pending)
			{
				try
				{
					object applicationState = pending.GetApplicationState();
					NUnit.Framework.Assert.AreEqual(1, applicationState);
					int saved = (int)applicationState;
					cx.ResumeContinuation(pending.GetContinuation(), globalScope, saved + 1);
					NUnit.Framework.Assert.Fail("Should throw another ContinuationPending");
				}
				catch (ContinuationPending pending2)
				{
					object applicationState2 = pending2.GetApplicationState();
					NUnit.Framework.Assert.AreEqual(4, applicationState2);
					int saved2 = (int)applicationState2;
					object result2 = cx.ResumeContinuation(pending2.GetContinuation(), globalScope, saved2 + 2);
					NUnit.Framework.Assert.AreEqual(8, System.Convert.ToInt32(result2));
				}
			}
			finally
			{
				Context.Exit();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestFunctionWithContinuations()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				cx.EvaluateString(globalScope, "function f(a) { return myObject.f(a); }", "function test source", 1, null);
				Function f = (Function)globalScope.Get("f", globalScope);
				object[] args = new object[] { 7 };
				cx.CallFunctionWithContinuations(f, globalScope, args);
				NUnit.Framework.Assert.Fail("Should throw ContinuationPending");
			}
			catch (ContinuationPending pending)
			{
				object applicationState = pending.GetApplicationState();
				NUnit.Framework.Assert.AreEqual(7, System.Convert.ToInt32(applicationState));
				int saved = (int)applicationState;
				object result = cx.ResumeContinuation(pending.GetContinuation(), globalScope, saved + 1);
				NUnit.Framework.Assert.AreEqual(8, System.Convert.ToInt32(result));
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <summary>
		/// Since a continuation can only capture JavaScript frames and not Java
		/// frames, ensure that Rhino throws an exception when the JavaScript frames
		/// don't reach all the way to the code called by
		/// executeScriptWithContinuations or callFunctionWithContinuations.
		/// </summary>
		/// <remarks>
		/// Since a continuation can only capture JavaScript frames and not Java
		/// frames, ensure that Rhino throws an exception when the JavaScript frames
		/// don't reach all the way to the code called by
		/// executeScriptWithContinuations or callFunctionWithContinuations.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestErrorOnEvalCall()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				Script script = cx.CompileString("eval('myObject.f(3);');", "test source", 1, null);
				cx.ExecuteScriptWithContinuations(script, globalScope);
				NUnit.Framework.Assert.Fail("Should throw IllegalStateException");
			}
			catch (WrappedException we)
			{
				Exception t = we.GetWrappedException();
				NUnit.Framework.Assert.IsTrue(t is InvalidOperationException);
				NUnit.Framework.Assert.IsTrue(t.Message.StartsWith("Cannot capture continuation"));
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSerializationWithContinuations()
		{
			Context cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				cx.EvaluateString(globalScope, "function f(a) { var k = myObject.f(a); var t = []; return k; }", "function test source", 1, null);
				Function f = (Function)globalScope.Get("f", globalScope);
				object[] args = new object[] { 7 };
				cx.CallFunctionWithContinuations(f, globalScope, args);
				NUnit.Framework.Assert.Fail("Should throw ContinuationPending");
			}
			catch (ContinuationPending pending)
			{
				// serialize
				MemoryStream baos = new MemoryStream();
				ScriptableOutputStream sos = new ScriptableOutputStream(baos, globalScope);
				sos.WriteObject(globalScope);
				sos.WriteObject(pending.GetContinuation());
				sos.Close();
				baos.Close();
				byte[] serializedData = baos.ToArray();
				// deserialize
				MemoryStream bais = new MemoryStream(serializedData);
				ScriptableInputStream sis = new ScriptableInputStream(bais, globalScope);
				globalScope = (Scriptable)sis.ReadObject();
				object continuation = sis.ReadObject();
				sis.Close();
				bais.Close();
				object result = cx.ResumeContinuation(continuation, globalScope, 8);
				NUnit.Framework.Assert.AreEqual(8, System.Convert.ToInt32((result)));
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestContinuationsPrototypesAndSerialization()
		{
			byte[] serializedData = null;
			{
				Scriptable globalScope;
				Context cx = Context.Enter();
				try
				{
					globalScope = cx.InitStandardObjects();
					cx.SetOptimizationLevel(-1);
					// must use interpreter mode
					globalScope.Put("myObject", globalScope, Context.JavaToJS(new ContinuationsApiTest.MyClass(), globalScope));
				}
				finally
				{
					Context.Exit();
				}
				cx = Context.Enter();
				try
				{
					cx.SetOptimizationLevel(-1);
					// must use interpreter mode
					cx.EvaluateString(globalScope, "function f(a) { Number.prototype.blargh = function() {return 'foo';}; var k = myObject.f(a); var t = []; return new Number(8).blargh(); }", "function test source", 1, null);
					Function f = (Function)globalScope.Get("f", globalScope);
					object[] args = new object[] { 7 };
					cx.CallFunctionWithContinuations(f, globalScope, args);
					NUnit.Framework.Assert.Fail("Should throw ContinuationPending");
				}
				catch (ContinuationPending pending)
				{
					// serialize
					MemoryStream baos = new MemoryStream();
					ObjectOutputStream sos = new ObjectOutputStream(baos);
					sos.WriteObject(globalScope);
					sos.WriteObject(pending.GetContinuation());
					sos.Close();
					baos.Close();
					serializedData = baos.ToArray();
				}
				finally
				{
					Context.Exit();
				}
			}
			{
				try
				{
					Context cx = Context.Enter();
					Scriptable globalScope;
					// deserialize
					MemoryStream bais = new MemoryStream(serializedData);
					ObjectInputStream sis = new ObjectInputStream(bais);
					globalScope = (Scriptable)sis.ReadObject();
					object continuation = sis.ReadObject();
					sis.Close();
					bais.Close();
					object result = cx.ResumeContinuation(continuation, globalScope, 8);
					NUnit.Framework.Assert.AreEqual("foo", result);
				}
				finally
				{
					Context.Exit();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestContinuationsInlineFunctionsSerialization()
		{
			Scriptable globalScope;
			Context cx = Context.Enter();
			try
			{
				globalScope = cx.InitStandardObjects();
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				globalScope.Put("myObject", globalScope, Context.JavaToJS(new ContinuationsApiTest.MyClass(), globalScope));
			}
			finally
			{
				Context.Exit();
			}
			cx = Context.Enter();
			try
			{
				cx.SetOptimizationLevel(-1);
				// must use interpreter mode
				cx.EvaluateString(globalScope, "function f(a) { var k = eval(myObject.h()); var t = []; return k; }", "function test source", 1, null);
				Function f = (Function)globalScope.Get("f", globalScope);
				object[] args = new object[] { 7 };
				cx.CallFunctionWithContinuations(f, globalScope, args);
				NUnit.Framework.Assert.Fail("Should throw ContinuationPending");
			}
			catch (ContinuationPending pending)
			{
				// serialize
				MemoryStream baos = new MemoryStream();
				ScriptableOutputStream sos = new ScriptableOutputStream(baos, globalScope);
				sos.WriteObject(globalScope);
				sos.WriteObject(pending.GetContinuation());
				sos.Close();
				baos.Close();
				byte[] serializedData = baos.ToArray();
				// deserialize
				MemoryStream bais = new MemoryStream(serializedData);
				ScriptableInputStream sis = new ScriptableInputStream(bais, globalScope);
				globalScope = (Scriptable)sis.ReadObject();
				object continuation = sis.ReadObject();
				sis.Close();
				bais.Close();
				object result = cx.ResumeContinuation(continuation, globalScope, "2+3");
				NUnit.Framework.Assert.AreEqual(5, System.Convert.ToInt32((result)));
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestConsStringSerialization()
		{
			string r1 = "foo" + "bar";
			MemoryStream baos = new MemoryStream();
			ObjectOutputStream oos = new ObjectOutputStream(baos);
			oos.WriteObject(r1);
			oos.Flush();
			MemoryStream bais = new MemoryStream(baos.ToArray());
			ObjectInputStream ois = new ObjectInputStream(bais);
			string r2 = (string)ois.ReadObject();
			NUnit.Framework.Assert.AreEqual("still the same at the other end", r1.ToString(), r2.ToString());
		}
	}
}
